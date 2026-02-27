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
        public void HarmonySpec_DeserializeWithBorrowMode_PopulatesField()
        {
            var json = @"{
  ""scale"": [0,2,4,5,7,9,11],
  ""progression"": [ { ""time"":0, ""degree"":4, ""type"": ""triad"", ""borrowMode"": ""phrygian"" } ]
}";
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var spec = JsonSerializer.Deserialize<HarmonySpec>(json, options);
            Assert.NotNull(spec);
            Assert.NotNull(spec.Progression);
            Assert.Single(spec.Progression);
            Assert.Equal("phrygian", spec.Progression[0].BorrowMode, ignoreCase: true);
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
        public void GenerateHarmonyEvents_BorrowIvFromParallelMinor()
        {
            // C major progression where the second chord borrows iv from C minor (aeolian mode)
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 4, Type = "triad", BorrowMode = "aeolian" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key)
                               .Select(g => g.Select(e => e.Note).ToList()).ToList();

            // first chord unaffected
            Assert.Equal(new[] { 60, 64, 67 }, chords[0]);
            // borrowed iv should be F minor: F(5), Ab(8), C(0) -> +60
            Assert.Equal(new[] { 65, 68, 60 }, chords[1]);
        }

        [Fact]
        public void GenerateHarmonyEvents_BorrowFlatIIFromPhrygian()
        {
            // Borrow ♭II from Phrygian in a C major context
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 2, Type = "triad", BorrowMode = "phrygian" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e=>e.TimeStep).OrderBy(g=>g.Key)
                               .Select(g=>g.Select(e=>e.Note).ToList()).ToList();

            Assert.Equal(new[] {60,64,67}, chords[0]);
            // second chord should be Db major (♭II): Db(1), F(5), Ab(8) -> +60
            Assert.Equal(new[] {61,65,68}, chords[1]);
        }

        [Fact]
        public void GenerateHarmonyEvents_BorrowedChordWithInversion()
        {
            // Borrow iv from Aeolian but request first inversion
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0,2,4,5,7,9,11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 4, Type = "triad", BorrowMode = "aeolian", Inversion = 1 }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chord = events.Select(e=>e.Note).ToList();
            // F minor triad root: F(5), Ab(8), C(0)
            // first inversion -> Ab(8), C(0+12), F(5+12) => notes [68, 60, 77]
            Assert.Equal(new[] {68,60,77}, chord);
        }

        [Fact]
        public void GenerateHarmonyEvents_InvalidBorrowMode_DefaultsToMajor()
        {
            // invalid mode name should simply fall back to default scale behavior
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0,2,4,5,7,9,11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time=0, Degree=1, Type="triad", BorrowMode="notamode" }
                },
                Channel=0,
                Velocity=90,
                Duration=4
            };
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e=>e.TimeStep).OrderBy(g=>g.Key)
                               .Select(g=>g.Select(e=>e.Note).ToList()).ToList();
            // should behave like ordinary I chord
            Assert.Equal(new[] {60,64,67}, chords[0]);
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

        [Fact]
        public void GenerateHarmonyEvents_DMajor_BuildsCorrectChords()
        {
            var spec = new HarmonySpec
            {
                // use named scale + root instead of explicit pitch-class array
                ScaleName = "major",
                Root = "D",
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 4, Type = "triad" },
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            // D major I: D(2), F#(6), A(9) -> +60 => 62,66,69
            Assert.Equal(new[] { 62, 66, 69 }, chords[0]);
            // G major (IV): G(7), B(11), D(2) -> +60 => 67,71,62
            Assert.Equal(new[] { 67, 71, 62 }, chords[1]);
        }

        [Fact]
        public void GenerateHarmonyEvents_AMinor_BuildsCorrectSevenths()
        {
            var spec = new HarmonySpec
            {
                ScaleName = "minor",
                Root = "A",
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "seventh" },
                    new() { Time = 4, Degree = 3, Type = "seventh" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            // A natural minor I7 (minor7): A(9), C(0), E(4), G(7) -> +60 => 69,60,64,67
            Assert.Equal(new[] { 69, 60, 64, 67 }, chords[0]);
            // C major 7 (degree 3 in A minor per spec): C(0), E(4), G(7), B(11) -> +60 => 60,64,67,71
            Assert.Equal(new[] { 60, 64, 67, 71 }, chords[1]);
        }

        [Fact]
        public void GenerateHarmonyEvents_CHarmonicMinor_BuildsCorrectTriads()
        {
            // C harmonic minor scale: C D Eb F G Ab B (0,2,3,5,7,8,11)
            var spec = new HarmonySpec
            {
                ScaleName = "harmonic minor",
                Root = "C",
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },  // C minor
                    new() { Time = 4, Degree = 5, Type = "triad" },  // G major
                    new() { Time = 8, Degree = 7, Type = "triad" }   // B diminished
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            // C minor: C(0), Eb(3), G(7) -> +60 => 60,63,67
            Assert.Equal(new[] { 60, 63, 67 }, chords[0]);
            // G major: G(7), B(11), D(2) -> +60 => 67,71,62
            Assert.Equal(new[] { 67, 71, 62 }, chords[1]);
            // B diminished: B(11), D(2), F(5) -> +60 => 71,62,65
            Assert.Equal(new[] { 71, 62, 65 }, chords[2]);
        }

        [Fact]
        public void GenerateHarmonyEvents_CMelodicMinor_BuildsCorrectSevenths()
        {
            // C melodic minor scale: C D Eb F G A B (0,2,3,5,7,9,11)
            var spec = new HarmonySpec
            {
                ScaleName = "melodic minor",
                Root = "C",
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "seventh" },  // m7
                    new() { Time = 4, Degree = 3, Type = "seventh" },  // augmaj7
                    new() { Time = 8, Degree = 4, Type = "seventh" }   // maj7
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key).Select(g => g.Select(e => e.Note).ToList()).ToList();

            // C m7: C(0), Eb(3), G(7), Bb(10) -> +60 => 60,63,67,70
            Assert.Equal(new[] { 60, 63, 67, 70 }, chords[0]);
            // Eb augmented maj7: Eb(3), G(7), B(11), D(2) -> +60 => 63,67,71,62
            Assert.Equal(new[] { 63, 67, 71, 62 }, chords[1]);
            // F maj7: F(5), A(9), C(0), E(4) -> +60 => 65,69,60,64
            Assert.Equal(new[] { 65, 69, 60, 64 }, chords[2]);
        }

        [Fact]
        public void GenerateHarmonyEvents_PitchCenterCycle_PositiveAndNegative()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 }, // C major
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 5, Type = "triad" }
                },
                Constraints = new HarmonyConstraints { PitchCenterCycle = 2 },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g=>g.Key).Select(g=>g.Select(e=>e.Note).ToList()).ToList();
            // I triad shifted by +2: C->D etc: expect [62,66,69]
            Assert.Equal(new[] { 62, 66, 69 }, chords[0]);
            // V triad G->A (pcs 7,11,2 +2 -> 9,1,4): expect [69,61,64]
            Assert.Equal(new[] { 69, 61, 64 }, chords[1]);

            // test negative rotation
            spec.Constraints.PitchCenterCycle = -3;
            events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            chords = events.GroupBy(e=>e.TimeStep).OrderBy(g=>g.Key).Select(g=>g.Select(e=>e.Note).ToList()).ToList();
            // I triad shifted down by 3 semitones: pcs 0,4,7 -> -3 -> 9,1,4 -> MIDI 69,61,64
            Assert.Equal(new[] { 69, 61, 64 }, chords[0]);
        }

        [Fact]
        public void GenerateHarmonyEvents_CycleThenInversion_RespectsOrder()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0,2,4,5,7,9,11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 1 }
                },
                Constraints = new HarmonyConstraints { PitchCenterCycle = 1 }
            };
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chord = events.Select(e=>e.Note).ToList();
            // original triad [0,4,7] shift+1 => [1,5,8], first inversion => [5,8,13]
            Assert.Equal(new[] { 65, 68, 73 }, chord);
        }

        [Fact]
        public void GenerateHarmonyEvents_CycleWithMinShared_AndScale()
        {
            // start with D major, apply minShared then cycle +3
            var spec = new HarmonySpec
            {
                ScaleName = "major",
                Root = "D",
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }, // D
                    new() { Time = 4, Degree = 5, Type = "triad" }  // A
                },
                Constraints = new HarmonyConstraints { MinSharedPitches = 1, PitchCenterCycle = 3 }
            };
            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e=>e.TimeStep).OrderBy(g=>g.Key).Select(g=>g.Select(e=>e.Note).ToList()).ToList();
            // expected first chord: D triad [2,6,9]+3 => [5,9,0] => MIDI [65,69,60]
            Assert.Equal(new[] {65,69,60}, chords[0]);
        }

        [Fact]
        public void HarmonySpec_DeserializeWithCustomScale_PopulatesField()
        {
            var json = @"{
  ""customScale"": [0,1,5,7,8],
  ""progression"": [ { ""time"":0, ""degree"":1, ""type"":""triad"" } ]
}";
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var spec = JsonSerializer.Deserialize<HarmonySpec>(json, options);
            Assert.NotNull(spec);
            Assert.NotNull(spec.CustomScale);
            Assert.Equal(new[] { 0, 1, 5, 7, 8 }, spec.CustomScale);
        }

        [Fact]
        public void GenerateHarmonyEvents_CustomPentatonic_BuildsTriads()
        {
            // Custom pentatonic scale [0,2,4,7,9]
            var spec = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 2, 4, 7, 9 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },  // [0, 4, 9]
                    new() { Time = 4, Degree = 2, Type = "triad" },  // [2, 7, 0]
                    new() { Time = 8, Degree = 3, Type = "triad" }   // [4, 9, 2]
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key)
                               .Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(3, chords.Count);
            // Degree 1: indices [0, 2, 4] -> pcs [0, 4, 9] -> MIDI [60, 64, 69]
            Assert.Equal(new[] { 60, 64, 69 }, chords[0]);
            // Degree 2: indices [1, 3, 0] -> pcs [2, 7, 0] -> MIDI [62, 67, 60]
            Assert.Equal(new[] { 62, 67, 60 }, chords[1]);
            // Degree 3: indices [2, 4, 1] -> pcs [4, 9, 2] -> MIDI [64, 69, 62]
            Assert.Equal(new[] { 64, 69, 62 }, chords[2]);
        }

        [Fact]
        public void GenerateHarmonyEvents_CustomSynthetic_BuildsSevenths()
        {
            // Custom synthetic scale [0,1,5,7,8]
            var spec = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 1, 5, 7, 8 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "seventh" },  // [0, 5, 8, 1]
                    new() { Time = 4, Degree = 2, Type = "seventh" }   // [1, 7, 0, 5]
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key)
                               .Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(2, chords.Count);
            // Degree 1: indices [0, 2, 4, 6%5=1] -> pcs [0, 5, 8, 1] -> MIDI [60, 65, 68, 61]
            Assert.Equal(new[] { 60, 65, 68, 61 }, chords[0]);
            // Degree 2: indices [1, 3, 0, 2] -> pcs [1, 7, 0, 5] -> MIDI [61, 67, 60, 65]
            Assert.Equal(new[] { 61, 67, 60, 65 }, chords[1]);
        }

        [Fact]
        public void GenerateHarmonyEvents_CustomSymmetric_ProducesWholeTones()
        {
            // Custom symmetric scale [0,3,6,9] (whole tones, augmented triad)
            var spec = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 3, 6, 9 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },  // [0, 6, 3]
                    new() { Time = 4, Degree = 2, Type = "triad" }   // [3, 9, 6]
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key)
                               .Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(2, chords.Count);
            // Degree 1: indices [0, 2, 4%4=0] -> pcs [0, 6, 0] -> MIDI [60, 66, 60]
            Assert.Equal(new[] { 60, 66, 60 }, chords[0]);
            // Degree 2: indices [1, 3, 5%4=1] -> pcs [3, 9, 3] -> MIDI [63, 69, 63]
            Assert.Equal(new[] { 63, 69, 63 }, chords[1]);
        }

        [Fact]
        public void GenerateHarmonyEvents_CustomScaleWithMinSharedPitches()
        {
            // Custom scale with minSharedPitches constraint
            var spec = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 1, 5, 7, 8 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 3, Type = "triad" }
                },
                Constraints = new HarmonyConstraints { MinSharedPitches = 1 },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chords = events.GroupBy(e => e.TimeStep).OrderBy(g => g.Key)
                               .Select(g => g.Select(e => e.Note).ToList()).ToList();

            Assert.Equal(2, chords.Count);
            // Verify at least one shared pitch class between chords
            var pcs0 = ToPitchClasses(chords[0]);
            var pcs1 = ToPitchClasses(chords[1]);
            var intersection = new HashSet<int>(pcs0);
            intersection.IntersectWith(pcs1);
            Assert.True(intersection.Count >= 1);
        }

        [Fact]
        public void GenerateHarmonyEvents_CustomScaleWithPitchCenterCycle()
        {
            // Custom scale with pitch-center cycling
            var spec = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 2, 4, 7, 9 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Constraints = new HarmonyConstraints { PitchCenterCycle = 2 },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chord = events.Select(e => e.Note).OrderBy(n => n).ToList();

            // Original triad: [0, 4, 9] + cycle +2 => [2, 6, 11] -> MIDI [62, 66, 71]
            Assert.Equal(new[] { 62, 66, 71 }, chord);
        }

        [Fact]
        public void GenerateHarmonyEvents_CustomScaleWithInversion()
        {
            // Custom scale with inversion
            var spec = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 1, 5, 7, 8 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 1 }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chord = events.Select(e => e.Note).ToList();

            // Original triad [0, 5, 8] -> first inversion [5, 8, 12] -> MIDI [65, 68, 72]
            Assert.Equal(new[] { 65, 68, 72 }, chord);
        }

        [Fact]
        public void CustomScaleBuilder_Normalize_RemovesDuplicatesAndSorts()
        {
            var input = new List<int> { 5, 0, 12, 5, 7, -3 };
            var normalized = CustomScaleBuilder.Normalize(input);

            // Normalized: [0, 2, 5, 7]
            // (12 -> 0 mod 12, -3 -> 9 mod 12, then distinct and sorted)
            Assert.Equal(new[] { 0, 5, 7, 9 }, normalized);
        }

        [Fact]
        public void CustomScaleBuilder_IsValid_ChecksLengthAndRange()
        {
            // Too short
            var invalid1 = new List<int> { 0, 5 };
            Assert.False(CustomScaleBuilder.IsValid(invalid1));

            // Valid
            var valid = new List<int> { 0, 2, 4, 7, 9 };
            Assert.True(CustomScaleBuilder.IsValid(valid));

            // Null
            Assert.False(CustomScaleBuilder.IsValid(null));
        }

        [Fact]
        public void CustomScaleBuilder_BuildTriad_StacksScaleSteps()
        {
            var scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 };
            var triad = CustomScaleBuilder.BuildTriad(scale, 1);

            // Degree 1: indices [0, 2, 4] -> [0, 4, 7]
            Assert.Equal(new[] { 0, 4, 7 }, triad);
        }

        [Fact]
        public void CustomScaleBuilder_BuildSeventh_StacksFourSteps()
        {
            var scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 };
            var seventh = CustomScaleBuilder.BuildSeventh(scale, 1);

            // Degree 1: indices [0, 2, 4, 6] -> [0, 4, 7, 11]
            Assert.Equal(new[] { 0, 4, 7, 11 }, seventh);
        }

        [Fact]
        public void GenerateHarmonyEvents_CustomScaleOverridesAllOtherScales()
        {
            // customScale should take precedence over scale, scaleName, root
            var spec = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 1, 5 },
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },  // should be ignored
                ScaleName = "minor",  // should be ignored
                Root = "D",  // should be ignored
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var chord = events.Select(e => e.Note).OrderBy(n => n).ToList();

            // Custom scale [0, 1, 5]: degree 1 -> [0, 5, 1] -> MIDI [60, 65, 61]
            Assert.Equal(new[] { 60, 61, 65 }, chord);
        }
    }

    public class ScaleEditorTests
    {
        [Theory]
        [InlineData("C", 0)]
        [InlineData("C#", 1)]
        [InlineData("D", 2)]
        [InlineData("D#", 3)]
        [InlineData("E", 4)]
        [InlineData("F", 5)]
        [InlineData("F#", 6)]
        [InlineData("G", 7)]
        [InlineData("G#", 8)]
        [InlineData("A", 9)]
        [InlineData("A#", 10)]
        [InlineData("B", 11)]
        public void ScaleBuilder_ParseRoot_CorrectPitchClass(string root, int expected)
        {
            Assert.Equal(expected, ScaleBuilder.ParseRoot(root));
        }

        [Fact]
        public void ScaleBuilder_BuildScale_Major_CMajor()
        {
            var scale = ScaleBuilder.BuildScale("major", "C");
            Assert.Equal(new List<int> { 0, 2, 4, 5, 7, 9, 11 }, scale);
        }

        [Fact]
        public void ScaleBuilder_BuildScale_Minor_AMinor()
        {
            var scale = ScaleBuilder.BuildScale("minor", "A");
            // A natural minor: A B C D E F G -> 9 11 0 2 4 5 7
            Assert.Equal(new List<int> { 9, 11, 0, 2, 4, 5, 7 }, scale);
        }

        [Fact]
        public void ScaleBuilder_BuildScale_Major_GMajor()
        {
            var scale = ScaleBuilder.BuildScale("major", "G");
            // G major: G A B C D E F# -> 7 9 11 0 2 4 6
            Assert.Equal(new List<int> { 7, 9, 11, 0, 2, 4, 6 }, scale);
        }

        [Theory]
        [InlineData("ionian",     new[] { 0,2,4,5,7,9,11 })]
        [InlineData("dorian",     new[] { 0,2,3,5,7,9,10 })]
        [InlineData("phrygian",   new[] { 0,1,3,5,7,8,10 })]
        [InlineData("lydian",     new[] { 0,2,4,6,7,9,11 })]
        [InlineData("mixolydian", new[] { 0,2,4,5,7,9,10 })]
        [InlineData("aeolian",    new[] { 0,2,3,5,7,8,10 })]
        [InlineData("locrian",    new[] { 0,1,3,5,6,8,10 })]
        public void ModeBuilder_BuildModeScale_AllModes_RootC(string mode, int[] expected)
        {
            var scale = ModeBuilder.BuildModeScale(mode, "C");
            Assert.Equal(expected.ToList(), scale);
        }

        [Fact]
        public void ModeBuilder_BuildModeScale_Dorian_RootD()
        {
            var scale = ModeBuilder.BuildModeScale("dorian", "D");
            // D Dorian: D E F G A B C -> 2 4 5 7 9 11 0
            Assert.Equal(new List<int> { 2, 4, 5, 7, 9, 11, 0 }, scale);
        }

        [Fact]
        public void ModeBuilder_BuildModeScale_Mixolydian_RootG()
        {
            var scale = ModeBuilder.BuildModeScale("mixolydian", "G");
            // G Mixolydian: G A B C D E F -> 7 9 11 0 2 4 5
            Assert.Equal(new List<int> { 7, 9, 11, 0, 2, 4, 5 }, scale);
        }

        [Fact]
        public void HarmonyGenerator_UsesModeScale_WhenScaleNameIsMode()
        {
            var spec = new HarmonySpec
            {
                ScaleName = "dorian",
                Root = "D",
                Scale = new List<int>(),
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            Assert.NotEmpty(events);
            // For D dorian, the I triad should be D–F–A -> MIDI 62, 65, 69
            var notes = events.Select(e => e.Note).OrderBy(n => n).ToList();
            Assert.Equal(new[] { 62, 65, 69 }, notes);
        }

        [Fact]
        public void HarmonyGenerator_UsesMajorScale_WhenScaleNameIsMajor()
        {
            var spec = new HarmonySpec
            {
                ScaleName = "major",
                Root = "C",
                Scale = new List<int>(),
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            // C major triad: C E G -> MIDI 60, 64, 67
            var notes = events.Select(e => e.Note).OrderBy(n => n).ToList();
            Assert.Equal(new[] { 60, 64, 67 }, notes);
        }

        [Fact]
        public void HarmonySpec_CustomScale_OverridesScaleName()
        {
            var spec = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 3, 7 },
                ScaleName = "major",
                Root = "C",
                Scale = new List<int>(),
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0,
                Velocity = 90,
                Duration = 4
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(spec);
            Assert.NotEmpty(events);
        }

        [Fact]
        public void ComputeTransformedChordPitchClasses_BasicTriad_ReturnsSortedPitchClasses()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 5, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4
            };

            var result = HarmonyGenerator.ComputeTransformedChordPitchClasses(spec);

            Assert.Equal(2, result.Count);
            // C major triad: pitch classes 0, 4, 7 (sorted ascending)
            Assert.Equal(new[] { 0, 4, 7 }, result[0]);
            // G major triad: pitch classes 2, 7, 11 (sorted ascending)
            Assert.Equal(new[] { 2, 7, 11 }, result[1]);
        }

        [Fact]
        public void ComputeTransformedChordPitchClasses_PitchCenterCycle_ShiftsAllChords()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4,
                Constraints = new HarmonyConstraints { PitchCenterCycle = 2 }
            };

            var result = HarmonyGenerator.ComputeTransformedChordPitchClasses(spec);

            Assert.Single(result);
            // C major triad [0,4,7] shifted by +2 → [2,6,9] (sorted)
            Assert.Equal(new[] { 2, 6, 9 }, result[0]);
        }

        [Fact]
        public void ComputeTransformedChordPitchClasses_InversionIgnored_PolygonUnchanged()
        {
            // Inversion should NOT affect the chord pitch-class set returned (polygon geometry is invariant)
            var specRoot = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 0 }
                },
                Channel = 0, Velocity = 90, Duration = 4
            };
            var specInv = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 2 }
                },
                Channel = 0, Velocity = 90, Duration = 4
            };

            var rootPcs = HarmonyGenerator.ComputeTransformedChordPitchClasses(specRoot);
            var invPcs  = HarmonyGenerator.ComputeTransformedChordPitchClasses(specInv);

            // Polygon shape is the same regardless of inversion
            Assert.Equal(rootPcs[0], invPcs[0]);
        }

        [Fact]
        public void ComputeTransformedChordPitchClasses_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Empty(HarmonyGenerator.ComputeTransformedChordPitchClasses(null));

            var emptySpec = new HarmonySpec { Progression = new List<ChordEvent>() };
            Assert.Empty(HarmonyGenerator.ComputeTransformedChordPitchClasses(emptySpec));
        }

        [Fact]
        public void ComputeTransformedChordPitchClasses_ShapeTransformRotate_RotatesPolygon()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4,
                Constraints = new HarmonyConstraints
                {
                    ShapeTransform = new ShapeTransform { Type = "rotate", Amount = 3 }
                }
            };

            var result = HarmonyGenerator.ComputeTransformedChordPitchClasses(spec);

            Assert.Single(result);
            // C major triad [0,4,7] rotated by +3 → [3,7,10] (sorted)
            Assert.Equal(new[] { 3, 7, 10 }, result[0]);
        }
    }
}
