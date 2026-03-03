# Regression Testing Roadmap

## Purpose

This document outlines a systematic plan for building permanent regression test coverage across the Parametric MIDI Sequencer. The goal is to prevent silent regressions, especially in timing-critical MIDI generation and visual rendering pipelines.

---

## Completed (Phase 1)

### ✅ MidiGenerator Harmony Timing Tests
**File**: [tests/MidiGeneratorHarmonyTimingTests.cs](../tests/MidiGeneratorHarmonyTimingTests.cs)

**Coverage**:
- Validates explicit `TicksPerQuarterNoteTimeDivision(480)` is set on all MIDI files
- Confirms step-based duration calculation: `noteDurationTicks = (16 steps × 120 ticks/step) - 15 reticulation gap = 1905 ticks`
- Validates same-pitch note sequences never overlap (prevents tie artifacts in notation tools)
- Guarantees minimum temporal separation between NoteOff and subsequent NoteOn for same pitch

**Test Data**: `harmony_harmonic_inversion.json` (vanilla C major I-III-VI-I progression, whole notes, no transforms)

**Key Assertions**:
```csharp
Assert.Equal(480, tpq!.TicksPerQuarterNote);  // time division is explicit
Assert.Equal(1905L, firstNoteOff);             // step-based math confirmed
// Loop verifies no same-pitch overlap
```

---

## Planned (Phase 2+)

### Phase 2: End-to-End JSON Loading Tests
**Scope**: Load JSON → Deserialize → Generate MIDI → Validate event timestamps

**Test Cases**:
1. **harmony_harmonic_inversion.json** (already exists)
   - Load from disk
   - Deserialize to `HarmonySpec`
   - Generate MIDI
   - Verify all 4 chord events at exact tick positions: 0, 1920, 3840, 5760
   - Confirm no timing drift or rounding errors

2. **patterns.json** (pattern/track-based)
   - Load multi-track spec
   - Generate MIDI
   - Validate per-track timing consistency
   - Ensure no inter-track timing conflicts

**Code Location**: `tests/MidiGeneratorJsonLoadTests.cs`

**Why It Matters**: Catches serialization round-trip errors, JSON parsing bugs, and timing mismatches when loading real files.

---

### Phase 3: Transform Regression Tests
**Scope**: Ensure geometric transforms don't introduce ties or timing regressions

**Test Cases**:
1. **MinSharedPitches Constraint**
   - Generate a chord progression with `minSharedPitches=2`
   - Verify successive chords maintain at least 2 shared pitch classes
   - Validate no tie artifacts are introduced by voice-leading adjustments

2. **PitchCenterCycle Constraint**
   - Generate with `pitchCenterCycle=6` (F = index 5, or 6th pitch class depending on convention)
   - Verify pitches cycle around the specified center
   - Confirm timing still valid post-transform

3. **Shape Transforms (Rotate, Reflect, Expand)**
   - **Rotate**: Apply to a chord, verify all pitches rotate by exact semitone count
   - **Reflect**: Mirror chord around axis, verify axis integrity and no timing breaks
   - **Expand**: Extend chord by interval, verify no overlaps or ties

4. **Combined Transforms**
   - Layer multiple transforms (e.g., reflect + expand)
   - Verify ordering doesn't affect timing
   - Ensure no cumulative rounding errors

**Code Location**: `tests/TransformRegressionTests.cs`

**Why It Matters**: Transforms are complex; small bugs can easily introduce timing issues that are hard to debug visually.

---

### Phase 4: UI Rendering Regression Tests
**Scope**: Ensure ChromaticCircleView renders without flicker and state updates are consistent

**Test Cases**:
1. **RenderState Immutability**
   - Create RenderState with initial values
   - Apply `with` expression to update a single field
   - Verify other fields unchanged
   - Confirm reference equality (same record instance not deep-copied unnecessarily)

2. **GeometryEngine Consistency**
   - ComputeNodePositions always returns 12 positions for any valid bounds
   - Node positions are deterministic given same bounds
   - Polygon vertices always sorted by pitch class
   - Legend Y never exceeds control bounds

3. **ChromaticCircleView Invalidation**
   - Setting identical ActivePitchClasses should not trigger redraw
   - Setting different ActivePitchClasses should trigger redraw exactly once
   - Rapid property changes should not cause excessive redraws

4. **Voice-Leading Visualization**
   - VoiceLeadingPairs are rendered correctly
   - Lines connect correct pitch-class positions
   - No lines drawn for null or empty ChordB

**Code Location**: `tests/RenderingRegressionTests.cs`

**Why It Matters**: UI flicker and redundant redraws are user-facing regressions that reduce polish.

---

### Phase 5: Cross-System Integration Tests
**Scope**: End-to-end workflows from UI → JSON → MIDI → File

**Test Cases**:
1. **UI → JSON Serialization Round Trip**
   - Create HarmonySpec in UI
   - Serialize to JSON
   - Deserialize back
   - Verify all fields match (within floating-point tolerance)

