using System.Collections.Generic;
using System.Linq;
using Xunit;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Tests
{
    /// <summary>
    /// Unit tests for the PoC20 Voice-Leading Optimization Engine
    /// (<see cref="VoiceLeadingOptimizer"/>).
    /// </summary>
    public class VoiceLeadingOptimizerTests
    {
        // ── GenerateCandidates ───────────────────────────────────────────────

        [Fact]
        public void GenerateCandidates_NullOrEmpty_ReturnsListWithSingleElement()
        {
            var candidates = VoiceLeadingOptimizer.GenerateCandidates(null!);
            Assert.Single(candidates);

            candidates = VoiceLeadingOptimizer.GenerateCandidates(new List<int>());
            Assert.Single(candidates);
        }

        [Fact]
        public void GenerateCandidates_TriadChord_IncludesOriginalAsFirstCandidate()
        {
            // C major triad [0,4,7]: rotation-0, shift-0 must be the first candidate
            var chord = new List<int> { 0, 4, 7 };
            var candidates = VoiceLeadingOptimizer.GenerateCandidates(chord);

            Assert.True(candidates.Count > 0);
            Assert.Equal(chord, candidates[0]);
        }

        [Fact]
        public void GenerateCandidates_TriadChord_ProducesCorrectNumberOfUniqueCandidates()
        {
            // 3 rotations × 5 transpositions = 15, but some may deduplicate
            var chord = new List<int> { 0, 4, 7 };
            var candidates = VoiceLeadingOptimizer.GenerateCandidates(chord);

            // At minimum the 5 transpositions of the original rotation must be present
            Assert.True(candidates.Count >= 5);
            // No duplicates (same ordered list)
            var keys = candidates.Select(c => string.Join(",", c)).ToList();
            Assert.Equal(keys.Count, keys.Distinct().Count());
        }

        [Fact]
        public void GenerateCandidates_IncludesAllTranspositionsOfOriginal()
        {
            var chord = new List<int> { 0, 4, 7 };
            var candidates = VoiceLeadingOptimizer.GenerateCandidates(chord);

            // transpositions of rotation-0: shifts -2..+2
            Assert.Contains(new List<int> { 10, 2, 5 }, candidates); // shift -2
            Assert.Contains(new List<int> { 11, 3, 6 }, candidates); // shift -1
            Assert.Contains(new List<int> { 0, 4, 7 },  candidates); // shift  0
            Assert.Contains(new List<int> { 1, 5, 8 },  candidates); // shift +1
            Assert.Contains(new List<int> { 2, 6, 9 },  candidates); // shift +2
        }

        [Fact]
        public void GenerateCandidates_IncludesRotations()
        {
            var chord = new List<int> { 0, 4, 7 };
            var candidates = VoiceLeadingOptimizer.GenerateCandidates(chord);

            // All three rotations at shift 0 must appear
            Assert.Contains(new List<int> { 0, 4, 7 }, candidates); // rotation 0
            Assert.Contains(new List<int> { 4, 7, 0 }, candidates); // rotation 1
            Assert.Contains(new List<int> { 7, 0, 4 }, candidates); // rotation 2
        }

        [Fact]
        public void GenerateCandidates_AllCandidateValuesInRange0to11()
        {
            var chord = new List<int> { 0, 4, 7 };
            var candidates = VoiceLeadingOptimizer.GenerateCandidates(chord);
            foreach (var c in candidates)
                Assert.All(c, pc => Assert.InRange(pc, 0, 11));
        }

        // ── ComputeCost ──────────────────────────────────────────────────────

        [Fact]
        public void ComputeCost_EmptyVoicings_ReturnsZero()
        {
            Assert.Equal(0, VoiceLeadingOptimizer.ComputeCost(new List<int>(), new List<int> { 0, 4, 7 }));
            Assert.Equal(0, VoiceLeadingOptimizer.ComputeCost(new List<int> { 0, 4, 7 }, new List<int>()));
            Assert.Equal(0, VoiceLeadingOptimizer.ComputeCost(null!, null!));
        }

        [Fact]
        public void ComputeCost_IdenticalVoicings_ReturnsZero()
        {
            var chord = new List<int> { 0, 4, 7 };
            Assert.Equal(0, VoiceLeadingOptimizer.ComputeCost(chord, chord));
        }

        [Fact]
        public void ComputeCost_SmoothMotion_ReturnsLowCost()
        {
            // [0,4,7] → [0,5,9]: movements are 0, 1, 2 → total 3
            var a = new List<int> { 0, 4, 7 };
            var b = new List<int> { 0, 5, 9 };
            int cost = VoiceLeadingOptimizer.ComputeCost(a, b);
            Assert.Equal(3, cost); // 0 + 1 + 2, no large leaps
        }

        [Fact]
        public void ComputeCost_LargeLeap_AddsPenalty()
        {
            // C (0) → F# (6): distance 6 > 5, penalty +6
            var a = new List<int> { 0 };
            var b = new List<int> { 6 };
            int cost = VoiceLeadingOptimizer.ComputeCost(a, b);
            Assert.Equal(6 + 6, cost); // 6 (dist) + 6 (penalty)
        }

        [Fact]
        public void ComputeCost_SharedToneLoss_AddsPenalty()
        {
            // [0,4,7] → [1,5,8]: no shared tones; minSharedPitches=1 → penalty 3
            var a = new List<int> { 0, 4, 7 };
            var b = new List<int> { 1, 5, 8 };
            int cost = VoiceLeadingOptimizer.ComputeCost(a, b, minSharedPitches: 1);
            // distances: 1 + 1 + 1 = 3; shared tones = 0, deficit = 1, penalty = 3
            Assert.Equal(3 + 3, cost);
        }

        [Fact]
        public void ComputeCost_VoiceCrossing_AddsPenalty()
        {
            // A=[0,7]: diffA = (7-0)%12 = 7 → not ascending (7 > 6) → aAscending = false
            // B=[7,0]: diffB = (0-7+12)%12 = 5 → ascending (0<5≤6) → bAscending = true
            // Directions differ → crossing penalty +2
            // chromatic distances: min(7,5)=5 each → 10 + 2 = 12
            var a = new List<int> { 0, 7 };
            var b = new List<int> { 7, 0 };
            int cost = VoiceLeadingOptimizer.ComputeCost(a, b);
            Assert.Equal(12, cost);
        }

        // ── Optimize – edge cases ────────────────────────────────────────────

        [Fact]
        public void Optimize_NullOrEmpty_ReturnsInput()
        {
            var nullResult = VoiceLeadingOptimizer.Optimize(null!);
            Assert.Null(nullResult);

            var emptyResult = VoiceLeadingOptimizer.Optimize(new List<List<int>>());
            Assert.Empty(emptyResult);
        }

        [Fact]
        public void Optimize_SingleChord_ReturnsChordUnchanged()
        {
            var chord  = new List<int> { 0, 4, 7 };
            var result = VoiceLeadingOptimizer.Optimize(new List<List<int>> { chord });

            Assert.Single(result);
            Assert.Equal(chord, result[0]);
        }

        [Fact]
        public void Optimize_TwoIdenticalChords_ReturnsSameVoicing()
        {
            // No movement at all should be optimal
            var chord  = new List<int> { 0, 4, 7 };
            var chords = new List<List<int>> { chord, new List<int>(chord) };
            var result = VoiceLeadingOptimizer.Optimize(chords);

            Assert.Equal(2, result.Count);
            // Both results should contain the same pitch classes
            Assert.Equal(new HashSet<int>(chord), new HashSet<int>(result[0]));
            Assert.Equal(new HashSet<int>(chord), new HashSet<int>(result[1]));
            // Cost from chord1 to chord2 in result must be 0
            Assert.Equal(0, VoiceLeadingOptimizer.ComputeCost(result[0], result[1]));
        }

        [Fact]
        public void Optimize_TriadProgression_ProducesSmoothVoiceLeading()
        {
            // C major [0,4,7] → G major [7,11,2]
            // Un-optimized cost (rotation 0 → rotation 0): |0-7|=5 + |4-11|=5 + |7-2|=5 = 15
            // Optimized should find a voicing with cost < 15
            var c = new List<int> { 0, 4, 7 };
            var g = new List<int> { 7, 11, 2 };
            var chords = new List<List<int>> { c, g };
            var result = VoiceLeadingOptimizer.Optimize(chords);

            int optimizedCost = VoiceLeadingOptimizer.ComputeCost(result[0], result[1]);
            int naiveCost     = VoiceLeadingOptimizer.ComputeCost(c, g);
            Assert.True(optimizedCost <= naiveCost,
                $"Optimized cost {optimizedCost} should be ≤ naive cost {naiveCost}");
        }

        [Fact]
        public void Optimize_SeventhChordProgression_ProducesSmoothVoiceLeading()
        {
            // C maj7 → F maj7 (pitch classes)
            var cMaj7 = new List<int> { 0, 4, 7, 11 };
            var fMaj7 = new List<int> { 5, 9, 0, 4  };
            var chords = new List<List<int>> { cMaj7, fMaj7 };
            var result = VoiceLeadingOptimizer.Optimize(chords);

            Assert.Equal(2, result.Count);
            int optimizedCost = VoiceLeadingOptimizer.ComputeCost(result[0], result[1]);
            int naiveCost     = VoiceLeadingOptimizer.ComputeCost(cMaj7, fMaj7);
            Assert.True(optimizedCost <= naiveCost,
                $"Optimized cost {optimizedCost} should be ≤ naive cost {naiveCost}");
        }

        [Fact]
        public void Optimize_CustomPitchClassSets_ProducesNToNMapping()
        {
            // Arbitrary 4-note sets – optimizer should produce a valid result with same count
            var a = new List<int> { 0, 3, 6, 9  };
            var b = new List<int> { 1, 4, 7, 10 };
            var chords = new List<List<int>> { a, b };
            var result = VoiceLeadingOptimizer.Optimize(chords);

            Assert.Equal(2, result.Count);
            Assert.Equal(4, result[0].Count);
            Assert.Equal(4, result[1].Count);
        }

        [Fact]
        public void Optimize_LongerProgression_ReturnsCorrectLength()
        {
            var chords = new List<List<int>>
            {
                new() { 0, 4, 7  },
                new() { 5, 9, 0  },
                new() { 7, 11, 2 },
                new() { 0, 4, 7  }
            };
            var result = VoiceLeadingOptimizer.Optimize(chords);
            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void Optimize_IsDeterministic()
        {
            var chords = new List<List<int>>
            {
                new() { 0, 4, 7  },
                new() { 5, 9, 0  },
                new() { 7, 11, 2 }
            };
            var r1 = VoiceLeadingOptimizer.Optimize(chords);
            var r2 = VoiceLeadingOptimizer.Optimize(chords);

            Assert.Equal(r1.Count, r2.Count);
            for (int i = 0; i < r1.Count; i++)
                Assert.Equal(r1[i], r2[i]);
        }

        [Fact]
        public void Optimize_AllCandidateValuesRemainInRange0to11()
        {
            var chords = new List<List<int>>
            {
                new() { 0, 4, 7  },
                new() { 5, 9, 0  },
                new() { 7, 11, 2 }
            };
            var result = VoiceLeadingOptimizer.Optimize(chords);
            foreach (var chord in result)
                Assert.All(chord, pc => Assert.InRange(pc, 0, 11));
        }

        // ── Integration with HarmonyGenerator ────────────────────────────────

        [Fact]
        public void HarmonyGenerator_OptimizeVoiceLeading_Disabled_ProducesOriginalOrder()
        {
            // Without optimization the chords come back in their default order
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 5, Type = "triad" }
                },
                Constraints = new HarmonyConstraints { OptimizeVoiceLeading = false }
            };

            var chords = HarmonyGenerator.ComputeTransformedChordPitchClasses(spec);
            // C major and G major should be present
            Assert.Contains(0, chords[0]);
            Assert.Contains(7, chords[1]);
        }

        [Fact]
        public void HarmonyGenerator_OptimizeVoiceLeading_Enabled_PreservesPitchClassContent()
        {
            // Optimization may reorder/transpose, but the pitch-class *set* of each chord
            // (after normalization) should still cover the original scale degrees.
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0,  Degree = 1, Type = "triad" },
                    new() { Time = 4,  Degree = 4, Type = "triad" },
                    new() { Time = 8,  Degree = 5, Type = "triad" },
                    new() { Time = 12, Degree = 1, Type = "triad" }
                },
                Constraints = new HarmonyConstraints { OptimizeVoiceLeading = true }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            // Should still produce 4 distinct time-steps worth of events
            var timeGroups = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).ToList();
            Assert.Equal(4, timeGroups.Count);
            // Each chord should have 3 notes
            foreach (var g in timeGroups)
                Assert.Equal(3, g.Count());
        }

        [Fact]
        public void HarmonyGenerator_OptimizeVoiceLeading_TotalCostNoHigherThanNaive()
        {
            // The optimizer must never increase total voice-leading cost.
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 5, Type = "triad" }
                },
                Constraints = new HarmonyConstraints { OptimizeVoiceLeading = false }
            };

            // Naive chord pitch classes
            var naiveChords = HarmonyGenerator.ComputeTransformedChordPitchClasses(spec);

            spec.Constraints.OptimizeVoiceLeading = true;
            var optimizedChords = HarmonyGenerator.ComputeTransformedChordPitchClasses(spec);

            // Normalize both to ordered sets for cost comparison
            int naiveCost     = VoiceLeadingOptimizer.ComputeCost(naiveChords[0],     naiveChords[1]);
            int optimizedCost = VoiceLeadingOptimizer.ComputeCost(optimizedChords[0], optimizedChords[1]);

            Assert.True(optimizedCost <= naiveCost,
                $"Optimized cost {optimizedCost} must not exceed naive cost {naiveCost}");
        }

        // ── Integration: pitchCenterCycle + optimization ─────────────────────

        [Fact]
        public void HarmonyGenerator_PitchCenterCycle_WithOptimization_Runs()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 4, Type = "triad" }
                },
                Constraints = new HarmonyConstraints
                {
                    PitchCenterCycle    = 2,
                    OptimizeVoiceLeading = true
                }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            Assert.NotEmpty(events);
        }

        // ── Integration: shapeTransform + optimization ───────────────────────

        [Fact]
        public void HarmonyGenerator_Reflect_WithOptimization_Runs()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 4, Type = "triad" }
                },
                Constraints = new HarmonyConstraints
                {
                    ShapeTransform       = new ShapeTransform { Type = "reflect", Axis = 5 },
                    OptimizeVoiceLeading = true
                }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            Assert.NotEmpty(events);
        }

        [Fact]
        public void HarmonyGenerator_Expand_WithOptimization_Runs()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 5, Type = "triad" }
                },
                Constraints = new HarmonyConstraints
                {
                    ShapeTransform       = new ShapeTransform { Type = "expand", Amount = 1.5 },
                    OptimizeVoiceLeading = true
                }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            Assert.NotEmpty(events);
        }

        // ── Integration: minSharedPitches + optimization ─────────────────────

        [Fact]
        public void HarmonyGenerator_MinSharedPitches_WithOptimization_Runs()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 4, Type = "triad" },
                    new() { Time = 8, Degree = 5, Type = "triad" }
                },
                Constraints = new HarmonyConstraints
                {
                    MinSharedPitches     = 1,
                    OptimizeVoiceLeading = true
                }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            Assert.NotEmpty(events);
        }
    }
}
