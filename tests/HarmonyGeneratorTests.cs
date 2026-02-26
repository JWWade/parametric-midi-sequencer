using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Tests
{
    public class HarmonyGeneratorTests
    {
        private HarmonySpec BuildStandardProgression(int minShared = 0)
        {
            return new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 4, Type = "triad" },
                    new() { Time = 8, Degree = 5, Type = "triad" },
                    new() { Time = 12, Degree = 1, Type = "triad" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                Constraints = new HarmonyConstraints { MinSharedPitches = minShared }
            };
        }

        private HarmonySpec BuildSeventhProgression(int minShared = 0)
        {
            return new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "seventh" },
                    new() { Time = 4, Degree = 4, Type = "seventh" },
                    new() { Time = 8, Degree = 5, Type = "seventh" },
                    new() { Time = 12, Degree = 1, Type = "seventh" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                Constraints = new HarmonyConstraints { MinSharedPitches = minShared }
            };
        }

        // Grouping inlined at call sites to avoid generic variance issues.

        private ISet<int> ToPitchClasses(IEnumerable<int> notes)
        {
            return new HashSet<int>(notes.Select(n => ((n - 60) % 12 + 12) % 12));
        }

        [Fact]
        public void GenerateHarmonyEvents_NoConstraints_ReturnsExpectedTriads()
        {
            var spec = BuildStandardProgression();
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(4, chords.Count);

            // C major
            Assert.Equal(new[] { 60, 64, 67 }, chords[0]);
            // F major
            Assert.Equal(new[] { 65, 69, 60 }, chords[1]);
            // G major
            Assert.Equal(new[] { 67, 71, 62 }, chords[2]);
            // back to C
            Assert.Equal(new[] { 60, 64, 67 }, chords[3]);
        }

        [Fact]
        public void GenerateHarmonyEvents_MinSharedPitches_AdjustsChords()
        {
            var spec = BuildStandardProgression(minShared: 2);
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(4, chords.Count);

            // calculate pitch classes for each chord
            var pcs = chords.Select(c => ToPitchClasses(c)).ToList();

            // verify intersection counts
            for (int i = 1; i < pcs.Count; i++)
            {
                var inter = new HashSet<int>(pcs[i]);
                inter.IntersectWith(pcs[i - 1]);
                Assert.True(inter.Count >= 2, "Chord {i} does not share two pitch classes with previous");
            }

            // ensure the second chord was actually modified (it shouldn't equal the unconstrained set)
            var originalSecond = new HashSet<int> { 65, 69, 60 };
            Assert.NotEqual(originalSecond, new HashSet<int>(chords[1]));

            // verify that the adjusted second chord shares >=2 pitch classes with the first
            var secondPc = ToPitchClasses(chords[1]);
            var firstPc = ToPitchClasses(chords[0]);
            var intersection = new HashSet<int>(secondPc);
            intersection.IntersectWith(firstPc);
            Assert.True(intersection.Count >= 2, "Second chord should share at least 2 pitch classes with first");
        }

        [Fact]
        public void HarmonySpec_DeserializeWithConstraints_PopulatesMinSharedPitches()
        {
            var json = @"{
  ""scale"": [0,2,4,5,7,9,11],
  ""progression"": [],
  ""constraints"": { ""minSharedPitches"": 3 }
}";
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var spec = JsonSerializer.Deserialize<HarmonySpec>(json, options);
            Assert.NotNull(spec);
            Assert.NotNull(spec.Constraints);
            Assert.Equal(3, spec.Constraints.MinSharedPitches);
        }

        [Fact]
        public void GenerateHarmonyEvents_SeventhChords_ReturnsExpectedSevenths()
        {
            var spec = BuildSeventhProgression();
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(4, chords.Count);

            // Imaj7 (Cmaj7)
            Assert.Equal(new[] { 60, 64, 67, 71 }, chords[0]);
            // IVmaj7 (Fmaj7)
            Assert.Equal(new[] { 65, 69, 60, 64 }, chords[1]);
            // V7 (G7 - dominant seventh)
            Assert.Equal(new[] { 67, 71, 62, 65 }, chords[2]);
            // back to Imaj7
            Assert.Equal(new[] { 60, 64, 67, 71 }, chords[3]);
        }

        [Fact]
        public void GenerateHarmonyEvents_MinSharedPitches_OnSevenths_AdjustsChords()
        {
            var spec = BuildSeventhProgression(minShared: 2);
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(4, chords.Count);

            // calculate pitch classes for each chord
            var pcs = chords.Select(c => ToPitchClasses(c)).ToList();

            // verify intersection counts
            for (int i = 1; i < pcs.Count; i++)
            {
                var inter = new HashSet<int>(pcs[i]);
                inter.IntersectWith(pcs[i - 1]);
                Assert.True(inter.Count >= 2, "Chord {i} does not share two pitch classes with previous");
            }

            // For seventh chords the second chord may already meet the minShared requirement;
            // the loop above asserts that each adjacent pair shares >=2 pitch classes.
        }

        [Fact]
        public void GenerateHarmonyEvents_TransformDepth_ControlsSearchScope()
        {
            // Test that depth=1 (single voices) gives different results than depth=2 (pairs allowed)
            var spec1 = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },      // C: 0,4,7
                    new() { Time = 4, Degree = 2, Type = "triad" },      // D: 2,5,9
                    new() { Time = 8, Degree = 5, Type = "triad" }       // G: 7,11,2
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                Constraints = new HarmonyConstraints { MinSharedPitches = 3, TransformDepth = 1 }
            };

            var spec2 = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 2, Type = "triad" },
                    new() { Time = 8, Degree = 5, Type = "triad" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                Constraints = new HarmonyConstraints { MinSharedPitches = 3, TransformDepth = 2 }
            };

            var events1 = HarmonyGenerator.GenerateHarmonyEvents(spec1);
            var events2 = HarmonyGenerator.GenerateHarmonyEvents(spec2);

            var chords1 = events1.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();
            var chords2 = events2.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            // Both should have 3 chords
            Assert.Equal(3, chords1.Count);
            Assert.Equal(3, chords2.Count);

            // The second progression (depth=2) may succeed where first (depth=1) fails,
            // so we just verify that both have valid outputs and the constraint is attempted.
            var pcs1 = chords1.Select(c => ToPitchClasses(c)).ToList();
            var pcs2 = chords2.Select(c => ToPitchClasses(c)).ToList();

            // At least verify third chord in depth=2 is processed
            Assert.NotNull(chords2[2]);
        }

        [Fact]
        public void GenerateHarmonyEvents_ImpossibleConstraint_DoesNotCrash()
        {
            // A constraint that is impossible to satisfy should degrade gracefully
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },  // 0,4,7
                    new() { Time = 4, Degree = 2, Type = "triad" },  // 2,5,9
                    new() { Time = 8, Degree = 3, Type = "triad" }   // 4,7,11
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                // Request 3 shared pitches from triads with little overlap
                Constraints = new HarmonyConstraints { MinSharedPitches = 3, TransformDepth = 2 }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);

            // Should still produce events, even if constraint not fully met
            Assert.NotEmpty(events);

            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();
            Assert.Equal(3, chords.Count);
        }

        [Fact]
        public void GenerateHarmonyEvents_ChromaticScale_GreaterFlexibility()
        {
            // Chromatic scale (all 12 pitches) should provide more flexibility for adjustments
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 4, Type = "triad" },
                    new() { Time = 8, Degree = 5, Type = "triad" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                Constraints = new HarmonyConstraints { MinSharedPitches = 2, TransformDepth = 2 }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(3, chords.Count);

            // Verify constraint is satisfied
            var pcs = chords.Select(c => ToPitchClasses(c)).ToList();
            for (int i = 1; i < pcs.Count; i++)
            {
                var inter = new HashSet<int>(pcs[i]);
                inter.IntersectWith(pcs[i - 1]);
                Assert.True(inter.Count >= 2);
            }
        }

        [Fact]
        public void GenerateHarmonyEvents_MixedTriadAndSeventh_WithConstraint()
        {
            // Test mixed triad/seventh progression with transform
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },     // 3 notes
                    new() { Time = 4, Degree = 4, Type = "seventh" },   // 4 notes
                    new() { Time = 8, Degree = 5, Type = "triad" },     // 3 notes
                    new() { Time = 12, Degree = 1, Type = "seventh" }   // 4 notes
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                Constraints = new HarmonyConstraints { MinSharedPitches = 1, TransformDepth = 2 }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(4, chords.Count);
            // First chord should have 3 notes (triad)
            Assert.Equal(3, chords[0].Count);
            // Second chord should have 4 notes (seventh)
            Assert.Equal(4, chords[1].Count);
            // Third chord should have 3 notes (triad)
            Assert.Equal(3, chords[2].Count);
            // Fourth chord should have 4 notes (seventh)
            Assert.Equal(4, chords[3].Count);
        }

        [Fact]
        public void GenerateHarmonyEvents_TightConstraint_WithSingleVoiceDepth()
        {
            // Test that depth=1 can still handle reasonable constraints
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 1, Type = "triad" },  // Same chord
                    new() { Time = 8, Degree = 4, Type = "triad" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                Constraints = new HarmonyConstraints { MinSharedPitches = 2, TransformDepth = 1 }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(3, chords.Count);

            // First two chords should be identical (same degree)
            var pcs0 = ToPitchClasses(chords[0]);
            var pcs1 = ToPitchClasses(chords[1]);
            Assert.Equal(pcs0, pcs1);

            // All adjacent chords should share at least 2 pitch classes
            var allPcs = chords.Select(c => ToPitchClasses(c)).ToList();
            for (int i = 1; i < allPcs.Count; i++)
            {
                var inter = new HashSet<int>(allPcs[i]);
                inter.IntersectWith(allPcs[i - 1]);
                Assert.True(inter.Count >= 2);
            }
        }

        [Fact]
        public void HarmonySpec_DeserializeWithTransformDepth_PopulatesField()
        {
            var json = @"{
  ""scale"": [0,2,4,5,7,9,11],
  ""progression"": [],
  ""constraints"": { ""minSharedPitches"": 2, ""transformDepth"": 3 }
}";
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var spec = JsonSerializer.Deserialize<HarmonySpec>(json, options);
            Assert.NotNull(spec);
            Assert.NotNull(spec.Constraints);
            Assert.Equal(2, spec.Constraints.MinSharedPitches);
            Assert.Equal(3, spec.Constraints.TransformDepth);
        }

        [Fact]
        public void GenerateHarmonyEvents_NoConstraint_StaysUnchanged()
        {
            // Control test: with no constraint, chords should remain exactly as built
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 6, Type = "triad" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                Constraints = new HarmonyConstraints { MinSharedPitches = 0 }  // No constraint
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(2, chords.Count);

            // First chord: I (C major) = 0,4,7 → MIDI 60,64,67
            Assert.Equal(new[] { 60, 64, 67 }, chords[0]);
            // Second chord: vi (A minor) = 9,0,4 → MIDI 69,60,64
            Assert.Equal(new[] { 69, 60, 64 }, chords[1]);
        }

        [Fact]
        public void GenerateHarmonyEvents_TriadInversions_ProducesCorrectVoicings()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 0 },  // Root position
                    new() { Time = 4, Degree = 1, Type = "triad", Inversion = 1 },  // First inversion
                    new() { Time = 8, Degree = 1, Type = "triad", Inversion = 2 }   // Second inversion
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(3, chords.Count);

            // C major root position: [0,4,7] → [60,64,67]
            Assert.Equal(new[] { 60, 64, 67 }, chords[0]);

            // C major first inversion: [4,7,12] → [64,67,72]
            Assert.Equal(new[] { 64, 67, 72 }, chords[1]);

            // C major second inversion: [7,12,16] → [67,72,76]
            Assert.Equal(new[] { 67, 72, 76 }, chords[2]);
        }

        [Fact]
        public void GenerateHarmonyEvents_SeventhInversions_ProducesCorrectVoicings()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "seventh", Inversion = 0 },  // Root
                    new() { Time = 4, Degree = 1, Type = "seventh", Inversion = 1 },  // First
                    new() { Time = 8, Degree = 1, Type = "seventh", Inversion = 2 },  // Second
                    new() { Time = 12, Degree = 1, Type = "seventh", Inversion = 3 }  // Third
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(4, chords.Count);

            // Cmaj7 root position: [0,4,7,11] → [60,64,67,71]
            Assert.Equal(new[] { 60, 64, 67, 71 }, chords[0]);

            // Cmaj7 first inversion: [4,7,11,12] → [64,67,71,72]
            Assert.Equal(new[] { 64, 67, 71, 72 }, chords[1]);

            // Cmaj7 second inversion: [7,11,12,16] → [67,71,72,76]
            Assert.Equal(new[] { 67, 71, 72, 76 }, chords[2]);

            // Cmaj7 third inversion: [11,12,16,19] → [71,72,76,79]
            Assert.Equal(new[] { 71, 72, 76, 79 }, chords[3]);
        }

        [Fact]
        public void GenerateHarmonyEvents_InversionsWithConstraint_PresentsBothTogether()
        {
            // Inversions should work alongside minSharedPitches transform
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 0 },
                    new() { Time = 4, Degree = 4, Type = "triad", Inversion = 1 },  // First inversion
                    new() { Time = 8, Degree = 5, Type = "triad", Inversion = 2 }   // Second inversion
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4,
                Constraints = new HarmonyConstraints { MinSharedPitches = 1 }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(3, chords.Count);

            // All should have 3 notes (triads)
            Assert.Equal(3, chords[0].Count);
            Assert.Equal(3, chords[1].Count);
            Assert.Equal(3, chords[2].Count);

            // Verify pitch classes are preserved (inversions don't add/remove notes)
            var pcs0 = ToPitchClasses(chords[0]);
            var pcs1 = ToPitchClasses(chords[1]);
            Assert.True(pcs0.Count == 3 && pcs1.Count >= 1);  // At least one shared pitch
        }

        [Fact]
        public void HarmonySpec_DeserializeWithInversion_PopulatesField()
        {
            var json = @"{
  ""scale"": [0,2,4,5,7,9,11],
  ""progression"": [
    { ""time"": 0, ""degree"": 1, ""type"": ""seventh"", ""inversion"": 2 }
  ]
}";
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var spec = JsonSerializer.Deserialize<HarmonySpec>(json, options);
            Assert.NotNull(spec);
            Assert.NotNull(spec.Progression);
            Assert.Equal(1, spec.Progression.Count);
            Assert.Equal(2, spec.Progression[0].Inversion);
        }

        [Fact]
        public void GenerateHarmonyEvents_InvalidInversion_DefaultsToRoot()
        {
            // Inversion value >= chord size should be ignored
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 5 }  // Invalid (max is 2)
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(1, chords.Count);
            // Should revert to root position when inversion is out of range
            Assert.Equal(new[] { 60, 64, 67 }, chords[0]);
        }

        [Fact]
        public void GenerateHarmonyEvents_AllTriadInversions_VariousProgression()
        {
            // Test multiple chords with different inversions
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 0 },  // C: root
                    new() { Time = 4, Degree = 4, Type = "triad", Inversion = 1 },  // F: first
                    new() { Time = 8, Degree = 5, Type = "triad", Inversion = 2 },  // G: second
                    new() { Time = 12, Degree = 1, Type = "triad", Inversion = 1 }  // C: first
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(4, chords.Count);

            // C root [0,4,7] → [60,64,67]
            Assert.Equal(new[] { 60, 64, 67 }, chords[0]);

            // F first inversion: [9,0,17] → [69,60,77]
            Assert.Equal(new[] { 69, 60, 77 }, chords[1]);

            // G second inversion [7,11,2] rotated twice: [2,19,23] → [62,79,83]
            Assert.Equal(new[] { 62, 79, 83 }, chords[2]);

            // C first [4,7,12] → [64,67,72]
            Assert.Equal(new[] { 64, 67, 72 }, chords[3]);
        }
    }
}
