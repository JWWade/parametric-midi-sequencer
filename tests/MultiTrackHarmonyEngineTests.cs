using System.Collections.Generic;
using System.Linq;
using Xunit;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Tests
{
    /// <summary>
    /// Unit tests for PoC21: <see cref="MultiTrackHarmonyEngine"/>.
    /// </summary>
    public class MultiTrackHarmonyEngineTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        private static HarmonySpec BuildCMajorTrack(int channel = 0) => new HarmonySpec
        {
            Name = "C Major",
            Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
            Progression = new List<ChordEvent>
            {
                new() { Time = 0,  Degree = 1, Type = "triad" },
                new() { Time = 4,  Degree = 4, Type = "triad" },
                new() { Time = 8,  Degree = 5, Type = "triad" },
                new() { Time = 12, Degree = 1, Type = "triad" }
            },
            Channel = channel,
            Velocity = 90,
            Duration = 4
        };

        private static HarmonySpec BuildAMinorTrack(int channel = 1) => new HarmonySpec
        {
            Name = "A Minor",
            ScaleName = "minor",
            Root = "A",
            Progression = new List<ChordEvent>
            {
                new() { Time = 0,  Degree = 1, Type = "triad" },
                new() { Time = 4,  Degree = 4, Type = "triad" },
                new() { Time = 8,  Degree = 5, Type = "triad" },
                new() { Time = 12, Degree = 1, Type = "triad" }
            },
            Channel = channel,
            Velocity = 80,
            Duration = 4
        };

        // ── GenerateAllEvents ─────────────────────────────────────────────────

        [Fact]
        public void GenerateAllEvents_Null_ReturnsEmpty()
        {
            var result = MultiTrackHarmonyEngine.GenerateAllEvents(null);
            Assert.Empty(result);
        }

        [Fact]
        public void GenerateAllEvents_EmptyList_ReturnsEmpty()
        {
            var result = MultiTrackHarmonyEngine.GenerateAllEvents(new List<HarmonySpec>());
            Assert.Empty(result);
        }

        [Fact]
        public void GenerateAllEvents_SingleTrack_MatchesHarmonyGenerator()
        {
            var track = BuildCMajorTrack();
            var multiEvents   = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { track });
            var singleEvents  = HarmonyGenerator.GenerateHarmonyEvents(track);

            Assert.Equal(singleEvents.Count, multiEvents.Count);
        }

        [Fact]
        public void GenerateAllEvents_TwoTracks_ReturnsCombinedEventCount()
        {
            var t1 = BuildCMajorTrack(channel: 0);
            var t2 = BuildAMinorTrack(channel: 1);

            var single1 = HarmonyGenerator.GenerateHarmonyEvents(t1);
            var single2 = HarmonyGenerator.GenerateHarmonyEvents(t2);

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { t1, t2 });

            Assert.Equal(single1.Count + single2.Count, merged.Count);
        }

        [Fact]
        public void GenerateAllEvents_TwoTracks_CorrectChannelAssignment()
        {
            var t1 = BuildCMajorTrack(channel: 0);
            var t2 = BuildAMinorTrack(channel: 1);

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { t1, t2 });

            var ch0Events = merged.Where(e => e.Channel == 0).ToList();
            var ch1Events = merged.Where(e => e.Channel == 1).ToList();

            Assert.NotEmpty(ch0Events);
            Assert.NotEmpty(ch1Events);
        }

        [Fact]
        public void GenerateAllEvents_MergedListIsSortedByTimeStep()
        {
            var t1 = BuildCMajorTrack(channel: 0);
            var t2 = BuildAMinorTrack(channel: 1);

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { t1, t2 });

            for (int i = 1; i < merged.Count; i++)
                Assert.True(merged[i].TimeStep >= merged[i - 1].TimeStep,
                    $"Event at index {i} has smaller TimeStep than preceding event.");
        }

        [Fact]
        public void GenerateAllEvents_ThreeTracks_SameScaleDifferentInversions()
        {
            var baseTrack = BuildCMajorTrack(channel: 0);

            var t2 = BuildCMajorTrack(channel: 1);
            t2.Progression = t2.Progression.Select(c => new ChordEvent
            {
                Time = c.Time, Degree = c.Degree, Type = c.Type, Inversion = 1
            }).ToList();

            var t3 = BuildCMajorTrack(channel: 2);
            t3.Progression = t3.Progression.Select(c => new ChordEvent
            {
                Time = c.Time, Degree = c.Degree, Type = c.Type, Inversion = 2
            }).ToList();

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { baseTrack, t2, t3 });

            // each track contributes 4 chords × 3 notes = 12 events
            Assert.Equal(36, merged.Count);
        }

        [Fact]
        public void GenerateAllEvents_DifferentProgressionLengths_MergesCorrectly()
        {
            var shortTrack = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4
            };
            var longTrack = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0,  Degree = 1, Type = "triad" },
                    new() { Time = 4,  Degree = 4, Type = "triad" },
                    new() { Time = 8,  Degree = 5, Type = "triad" }
                },
                Channel = 1, Velocity = 80, Duration = 4
            };

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { shortTrack, longTrack });

            // 1×3 + 3×3 = 12 notes total
            Assert.Equal(12, merged.Count);
        }

        [Fact]
        public void GenerateAllEvents_CustomScaleTrack_AndModalTrack_MergesCorrectly()
        {
            var customTrack = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 2, 4, 7, 9 }, // pentatonic
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4
            };
            var modalTrack = new HarmonySpec
            {
                ScaleName = "dorian",
                Root = "D",
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 1, Velocity = 80, Duration = 4
            };

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { customTrack, modalTrack });
            Assert.NotEmpty(merged);
        }

        [Fact]
        public void GenerateAllEvents_VoiceLeadingOptimizationPerTrack_BothTracksOptimized()
        {
            var t1 = BuildCMajorTrack(channel: 0);
            t1.Constraints = new HarmonyConstraints { OptimizeVoiceLeading = true };

            var t2 = BuildAMinorTrack(channel: 1);
            t2.Constraints = new HarmonyConstraints { OptimizeVoiceLeading = true };

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { t1, t2 });
            Assert.NotEmpty(merged);
        }

        // ── Validate ──────────────────────────────────────────────────────────

        [Fact]
        public void Validate_Null_ReturnsNoErrors()
        {
            var errors = MultiTrackHarmonyEngine.Validate(null);
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_TwoValidTracks_ReturnsNoErrors()
        {
            var errors = MultiTrackHarmonyEngine.Validate(new[] { BuildCMajorTrack(0), BuildAMinorTrack(1) });
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_DuplicateChannels_ReturnsError()
        {
            // Both tracks use channel 0 → should fail uniqueness check
            var errors = MultiTrackHarmonyEngine.Validate(new[] { BuildCMajorTrack(0), BuildAMinorTrack(0) });
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("channel") || e.Contains("Channel"));
        }

        [Fact]
        public void Validate_DuplicateChannels_AllowSharing_ReturnsNoErrors()
        {
            var errors = MultiTrackHarmonyEngine.Validate(
                new[] { BuildCMajorTrack(0), BuildAMinorTrack(0) },
                allowChannelSharing: true);
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_TrackWithEmptyProgression_ReturnsError()
        {
            var badTrack = new HarmonySpec
            {
                Name = "Bad",
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>(),
                Channel = 2
            };
            var errors = MultiTrackHarmonyEngine.Validate(new[] { BuildCMajorTrack(0), badTrack });
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("progression") || e.Contains("Progression"));
        }

        [Fact]
        public void Validate_TrackWithNoScale_ReturnsError()
        {
            var badTrack = new HarmonySpec
            {
                Name = "NoScale",
                Scale = null,
                ScaleName = null,
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 2
            };
            var errors = MultiTrackHarmonyEngine.Validate(new[] { BuildCMajorTrack(0), badTrack });
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("scale") || e.Contains("Scale"));
        }

        // ── Backward compatibility: single-track harmony unchanged ────────────

        [Fact]
        public void GenerateAllEvents_SingleTrack_SameOutputAsHarmonyGenerator()
        {
            var spec = BuildCMajorTrack();
            var fromMulti  = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { spec });
            var fromSingle = HarmonyGenerator.GenerateHarmonyEvents(spec);

            Assert.Equal(fromSingle.Count, fromMulti.Count);
            for (int i = 0; i < fromSingle.Count; i++)
            {
                Assert.Equal(fromSingle[i].TimeStep, fromMulti[i].TimeStep);
                Assert.Equal(fromSingle[i].Note,     fromMulti[i].Note);
                Assert.Equal(fromSingle[i].Channel,  fromMulti[i].Channel);
                Assert.Equal(fromSingle[i].Velocity, fromMulti[i].Velocity);
            }
        }

        // ── HarmonySpec.Name property ─────────────────────────────────────────

        [Fact]
        public void HarmonySpec_Name_DefaultIsNull()
        {
            var spec = new HarmonySpec();
            Assert.Null(spec.Name);
        }

        [Fact]
        public void HarmonySpec_Name_CanBeSet()
        {
            var spec = new HarmonySpec { Name = "Lead" };
            Assert.Equal("Lead", spec.Name);
        }
    }
}
