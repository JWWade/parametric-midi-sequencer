using System;
using System.Collections.Generic;
using System.Linq;

namespace ParametricMidiSequencer.Models
{
    public static class ScaleBuilder
    {
        private static readonly int[] MajorIntervals = new[] { 0, 2, 4, 5, 7, 9, 11 };
        private static readonly int[] NaturalMinorIntervals = new[] { 0, 2, 3, 5, 7, 8, 10 };

        public static List<int> BuildScale(string scaleName, string root)
        {
            if (string.IsNullOrWhiteSpace(scaleName))
                scaleName = "major";
            if (string.IsNullOrWhiteSpace(root))
                root = "C";

            int rootPc = ParseRoot(root);

            var intervals = scaleName.ToLowerInvariant() switch
            {
                "minor" => NaturalMinorIntervals,
                _ => MajorIntervals
            };

            return intervals.Select(i => (rootPc + i) % 12).ToList();
        }

        public static int ParseRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
                return 0;

            var r = root.Trim();
            // Accept formats like C, C#, Db, Eb, F#, Gb, etc.
            // Normalize flats to equivalent sharps
            if (r.Length >= 2 && r[1] == 'b')
            {
                // e.g. Db -> C# equivalent
                var baseNote = r[0].ToString().ToUpperInvariant();
                int basePc = NoteNameToPc(baseNote);
                // flat = -1
                return (basePc + 11) % 12;
            }

            return NoteNameToPc(r.ToUpperInvariant());
        }

        private static int NoteNameToPc(string name)
        {
            return name switch
            {
                "C" => 0,
                "C#" => 1,
                "DB" => 1,
                "D" => 2,
                "D#" => 3,
                "EB" => 3,
                "E" => 4,
                "F" => 5,
                "F#" => 6,
                "GB" => 6,
                "G" => 7,
                "G#" => 8,
                "AB" => 8,
                "A" => 9,
                "A#" => 10,
                "BB" => 10,
                "B" => 11,
                _ => 0
            };
        }

        // Return intervals for triad/seventh based on scale type and degree per PoC5 rules.
        public static int[] GetIntervalsForType(string scaleName, int degree, string type)
        {
            var t = (type ?? "triad").ToLowerInvariant();
            var s = (scaleName ?? "major").ToLowerInvariant();

            if (t == "seventh")
            {
                if (s == "minor")
                {
                    return degree switch
                    {
                        1 => new[] { 0, 3, 7, 10 },   // minor7
                        2 => new[] { 0, 3, 6, 10 },   // half-diminished
                        3 => new[] { 0, 4, 7, 11 },   // major7
                        4 => new[] { 0, 3, 7, 10 },   // minor7
                        5 => new[] { 0, 3, 7, 10 },   // minor7
                        6 => new[] { 0, 4, 7, 11 },   // major7
                        7 => new[] { 0, 4, 7, 10 },   // dominant7
                        _ => new[] { 0, 4, 7, 10 }
                    };
                }

                // major scale mapping (default)
                return degree switch
                {
                    1 => new[] { 0, 4, 7, 11 },   // maj7
                    2 => new[] { 0, 3, 7, 10 },   // m7
                    3 => new[] { 0, 3, 7, 10 },   // m7
                    4 => new[] { 0, 4, 7, 11 },   // maj7
                    5 => new[] { 0, 4, 7, 10 },   // dom7
                    6 => new[] { 0, 3, 7, 10 },   // m7
                    7 => new[] { 0, 3, 6, 10 },   // half-diminished
                    _ => new[] { 0, 4, 7, 10 }
                };
            }

            // Triad intervals
            if (s == "minor")
            {
                // Triad qualities per PoC5 section 5.2
                return degree switch
                {
                    1 => new[] { 0, 3, 7 },  // minor
                    2 => new[] { 0, 3, 6 },  // diminished
                    3 => new[] { 0, 4, 7 },  // major
                    4 => new[] { 0, 3, 7 },  // minor
                    5 => new[] { 0, 3, 7 },  // minor
                    6 => new[] { 0, 4, 7 },  // major
                    7 => new[] { 0, 4, 7 },  // major
                    _ => new[] { 0, 3, 7 }
                };
            }

            // Major triad mapping (default)
            return degree switch
            {
                1 => new[] { 0, 4, 7 },
                2 => new[] { 0, 3, 7 },
                3 => new[] { 0, 3, 7 },
                4 => new[] { 0, 4, 7 },
                5 => new[] { 0, 4, 7 },
                6 => new[] { 0, 3, 7 },
                7 => new[] { 0, 3, 6 },
                _ => new[] { 0, 4, 7 }
            };
        }
    }
}
