# Parametric MIDI Sequencer

## Overview

The Parametric MIDI Sequencer is a .NET 10.0 toolset for generating MIDI files from JSON pattern specifications. It includes both:

- a **CLI** for scriptable generation workflows
- a **Windows Forms UI** for interactive harmony editing, preview, and MIDI export

It supports flexible pattern definitions with modulo intervals, function-based triggers, fractional timing, multi-track sequencing, and automatic bar extension to accommodate note overflows.

## Features

- **JSON-based pattern definition**: Define tracks, patterns, and events in a flexible JSON format
- **Desktop UI (WinForms)**: Edit harmony settings, progression rows, constraints, and generate MIDI interactively
- **Modulo patterns**: Trigger notes at regular intervals (e.g., every 8 steps)
- **Function patterns**: Play notes based on expressions like `sin(x) > 0.5`
- **Fractional timing**: Support for fractional offsets and durations in steps
- **Per-pattern control**: Set velocity, duration, offset, and MIDI channel per pattern
- **Percussion mapping**: Auto-map to MIDI channel 10 for drum tracks
- **Auto-extension**: Automatically extend bars to prevent notes from being cut off
- **Diagnostic output**: List all generated events with `--list-events` flag
- **Harmony generation**: Define chord progressions with scale, degree, type, and inversion (see docs/poc/* for evolving proof‑of‑concept specs)
- **Multi-track harmony** (PoC21): Use `harmonyTracks` array to define and merge multiple independent harmony tracks with separate scales, progressions, and channels
- **Per-track geometric transforms** (PoC22): Apply fully independent transform pipelines to each harmony track—shape transforms, pitch-center cycling, inversions, and voice-leading optimization all per-track
- **Modal interchange**: Optionally borrow chords from other modes using a `borrowMode` field on individual progression entries (per PoC7 spec)
- **Transform constraints**: Optional `constraints` or new `transforms` field on harmony specs for shared‑pitch minimums, pitch‑center cycling, geometric shape transforms, and voice-leading optimization
- **CLI overrides**: Control tempo, steps, bars, and auto-extend behavior from command line
- **Helper scripts**: Quick build/run workflows via batch scripts in `scripts/`

## Project Structure

```
parametric-midi-sequencer
├── src/
│   ├── Program.cs                           # CLI parsing and MIDI generation entry point
│   ├── ParametricMidiSequencer.csproj       # .NET 10.0 project file
│   ├── Midi/
│   │   └── MidiGenerator.cs                 # Two-pass scheduling engine
│   ├── Models/
│   │   ├── JsonSpec.cs                      # MetaSpec, TrackSpec, PatternSpecJson models
│   │   └── PatternSpec.cs                   # Legacy model (unused)
│   ├── Parsers/
│   │   └── PatternParser.cs                 # Legacy text parser (unused)
│   └── Utils/
│       └── JsonSchema.cs                    # JSON schema utilities
├── ui/
│   ├── Program.cs                            # WinForms entry point
│   ├── MainForm.cs                           # Primary UI surface and interactions
│   ├── ChromaticCircleControl.cs             # Harmonic/chromatic visual control
│   ├── Utilities/                            # UI formatting and conversion helpers
│   └── ParametricMidiSequencer.UI.csproj     # .NET Windows UI project
├── tests/
│   ├── ParametricMidiSequencer.Tests.csproj
│   ├── PatternParserTests.cs
│   ├── HarmonyGeneratorTests.cs
│   └── GeometricShapeTransformTests.cs
├── examples/
│   ├── patterns.json                        # Example patterns
│   ├── patterns3.json                       # Kick/snare alternating pattern
│   ├── patterns_no_extend.json              # Test with autoExtend disabled
│   └── patterns_triplet.json                # Triplet grid example
├── docs/
│   └── initial-idea/notes.md
├── scripts/
│   └── build-latest.bat                      # Build solution and launch UI
├── ARCHITECTURE.md                          # Full system architecture with Mermaid diagram
├── ParametricMidiSequencer.sln
└── README.md
```

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later

### Building

```bash
dotnet build src/ParametricMidiSequencer.csproj
```

### Building + Launching UI (Windows)

```bat
.\scripts\build-latest.bat
```

This script builds the solution and launches the UI project on success.

### Running

```bash
dotnet run --project src/ParametricMidiSequencer.csproj -- <inputFile.json> [options]
```

### Running the UI directly

```bash
dotnet run --project ui/ParametricMidiSequencer.UI.csproj
```

### Basic Example

```bash
dotnet run --project src/ParametricMidiSequencer.csproj -- examples/patterns3.json --out out/my_sequence.mid
```

## JSON Input Format

The application accepts JSON files in two formats:

### Full Format (with metadata)

```json
{
  "meta": {
    "tempo": 100,
    "steps": 16,
    "bars": 8,
    "ppq": 480,
    "autoExtend": true
  },
  "tracks": [
    {
      "name": "Kick",
      "channel": 10,
      "isPercussion": true,
      "patterns": [
        {
          "id": "K",
          "type": "modulo",
          "note": 36,
          "interval": 8,
          "velocity": 100,
          "duration": 4
        }
      ]
    }
  ]
}
```

### Array Format (shorthand)

```json
[
  {
    "name": "Track1",
    "patterns": [...]
  }
]
```

### Pattern Types

#### Modulo Pattern
```json
{
  "id": "K",
  "type": "modulo",
  "note": 36,
  "interval": 8,
  "velocity": 100,
  "duration": 4,
  "offset": 0.5,
  "channel": 10
}
```

#### Function Pattern
```json
{
  "id": "Melody",
  "type": "function",
  "note": 60,
  "expression": "sin(x) > 0.5",
  "velocity": 80,
  "duration": 2
}
```

#### Bar-Relative Pattern (HitsPerBar)
```json
{
  "id": "Snare",
  "type": "modulo",
  "note": 38,
  "hitsPerBar": 2,
  "mode": "bar",
  "velocity": 90,
  "duration": 3
}
```

### Manual Events

Insert notes at specific steps:

```json
{
  "tracks": [
    {
      "name": "Manual Notes",
      "events": [
        {
          "timeStep": 0,
          "note": 60,
          "velocity": 100,
          "duration": 4,
          "channel": 0
        }
      ]
    }
  ]
}
```

### Multi-Track Harmony (PoC21+)

Define multiple independent harmony tracks with fully separate transform pipelines:

```json
{
  "meta": {
    "tempo": 100,
    "steps": 16,
    "bars": 4,
    "ppq": 480
  },
  "tracks": [],
  "harmonyTracks": [
    {
      "name": "Lead",
      "scale": [0, 2, 4, 5, 7, 9, 11],
      "progression": [
        { "time": 0,  "degree": 1, "type": "triad" },
        { "time": 8,  "degree": 4, "type": "triad" }
      ],
      "channel": 0,
      "velocity": 90,
      "duration": 4
    },
    {
      "name": "Bass",
      "scaleName": "minor",
      "root": "A",
      "progression": [
        { "time": 0,  "degree": 1, "type": "triad" },
        { "time": 8,  "degree": 5, "type": "triad" }
      ],
      "channel": 1,
      "velocity": 80,
      "duration": 8,
      "transforms": {
        "pitchCenterCycle": 2,
        "shapeTransform": { "type": "reflect", "axis": 3 }
      }
    }
  ]
}
```

Each harmony track:
- Runs independently with its own scale, progression, and transform pipeline
- Uses the `transforms` property for per-track geometric operations (takes precedence over legacy `constraints`)
- Merges into a unified event timeline sorted by time-step then channel

## CLI Options

| Option | Description | Example |
|--------|-------------|----------|
| `--tempo` | Override tempo (BPM) | `--tempo 120` |
| `--steps` | Override steps per bar | `--steps 16` |
| `--bars` | Override bar count | `--bars 16` |
| `--extend` | Force auto-extension enabled | `--extend` |
| `--no-extend` | Force auto-extension disabled | `--no-extend` |
| `--out` | Output MIDI file path | `--out out/music.mid` |
| `--list-events` | Print all scheduled events | `--list-events` |

### Examples

```bash
# Generate with custom tempo
dotnet run --project src/ParametricMidiSequencer.csproj -- examples/patterns3.json --tempo 140

# Generate with extended bars and list all events
dotnet run --project src/ParametricMidiSequencer.csproj -- examples/patterns.json --extend --list-events

# Generate without auto-extension and save to custom path
dotnet run --project src/ParametricMidiSequencer.csproj -- examples/patterns.json --no-extend --out out/custom.mid
```

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for a detailed explanation of the system architecture, including:
- Data flow diagram (Mermaid)
- Two-pass scheduling algorithm
- Auto-extension logic
- Component descriptions

## Testing

```bash
dotnet test tests/ParametricMidiSequencer.Tests.csproj
```

## Contributing

Contributions are welcome! Please feel free to submit a pull request or open an issue for any enhancements or bug fixes.

## License

This project is licensed under the MIT License.