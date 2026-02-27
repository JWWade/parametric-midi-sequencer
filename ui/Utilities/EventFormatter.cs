using ParametricMidiSequencer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParametricMidiSequencer.UI.Utilities
{
    /// <summary>
    /// Formats MIDI events for human-readable display.
    /// Hides MIDI internals and presents events as musical concepts.
    /// </summary>
    public static class EventFormatter
    {
        private static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        /// <summary>
        /// Create human-readable descriptions of chord events from harmony spec.
        /// </summary>
        public static List<string> FormatChordEvents(HarmonySpec spec)
        {
            var events = new List<string>();

            if (spec?.Progression == null || spec.Progression.Count == 0)
            {
                events.Add("No chords in progression.");
                return events;
            }

            // Generate the actual harmony events to get transformed pitch classes
            var harmonyEvents = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chordsByTime = GroupChordsByTime(harmonyEvents);

            foreach (var chord in spec.Progression.Take(10)) // Show first 10 chords
            {
                var pitchClasses = chordsByTime.ContainsKey(chord.Time)
                    ? chordsByTime[chord.Time]
                    : new List<int>();
                
                var description = FormatChordEvent(chord, pitchClasses);
                events.Add(description);
            }

            if (spec.Progression.Count > 10)
                events.Add($"… and {spec.Progression.Count - 10} more chords");

            return events;
        }

        /// <summary>
        /// Group harmony events by time and extract pitch classes.
        /// </summary>
        private static Dictionary<int, List<int>> GroupChordsByTime(List<ManualEvent> events)
        {
            var result = new Dictionary<int, List<int>>();
            
            foreach (var evt in events)
            {
                if (!result.ContainsKey(evt.TimeStep))
                    result[evt.TimeStep] = new List<int>();
                
                // Convert MIDI note to pitch class (0-11)
                int pitchClass = ((evt.Note % 12) + 12) % 12;
                if (!result[evt.TimeStep].Contains(pitchClass))
                    result[evt.TimeStep].Add(pitchClass);
            }

            // Sort pitch classes in each chord
            foreach (var key in result.Keys.ToList())
                result[key].Sort();

            return result;
        }

        /// <summary>
        /// Format a single chord event as a human-readable string.
        /// </summary>
        private static string FormatChordEvent(ChordEvent chord, List<int> pitchClasses)
        {
            var timePart = $"Time {chord.Time}";
            var degreePart = GetDegreeName(chord.Degree);
            var typePart = FormatChordType(chord.Type);
            var inversionPart = GetInversionName(chord.Inversion);

            var chordName = $"{degreePart} ({typePart})";
            if (!string.IsNullOrEmpty(inversionPart))
                chordName = $"{degreePart} ({typePart}, {inversionPart})";

            // Add pitch classes if available
            if (pitchClasses.Count > 0)
            {
                var pitchClassStr = string.Join(", ", pitchClasses.Select(pc => NoteNames[pc]));
                return $"{timePart}: {chordName} → [{pitchClassStr}]";
            }
            else
            {
                return $"{timePart}: {chordName}";
            }
        }

        /// <summary>
        /// Format chord type with proper spacing and symbols.
        /// </summary>
        private static string FormatChordType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return "triad";

            // Handle common chord types
            return type switch
            {
                "triad" => "triad",
                "seventh" => "seventh",
                "ninth" => "ninth",
                "sus2" => "sus2",
                "sus4" => "sus4",
                _ => type
            };
        }

        /// <summary>
        /// Get the Roman numeral degree name (I, ii, iii, etc.).
        /// </summary>
        private static string GetDegreeName(int degree)
        {
            var romanNumerals = new[] { "I", "ii", "iii", "IV", "V", "vi", "vii°" };
            if (degree >= 0 && degree < romanNumerals.Length)
                return romanNumerals[degree];
            return $"Scale degree {degree}";
        }

        /// <summary>
        /// Get the inversion name (root, 1st, 2nd, 3rd).
        /// </summary>
        private static string GetInversionName(int inversion)
        {
            return inversion switch
            {
                0 => "",
                1 => "1st inversion",
                2 => "2nd inversion",
                3 => "3rd inversion",
                _ => $"inversion {inversion}"
            };
        }

        /// <summary>
        /// Format a summary of MIDI generation results.
        /// </summary>
        public static string FormatGenerationSummary(int eventCount, HarmonySpec spec)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Generated {eventCount} musical events");
            sb.AppendLine();
            sb.AppendLine("First chord events:");

            var chordDescriptions = FormatChordEvents(spec);
            foreach (var desc in chordDescriptions)
                sb.AppendLine("  " + desc);

            return sb.ToString();
        }
    }
}
