using System.Collections.Generic;
using Xunit;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Tests
{
    public class VoiceLeadingTests
    {
        // ── ChromaticDistance ────────────────────────────────────────────────

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 6, 6)]
        [InlineData(0, 7, 5)]  // wraps: min(7, 5)
        [InlineData(11, 0, 1)] // B → C wraps
        [InlineData(1, 11, 2)] // C# → B wraps
        [InlineData(4, 8, 4)]
        public void ChromaticDistance_ReturnsMinCircularDistance(int a, int b, int expected)
        {
            Assert.Equal(expected, VoiceLeadingHelper.ChromaticDistance(a, b));
        }

        // ── ComputeVoiceLeadingPairs ─────────────────────────────────────────

        [Fact]
        public void ComputeVoiceLeadingPairs_EmptyInputs_ReturnsEmpty()
        {
            var pairs = VoiceLeadingHelper.ComputeVoiceLeadingPairs(new List<int>(), new List<int> { 0, 4, 7 });
            Assert.Empty(pairs);

            pairs = VoiceLeadingHelper.ComputeVoiceLeadingPairs(new List<int> { 0, 4, 7 }, new List<int>());
            Assert.Empty(pairs);

            pairs = VoiceLeadingHelper.ComputeVoiceLeadingPairs(null!, null!);
            Assert.Empty(pairs);
        }

        [Fact]
        public void ComputeVoiceLeadingPairs_IdenticalChords_MapsToSelf()
        {
            // C major triad → C major triad: each note maps to itself
            var chord = new List<int> { 0, 4, 7 };
            var pairs = VoiceLeadingHelper.ComputeVoiceLeadingPairs(chord, chord);

            Assert.Equal(3, pairs.Count);
            foreach (var (from, to) in pairs)
                Assert.Equal(from, to);
        }

        [Fact]
        public void ComputeVoiceLeadingPairs_SmoothMotion_ChoosesNearestNeighbor()
        {
            // C major (0,4,7) → F major (0,5,9): G→A is 2 semitones, G→F is 2 semitones (both 2)
            // Expected: 0→0, 4→5, 7→9 (each moves by minimum distance)
            var chordA = new List<int> { 0, 4, 7 };
            var chordB = new List<int> { 0, 5, 9 };
            var pairs = VoiceLeadingHelper.ComputeVoiceLeadingPairs(chordA, chordB);

            Assert.Equal(3, pairs.Count);
            // 0 is nearest to 0 (dist=0)
            Assert.Contains((0, 0), pairs);
            // 4 nearest to 5 (dist=1) vs 9 (dist=5)
            Assert.Contains((4, 5), pairs);
            // 7 nearest remaining (9, dist=2)
            Assert.Contains((7, 9), pairs);
        }

        [Fact]
        public void ComputeVoiceLeadingPairs_OneToOneMapping_NoDuplicateToBValues()
        {
            // Ensure each target pitch class is used at most once
            var chordA = new List<int> { 0, 2, 4 };
            var chordB = new List<int> { 1, 3, 5 };
            var pairs = VoiceLeadingHelper.ComputeVoiceLeadingPairs(chordA, chordB);

            var toValues = new HashSet<int>();
            foreach (var (_, to) in pairs)
                Assert.True(toValues.Add(to), $"Duplicate target pitch class {to}");
        }

        [Fact]
        public void ComputeVoiceLeadingPairs_SmallerChordA_ProducesPairsForChordAOnly()
        {
            // Triad → seventh: only 3 pairs produced
            var chordA = new List<int> { 0, 4, 7 };
            var chordB = new List<int> { 0, 4, 7, 11 };
            var pairs = VoiceLeadingHelper.ComputeVoiceLeadingPairs(chordA, chordB);

            Assert.Equal(3, pairs.Count);
        }

        [Fact]
        public void ComputeVoiceLeadingPairs_LargerChordA_ProducesPairsUntilChordBExhausted()
        {
            // Seventh → triad: 3 pairs produced (B exhausted after 3)
            var chordA = new List<int> { 0, 4, 7, 11 };
            var chordB = new List<int> { 0, 5, 9 };
            var pairs = VoiceLeadingHelper.ComputeVoiceLeadingPairs(chordA, chordB);

            Assert.Equal(3, pairs.Count);
        }

        [Fact]
        public void ComputeVoiceLeadingPairs_WrapAroundDistance_ChoosesShortestPath()
        {
            // B (11) to C (0): distance is 1 (wraps), not 11
            var chordA = new List<int> { 11 };
            var chordB = new List<int> { 0, 6 };
            var pairs = VoiceLeadingHelper.ComputeVoiceLeadingPairs(chordA, chordB);

            Assert.Single(pairs);
            // 11→0 is distance 1; 11→6 is distance 5
            Assert.Equal((11, 0), pairs[0]);
        }
    }
}
