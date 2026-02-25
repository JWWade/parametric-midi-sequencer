
ideas.md (draft 1)
A living document for early‑stage concepts, experiments, and design explorations.

---

Parametric MIDI Sequencer — Ideas & Explorations

This document collects early‑stage thoughts, conceptual sketches, and exploratory ideas for the parametric MIDI sequencer project. Nothing here is final; this is a sandbox for shaping the system’s direction.

---

1. Core Vision

A modular generative music engine where:

- Rule‑based modules (patterns, functions, interval logic)  
- ML‑based modules (Markov, HMM, small sequence models)  

…both produce a unified JSON specification, which the C# engine converts into MIDI.

The JSON spec becomes the “lingua franca” between human intent, algorithmic structure, and machine‑learned patterns.

---

2. Why JSON as the Central Representation

JSON is:

- Human‑readable  
- Machine‑friendly  
- Easy to generate from UI or ML  
- Easy to parse in C#  
- Flexible enough to represent scales, chords, rhythms, and patterns  

Everything—rule‑based or ML‑based—compiles down to the same JSON.

---

3. Two Generative Worlds (Rule‑Based + ML)

Rule‑Based Modules
These include:

- Modulo patterns  
- Function‑based patterns (sin, noise, etc.)  
- Euclidean rhythms  
- Intervallic chord construction (quartal, cluster, etc.)  
- Scale‑aware movement rules  
- User‑defined constraints  

These are deterministic, controllable, and transparent.

ML‑Based Modules
Lightweight models trained on small datasets (dozens of examples):

- Markov chains for chord transitions  
- Hidden Markov Models for harmonic “zones”  
- Simple sequence models for rhythm or chord movement  
- ML‑generated variations on user‑defined patterns  

These provide stylistic nuance and emergent behavior.

Both worlds output the same JSON chord/rhythm spec.

---

4. Focus Area: Chord Progressions

The system should support:

- Unusual scales  
- Non‑functional harmony  
- Quartal harmony  
- Interval‑based chord construction  
- Pitch‑class‑set‑based thinking  
- Atypical progressions and modal movement  

Key idea:
Represent chords not as “Cmaj7” but as:

- Scale degree + chord type  
- Interval structure  
- Pitch‑class set  

This avoids forcing Western functional harmony onto the system.

---

5. Representing Scales

Allow arbitrary pitch‑class sets:

`json
"scale_pitches": [0, 2, 5, 7, 10]
`

This supports:

- Custom modes  
- Synthetic scales  
- Microtonal extensions (future)  
- User‑defined harmonic universes  

---

6. Representing Chords

Chords can be defined in multiple ways:

A. Interval structure (quartal, quintal, etc.)
`json
{ "type": "quartal", "root": 0, "structure": [0, 5, 10] }
`

B. Explicit pitch‑class sets
`json
{ "type": "cluster", "pitches": [2, 5, 7, 11] }
`

C. Scale‑degree based
`json
{ "degree": 3, "type": "quartal" }
`

The engine resolves these into MIDI notes.

---

7. Representing Progressions

A progression is a time‑ordered list of chord events:

`json
{
  "chord_track": {
    "resolution": "per_beat",
    "progression": [
      { "time": 0, "chord": { ... } },
      { "time": 4, "chord": { ... } }
    ]
  }
}
`

This is the shared output format for both rule‑based and ML modules.

---

8. ML Integration (Small‑Data Friendly)

Why small data works here
Chord progressions are symbolic and low‑dimensional.  
A Markov or HMM model can learn meaningful transitions from:

- 20–50 of your own sketches  
- A handful of progressions in unusual scales  
- A small curated dataset of quartal or modal harmony  

What the ML learns
- Transition tendencies between chord states  
- Preferred intervallic movements  
- Your personal harmonic “voice”  
- How you move within unusual scales  

State representation options
- (scaledegree, chordtype)  
- (pitchclassset_id)  
- (interval_vector)  

The ML module outputs a sequence of states → converted to JSON → rendered to MIDI.

---

9. Hybrid Workflow Example

1. User defines a custom scale.  
2. User chooses “quartal harmony only.”  
3. User selects a mode:
   - Rule‑based progression  
   - ML‑generated progression  
4. System outputs JSON.  
5. C# engine renders MIDI.  
6. User tweaks the JSON or regenerates variations.  

This creates a loop between human creativity and algorithmic exploration.

---

10. Future Directions (Exploratory)

- Latent‑space interpolation between chord progressions  
- ML‑generated rhythmic patterns feeding into the same JSON  
- React UI for visualizing and editing patterns  
- Pattern “mutation” and evolutionary algorithms  
- Multi‑track generative structures  
- Integration with DAWs via MIDI drag‑and‑drop  
