using System;
using System.Collections.Generic;
using System.Linq;

namespace ParametricMidiSequencer.Models
{
    public static class GeometricShapeTransform
    {
        /// <summary>
        /// Apply a geometric transform to a chord.
        /// </summary>
        public static List<int> Apply(List<int> chord, ShapeTransform transform)
        {
            if (transform == null || string.IsNullOrWhiteSpace(transform.Type) || chord == null || chord.Count == 0)
                return chord;

            string type = transform.Type.ToLowerInvariant();

            return type switch
            {
                "rotate" => ApplyRotate(chord, transform.Amount),
                "reflect" => ApplyReflect(chord, transform.Axis),
                "expand" => ApplyExpand(chord, transform.Amount),
                _ => chord  // Unknown transform, return unchanged
            };
        }

        /// <summary>
        /// Rotate the chord: pc' = (pc + amount) % 12
        /// </summary>
        private static List<int> ApplyRotate(List<int> chord, double amount)
        {
            int shift = ((int)amount % 12 + 12) % 12;
            return chord.Select(pc => ((pc + shift) % 12 + 12) % 12).ToList();
        }

        /// <summary>
        /// Reflect the chord across an axis: pc' = (2*axis - pc) % 12
        /// </summary>
        private static List<int> ApplyReflect(List<int> chord, int axis)
        {
            int a = ((axis % 12) + 12) % 12;
            return chord.Select(pc =>
            {
                int reflected = (2 * a - pc) % 12;
                return ((reflected % 12) + 12) % 12;
            }).ToList();
        }

        /// <summary>
        /// Expand/contract the chord around its centroid.
        /// amount > 1: expand; 0 < amount < 1: contract.
        /// pc' = C + (pc - C) * amount
        /// </summary>
        private static List<int> ApplyExpand(List<int> chord, double amount)
        {
            if (amount <= 0)
                return chord;

            // Compute centroid (average pitch class)
            double centroid = chord.Average(pc => (double)pc);

            // Expand each pitch class around the centroid
            var expanded = chord.Select(pc =>
            {
                double expanded_pc = centroid + (pc - centroid) * amount;
                // Normalize to 0-11
                int result = (int)Math.Round(expanded_pc) % 12;
                return ((result % 12) + 12) % 12;
            }).ToList();

            return expanded;
        }
    }
}
