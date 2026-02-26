using System;
using System.Collections.Generic;
using System.Linq;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Models
{
    public class HarmonyGenerator
    {
        public static List<ManualEvent> GenerateHarmonyEvents(HarmonySpec harmony)
        {
            var events = new List<ManualEvent>();

            if (harmony == null || harmony.Progression == null)
                return events;

            // Determine effective scale: explicit pitch-class `Scale` takes precedence,
            // otherwise build from `ScaleName` + `Root` (backwards-compatible).
            var effectiveScale = (harmony.Scale != null && harmony.Scale.Count > 0)
                ? harmony.Scale
                : ScaleBuilder.BuildScale(harmony.ScaleName, harmony.Root);

            // build raw chords (pitch classes) for each progression entry
            var chords = new List<List<int>>();
            foreach (var chord in harmony.Progression)
            {
                chords.Add(BuildChord(effectiveScale, harmony.ScaleName, chord.Degree, chord.Type));
            }

            // apply transform layer if requested
            if (harmony.Constraints != null && harmony.Constraints.MinSharedPitches > 0)
            {
                int depth = harmony.Constraints.TransformDepth > 0 ? harmony.Constraints.TransformDepth : int.MaxValue;
                ApplyMinSharedPitches(chords, harmony.Constraints.MinSharedPitches, effectiveScale, depth);
            }

            // convert final chords into manual events, preserving original times
            // Apply inversions before event generation
            for (int i = 0; i < chords.Count; i++)
            {
                var chord = chords[i];
                int time = harmony.Progression[i].Time;
                int inversion = harmony.Progression[i].Inversion;
                
                // Apply inversion if specified
                var finalChord = ApplyInversion(chord, inversion);
                
                foreach (var note in finalChord)
                {
                    events.Add(new ManualEvent
                    {
                        TimeStep = time,
                        Note = note + 60, // C4 as baseline
                        Velocity = harmony.Velocity,
                        Duration = harmony.Duration,
                        Channel = harmony.Channel
                    });
                }
            }

            return events;
        }

        #region transform helpers

        // Enforce that each chord (after the first) shares at least `minShared` pitch classes with the previous chord.
        // depth: controls multi-voice search scope (1=single, 2=pairs, 3+=triples, etc.)
        private static void ApplyMinSharedPitches(List<List<int>> chords, int minShared, List<int> scale, int depth = 2)
        {
            for (int i = 1; i < chords.Count; i++)
            {
                var prev = chords[i - 1];
                var curr = chords[i];
                if (CountShared(prev, curr) >= minShared)
                    continue;

                var adjusted = AdjustChord(curr, prev, minShared, scale, depth);
                // if adjustment returned non-null, replace
                if (adjusted != null)
                    chords[i] = adjusted;
            }
        }

        private static int CountShared(List<int> a, List<int> b)
        {
            var set = new HashSet<int>(a.Select(x => ((x % 12) + 12) % 12));
            return b.Select(x => ((x % 12) + 12) % 12).Distinct().Count(pc => set.Contains(pc));
        }

        // Make a minimal change to `curr` so that it shares at least minShared pitch classes with prev.
        // Returns the new chord (same size) or null if no acceptable adjustment found.
        // The algorithm now handles arbitrary chord sizes (root + N voices) and will
        // attempt small semitone moves on non-root voices to increase shared pitch classes.
        // depth: controls search scope (1=single voices, 2=single+pairs, 3=single+pairs+triples, etc.)
        private static List<int> AdjustChord(List<int> curr, List<int> prev, int minShared, List<int> scale, int depth = 2)
        {
            if (curr == null || curr.Count < 2)
                return null;

            int root = curr[0]; // never change

            bool Meets(List<int> cand)
            {
                return CountShared(prev, cand) >= minShared;
            }

            int voices = curr.Count;

            // attempt ±1 and ±2 semitone moves for each non-root voice
            // prefer scale-aware adjustments first
            for (int j = 1; j < voices; j++)
            {
                foreach (int delta in new[] { -1, 1, -2, 2 })
                {
                    var candidateChord = new List<int>(curr);
                    candidateChord[j] = curr[j] + delta;

                    // ensure distinct pitch classes (mod12)
                    var pcs = new HashSet<int>(candidateChord.Select(x => ((x % 12) + 12) % 12));
                    if (pcs.Count < voices)
                        continue;

                    // prefer scale members if possible
                    if (scale != null && scale.Count > 0)
                    {
                        int pc = ((candidateChord[j] % 12) + 12) % 12;
                        if (!scale.Contains(pc))
                            continue;
                    }

                    if (Meets(candidateChord))
                        return candidateChord;
                }
            }

            // fallback: try adjustments without scale filter
            for (int j = 1; j < voices; j++)
            {
                foreach (int delta in new[] { -1, 1, -2, 2 })
                {
                    var candidateChord = new List<int>(curr);
                    candidateChord[j] = curr[j] + delta;

                    var pcs = new HashSet<int>(candidateChord.Select(x => ((x % 12) + 12) % 12));
                    if (pcs.Count < voices)
                        continue;

                    if (Meets(candidateChord))
                        return candidateChord;
                }
            }

            // Try pairwise adjustments (two non-root voices) to allow reaching the target
            // when single-voice moves are insufficient. Only if depth >= 2.
            if (depth >= 2)
            {
                // Scale-aware pairs first
                for (int j1 = 1; j1 < voices; j1++)
                {
                    for (int j2 = j1 + 1; j2 < voices; j2++)
                    {
                        foreach (int d1 in new[] { -1, 1, -2, 2 })
                        foreach (int d2 in new[] { -1, 1, -2, 2 })
                        {
                            var candidate = new List<int>(curr);
                            candidate[j1] = curr[j1] + d1;
                            candidate[j2] = curr[j2] + d2;

                            var pcs = new HashSet<int>(candidate.Select(x => ((x % 12) + 12) % 12));
                            if (pcs.Count < voices)
                                continue;

                            // prefer scale tones
                            if (scale != null && scale.Count > 0)
                            {
                                int pc1 = ((candidate[j1] % 12) + 12) % 12;
                                int pc2 = ((candidate[j2] % 12) + 12) % 12;
                                if (!scale.Contains(pc1) || !scale.Contains(pc2))
                                    continue;
                            }

                            if (Meets(candidate))
                                return candidate;
                        }
                    }
                }

                // Fallback pairs without scale filter
                for (int j1 = 1; j1 < voices; j1++)
                {
                    for (int j2 = j1 + 1; j2 < voices; j2++)
                    {
                        foreach (int d1 in new[] { -1, 1, -2, 2 })
                        foreach (int d2 in new[] { -1, 1, -2, 2 })
                        {
                            var candidate = new List<int>(curr);
                            candidate[j1] = curr[j1] + d1;
                            candidate[j2] = curr[j2] + d2;

                            var pcs = new HashSet<int>(candidate.Select(x => ((x % 12) + 12) % 12));
                            if (pcs.Count < voices)
                                continue;

                            if (Meets(candidate))
                                return candidate;
                        }
                    }
                }
            }

            // Try triple adjustments if depth >= 3
            if (depth >= 3 && voices >= 4)
            {
                // Scale-aware triples
                for (int j1 = 1; j1 < voices; j1++)
                {
                    for (int j2 = j1 + 1; j2 < voices; j2++)
                    {
                        for (int j3 = j2 + 1; j3 < voices; j3++)
                        {
                            foreach (int d1 in new[] { -1, 1 })
                            foreach (int d2 in new[] { -1, 1 })
                            foreach (int d3 in new[] { -1, 1 })
                            {
                                var candidate = new List<int>(curr);
                                candidate[j1] = curr[j1] + d1;
                                candidate[j2] = curr[j2] + d2;
                                candidate[j3] = curr[j3] + d3;

                                var pcs = new HashSet<int>(candidate.Select(x => ((x % 12) + 12) % 12));
                                if (pcs.Count < voices)
                                    continue;

                                if (scale != null && scale.Count > 0)
                                {
                                    int pc1 = ((candidate[j1] % 12) + 12) % 12;
                                    int pc2 = ((candidate[j2] % 12) + 12) % 12;
                                    int pc3 = ((candidate[j3] % 12) + 12) % 12;
                                    if (!scale.Contains(pc1) || !scale.Contains(pc2) || !scale.Contains(pc3))
                                        continue;
                                }

                                if (Meets(candidate))
                                    return candidate;
                            }
                        }
                    }
                }

                // Fallback triples without scale filter
                for (int j1 = 1; j1 < voices; j1++)
                {
                    for (int j2 = j1 + 1; j2 < voices; j2++)
                    {
                        for (int j3 = j2 + 1; j3 < voices; j3++)
                        {
                            foreach (int d1 in new[] { -1, 1 })
                            foreach (int d2 in new[] { -1, 1 })
                            foreach (int d3 in new[] { -1, 1 })
                            {
                                var candidate = new List<int>(curr);
                                candidate[j1] = curr[j1] + d1;
                                candidate[j2] = curr[j2] + d2;
                                candidate[j3] = curr[j3] + d3;

                                var pcs = new HashSet<int>(candidate.Select(x => ((x % 12) + 12) % 12));
                                if (pcs.Count < voices)
                                    continue;

                                if (Meets(candidate))
                                    return candidate;
                            }
                        }
                    }
                }
            }

            // no valid adjustment found
            return null;
        }

        #endregion

        #region inversion helpers

        // Apply inversion to a chord (triad or seventh).
        // inversion: 0=root position, 1=first, 2=second, 3=third (seventh only).
        // Returns the reordered pitch classes with octave adjustments.
        private static List<int> ApplyInversion(List<int> chord, int inversion)
        {
            if (chord == null || chord.Count < 2 || inversion <= 0)
                return chord;  // Root position, no change

            var result = new List<int>(chord);
            int voices = chord.Count;

            // Validate inversion range
            if (inversion >= voices)
                return chord;  // Invalid inversion, return unchanged

            // Rotate by moving voices to the end and adding 12 (octave)
            for (int i = 0; i < inversion && i < voices; i++)
            {
                int firstNote = result[0];
                result.RemoveAt(0);
                result.Add(firstNote + 12);  // Move to higher octave
            }

            return result;
        }

        #endregion

        private static List<int> BuildTriad(List<int> scale, int degree)
        {
            var triad = new List<int>();

            // Validate degree (1-7)
            if (degree < 1 || degree > 7 || scale.Count == 0)
                return triad;

            // Root
            int root = scale[degree - 1];
            triad.Add(root);

            // Third and fifth based on scale degree quality in major scale
            int[] intervals = GetTriadIntervals(degree);
            // Wrap pitch classes to 0-11 range to ensure valid MIDI note calculation
            triad.Add(((root + intervals[1]) % 12 + 12) % 12); // major/minor third
            triad.Add(((root + intervals[2]) % 12 + 12) % 12); // perfect/diminished fifth

            return triad;
        }

        private static int[] GetTriadIntervals(int degree)
        {
            // Returns [root, third, fifth] intervals in semitones
            return degree switch
            {
                1 => new[] { 0, 4, 7 },  // major
                2 => new[] { 0, 3, 7 },  // minor
                3 => new[] { 0, 3, 7 },  // minor
                4 => new[] { 0, 4, 7 },  // major
                5 => new[] { 0, 4, 7 },  // major
                6 => new[] { 0, 3, 7 },  // minor
                7 => new[] { 0, 3, 6 },  // diminished
                _ => new[] { 0, 4, 7 }   // default major
            };
        }

        // Build a chord (triad or seventh) as pitch classes (0-11) based on scale degree and type.
        // Build a chord (triad or seventh) as pitch classes (0-11) based on scale degree and type.
        // `scaleName` guides interval selection when a named scale (major/minor) is used.
        private static List<int> BuildChord(List<int> scale, string scaleName, int degree, string type)
        {
            var chord = new List<int>();
            if (degree < 1 || degree > 7 || scale == null || scale.Count == 0)
                return chord;

            string t = (type ?? "triad").ToLowerInvariant();
            int root = scale[degree - 1];
            chord.Add(((root % 12) + 12) % 12);

            int[] intervals = ScaleBuilder.GetIntervalsForType(scaleName, degree, t);
            for (int i = 1; i < intervals.Length; i++)
            {
                chord.Add(((root + intervals[i]) % 12 + 12) % 12);
            }

            return chord;
        }

        // Note: interval selection is now delegated to ScaleBuilder for major/minor support.
    }
}
