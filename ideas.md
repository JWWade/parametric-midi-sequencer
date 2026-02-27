ideas.md

A living document for early‑stage concepts, experiments, and design explorations.

Parametric MIDI Sequencer — Ideas & Explorations

This document collects early‑stage thoughts, conceptual sketches, and exploratory ideas for the parametric MIDI sequencer project. Nothing here is final; this is a sandbox for shaping the system’s direction.

1. Core Vision

A modular generative music engine where:

Rule‑based modules (patterns, functions, interval logic)

ML‑based modules (Markov, HMM, small sequence models)

…both produce a unified JSON specification, which the C# engine converts into MIDI.

The JSON spec becomes the “lingua franca” between human intent, algorithmic structure, and machine‑learned patterns.

2. Why JSON as the Central Representation

JSON is:

Human‑readable

Machine‑friendly

Easy to generate from UI or ML

Easy to parse in C#

Flexible enough to represent scales, chords, rhythms, and patterns

Everything—rule‑based or ML‑based—compiles down to the same JSON.

3. Two Generative Worlds (Rule‑Based + ML)

Rule‑Based Modules

These include:

Modulo patterns

Function‑based patterns (sin, noise, etc.)

Euclidean rhythms

Intervallic chord construction (quartal, cluster, etc.)

Scale‑aware movement rules

User‑defined constraints

These are deterministic, controllable, and transparent.

ML‑Based Modules

Lightweight models trained on small datasets (dozens of examples):

Markov chains for chord transitions

Hidden Markov Models for harmonic “zones”

Simple sequence models for rhythm or chord movement

ML‑generated variations on user‑defined patterns

These provide stylistic nuance and emergent behavior.

Both worlds output the same JSON chord/rhythm spec.

4. Focus Area: Chord Progressions

The system should support:

Unusual scales

Non‑functional harmony

Quartal harmony

Interval‑based chord construction

Pitch‑class‑set‑based thinking

Atypical progressions and modal movement

Key idea: Represent chords not as “Cmaj7” but as:

Scale degree + chord type

Interval structure

Pitch‑class set

This avoids forcing Western functional harmony onto the system.

5. Representing Scales

Allow arbitrary pitch‑class sets:

"scale_pitches": [0, 2, 5, 7, 10]

This supports:

Custom modes

Synthetic scales

Microtonal extensions (future)

User‑defined harmonic universes

6. Representing Chords

Chords can be defined in multiple ways:

A. Interval structure (quartal, quintal, etc.)

{ "type": "quartal", "root": 0, "structure": [0, 5, 10] }

B. Explicit pitch‑class sets

{ "type": "cluster", "pitches": [2, 5, 7, 11] }

C. Scale‑degree based

{ "degree": 3, "type": "quartal" }

The engine resolves these into MIDI notes.

7. Representing Progressions

A progression is a time‑ordered list of chord events:

{
  "chord_track": {
    "resolution": "per_beat",
    "progression": [
      { "time": 0, "chord": { ... } },
      { "time": 4, "chord": { ... } }
    ]
  }
}

This is the shared output format for both rule‑based and ML modules.

8. ML Integration (Small‑Data Friendly)

Why small data works here

Chord progressions are symbolic and low‑dimensional. A Markov or HMM model can learn meaningful transitions from:

20–50 of your own sketches

A handful of progressions in unusual scales

A small curated dataset of quartal or modal harmony

What the ML learns

Transition tendencies between chord states

Preferred intervallic movements

Your personal harmonic “voice”

How you move within unusual scales

State representation options

(scale_degree, chord_type)

(pitch_class_set_id)

(interval_vector)

The ML module outputs a sequence of states → converted to JSON → rendered to MIDI.

9. Hybrid Workflow Example

User defines a custom scale.

User chooses “quartal harmony only.”

User selects a mode:

Rule‑based progression

ML‑generated progression

System outputs JSON.

C# engine renders MIDI.

User tweaks the JSON or regenerates variations.

This creates a loop between human creativity and algorithmic exploration.

10. Future Directions (Exploratory)

Latent‑space interpolation between chord progressions

ML‑generated rhythmic patterns feeding into the same JSON

React UI for visualizing and editing patterns

Pattern “mutation” and evolutionary algorithms

Multi‑track generative structures

Integration with DAWs via MIDI drag‑and‑drop

11. Scale‑Based Harmonization Workflow (Mixolydian Example)

A workflow where harmony is generated directly from a scale.

1. Scale as the Harmonic Universe

Example: C major with a flat 7 (Bb) → C Mixolydian.

2. Direct Harmonization

Stacking thirds yields:

C7

Dm7

Em7♭5

Fmaj7

Gm7

Am7

Bbmaj7

3. Substitution Layer

After harmonization, apply substitutions:

Secondary dominants

Tritone subs

Parallel minor borrowing

Modal interchange

ii–V insertions

Chromatic approaches

4. Why This Matters

This workflow is:

Scale‑driven

Harmonically constrained

Transformative

Ideal for modal colors and non‑functional harmony

5. Fit Within the System

Reinforces the need for:

Flexible scale system

Chord‑construction layer

Substitution layer

Unified JSON output

12. Scale‑Based Harmonization Workflow (Gypsy Scale Example)

1. Scale Definition

Example in C:

C  D  Eb  F#  G  Ab  B

Pitch‑class set:

[0, 2, 3, 6, 7, 8, 11]

2. Harmonization

Stacking thirds yields:

Cmaj7♯5

Dm(maj7)

Eb+maj7

F#dim7 or F#7alt

G7♭9♭13

Abmaj7♯5

Bdim or B7alt

3. Substitutions

Often emphasizes:

Augmented triads

Altered dominants

Chromatic mediants

Symmetry‑based movements

