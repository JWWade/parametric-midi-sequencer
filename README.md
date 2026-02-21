# Parametric MIDI Sequencer

## Overview
The Parametric MIDI Sequencer is a C# console application designed to generate MIDI files based on user-defined musical patterns. It utilizes a simple text input format that is parsed into a JSON structure, which is then used to create MIDI sequences.

## Project Structure
```
parametric-midi-sequencer
├── src
│   ├── ParametricMidiSequencer.csproj
│   ├── Program.cs
│   ├── Models
│   │   └── PatternSpec.cs
│   ├── Parsers
│   │   └── PatternParser.cs
│   ├── Midi
│   │   └── MidiGenerator.cs
│   └── Utils
│       └── JsonSchema.cs
├── tests
│   ├── ParametricMidiSequencer.Tests.csproj
│   └── PatternParserTests.cs
├── docs
│   └── initial-idea
│       └── notes.md
├── ParametricMidiSequencer.sln
└── README.md
```

## Getting Started

### Prerequisites
- .NET SDK (version 5.0 or later)

### Running the Application
1. Open the solution file `ParametricMidiSequencer.sln` in your development environment.
2. Build the project to restore dependencies.
3. Run the application directly from your IDE, or use the command line:
   ```
   dotnet run --project src/ParametricMidiSequencer.csproj
   ```

### Input Format
The application accepts a simple text input format for defining musical patterns. For example:
```
K:1/4
S:1/3
H:sin>0.5
```

### Output
The application generates MIDI files based on the defined patterns, which can be played back using any MIDI-compatible software.

## Contributing
Contributions are welcome! Please feel free to submit a pull request or open an issue for any enhancements or bug fixes.

## License
This project is licensed under the MIT License. See the LICENSE file for more details.