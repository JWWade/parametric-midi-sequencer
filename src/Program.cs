using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ParametricMidiSequencer.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using System.Linq;
using ParametricMidiSequencer.Midi;

namespace ParametricMidiSequencer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ParametricMidiSequencer <inputFile.json> [--tempo BPM] [--steps N] [--bars B] [--out path] [--extend | --no-extend] [--list-events]");
                Console.WriteLine("Example: ParametricMidiSequencer examples/patterns.json --tempo 120 --steps 16 --out out/my.mid --extend");
                return;
            }

            // Parse args: first non-option is input file path
            string inputFilePath = null;
            int tempo = 120;
            int steps = 16;
            int bars = 8;
            string outPathArg = null;
            bool? autoExtend = null; // null = use JSON meta value; true/false = CLI override

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a.StartsWith("--"))
                {
                    var eq = a.IndexOf('=');
                    string name = eq > 0 ? a.Substring(2, eq - 2) : a.Substring(2);
                    string val = eq > 0 ? a.Substring(eq + 1) : null;
                    if (val == null && i + 1 < args.Length)
                    {
                        // next token may be the value if it's not another option
                        if (!args[i + 1].StartsWith("--"))
                        {
                            val = args[i + 1];
                            i++;
                        }
                    }

                    if (string.Equals(name, "tempo", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out var t))
                        tempo = t;
                    else if (string.Equals(name, "steps", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out var s))
                        steps = s;
                    else if (string.Equals(name, "bars", StringComparison.OrdinalIgnoreCase) && int.TryParse(val, out var b))
                        bars = b;
                    else if (string.Equals(name, "out", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val))
                        outPathArg = val;
                    else if (string.Equals(name, "extend", StringComparison.OrdinalIgnoreCase))
                        autoExtend = true;
                    else if (string.Equals(name, "no-extend", StringComparison.OrdinalIgnoreCase))
                        autoExtend = false;
                }
                else if (inputFilePath == null)
                {
                    inputFilePath = a;
                }
            }

            if (inputFilePath == null)
            {
                Console.WriteLine("Please provide the input file path.");
                return;
            }

            if (!inputFilePath.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: Only JSON input files are supported. Please provide a .json file.");
                return;
            }

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine($"File not found: {inputFilePath}");
                return;
            }

            string input = await File.ReadAllTextAsync(inputFilePath);
            string jsonOutput;

            // Parse and echo back JSON input as formatted
            try
            {
                using var doc = JsonDocument.Parse(input);
                jsonOutput = System.Text.Json.JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse JSON: {ex.Message}");
                return;
            }
            Console.WriteLine("Using JSON input:");
            Console.WriteLine(jsonOutput);

            // Determine output paths
            var outDir = Path.Combine(Directory.GetCurrentDirectory(), "out");
            Directory.CreateDirectory(outDir);

            string midiPath;
            if (!string.IsNullOrEmpty(outPathArg))
            {
                // if outPathArg is a directory, use output.mid inside it
                if (Directory.Exists(outPathArg))
                {
                    midiPath = Path.Combine(outPathArg, "output.mid");
                }
                else
                {
                    midiPath = outPathArg;
                }
            }
            else
            {
                midiPath = Path.Combine(outDir, "output.mid");
            }

            var jsonPath = Path.Combine(Path.GetDirectoryName(midiPath) ?? outDir, "output.json");
            await File.WriteAllTextAsync(jsonPath, jsonOutput);
            Console.WriteLine($"Wrote JSON to: {jsonPath}");

            // Generate MIDI file from JSON spec
            var midiGenerator = new MidiGenerator(midiPath);
            // final values actually used for generation (may come from JSON meta)
            int finalTempo = tempo;
            int finalSteps = steps;
            int finalBars = bars;
            int finalPpq = 480;

            // option to list events after generation
            bool listEvents = args.Any(a => string.Equals(a, "--list-events", StringComparison.OrdinalIgnoreCase));
            // Deserialize JSON spec to richer model
            using var docSpec = System.Text.Json.JsonDocument.Parse(input);
            var root = docSpec.RootElement;

            // If top-level has "tracks" property, deserialize wrapper
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object && root.TryGetProperty("tracks", out var _))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var meta = root.TryGetProperty("meta", out var m) ? m.Deserialize<ParametricMidiSequencer.Models.MetaSpec>(options) : new ParametricMidiSequencer.Models.MetaSpec { Tempo = tempo, Steps = steps, Bars = bars };
                // Apply CLI override for autoExtend if provided
                if (autoExtend.HasValue && meta != null)
                    meta.AutoExtend = autoExtend.Value;
                var tracks = root.GetProperty("tracks").Deserialize<ParametricMidiSequencer.Models.TrackSpec[]>(options);
                if (tracks == null) tracks = new ParametricMidiSequencer.Models.TrackSpec[0];
                
                // Process harmonyTracks (PoC21 multi-track) if present; takes priority over legacy harmony.
                if (root.TryGetProperty("harmonyTracks", out var harmonyTracksJson))
                {
                    var harmonySpecs = harmonyTracksJson.Deserialize<ParametricMidiSequencer.Models.HarmonySpec[]>(options);
                    if (harmonySpecs != null && harmonySpecs.Length > 0)
                    {
                        var errors = ParametricMidiSequencer.Models.MultiTrackHarmonyEngine.Validate(harmonySpecs);
                        foreach (var err in errors)
                            Console.WriteLine($"Warning: {err}");

                        var allHarmonyEvents = ParametricMidiSequencer.Models.MultiTrackHarmonyEngine.GenerateAllEvents(harmonySpecs);
                        var tracksList = new System.Collections.Generic.List<ParametricMidiSequencer.Models.TrackSpec>(tracks);
                        // Each ManualEvent already carries the correct channel set by the
                        // individual HarmonySpec; the TrackSpec channel is a fallback only.
                        tracksList.Add(new ParametricMidiSequencer.Models.TrackSpec
                        {
                            Name = "Harmony",
                            Events = allHarmonyEvents
                        });
                        tracks = tracksList.ToArray();
                    }
                }
                // Process legacy single-track harmony section if present and harmonyTracks is absent
                else if (root.TryGetProperty("harmony", out var harmonyJson))
                {
                    var harmony = harmonyJson.Deserialize<ParametricMidiSequencer.Models.HarmonySpec>(options);
                    if (harmony != null)
                    {
                        // Generate events from the harmony spec.  HarmonyGenerator is now
                        // responsible for applying any transform layers defined in
                        // `harmony.Constraints` (e.g. minSharedPitches) before creating
                        // manual events. This keeps the scheduler unchanged.
                        var harmonyEvents = ParametricMidiSequencer.Models.HarmonyGenerator.GenerateHarmonyEvents(harmony);
                        // Add harmony events to the first track or create a new one
                        if (tracks.Length == 0)
                        {
                            var harmonyTrack = new ParametricMidiSequencer.Models.TrackSpec
                            {
                                Name = "Harmony",
                                Channel = harmony.Channel,
                                Events = harmonyEvents
                            };
                            var tracksList = new System.Collections.Generic.List<ParametricMidiSequencer.Models.TrackSpec> { harmonyTrack };
                            tracks = tracksList.ToArray();
                        }
                        else
                        {
                            if (tracks[0].Events == null)
                                tracks[0].Events = new System.Collections.Generic.List<ParametricMidiSequencer.Models.ManualEvent>();
                            tracks[0].Events.AddRange(harmonyEvents);
                        }
                    }
                }
                
                var usedBars = midiGenerator.GenerateFromSpec(meta ?? new ParametricMidiSequencer.Models.MetaSpec(), tracks);
                // reflect actual values used
                finalTempo = (meta != null && meta.Tempo > 0) ? meta.Tempo : tempo;
                finalSteps = (meta != null && meta.Steps > 0) ? meta.Steps : steps;
                finalBars = usedBars > 0 ? usedBars : ((meta != null && meta.Bars > 0) ? meta.Bars : bars);
                finalPpq = (meta != null && meta.Ppq > 0) ? meta.Ppq : finalPpq;
            }
            else if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                // If user provided an array of tracks
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var tracks = System.Text.Json.JsonSerializer.Deserialize<ParametricMidiSequencer.Models.TrackSpec[]>(input, options);
                var m = new ParametricMidiSequencer.Models.MetaSpec { Tempo = tempo, Steps = steps, Bars = bars };
                // Apply CLI override for autoExtend if provided
                if (autoExtend.HasValue)
                    m.AutoExtend = autoExtend.Value;
                var usedBars2 = midiGenerator.GenerateFromSpec(m, tracks);
                finalTempo = m.Tempo;
                finalSteps = m.Steps;
                finalBars = usedBars2 > 0 ? usedBars2 : m.Bars;
                finalPpq = m.Ppq > 0 ? m.Ppq : finalPpq;
            }
            else
            {
                Console.WriteLine("Could not parse JSON input. Expected { meta:..., tracks:[...] } or an array of tracks.");
                return;
            }

            Console.WriteLine($"Wrote MIDI to: {midiPath} (tempo={finalTempo}, steps={finalSteps}, bars={finalBars})");

            // If user requested, or env var set, list NoteOn events with absolute ticks and computed step index
            try
            {
                var dbg = Environment.GetEnvironmentVariable("PMS_DEBUG_SCHEDULE");
                if (listEvents || (!string.IsNullOrEmpty(dbg) && dbg != "0"))
                {
                    var midi = MidiFile.Read(midiPath);
                    Console.WriteLine("Notes (startStep, startTick, durationTicks, durationSteps, note, channel, velocity):");
                    var ticksPerStep = finalPpq * 4 / Math.Max(1, finalSteps);
                    var notes = midi.GetNotes();
                    foreach (var n in notes)
                    {
                        var start = n.Time;
                        var len = n.Length;
                        var startStep = ticksPerStep > 0 ? start / ticksPerStep : 0;
                        var durSteps = ticksPerStep > 0 ? (double)len / ticksPerStep : 0.0;
                        Console.WriteLine($" startStep={startStep} startTick={start} durTicks={len} durSteps={durSteps:0.###} note={n.NoteNumber} ch={n.Channel} vel={n.Velocity}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to list events: {ex.Message}");
            }
        }


    }
}