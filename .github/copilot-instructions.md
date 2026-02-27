# Copilot Instructions for Parametric MIDI Sequencer

## Project Overview

This is a **.NET 10.0 C# console application** that generates MIDI files from JSON pattern specifications. It supports modulo patterns, function-based expression patterns, fractional timing, multi-track sequencing, and a harmony module for chord progression generation.

## Tech Stack

- **Language**: C# (.NET 10.0)
- **Test framework**: xUnit
- **Key dependency**: [DryWetMIDI](https://melanchall.github.io/drywetmidi/) (`Melanchall.DryWetMidi`) for MIDI I/O
- **JSON**: `System.Text.Json` (case-insensitive deserialization) and `Newtonsoft.Json`
- **Solution file**: `ParametricMidiSequencer.sln`

## Project Structure

```
src/                         # Main application
  Program.cs                 # CLI entry point (arg parsing, JSON loading, MIDI generation)
  ParametricMidiSequencer.csproj
  Midi/
    MidiGenerator.cs         # Two-pass scheduling engine (core logic)
  Models/
    JsonSpec.cs              # MetaSpec, TrackSpec, PatternSpecJson, ManualEvent
    HarmonySpec.cs           # HarmonySpec, ProgressionEntry, HarmonyConstraints
    HarmonyGenerator.cs      # Builds chords from scale/degree, applies transforms
    GeometricShapeTransform.cs # pitch-class set transforms (rotate, reflect, expand)
    ScaleBuilder.cs          # Builds scale pitch-class sets from mode/root
    ModeBuilder.cs           # Maps mode names to interval arrays
    CustomScaleBuilder.cs    # User-defined scale support
    PatternSpec.cs           # Legacy model (unused)
  Parsers/
    PatternParser.cs         # Legacy text parser (unused)
  Utils/
    JsonSchema.cs            # JSON schema utilities
tests/                       # xUnit test project
  ParametricMidiSequencer.Tests.csproj
  PatternParserTests.cs
  GeometricShapeTransformTests.cs
  HarmonyGeneratorTests.cs
examples/                    # Sample JSON input files
docs/                        # Architecture and idea notes
scripts/                     # Utility scripts
```

## Build & Run

```bash
# Build
dotnet build src/ParametricMidiSequencer.csproj

# Run
dotnet run --project src/ParametricMidiSequencer.csproj -- examples/patterns3.json --out out/output.mid

# Common CLI options: --tempo BPM, --steps N, --bars B, --extend, --no-extend, --out path, --list-events
```

## Testing

```bash
# Run all tests
dotnet test tests/ParametricMidiSequencer.Tests.csproj

# Build and test in one step
dotnet build ParametricMidiSequencer.sln && dotnet test tests/ParametricMidiSequencer.Tests.csproj
```

Tests use **xUnit** (`[Fact]` and `[Theory]`). Place new test files in `tests/` and follow the existing naming pattern `*Tests.cs`.

## Architecture Notes

### Two-Pass Scheduling (`MidiGenerator.cs`)

1. **Scan Pass** – Evaluate all patterns/events, detect note overflows past the sequence end.
2. **Auto-Extend** – If `autoExtend=true`, calculate extra bars needed and extend `meta.Bars`.
3. **Schedule Pass** – Re-evaluate using extended bars; emit absolute tick positions for all events.
4. **Sort & Convert** – Sort by time (meta events first, NoteOff before NoteOn), write delta times to MIDI.

### Tick Calculation

```
ticksPerStep = PPQ × 4 / Steps
noteOnTick   = (stepIndex × ticksPerStep) + (offset × ticksPerStep)
noteOffTick  = noteOnTick + (duration × ticksPerStep)
```

### Harmony Module

`HarmonyGenerator` converts a `HarmonySpec` (scale, root, progression) into `ManualEvent` lists:
- Builds pitch-class sets via `ScaleBuilder` / `ModeBuilder` / `CustomScaleBuilder`
- Supports modal interchange via `borrowMode` on individual `ProgressionEntry` items
- Applies optional transform layers from `HarmonyConstraints` (e.g. `minSharedPitches`, pitch-center cycling)
- Applies chord inversions

## Coding Conventions

- **Namespaces**: `ParametricMidiSequencer`, `ParametricMidiSequencer.Models`, `ParametricMidiSequencer.Midi`, `ParametricMidiSequencer.Parsers`
- **Null handling**: Use `??` / null-conditional operators; avoid throwing when a null input can be handled gracefully (see `GeometricShapeTransform.Apply` returning `null` for null chord)
- **JSON deserialization**: Always use `PropertyNameCaseInsensitive = true` for `JsonSerializerOptions`
- **No external frameworks** beyond the packages already listed in the `.csproj` files; prefer the standard library
- **MIDI channels**: MIDI channel 10 (index 9) is percussion; `isPercussion: true` on a `TrackSpec` auto-maps to channel 10
- **Step arithmetic**: Steps are 0-indexed; `totalSteps = Steps × Bars`

## Key JSON Input Shapes

```jsonc
// Full spec
{ "meta": { "tempo": 100, "steps": 16, "bars": 8, "ppq": 480, "autoExtend": true },
  "tracks": [ { "name": "Kick", "channel": 10, "patterns": [...] } ],
  "harmony": { "scale": [...], "progression": [...] } }

// Shorthand (array of tracks)
[ { "name": "Track1", "patterns": [...] } ]
```

Pattern types: `"modulo"` (interval-based), `"function"` (expression like `"sin(x) > 0.5"`).
