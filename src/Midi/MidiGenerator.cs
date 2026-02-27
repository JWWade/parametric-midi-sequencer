// This file is responsible for generating MIDI files based on the parsed patterns and JSON data.

using System;
using System.IO;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Interaction;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Midi
{
    public class MidiGenerator
    {
        private readonly string _outputFilePath;

        public MidiGenerator(string outputFilePath)
        {
            _outputFilePath = outputFilePath;
        }
        private HarmonySpec _harmonySpec;

        public MidiGenerator(HarmonySpec harmonySpec)
        {
            _harmonySpec = harmonySpec;
            _outputFilePath = null;
        }

        public int GenerateFromSpec(MetaSpec meta, TrackSpec[] tracks)
        {
            var midiFile = new MidiFile();
            var trackChunk = new TrackChunk();

            var ticksPerQuarter = meta?.Ppq > 0 ? meta.Ppq : 480;
            var steps = meta?.Steps > 0 ? meta.Steps : 16;
            var bars = meta?.Bars > 0 ? meta.Bars : 4;
            var tempo = meta?.Tempo > 0 ? meta.Tempo : 120;

            var ticksPerStep = ticksPerQuarter * 4 / Math.Max(1, steps);
            var totalSteps = Math.Max(1, steps * Math.Max(1, bars));

            // First pass: compute the maximum intended NoteOff time so we can auto-extend bars if needed
            var ticksPerBar = ticksPerQuarter * 4; // ticks in one 4/4 bar
            long maxIntendedOff = 0L;
            var initialTotalSteps = totalSteps;

            for (int step = 0; step < initialTotalSteps; step++)
            {
                long stepTime = (long)step * ticksPerStep;
                foreach (var track in tracks)
                {
                    if (track == null) continue;

                    if (track.Events != null)
                    {
                        foreach (var ev in track.Events)
                        {
                            if (ev.TimeStep == step)
                            {
                                var dur = Math.Max(1L, (long)Math.Round(ticksPerStep * (ev.Duration > 0 ? ev.Duration : 1.0)));
                                var noteOnTime = stepTime;
                                var intendedOff = noteOnTime + dur;
                                if (intendedOff > maxIntendedOff) maxIntendedOff = intendedOff;
                            }
                        }
                    }

                    if (track.Patterns == null) continue;
                    foreach (var pattern in track.Patterns)
                    {
                        if (!ShouldPlayJson(pattern, step, steps, initialTotalSteps))
                            continue;

                        var durFraction = (pattern.Duration > 0) ? pattern.Duration : 1.0;
                        var patternNoteLength = Math.Max(1L, (long)Math.Round(ticksPerStep * durFraction));
                        long offsetTicks = 0L;
                        if (pattern.Offset > 0)
                        {
                            offsetTicks = (long)Math.Round(pattern.Offset * ticksPerStep);
                        }
                        var noteOnTime = stepTime + offsetTicks;
                        var intendedOff = noteOnTime + patternNoteLength;
                        if (intendedOff > maxIntendedOff) maxIntendedOff = intendedOff;
                    }
                }
            }

            var currentMaxTime = (long)initialTotalSteps * ticksPerStep;
            int extendedBars = 0;
            var autoExtend = meta?.AutoExtend != false; // default true
            if (autoExtend && maxIntendedOff >= currentMaxTime)
            {
                var extraTicks = maxIntendedOff - currentMaxTime;
                extendedBars = (int)Math.Ceiling((double)extraTicks / ticksPerBar);
                // If the intended off falls exactly on the boundary after extension, add one more bar
                if (currentMaxTime + extendedBars * ticksPerBar <= maxIntendedOff)
                    extendedBars++;
                bars += extendedBars;
                totalSteps = Math.Max(1, steps * Math.Max(1, bars));
                Console.WriteLine($"Extended bars by {extendedBars} to allow notes to finish (new bars={bars}).");
            }
            else if (!autoExtend && maxIntendedOff >= currentMaxTime)
            {
                Console.WriteLine($"Warning: notes overflow the sequence end (maxIntendedOff={maxIntendedOff}, currentMaxTime={currentMaxTime}). Set autoExtend=true or increase bars.");
            }

            var scheduled = new System.Collections.Generic.List<(long time, MidiEvent ev)>();
            var clampedMessages = new System.Collections.Generic.List<string>();

            // Time signature and tempo
            scheduled.Add((0L, new TimeSignatureEvent(4, 4, 24, 8)));
            var microsecondsPerQuarter = (int)Math.Round(60000000.0 / Math.Max(1, tempo));
            scheduled.Add((0L, new SetTempoEvent(microsecondsPerQuarter)));

            // Second pass: schedule events using possibly extended totalSteps
            for (int step = 0; step < totalSteps; step++)
            {
                long stepTime = (long)step * ticksPerStep;

                foreach (var track in tracks)
                {
                    if (track == null) continue;

                    // Manual events
                    if (track.Events != null)
                    {
                        foreach (var ev in track.Events)
                        {
                            if (ev.TimeStep == step)
                            {
                                var ch = ev.Channel >= 0 ? (FourBitNumber)ev.Channel : (FourBitNumber) (track.IsPercussion ? 9 : (track.Channel >= 0 ? track.Channel : 0));
                                var vel = (SevenBitNumber)Math.Max(0, Math.Min(127, ev.Velocity));
                                var dur = Math.Max(1L, (long)Math.Round(ticksPerStep * (ev.Duration > 0 ? ev.Duration : 1.0)));
                                var on = new NoteOnEvent((SevenBitNumber)ev.Note, vel) { Channel = ch };
                                var off = new NoteOffEvent((SevenBitNumber)ev.Note, (SevenBitNumber)0) { Channel = ch };
                                var noteOnTime = stepTime;
                                var noteOffTime = stepTime + dur;
                                if (noteOffTime > noteOnTime)
                                {
                                    scheduled.Add((noteOnTime, on));
                                    scheduled.Add((noteOffTime, off));
                                }
                            }
                        }
                    }

                    if (track.Patterns == null) continue;
                    foreach (var pattern in track.Patterns)
                    {
                        if (!ShouldPlayJson(pattern, step, steps, totalSteps))
                            continue;

                        var isPercussion = pattern.Note >= 35 && pattern.Note <= 81;
                        var channel = pattern.Channel >= 0 ? (FourBitNumber)pattern.Channel : (isPercussion || track.IsPercussion ? (FourBitNumber)9 : (FourBitNumber)(track.Channel >= 0 ? track.Channel : 0));
                        var velocity = (SevenBitNumber)(pattern.Velocity > 0 ? Math.Min(127, pattern.Velocity) : 100);
                        var durFraction = (pattern.Duration > 0) ? pattern.Duration : 1.0;
                        var patternNoteLength = Math.Max(1L, (long)Math.Round(ticksPerStep * durFraction));

                        var noteOn = new NoteOnEvent((SevenBitNumber)pattern.Note, velocity) { Channel = channel };
                        var noteOff = new NoteOffEvent((SevenBitNumber)pattern.Note, (SevenBitNumber)0) { Channel = channel };

                        // support fractional-step offsets (pattern.Offset is double)
                        long offsetTicks = 0L;
                        if (pattern.Offset > 0)
                        {
                            offsetTicks = (long)Math.Round(pattern.Offset * ticksPerStep);
                        }

                        var noteOnTime = stepTime + offsetTicks;
                        var noteOffTime = noteOnTime + patternNoteLength;
                        if (noteOffTime > noteOnTime)
                        {
                            scheduled.Add((noteOnTime, noteOn));
                            scheduled.Add((noteOffTime, noteOff));
                        }
                    }
                }
            }

            // sort and convert to delta times (same ordering rules)
            scheduled.Sort((a, b) =>
            {
                var t = a.time.CompareTo(b.time);
                if (t != 0) return t;
                int order(MidiEvent ev)
                {
                    if (ev is SetTempoEvent) return -2;
                    if (ev is NoteOffEvent) return -1;
                    if (ev is NoteOnEvent) return 1;
                    return 0;
                }
                return order(a.ev).CompareTo(order(b.ev));
            });

            long lastTime = 0L;
            foreach (var item in scheduled)
            {
                var time = item.time;
                var ev = item.ev;
                ev.DeltaTime = time - lastTime;
                trackChunk.Events.Add(ev);
                lastTime = time;
            }
            midiFile.Chunks.Add(trackChunk);
            midiFile.Write(_outputFilePath, true);

            // report any clamped notes
            try
            {
                if (clampedMessages != null && clampedMessages.Count > 0)
                {
                    Console.WriteLine("Warning: The following notes were clamped to the sequence end:");
                    foreach (var m in clampedMessages)
                        Console.WriteLine(" - " + m);
                }
            }
            catch { }
            return bars;
        }

        private bool ShouldPlayJson(PatternSpecJson pattern, int step, int steps, int totalSteps)
        {
            if (pattern == null) return false;
            if (string.Equals(pattern.Type, "modulo", StringComparison.OrdinalIgnoreCase))
            {
                if (pattern.Interval <= 0) return false;
                if (pattern.HitsPerBar > 0 && steps > 0 && string.Equals(pattern.Mode, "bar", StringComparison.OrdinalIgnoreCase))
                {
                    int n = pattern.HitsPerBar;
                    int posInBar = step % steps;
                    for (int k = 0; k < n; k++)
                    {
                        var target = (int)Math.Round(k * (double)steps / n);
                        if (target == posInBar) return true;
                    }
                    return false;
                }
                // default global
                return (step % pattern.Interval) == 0;
            }
            if (string.Equals(pattern.Type, "function", StringComparison.OrdinalIgnoreCase))
            {
                var expr = pattern.Expression ?? string.Empty;
                expr = expr.Replace("\\u003E", ">");
                expr = expr.Replace("\\u003C", "<");
                char[] ops = new[] { '>', '<' };
                int opIndex = expr.IndexOfAny(ops);
                if (opIndex < 0) return false;
                var left = expr.Substring(0, opIndex).Trim();
                var op = expr[opIndex];
                var right = expr.Substring(opIndex + 1).Trim();
                if (!double.TryParse(right, out var threshold)) return false;
                double x = totalSteps <= 0 ? 0 : (double)step / totalSteps;
                double value = 0;
                if (left.Contains("sin", StringComparison.OrdinalIgnoreCase))
                {
                    value = Math.Sin(2 * Math.PI * x);
                }
                else return false;
                return op switch { '>' => value > threshold, '<' => value < threshold, _ => false };
            }
            return false;
        }
    
                /// <summary>
        /// Generate a MIDI file from the loaded HarmonySpec.
        /// </summary>
        public MidiFile GenerateMidiFile()
        {
            if (_harmonySpec == null)
                throw new InvalidOperationException("No HarmonySpec loaded.");

            var midiFile = new MidiFile();
            var trackChunk = new TrackChunk();

            var harmonyEvents = HarmonyGenerator.GenerateHarmonyEvents(_harmonySpec);
            if (harmonyEvents.Count == 0)
            {
                trackChunk.Events.Add(new TimeSignatureEvent(4, 4, 24, 8));
                trackChunk.Events.Add(new SetTempoEvent(500000));
                midiFile.Chunks.Add(trackChunk);
                return midiFile;
            }

            var ticksPerQuarter = 480;
            var tempo = 120;
            var scheduled = new System.Collections.Generic.List<(long time, MidiEvent ev)>();

            scheduled.Add((0L, new TimeSignatureEvent(4, 4, 24, 8)));
            var microsecondsPerQuarter = (int)Math.Round(60000000.0 / tempo);
            scheduled.Add((0L, new SetTempoEvent(microsecondsPerQuarter)));

            foreach (var harmonyEvent in harmonyEvents)
            {
                var channel = (FourBitNumber)(Math.Max(0, Math.Min(15, _harmonySpec.Channel)));
                var velocity = (SevenBitNumber)Math.Max(1, Math.Min(127, _harmonySpec.Velocity));
                var note = (SevenBitNumber)Math.Max(0, Math.Min(127, harmonyEvent.Note));
                
                // TimeStep is interpreted as steps (assuming 4 steps per quarter note)
                var noteOnTime = (long)harmonyEvent.TimeStep * (ticksPerQuarter / 4);
                var noteDuration = (long)Math.Round(_harmonySpec.Duration * ticksPerQuarter);
                var noteOffTime = noteOnTime + noteDuration;
                
                var noteOn = new NoteOnEvent(note, velocity) { Channel = channel };
                var noteOff = new NoteOffEvent(note, (SevenBitNumber)0) { Channel = channel };
                
                scheduled.Add((noteOnTime, noteOn));
                scheduled.Add((noteOffTime, noteOff));
            }

            scheduled.Sort((a, b) => {
                var t = a.time.CompareTo(b.time);
                if (t != 0) return t;
                int order(MidiEvent ev) => ev switch { NoteOffEvent => -1, NoteOnEvent => 1, _ => 0 };
                return order(a.ev).CompareTo(order(b.ev));
            });

            long lastTime = 0L;
            foreach (var item in scheduled)
            {
                item.ev.DeltaTime = item.time - lastTime;
                trackChunk.Events.Add(item.ev);
                lastTime = item.time;
            }

            midiFile.Chunks.Add(trackChunk);
            return midiFile;
        }    }
}


