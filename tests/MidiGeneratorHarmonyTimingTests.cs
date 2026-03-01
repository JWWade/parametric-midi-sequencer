using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using ParametricMidiSequencer.Midi;
using ParametricMidiSequencer.Models;
using Xunit;

namespace ParametricMidiSequencer.Tests
{
    public class MidiGeneratorHarmonyTimingTests
    {
        [Fact]
        public void GenerateMidiFile_HarmonySpec_UsesStepTimingAndAvoidsSamePitchOverlap()
        {
            var spec = new HarmonySpec
            {
                ScaleName = "major",
                Root = "C",
                Channel = 0,
                Velocity = 80,
                Duration = 16,
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 0 },
                    new() { Time = 16, Degree = 3, Type = "triad", Inversion = 0 },
                    new() { Time = 32, Degree = 6, Type = "triad", Inversion = 0 },
                    new() { Time = 48, Degree = 1, Type = "triad", Inversion = 0 }
                }
            };

            var generator = new MidiGenerator(spec);
            var midiFile = generator.GenerateMidiFile();

            var tpq = midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision;
            Assert.NotNull(tpq);
            Assert.Equal(480, tpq!.TicksPerQuarterNote);

            var track = midiFile.GetTrackChunks().Single();
            var timedEvents = ToTimedEvents(track.Events).ToList();

            var firstNoteOn = timedEvents
                .Where(x => x.ev is NoteOnEvent)
                .Min(x => x.time);
            var firstNoteOff = timedEvents
                .Where(x => x.ev is NoteOffEvent)
                .Min(x => x.time);

            Assert.Equal(0L, firstNoteOn);
            Assert.Equal(1905L, firstNoteOff);

            var byPitch = timedEvents
                .Where(x => x.ev is NoteOnEvent || x.ev is NoteOffEvent)
                .GroupBy(x => ((NoteEvent)x.ev).NoteNumber)
                .ToList();

            foreach (var pitchGroup in byPitch)
            {
                var ordered = pitchGroup.OrderBy(x => x.time).ToList();

                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    var current = ordered[i];
                    var next = ordered[i + 1];

                    if (current.ev is NoteOffEvent && next.ev is NoteOnEvent)
                    {
                        Assert.True(
                            next.time > current.time,
                            $"Pitch {pitchGroup.Key}: NoteOn at {next.time} must be strictly after prior NoteOff at {current.time} to avoid ties.");
                    }
                }
            }
        }

        private static IEnumerable<(long time, MidiEvent ev)> ToTimedEvents(IEnumerable<MidiEvent> events)
        {
            long current = 0;
            foreach (var ev in events)
            {
                current += ev.DeltaTime;
                yield return (current, ev);
            }
        }
    }
}
