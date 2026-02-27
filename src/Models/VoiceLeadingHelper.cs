using System;
using System.Collections.Generic;

namespace ParametricMidiSequencer.Models
{
    /// <summary>
    /// Provides voice-leading utilities for matching pitch classes between consecutive chords.
    /// </summary>
    public static class VoiceLeadingHelper
    {
        /// <summary>
        /// Computes voice-leading pairs using greedy nearest-neighbor matching on the chromatic circle.
        /// For each pitch class in <paramref name="chordA"/>, the nearest unused pitch class in
        /// <paramref name="chordB"/> is selected. One-to-one mapping is enforced.
        /// Supports N→M chord mappings; only min(|A|, |B|) pairs are produced.
        /// </summary>
        /// <param name="chordA">Pitch classes (0–11) for the current chord.</param>
        /// <param name="chordB">Pitch classes (0–11) for the next chord.</param>
        /// <returns>List of (From, To) pitch-class pairs.</returns>
        public static List<(int From, int To)> ComputeVoiceLeadingPairs(List<int> chordA, List<int> chordB)
        {
            var result = new List<(int, int)>();
            if (chordA == null || chordA.Count == 0 || chordB == null || chordB.Count == 0)
                return result;

            var remaining = new List<int>(chordB);
            foreach (var a in chordA)
            {
                if (remaining.Count == 0)
                    break;

                int best = remaining[0];
                int bestDist = ChromaticDistance(a, best);
                for (int i = 1; i < remaining.Count; i++)
                {
                    int d = ChromaticDistance(a, remaining[i]);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = remaining[i];
                    }
                }

                result.Add((a, best));
                remaining.Remove(best);
            }

            return result;
        }

        /// <summary>
        /// Returns the minimum circular distance between two pitch classes (0–11).
        /// </summary>
        public static int ChromaticDistance(int a, int b)
        {
            int diff = Math.Abs(a - b) % 12;
            return Math.Min(diff, 12 - diff);
        }
    }
}
