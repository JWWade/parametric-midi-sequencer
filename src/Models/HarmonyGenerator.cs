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

            if (harmony == null || harmony.Progression == null || harmony.Scale == null)
                return events;

            // build raw chords (pitch classes) for each progression entry
            var chords = new List<List<int>>();
            foreach (var chord in harmony.Progression)
            {
                chords.Add(BuildChord(harmony.Scale, chord.Degree, chord.Type));
            }

            // apply transform layer if requested
            if (harmony.Constraints != null && harmony.Constraints.MinSharedPitches > 0)
            {
                ApplyMinSharedPitches(chords, harmony.Constraints.MinSharedPitches, harmony.Scale);
            }

            // convert final chords into manual events, preserving original times
            for (int i = 0; i < chords.Count; i++)
            {
                var chord = chords[i];
                int time = harmony.Progression[i].Time;
                foreach (var note in chord)
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
        private static void ApplyMinSharedPitches(List<List<int>> chords, int minShared, List<int> scale)
        {
            for (int i = 1; i < chords.Count; i++)
            {
                var prev = chords[i - 1];
                var curr = chords[i];
                if (CountShared(prev, curr) >= minShared)
                    continue;

                var adjusted = AdjustChord(curr, prev, minShared, scale);
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
        private static List<int> AdjustChord(List<int> curr, List<int> prev, int minShared, List<int> scale)
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

            // no valid adjustment found
            return null;
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
        private static List<int> BuildChord(List<int> scale, int degree, string type)
        {
            var chord = new List<int>();
            if (degree < 1 || degree > 7 || scale == null || scale.Count == 0)
                return chord;

            string t = (type ?? "triad").ToLowerInvariant();
            int root = scale[degree - 1];
            chord.Add(((root % 12) + 12) % 12);

            int[] intervals = GetIntervalsForType(degree, t);
            for (int i = 1; i < intervals.Length; i++)
            {
                chord.Add(((root + intervals[i]) % 12 + 12) % 12);
            }

            return chord;
        }

        private static int[] GetIntervalsForType(int degree, string type)
        {
            if (type == "seventh")
            {
                // seventh chords mapping for major scale degrees
                return degree switch
                {
                    1 => new[] { 0, 4, 7, 11 },   // maj7
                    2 => new[] { 0, 3, 7, 10 },   // m7
                    3 => new[] { 0, 3, 7, 10 },   // m7
                    4 => new[] { 0, 4, 7, 11 },   // maj7
                    5 => new[] { 0, 4, 7, 10 },   // dom7
                    6 => new[] { 0, 3, 7, 10 },   // m7
                    7 => new[] { 0, 3, 6, 10 },   // half-diminished (m7b5)
                    _ => new[] { 0, 4, 7, 10 }
                };
            }

            // default to triad intervals
            return GetTriadIntervals(degree);
        }
    }
}
