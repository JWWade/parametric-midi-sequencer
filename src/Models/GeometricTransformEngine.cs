using System;
using System.Collections.Generic;
using System.Linq;

namespace ParametricMidiSequencer.Models
{
    /// <summary>
    /// PoC22: Geometric Transform Engine.
    /// Isolates geometric math (pitch-center cycle, shape transform, inversion)
    /// so each harmony track can apply its own independent transform pipeline.
    /// </summary>
    public class GeometricTransformEngine
    {
        /// <summary>
        /// Uniformly rotates every pitch class in <paramref name="pcs"/> by
        /// <paramref name="amount"/> semitones (mod 12).
        /// </summary>
        public List<int> ApplyPitchCenterCycle(List<int> pcs, int amount)
        {
            if (pcs == null || amount == 0)
                return pcs;

            int s = ((amount % 12) + 12) % 12;
            return pcs.Select(pc => ((pc + s) % 12 + 12) % 12).ToList();
        }

        /// <summary>
        /// Applies a geometric shape transform (rotate, reflect, or expand) to the
        /// given pitch-class set using the shared chromatic coordinate system.
        /// </summary>
        public List<int> ApplyShapeTransform(List<int> pcs, ShapeTransform transform)
        {
            return GeometricShapeTransform.Apply(pcs, transform);
        }

        /// <summary>
        /// Applies a chord inversion by rotating the voice ordering and raising the
        /// displaced voices by one octave.
        /// inversion: 0 = root position, 1 = first, 2 = second, 3 = third (seventh only).
        /// </summary>
        public List<int> ApplyInversion(List<int> pcs, int inversion)
        {
            if (pcs == null || pcs.Count < 2 || inversion <= 0)
                return pcs;

            if (inversion >= pcs.Count)
                return pcs;

            var result = new List<int>(pcs);
            for (int i = 0; i < inversion; i++)
            {
                int first = result[0];
                result.RemoveAt(0);
                result.Add(first + 12);
            }
            return result;
        }
    }
}