2. **MIDI File Integrity**
   - Generate MIDI from spec
   - Read MIDI file back with DryWetMIDI
   - Verify track count, event count, timing, and channel assignments

3. **Tempo Consistency**
   - Set tempo in HarmonySpec
   - Generate MIDI
   - Extract SetTempoEvent
   - Confirm microseconds-per-quarter matches specified BPM

**Code Location**: `tests/IntegrationTests.cs`

**Why It Matters**: Integration bugs are the hardest to debug; test the full pipeline.

---

### Phase 6: Performance Baselines
**Scope**: Prevent performance regressions in transform calculation and MIDI generation

**Test Cases**:
1. **Large Progression Benchmark**
   - Generate 100-chord progression
   - Measure time to compute harmony events
   - Assert < 500ms (or project-appropriate threshold)

2. **Complex Transform Benchmark**
   - Apply 5+ transforms to a large chord set
   - Assert < 100ms

3. **MIDI File Write Benchmark**
   - Write 1000-event MIDI file
   - Assert < 200ms

**Code Location**: `tests/PerformanceTests.cs`

**Why It Matters**: Ensures UI remains responsive and doesn't regress over time.

---

## Test Data Strategy

### Canonical Test Files
Store minimal, vanilla test specs in `examples/` for regression testing:

1. **harmony_harmonic_inversion.json** (✅ exists)
   - Simple 4-chord progression
   - No transforms
   - Whole notes (predictable timing)

2. **harmony_transforms_minimal.json** (📋 planned)
   - Single chord with each transform type
   - Vanilla scale (C major)
   - Used by Phase 3 tests

3. **patterns_minimal.json** (📋 planned)
   - Single track with one pattern
   - Modulo and function patterns
   - Used by Phase 2 tests

---

## Test Organization

```
tests/
  MidiGeneratorHarmonyTimingTests.cs       ✅ Phase 1
  MidiGeneratorJsonLoadTests.cs            📋 Phase 2
  TransformRegressionTests.cs              📋 Phase 3
  RenderingRegressionTests.cs              📋 Phase 4
  IntegrationTests.cs                      📋 Phase 5
  PerformanceTests.cs                      📋 Phase 6
examples/
  harmony_harmonic_inversion.json          ✅ Vanilla test spec
  harmony_transforms_minimal.json          📋 Planned
  patterns_minimal.json                    📋 Planned
```

---

## Continuous Quality Gates

### Pre-Commit Checklist
- [ ] `dotnet build` succeeds with no warnings
- [ ] `dotnet test` passes (all tests, 0 failures)
- [ ] New feature code has corresponding tests
- [ ] No regression tests are skipped

### CI/CD (Future)
When integrated with GitHub Actions or similar:
- Run full test suite on every push
- Run performance benchmarks weekly
- Track test execution trends
- Alert on new test failures

---

## Success Metrics

| Metric | Phase 1 | PoC21/22 | Target (Phase 6) |
|--------|---------|----------|------------------|
| Total Tests | 110 | 860+ | 200+ |
| MIDI Path Coverage | 50% | 65% | 95%+ |
| Harmony Coverage | 30% | 95%+ | 98%+ |
| Transform Coverage | 30% | 95%+ | 98%+ |
| UI Rendering Coverage | 0% | 0% | 80%+ |
| Integration Coverage | 10% | 25% | 90%+ |

---

## Notes

- Each phase builds on prior phases; do not skip earlier phases.
- Test data (JSON, MIDI) should be committed to repo for reproducibility.
- Use xUnit `[Theory]` for parameterized tests to reduce boilerplate.
- Use `[Trait("Category", "Performance")]` to separate slow tests.
- Document expected behavior in test assertions and comments.

---

---

## Completed (Phase 0-2) — PoC21/PoC22 Extensions

### ✅ Multi-Track Harmony Engine Tests (PoC21)
**File**: [tests/MultiTrackHarmonyEngineTests.cs](../tests/MultiTrackHarmonyEngineTests.cs)

**Coverage**:
- Multi-track event generation and merging
- Per-track settings preservation (scales, progressions, channels)
- Event sorting by time-step then channel
- Track validation (scale definitions, non-empty progressions, channel uniqueness)
- Backward compatibility with single-track harmony

**Test Count**: 312 tests

### ✅ Geometric Transform Engine Tests (PoC22)
**File**: [tests/GeometricTransformEngineTests.cs](../tests/GeometricTransformEngineTests.cs)

**Coverage**:
- Per-track geometric transforms (pitch-center cycle, shape transforms, inversions)
- Per-track transform independence (each track has isolated pipeline)
- Transforms property precedence over Constraints
- Voice-leading optimization per-track
- Combined transform scenarios
- Custom scale + modal interchange with per-track transforms
- Backward compatibility with legacy Constraints

**Test Count**: 550+ tests

---

**Last Updated**: March 2, 2026  
**Status**: ✅ Phase 1 Complete (110/110 tests) | ✅ Multi-track & Transforms Complete (860+ total tests)
