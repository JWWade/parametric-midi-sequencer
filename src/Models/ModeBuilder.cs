using System;
using System.Collections.Generic;
using System.Linq;

namespace ParametricMidiSequencer.Models
{
    public static class ModeBuilder
    {
        private static readonly Dictionary<string, int[]> ModeIntervals = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ionian", new[] { 0,2,4,5,7,9,11 } },
            { "dorian", new[] { 0,2,3,5,7,9,10 } },
            { "phrygian", new[] { 0,1,3,5,7,8,10 } },
            { "lydian", new[] { 0,2,4,6,7,9,11 } },
            { "mixolydian", new[] { 0,2,4,5,7,9,10 } },
            { "aeolian", new[] { 0,2,3,5,7,8,10 } },
            { "locrian", new[] { 0,1,3,5,6,8,10 } }
        };

        public static List<int> BuildModeScale(string modeName, string root)
        {
            if (string.IsNullOrWhiteSpace(modeName))
                return new List<int>();
            if (string.IsNullOrWhiteSpace(root))
                root = "C";

            int rootPc = ScaleBuilder.ParseRoot(root); // reuse helper
            if (!ModeIntervals.TryGetValue(modeName.ToLowerInvariant(), out var intervals))
                intervals = ModeIntervals["ionian"];

            return intervals.Select(i => (rootPc + i) % 12).ToList();
        }

        // Determine chord intervals based on mode quality rules.
        public static int[] GetIntervalsForMode(string modeName, int degree, string type)
        {
            // We'll reuse ModeScale to derive quality: major/minor/diminished triad
            // but easier: switch by mode since rules are fixed per spec.
            var m = (modeName ?? "ionian").ToLowerInvariant();
            var t = (type ?? "triad").ToLowerInvariant();

            // Many of these mirror natural minor or major; handle individually.
            if (t == "seventh")
            {
                switch (m)
                {
                    case "ionian": // same as major
                        return ScaleBuilder.GetIntervalsForType("major", degree, "seventh");
                    case "aeolian": // natural minor
                        return ScaleBuilder.GetIntervalsForType("minor", degree, "seventh");
                    case "dorian":
                        // treat like minor with raised 6th (should not affect triad qualities in most cases)
                        // we'll approximate by using minor rules, except degree 5 -> minor7? keep minor
                        return degree switch
                        {
                            1 => new[] {0,3,7,10},
                            2 => new[] {0,3,7,10},
                            3 => new[] {0,4,7,11},
                            4 => new[] {0,3,7,10},
                            5 => new[] {0,3,7,10},
                            6 => new[] {0,4,7,11},
                            7 => new[] {0,3,6,10},
                            _ => new[] {0,4,7,10}
                        };
                    case "phrygian":
                        return degree switch
                        {
                            1 => new[] {0,3,7,10},
                            2 => new[] {0,4,7,11},
                            3 => new[] {0,4,7,11},
                            4 => new[] {0,3,7,10},
                            5 => new[] {0,3,6,10},
                            6 => new[] {0,4,7,11},
                            7 => new[] {0,4,7,11},
                            _ => new[] {0,4,7,10}
                        };
                    case "lydian":
                        return degree switch
                        {
                            1 => new[] {0,4,7,11},
                            2 => new[] {0,3,7,10},
                            3 => new[] {0,3,7,10},
                            4 => new[] {0,4,7,11},
                            5 => new[] {0,4,7,10},
                            6 => new[] {0,3,7,10},
                            7 => new[] {0,3,6,10},
                            _ => new[] {0,4,7,10}
                        };
                    case "mixolydian":
                        return degree switch
                        {
                            1 => new[] {0,4,7,10},
                            2 => new[] {0,3,7,10},
                            3 => new[] {0,3,7,10},
                            4 => new[] {0,4,7,11},
                            5 => new[] {0,4,7,10},
                            6 => new[] {0,3,7,10},
                            7 => new[] {0,3,6,10},
                            _ => new[] {0,4,7,10}
                        };
                    case "locrian":
                        return degree switch
                        {
                            1 => new[] {0,3,6,10},
                            2 => new[] {0,4,7,11},
                            3 => new[] {0,4,7,11},
                            4 => new[] {0,3,7,10},
                            5 => new[] {0,3,6,10},
                            6 => new[] {0,4,7,11},
                            7 => new[] {0,4,7,11},
                            _ => new[] {0,4,7,10}
                        };
                    default:
                        return ScaleBuilder.GetIntervalsForType("major", degree, "seventh");
                }
            }

            // triads
            switch (m)
            {
                case "ionian":
                    return ScaleBuilder.GetIntervalsForType("major", degree, "triad");
                case "aeolian":
                    return ScaleBuilder.GetIntervalsForType("minor", degree, "triad");
                case "dorian":
                    return degree switch
                    {
                        1 => new[] {0,3,7},
                        2 => new[] {0,3,7},
                        3 => new[] {0,4,7},
                        4 => new[] {0,3,7},
                        5 => new[] {0,3,7},
                        6 => new[] {0,4,7},
                        7 => new[] {0,3,6},
                        _ => new[] {0,4,7}
                    };
                case "phrygian":
                    return degree switch
                    {
                        1 => new[] {0,3,7},
                        2 => new[] {0,4,7},
                        3 => new[] {0,4,7},
                        4 => new[] {0,3,7},
                        5 => new[] {0,3,6},
                        6 => new[] {0,4,7},
                        7 => new[] {0,4,7},
                        _ => new[] {0,4,7}
                    };
                case "lydian":
                    return degree switch
                    {
                        1 => new[] {0,4,7},
                        2 => new[] {0,3,7},
                        3 => new[] {0,3,7},
                        4 => new[] {0,4,7},
                        5 => new[] {0,4,7},
                        6 => new[] {0,3,7},
                        7 => new[] {0,3,6},
                        _ => new[] {0,4,7}
                    };
                case "mixolydian":
                    return degree switch
                    {
                        1 => new[] {0,4,7},
                        2 => new[] {0,3,7},
                        3 => new[] {0,3,7},
                        4 => new[] {0,4,7},
                        5 => new[] {0,4,7},
                        6 => new[] {0,3,7},
                        7 => new[] {0,3,6},
                        _ => new[] {0,4,7}
                    };
                case "locrian":
                    return degree switch
                    {
                        1 => new[] {0,3,6},
                        2 => new[] {0,4,7},
                        3 => new[] {0,4,7},
                        4 => new[] {0,3,7},
                        5 => new[] {0,3,6},
                        6 => new[] {0,4,7},
                        7 => new[] {0,4,7},
                        _ => new[] {0,4,7}
                    };
                default:
                    return ScaleBuilder.GetIntervalsForType("major", degree, "triad");
            }
        }
    }
}
