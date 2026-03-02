using System;
using System.Collections.Generic;
using System.Linq;

namespace ParametricMidiSequencer.Models
{
    /// <summary>
    /// Global voice-leading optimizer for chord progressions (PoC20).
    /// Generates candidate voicings (inversions × pitch-center offsets) for each chord and
    /// uses dynamic programming to find the minimal-cost voicing sequence across the entire
    /// progression. Fully deterministic — ties are broken by earliest candidate index.
    /// </summary>
    public static class VoiceLeadingOptimizer
    {
        /// <summary>
        /// Generates all candidate voicings for a chord by combining every cyclic rotation
        /// (inversion) with every pitch-center offset in {-2, -1, 0, +1, +2} semitones.
        /// Duplicates (same ordered pitch-class list after transposition) are removed.
        /// The original voicing (rotation 0, offset 0) is always the first candidate.
        /// </summary>
        public static List<List<int>> GenerateCandidates(List<int> chord)
        {
            if (chord == null || chord.Count == 0)
                return new List<List<int>> { chord ?? new List<int>() };

            var seen = new HashSet<string>();
            var candidates = new List<List<int>>();

            for (int rotation = 0; rotation < chord.Count; rotation++)
            {
                var rotated = CyclicRotate(chord, rotation);
                foreach (int shift in new[] { 0, -1, 1, -2, 2 })
                {
                    var candidate = rotated.Select(pc => ((pc + shift) % 12 + 12) % 12).ToList();
                    string key = string.Join(",", candidate);
                    if (seen.Add(key))
                        candidates.Add(candidate);
                }
            }

            return candidates;
        }

        /// <summary>
        /// Computes the voice-leading cost between two ordered pitch-class voicings.
        /// Lower cost indicates smoother voice leading. Includes:
        /// <list type="bullet">
        ///   <item>Sum of position-wise chromatic distances.</item>
        ///   <item>+6 penalty per voice that moves more than 5 semitones (large leap).</item>
        ///   <item>+2 penalty per pair of voices that cross between chords.</item>
        ///   <item>+3 × deficit penalty when shared tones fall below <paramref name="minSharedPitches"/>.</item>
        /// </list>
        /// </summary>
        public static int ComputeCost(List<int> voicingA, List<int> voicingB, int minSharedPitches = 0)
        {
            if (voicingA == null || voicingA.Count == 0 || voicingB == null || voicingB.Count == 0)
                return 0;

            int n = Math.Min(voicingA.Count, voicingB.Count);
            int cost = 0;

            // Position-by-position chromatic distance + large leap penalty
            for (int i = 0; i < n; i++)
            {
                int dist = VoiceLeadingHelper.ChromaticDistance(voicingA[i], voicingB[i]);
                cost += dist;
                if (dist > 5) cost += 6;
            }

            // Voice-crossing penalty: detect when the relative ordering of two voices flips
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    int diffA = ((voicingA[j] - voicingA[i]) % 12 + 12) % 12;
                    int diffB = ((voicingB[j] - voicingB[i]) % 12 + 12) % 12;
                    // A pair is "ascending" if the interval is in (0, 6]; "descending" or unison otherwise.
                    bool aAscending = diffA > 0 && diffA <= 6;
                    bool bAscending = diffB > 0 && diffB <= 6;
                    if (aAscending != bAscending)
                        cost += 2;
                }
            }

            // Shared-tone loss penalty
            if (minSharedPitches > 0)
            {
                var setA = new HashSet<int>(voicingA.Select(pc => ((pc % 12) + 12) % 12));
                int shared = voicingB.Select(pc => ((pc % 12) + 12) % 12)
                                     .Distinct()
                                     .Count(pc => setA.Contains(pc));
                if (shared < minSharedPitches)
                    cost += 3 * (minSharedPitches - shared);
            }

            return cost;
        }

        /// <summary>
        /// Optimizes a chord progression using dynamic programming.
        /// For each chord a set of candidate voicings is generated; the DP finds the path
        /// through these candidates that minimises total voice-leading cost.
        /// In case of ties the lexicographically earliest candidate index is preferred,
        /// ensuring fully deterministic, reproducible output.
        /// A single-chord progression is returned unchanged.
        /// </summary>
        public static List<List<int>> Optimize(List<List<int>> chords, int minSharedPitches = 0)
        {
            if (chords == null)
                return null;

            if (chords.Count == 0)
                return chords;

            if (chords.Count == 1)
                return new List<List<int>> { new List<int>(chords[0]) };

            int n = chords.Count;
            var candidatesPerChord = chords.Select(GenerateCandidates).ToList();

            // dp[i][j]         = min total cost reaching chord i with candidate j
            // prevChoice[i][j] = index into candidatesPerChord[i-1] that led here
            var dp         = new int[n][];
            var prevChoice = new int[n][];

            for (int i = 0; i < n; i++)
            {
                int count  = candidatesPerChord[i].Count;
                dp[i]         = new int[count];
                prevChoice[i] = new int[count];
                for (int j = 0; j < count; j++)
                {
                    dp[i][j]         = (i == 0) ? 0 : int.MaxValue / 2;
                    prevChoice[i][j] = 0;
                }
            }

            // Forward pass
            for (int i = 1; i < n; i++)
            {
                int curCount  = candidatesPerChord[i].Count;
                int prevCount = candidatesPerChord[i - 1].Count;

                for (int j = 0; j < curCount; j++)
                {
                    for (int k = 0; k < prevCount; k++)
                    {
                        int edgeCost  = ComputeCost(candidatesPerChord[i - 1][k],
                                                     candidatesPerChord[i][j],
                                                     minSharedPitches);
                        int totalCost = dp[i - 1][k] + edgeCost;

                        // Prefer smaller total cost; on tie, prefer smaller previous index (determinism)
                        if (totalCost < dp[i][j] ||
                            (totalCost == dp[i][j] && k < prevChoice[i][j]))
                        {
                            dp[i][j]         = totalCost;
                            prevChoice[i][j] = k;
                        }
                    }
                }
            }

            // Select best candidate for the last chord (smallest index on tie)
            int lastBest = 0;
            for (int j = 1; j < candidatesPerChord[n - 1].Count; j++)
            {
                if (dp[n - 1][j] < dp[n - 1][lastBest])
                    lastBest = j;
            }

            // Backtrack to reconstruct the optimal voicing sequence
            var result = new List<int>[n];
            int cur = lastBest;
            for (int i = n - 1; i >= 0; i--)
            {
                result[i] = candidatesPerChord[i][cur];
                if (i > 0)
                    cur = prevChoice[i][cur];
            }

            return result.ToList();
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static List<int> CyclicRotate(List<int> list, int rotation)
        {
            if (rotation == 0) return new List<int>(list);
            var result = new List<int>(list.Count);
            for (int i = 0; i < list.Count; i++)
                result.Add(list[(i + rotation) % list.Count]);
            return result;
        }
    }
}
