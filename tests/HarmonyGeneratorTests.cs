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
    }
}
