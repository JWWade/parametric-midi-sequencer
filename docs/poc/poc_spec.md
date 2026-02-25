
poc_spec.md — Proof‑of‑Concept Specification
A minimal harmonic generator for the Parametric MIDI Sequencer

---

1. Purpose

This proof‑of‑concept adds a simple harmonic generation module to the existing Parametric MIDI Sequencer. The goal is to demonstrate that:

- Harmony can be expressed in JSON  
- Harmony can be converted into MIDI note events  
- The existing scheduling engine can render those events  
- No new architecture is required  

This PoC intentionally avoids complexity. It is not a prototype of the full generative system — it is a vertical slice proving that the sequencer can host harmonic logic.

---

2. Scope

In Scope
- One harmony track  
- One scale (C major)  
- Triads only  
- Scale‑degree based chord selection  
- Fixed velocity and duration  
- Block chords (all notes start at the same time)  
- JSON → events → existing scheduler → MIDI  

Out of Scope
- Seventh chords  
- Transform layers  
- ML  
- Motif logic  
- Rhythm generation  
- Voice‑leading constraints  
- Substitutions  
- Geometry / spiral / FSMs  
- Multiple harmony tracks  
- UI  
- DAW integration  
- Real‑time generation  

This PoC is deliberately small.

---

3. JSON Format (New Harmony Section)

A new top‑level section is added:

`json
"harmony": {
  "scale": [0, 2, 4, 5, 7, 9, 11],
  "progression": [
    { "time": 0, "degree": 1, "type": "triad" },
    { "time": 4, "degree": 4, "type": "triad" },
    { "time": 8, "degree": 5, "type": "triad" },
    { "time": 12, "degree": 1, "type": "triad" }
  ],
  "channel": 0,
  "velocity": 90,
  "duration": 4
}
`

3.1 Fields

| Field | Type | Description |
|-------|------|-------------|
| scale | array<int> | Pitch‑class set for the scale. For PoC: C major = [0,2,4,5,7,9,11]. |
| progression | array<object> | List of chord events. |
| progression[].time | number | Step index where the chord begins. |
| progression[].degree | int | Scale degree (1–7). |
| progression[].type | string | Only "triad" supported in PoC. |
| channel | int | MIDI channel for harmony track. |
| velocity | int | Velocity for all chord tones. |
| duration | number | Duration in steps for each chord. |

---

4. Chord Construction Rules

4.1 Root Selection
`
root = scale[degree - 1]
`

Example:  
Degree 4 in C major → scale[3] = 5 → F.

4.2 Triad Construction
Triad = root + major/minor third + perfect fifth  
(based on scale‑degree quality in major scale)

| Degree | Quality | Intervals (semitones) |
|--------|----------|------------------------|
| 1 | major | 0, 4, 7 |
| 2 | minor | 0, 3, 7 |
| 3 | minor | 0, 3, 7 |
| 4 | major | 0, 4, 7 |
| 5 | major | 0, 4, 7 |
| 6 | minor | 0, 3, 7 |
| 7 | diminished | 0, 3, 6 |

4.3 MIDI Note Calculation
`
midiNote = rootPitchClass + 60  // C4 as baseline
`

Triad notes are:
`
root + interval
`

All notes share:
- same velocity  
- same duration  
- same channel  

---

5. Integration With Existing Sequencer

5.1 Event Generation
The harmony module produces events in the same format as manual events:

`json
{
  "timeStep": 0,
  "note": 60,
  "velocity": 90,
  "duration": 4,
  "channel": 0
}
`

5.2 Scheduling
Events are passed directly into the existing two‑pass scheduling engine.

5.3 Output
The sequencer outputs a standard MIDI file with one harmony track.

---

6. Example PoC JSON File

`json
{
  "meta": {
    "tempo": 100,
    "steps": 16,
    "bars": 2,
    "ppq": 480,
    "autoExtend": true
  },

  "harmony": {
    "scale": [0, 2, 4, 5, 7, 9, 11],
    "progression": [
      { "time": 0, "degree": 1, "type": "triad" },
      { "time": 4, "degree": 4, "type": "triad" },
      { "time": 8, "degree": 5, "type": "triad" },
      { "time": 12, "degree": 1, "type": "triad" }
    ],
    "channel": 0,
    "velocity": 90,
    "duration": 4
  }
}
`

Running:

`
dotnet run --project src/... -- examples/harmony.json --out out/harmony.mid
`

Produces a MIDI file with a simple I–IV–V–I progression.

---

7. Success Criteria

The PoC is successful if:

- The sequencer accepts a JSON file with a harmony section  
- The harmony module generates valid note events  
- The existing scheduler renders them correctly  
- The output MIDI file plays the expected triads  
- No changes are required to the core scheduling engine  

This proves the architecture can support the full generative system.

---

8. Next Steps (After PoC)

Once this PoC is working, the next incremental steps could be:

- Add seventh chords  
- Add multiple scales  
- Add transform layers  
- Add voice‑leading constraints  
- Add cycle‑based pitch centers  
- Add ML chord transitions  
- Add motif generation  
- Add geometric workflows  

But none of these are part of the PoC.
