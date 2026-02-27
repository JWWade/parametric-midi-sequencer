using System;
using System.Collections.Generic;
using System.Linq;

namespace ParametricMidiSequencer.Models
{
    public static class CustomScaleBuilder
    {
        /// <summary>
        /// Validates a custom pitch-class set.
        /// </summary>
        public static bool IsValid(List<int> customScale)
        {
            if (customScale == null || customScale.Count < 3)
                return false;

            var normalized = customScale.Select(pc => ((pc % 12) + 12) % 12).Distinct().ToList();
            return normalized.Count >= 3;
        }

        /// <summary>
        /// Normalizes the custom scale (0–11, no duplicates).
        /// </summary>
        public static List<int> Normalize(List<int> customScale)
        {
            if (customScale == null)
                return new List<int>();

            return customScale
                .Select(pc => ((pc % 12) + 12) % 12)
                .Distinct()
                .OrderBy(pc => pc)
                .ToList();
        }

        /// <summary>
        /// Get the pitch class at a given scale degree (1–N).
        /// Wraps around if degree exceeds scale length.
        /// </summary>
        public static int GetPitchClass(List<int> scale, int degree)
        {
            if (scale == null || scale.Count == 0 || degree < 1)
                return 0;

            int index = (degree - 1) % scale.Count;
            return scale[index];
        }

        /// <summary>
        /// Build a triad by stacking scale steps (indices i, i+2, i+4).
        /// </summary>
        public static List<int> BuildTriad(List<int> scale, int degree)
        {
            if (scale == null || scale.Count < 3 || degree < 1 || degree > 7)
                return new List<int>();

            int i = (degree - 1) % scale.Count;
            var triad = new List<int>
            {
                ((scale[i] % 12) + 12) % 12,
                ((scale[(i + 2) % scale.Count] % 12) + 12) % 12,
                ((scale[(i + 4) % scale.Count] % 12) + 12) % 12
            };

            return triad;
        }

        /// <summary>
        /// Build a seventh chord by stacking scale steps (indices i, i+2, i+4, i+6).
        /// </summary>
        public static List<int> BuildSeventh(List<int> scale, int degree)
        {
            if (scale == null || scale.Count < 4 || degree < 1 || degree > 7)
                return new List<int>();

            int i = (degree - 1) % scale.Count;
            var seventh = new List<int>
            {
                ((scale[i] % 12) + 12) % 12,
                ((scale[(i + 2) % scale.Count] % 12) + 12) % 12,
                ((scale[(i + 4) % scale.Count] % 12) + 12) % 12,
                ((scale[(i + 6) % scale.Count] % 12) + 12) % 12
            };

            return seventh;
        }

        /// <summary>
        /// Build a chord (triad or seventh) from a custom scale.
        /// </summary>
        public static List<int> BuildChord(List<int> scale, int degree, string type)
        {
            if (scale == null || scale.Count == 0)
                return new List<int>();

            string t = (type ?? "triad").ToLowerInvariant();
            if (t == "seventh")
                return BuildSeventh(scale, degree);

            return BuildTriad(scale, degree);
        }
    }
}