Parallel motion

4. Why This Matters

Supports:

Arbitrary pitch‑class sets

Non‑functional harmony

Intervallic chord construction

Transform layers

13. Voice‑Leading Constraint Workflow (Shared‑Pitch Rules)

A generative idea where each subsequent chord must share at least N pitch classes with the previous chord.

1. The Rule

N = 3 → very tight continuity

N = 2 → moderate continuity

N = 1 → loose continuity

N = 0 → no constraint

2. Why This Matters

This workflow is:

Intervallic

Voice‑leading‑driven

Non‑functional

Ideal for modal, ambient, or quartal textures

3. How the System Might Use It

Start with an initial chord.

Generate candidate chords.

Filter by shared‑pitch rule.

Choose based on randomness, weights, or ML.

Repeat.

4. Interaction With Other Workflows

Can be layered on top of:

Scale‑based harmonization

Cycle‑based pitch centers

Form‑based structures

ML‑generated sequences

5. Fit Within the System

Reinforces the need for:

Pitch‑class‑set chord representation

Interval‑based reasoning

Constraint‑driven transform layers

14. Imperceptible Change Workflow (Gradual Harmonic Morphing)

A workflow where chord progressions evolve so gradually that the listener doesn’t immediately notice the harmonic shift.

1. The Core Concept

Make small, incremental changes to a chord’s pitch‑class set:

Raise or lower one pitch by a semitone

Add or remove a pitch

Replace one pitch with a neighbor

Shift one voice at a time

Over many measures, these micro‑changes accumulate into a new chord or key.

2. Why This Matters

This approach is:

Non‑functional

Voice‑leading‑driven

Coloristic

Ideal for ambient or evolving textures

3. How the System Might Implement It

Start with an initial chord.

Generate micro‑adjustments.

Apply one per measure.

Repeat until reaching a target region.

4. Interaction With Other Workflows

Can combine with:

Scale‑based harmonization

Cycle‑based pitch centers

Voice‑leading constraints

ML‑guided adjustments

5. Fit Within the System

Reinforces the need for:

Pitch‑class‑set representation

Incremental transformations

Voice‑leading‑level operations

15. Motif Sliding Workflow (Interval‑Preserving Melodic Variation)

A melodic workflow where a motif is “slid” around within a key while preserving its interval structure.

1. The Core Concept

Start with a motif defined by intervals or contour, then generate variations by:

Moving it to different scale degrees

Keeping the interval pattern intact

Correcting notes to fit the scale

Optionally aligning with harmony

2. Why This Matters

This approach is:

Melodically driven

Interval‑preserving

Scale‑aware

Useful for thematic development

3. How the System Might Implement It

User defines a motif.

User defines a scale.

Engine generates variations by transposing within the scale.

Variations can be sequential, randomized, or ML‑guided.

4. Interaction With Other Workflows

Works with:

Scale‑based harmony

Cycle‑based pitch centers

Voice‑leading constraints

Imperceptible change

5. Fit Within the System

Reinforces the need for:

Interval‑based motif representation

Scale‑aware melodic generation

Transform layers for melody

16. Geometric and Spiral Representations

Explorations involving geometric or spiral mappings of pitch, rhythm, or time.

1. Spiral Diagrams

Ideas involving:

Radial distance as pitch or intensity

Angle as time or rhythmic position

Spiral segments as structural regions

Transformations applied geometrically

2. 12‑TET Geometric Diagrams

Using the 12‑tone circle as a geometric space:

Scales as polygons

Chords as shapes

Rotations as transpositions

Reflections as inversions

These geometric ideas can become generative engines or transform layers.

17. State‑Based Generative Models (FSMs, Petri Nets, Graphs)

A computational approach where musical processes are modeled as state machines.

1. Chord Progressions as FSMs

Each chord is a state.

Transitions define allowed movements.

2. Motifs as FSMs

Each motif variant is a state.

Transitions represent transformations.

3. Rhythm as FSMs

Each rhythmic cell is a state.

Transitions define rhythmic evolution.

4. Petri Nets for Concurrency

Useful for modeling:

Harmony evolving slowly

Rhythm evolving quickly

Independent melodic processes

5. Fit Within the System

State‑based models offer a formal way to express cycles, transformations, and multi‑layered processes.

18. Harmonic Templates (Blues, Rhythm Changes, Coltrane Changes)

The system should support canonical harmonic frameworks as starting points.

1. 12‑Bar Blues

A form template that can be transformed using cycles, substitutions, or scalar reharmonization.

2. Rhythm Changes

A harmonically dense template ideal for experimentation.

3. Coltrane Changes

A major‑third cycle structure that fits naturally into 12‑TET and geometric representations.

These templates provide structured starting points for generative exploration.

19. Project Guardrails (Scope Management)

To keep the project manageable and buildable, the following guardrails apply:

1. 12‑TET Only

No microtonality

No alternate tunings

A = 440 Hz

2. Pitch‑Class Sets Only

Everything represented as 0–11

Transposition‑invariant

3. One JSON Spec for Everything

All workflows must compile to the same JSON format.

4. Harmony First

Melody and rhythm come after the harmonic engine is stable.

5. Limited Initial Transform Layers

Start with:

Pitch‑center cycles

Substitution layer

Voice‑leading constraint

Imperceptible change

6. Limited Generative Modes Initially

Begin with:

Rule‑based

ML‑based

One geometric mode (12‑TET circle)

7. Simple UI at First

Console app → JSON → MIDI.

8. No DAW Integration Yet

MIDI export only.

9. No Real‑Time Generation Yet

Batch generation only.

10. Modular Workflows

Each workflow is explicit and composable, but not all active at once.

This document will continue to evolve as new ideas emerge and the system takes shape.