using System.Collections.Generic;
using System.Linq;
using Xunit;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Tests
{
    /// <summary>
    /// Unit tests for PoC22: <see cref="GeometricTransformEngine"/> and per-track
    /// geometric transforms via <see cref="HarmonySpec.Transforms"/>.
    /// </summary>
    public class GeometricTransformEngineTests
    {
        private readonly GeometricTransformEngine _engine = new GeometricTransformEngine();

        // ── ApplyPitchCenterCycle ─────────────────────────────────────────────

        [Fact]
        public void ApplyPitchCenterCycle_ZeroAmount_ReturnsUnchanged()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var result = _engine.ApplyPitchCenterCycle(pcs, 0);
            Assert.Equal(pcs, result);
        }

        [Fact]
        public void ApplyPitchCenterCycle_Null_ReturnsNull()
        {
            var result = _engine.ApplyPitchCenterCycle(null, 3);
            Assert.Null(result);
        }

        [Fact]
        public void ApplyPitchCenterCycle_ShiftBy2_RotatesCorrectly()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var result = _engine.ApplyPitchCenterCycle(pcs, 2);
            Assert.Equal(new List<int> { 2, 6, 9 }, result);
        }

        [Fact]
        public void ApplyPitchCenterCycle_ShiftBy12_IsIdentity()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var result = _engine.ApplyPitchCenterCycle(pcs, 12);
            Assert.Equal(new List<int> { 0, 4, 7 }, result);
        }

        [Fact]
        public void ApplyPitchCenterCycle_NegativeShift_WrapsCorrectly()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var result = _engine.ApplyPitchCenterCycle(pcs, -2);
            Assert.Equal(new List<int> { 10, 2, 5 }, result);
        }

        // ── ApplyShapeTransform ───────────────────────────────────────────────

        [Fact]
        public void ApplyShapeTransform_NullTransform_ReturnsOriginal()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var result = _engine.ApplyShapeTransform(pcs, null);
            Assert.Equal(pcs, result);
        }

        [Fact]
        public void ApplyShapeTransform_Rotate_ShiftsCorrectly()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "rotate", Amount = 3 };
            var result = _engine.ApplyShapeTransform(pcs, transform);
            Assert.Equal(new List<int> { 3, 7, 10 }, result);
        }

        [Fact]
        public void ApplyShapeTransform_Reflect_InvertsCorrectly()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "reflect", Axis = 0 };
            var result = _engine.ApplyShapeTransform(pcs, transform);
            // 0→0, 4→8, 7→5
            Assert.Equal(new List<int> { 0, 8, 5 }, result);
        }

        [Fact]
        public void ApplyShapeTransform_Expand_ReturnsCorrectCount()
        {
            var pcs = new List<int> { 0, 2, 4 };
            var transform = new ShapeTransform { Type = "expand", Amount = 1.5 };
            var result = _engine.ApplyShapeTransform(pcs, transform);
            Assert.NotNull(result);
            Assert.Equal(pcs.Count, result.Count);
        }

        // ── ApplyInversion ────────────────────────────────────────────────────

        [Fact]
        public void ApplyInversion_ZeroInversion_ReturnsUnchanged()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var result = _engine.ApplyInversion(pcs, 0);
            Assert.Equal(pcs, result);
        }

        [Fact]
        public void ApplyInversion_Null_ReturnsNull()
        {
            var result = _engine.ApplyInversion(null, 1);
            Assert.Null(result);
        }

        [Fact]
        public void ApplyInversion_FirstInversion_MovesRootToTop()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var result = _engine.ApplyInversion(pcs, 1);
            // Root (0) moves to top with +12
            Assert.Equal(new List<int> { 4, 7, 12 }, result);
        }

        [Fact]
        public void ApplyInversion_SecondInversion_MovesFirstTwoToTop()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var result = _engine.ApplyInversion(pcs, 2);
            Assert.Equal(new List<int> { 7, 12, 16 }, result);
        }

        [Fact]
        public void ApplyInversion_InversionBeyondVoiceCount_ReturnsUnchanged()
        {
            var pcs = new List<int> { 0, 4, 7 };
            var result = _engine.ApplyInversion(pcs, 5); // 5 >= 3 voices
            Assert.Equal(pcs, result);
        }

        // ── Per-track Transforms via HarmonySpec.Transforms ──────────────────

        [Fact]
        public void HarmonySpec_Transforms_DefaultIsNull()
        {
            var spec = new HarmonySpec();
            Assert.Null(spec.Transforms);
        }

        [Fact]
        public void HarmonySpec_Transforms_CanBeSet()
        {
            var spec = new HarmonySpec
            {
                Transforms = new HarmonyConstraints { PitchCenterCycle = 3 }
            };
            Assert.Equal(3, spec.Transforms.PitchCenterCycle);
        }

        [Fact]
        public void GenerateHarmonyEvents_TransformsPitchCenter_ShiftsAllNotes()
        {
            var baseSpec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4
            };

            var shiftedSpec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4,
                Transforms = new HarmonyConstraints { PitchCenterCycle = 2 }
            };

            var baseEvents    = HarmonyGenerator.GenerateHarmonyEvents(baseSpec);
            var shiftedEvents = HarmonyGenerator.GenerateHarmonyEvents(shiftedSpec);

            Assert.Equal(baseEvents.Count, shiftedEvents.Count);

            // Each base pitch class, shifted by 2, should appear in the result set.
            var basePCs    = baseEvents.Select(e => ((e.Note - 60) % 12 + 12) % 12).ToHashSet();
            var expectedPCs = basePCs.Select(pc => (pc + 2) % 12).ToHashSet();
            var resultPCs   = shiftedEvents.Select(e => ((e.Note - 60) % 12 + 12) % 12).ToHashSet();
            Assert.Equal(expectedPCs, resultPCs);
        }

        [Fact]
        public void GenerateHarmonyEvents_TransformsShapeTransform_ProducesTransformedNotes()
        {
            var baseSpec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4
            };

            var reflectSpec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4,
                Transforms = new HarmonyConstraints
                {
                    ShapeTransform = new ShapeTransform { Type = "reflect", Axis = 3 }
                }
            };

            var baseEvents    = HarmonyGenerator.GenerateHarmonyEvents(baseSpec);
            var reflectEvents = HarmonyGenerator.GenerateHarmonyEvents(reflectSpec);

            Assert.Equal(baseEvents.Count, reflectEvents.Count);
            // Notes should differ after reflection
            Assert.NotEqual(
                baseEvents.Select(e => e.Note).OrderBy(x => x).ToList(),
                reflectEvents.Select(e => e.Note).OrderBy(x => x).ToList());
        }

        [Fact]
        public void GenerateHarmonyEvents_TransformsTakesPrecedenceOverConstraints()
        {
            // Constraints shifts by 1, Transforms shifts by 5 — Transforms must win.
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4,
                Constraints = new HarmonyConstraints { PitchCenterCycle = 1 },
                Transforms  = new HarmonyConstraints { PitchCenterCycle = 5 }
            };

            var baseSpec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4
            };

            var baseEvents   = HarmonyGenerator.GenerateHarmonyEvents(baseSpec);
            var resultEvents = HarmonyGenerator.GenerateHarmonyEvents(spec);
            var shift1Events = HarmonyGenerator.GenerateHarmonyEvents(new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent> { new() { Time = 0, Degree = 1, Type = "triad" } },
                Channel = 0, Velocity = 90, Duration = 4,
                Constraints = new HarmonyConstraints { PitchCenterCycle = 1 }
            });

            Assert.Equal(baseEvents.Count, resultEvents.Count);

            // Result pitch-class set must equal the +5 shift, not the +1 shift.
            var basePCs    = baseEvents.Select(e => ((e.Note - 60) % 12 + 12) % 12).ToHashSet();
            var expectedPCs = basePCs.Select(pc => (pc + 5) % 12).ToHashSet();
            var resultPCs   = resultEvents.Select(e => ((e.Note - 60) % 12 + 12) % 12).ToHashSet();
            var shift1PCs   = shift1Events.Select(e => ((e.Note - 60) % 12 + 12) % 12).ToHashSet();

            Assert.Equal(expectedPCs, resultPCs);         // Transforms (+5) wins
            Assert.NotEqual(shift1PCs, resultPCs);         // not the Constraints (+1) result
        }

        [Fact]
        public void GenerateHarmonyEvents_NoTransforms_ConstraintsFallback_StillApplied()
        {
            var spec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4,
                Constraints = new HarmonyConstraints { PitchCenterCycle = 3 }
                // Transforms is null → should fall back to Constraints
            };

            var baseSpec = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0, Velocity = 90, Duration = 4
            };

            var baseEvents   = HarmonyGenerator.GenerateHarmonyEvents(baseSpec);
            var resultEvents = HarmonyGenerator.GenerateHarmonyEvents(spec);

            Assert.Equal(baseEvents.Count, resultEvents.Count);

            // Each base pitch class shifted by 3 should appear in result set.
            var basePCs     = baseEvents.Select(e => ((e.Note - 60) % 12 + 12) % 12).ToHashSet();
            var expectedPCs = basePCs.Select(pc => (pc + 3) % 12).ToHashSet();
            var resultPCs   = resultEvents.Select(e => ((e.Note - 60) % 12 + 12) % 12).ToHashSet();
            Assert.Equal(expectedPCs, resultPCs);
        }

        // ── Multi-track per-track geometric transforms (PoC22 core) ──────────

        [Fact]
        public void MultiTrack_TwoTracks_DifferentShapeTransforms_ProduceDifferentOutput()
        {
            var progression = new List<ChordEvent>
            {
                new() { Time = 0, Degree = 1, Type = "triad" },
                new() { Time = 4, Degree = 5, Type = "triad" }
            };
            var scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 };

            var trackReflect = new HarmonySpec
            {
                Scale = scale, Progression = progression, Channel = 0,
                Transforms = new HarmonyConstraints
                {
                    ShapeTransform = new ShapeTransform { Type = "reflect", Axis = 3 }
                }
            };
            var trackExpand = new HarmonySpec
            {
                Scale = scale, Progression = progression, Channel = 1,
                Transforms = new HarmonyConstraints
                {
                    ShapeTransform = new ShapeTransform { Type = "expand", Amount = 1.4 }
                }
            };

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { trackReflect, trackExpand });

            var ch0Notes = merged.Where(e => e.Channel == 0).Select(e => e.Note).OrderBy(x => x).ToList();
            var ch1Notes = merged.Where(e => e.Channel == 1).Select(e => e.Note).OrderBy(x => x).ToList();

            Assert.NotEmpty(ch0Notes);
            Assert.NotEmpty(ch1Notes);
            // The two transforms should produce different note sets
            Assert.NotEqual(ch0Notes, ch1Notes);
        }

        [Fact]
        public void MultiTrack_TwoTracks_DifferentPitchCenterCycle_ProduceDifferentOutput()
        {
            var progression = new List<ChordEvent>
            {
                new() { Time = 0, Degree = 1, Type = "triad" }
            };
            var scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 };

            var track1 = new HarmonySpec
            {
                Scale = scale, Progression = progression, Channel = 0,
                Transforms = new HarmonyConstraints { PitchCenterCycle = 2 }
            };
            var track2 = new HarmonySpec
            {
                Scale = scale, Progression = progression, Channel = 1,
                Transforms = new HarmonyConstraints { PitchCenterCycle = -1 }
            };

            var events1 = HarmonyGenerator.GenerateHarmonyEvents(track1);
            var events2 = HarmonyGenerator.GenerateHarmonyEvents(track2);

            var notes1 = events1.Select(e => e.Note).OrderBy(x => x).ToList();
            var notes2 = events2.Select(e => e.Note).OrderBy(x => x).ToList();

            Assert.NotEqual(notes1, notes2);
        }

        [Fact]
        public void MultiTrack_TwoTracks_DifferentInversions_ProduceDifferentOutput()
        {
            var scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 };

            var track1 = new HarmonySpec
            {
                Scale = scale,
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 0 }
                },
                Channel = 0
            };
            var track2 = new HarmonySpec
            {
                Scale = scale,
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad", Inversion = 1 }
                },
                Channel = 1
            };

            var events1 = HarmonyGenerator.GenerateHarmonyEvents(track1);
            var events2 = HarmonyGenerator.GenerateHarmonyEvents(track2);

            var notes1 = events1.Select(e => e.Note).OrderBy(x => x).ToList();
            var notes2 = events2.Select(e => e.Note).OrderBy(x => x).ToList();

            Assert.NotEqual(notes1, notes2);
        }

        [Fact]
        public void MultiTrack_PerTrackVoiceLeading_BothTracksProduceEvents()
        {
            var scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 };
            var progression = new List<ChordEvent>
            {
                new() { Time = 0, Degree = 1, Type = "triad" },
                new() { Time = 4, Degree = 4, Type = "triad" },
                new() { Time = 8, Degree = 5, Type = "triad" }
            };

            var track1 = new HarmonySpec
            {
                Scale = scale, Progression = progression, Channel = 0,
                Transforms = new HarmonyConstraints
                {
                    PitchCenterCycle = 2,
                    ShapeTransform = new ShapeTransform { Type = "rotate", Amount = 1 },
                    OptimizeVoiceLeading = true
                }
            };
            var track2 = new HarmonySpec
            {
                Scale = scale, Progression = progression, Channel = 1,
                Transforms = new HarmonyConstraints
                {
                    PitchCenterCycle = -1,
                    ShapeTransform = new ShapeTransform { Type = "reflect", Axis = 6 },
                    OptimizeVoiceLeading = true
                }
            };

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { track1, track2 });

            Assert.NotEmpty(merged.Where(e => e.Channel == 0));
            Assert.NotEmpty(merged.Where(e => e.Channel == 1));
        }

        [Fact]
        public void MultiTrack_CustomScaleOneTrack_ModalOther_WithTransforms_MergesCorrectly()
        {
            var customTrack = new HarmonySpec
            {
                CustomScale = new List<int> { 0, 2, 4, 7, 9 }, // pentatonic
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 0,
                Transforms = new HarmonyConstraints { PitchCenterCycle = 1 }
            };
            var modalTrack = new HarmonySpec
            {
                ScaleName = "dorian", Root = "D",
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" }
                },
                Channel = 1,
                Transforms = new HarmonyConstraints
                {
                    ShapeTransform = new ShapeTransform { Type = "rotate", Amount = 2 }
                }
            };

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { customTrack, modalTrack });
            Assert.NotEmpty(merged);
            Assert.NotEmpty(merged.Where(e => e.Channel == 0));
            Assert.NotEmpty(merged.Where(e => e.Channel == 1));
        }

        [Fact]
        public void MultiTrack_BackwardCompatibility_ConstraintsOnlyTrack_StillWorks()
        {
            // Tracks using old `Constraints` property (no `Transforms`) must still work.
            var track = new HarmonySpec
            {
                Scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 },
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 4, Degree = 5, Type = "triad" }
                },
                Channel = 0,
                Constraints = new HarmonyConstraints
                {
                    MinSharedPitches = 1,
                    PitchCenterCycle = 2,
                    ShapeTransform = new ShapeTransform { Type = "rotate", Amount = 1 }
                }
            };

            var events = HarmonyGenerator.GenerateHarmonyEvents(track);
            Assert.NotEmpty(events);
        }

        [Fact]
        public void MultiTrack_MergedOutput_SortedByTimeStepThenChannel()
        {
            var scale = new List<int> { 0, 2, 4, 5, 7, 9, 11 };
            var track1 = new HarmonySpec
            {
                Scale = scale,
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 1, Type = "triad" },
                    new() { Time = 8, Degree = 4, Type = "triad" }
                },
                Channel = 0,
                Transforms = new HarmonyConstraints { PitchCenterCycle = 3 }
            };
            var track2 = new HarmonySpec
            {
                Scale = scale,
                Progression = new List<ChordEvent>
                {
                    new() { Time = 0, Degree = 5, Type = "triad" },
                    new() { Time = 8, Degree = 1, Type = "triad" }
                },
                Channel = 1,
                Transforms = new HarmonyConstraints
                {
                    ShapeTransform = new ShapeTransform { Type = "reflect", Axis = 3 }
                }
            };

            var merged = MultiTrackHarmonyEngine.GenerateAllEvents(new[] { track1, track2 });

            for (int i = 1; i < merged.Count; i++)
                Assert.True(merged[i].TimeStep >= merged[i - 1].TimeStep,
                    $"Merged list not sorted at index {i}.");
        }
    }
}
