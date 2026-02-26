
---

# **poc6_spec.md — Second Transform Layer: Pitch‑Center Cycling**

This proof‑of‑concept introduces a new harmonic transform layer: **pitch‑center cycling**, a controlled, stepwise rotation of harmonic material around the chromatic circle. This transform shifts the *center of gravity* of each chord by a fixed number of semitones while preserving chord quality, scale‑degree identity, and inversion structure. It is the first explicitly geometric transform in the system.

---

## **1. Purpose**

Pitch‑center cycling allows the harmony engine to:

- Apply a uniform transpositional shift to each chord in the progression.
- Preserve chord quality (major/minor/diminished/seventh).
- Preserve inversion structure.
- Preserve relative interval structure.
- Integrate cleanly with the existing minSharedPitches transform.
- Produce musically coherent modulations, rotations, and color shifts.

This PoC demonstrates that the harmony engine can support **multiple sequential transform layers**, each operating independently and predictably.

---

## **2. Scope**

### In Scope
- A new transform layer: `"pitchCenterCycle"`.
- Semitone‑based rotation of chord pitch classes.
- Integration with PoC2 (minSharedPitches).
- Integration with PoC4 (inversions).
- Integration with PoC5 (multiple scales).
- One harmony track.

### Out of Scope
- Adaptive or dynamic pitch‑center movement.
- Scale‑aware or mode‑aware cycling.
- Voice‑leading optimization during cycling.
- Non‑uniform or chord‑specific cycling.
- Geometric UI or visualization.
- ML‑guided cycle selection.

These will be addressed in later PoCs.

---

## **3. JSON Format Update**

A new field is added under `constraints`:

```json
"constraints": {
  "minSharedPitches": 2,
  "pitchCenterCycle": 2
}
```

### Meaning:
- `pitchCenterCycle = N` means:  
  **Shift every chord by N semitones upward (mod 12).**

### Valid values:
- Integer from `-11` to `+11`.

### Backward compatibility:
- If omitted, no pitch‑center cycling is applied.

---

## **4. Transform Layer Ordering**

Transform layers must be applied in this order:

1. **Initial chord construction** (triad or seventh).
2. **minSharedPitches** (PoC2).
3. **pitchCenterCycle** (PoC6).
4. **Inversion** (PoC4).
5. **Event generation**.

This ordering ensures:

- minSharedPitches operates on scale‑degree harmony.
- pitchCenterCycle applies a global rotation.
- inversion applies to the final rotated chord.

---

## **5. Pitch‑Center Cycling Rules**

### 5.1 Core operation
For each pitch class `pc` in the chord:

```
pc' = (pc + pitchCenterCycle) % 12
```

### 5.2 Chord quality preservation
Cycling must not alter:

- interval structure,
- chord quality,
- chord size.

Because cycling is a uniform transposition, these properties are naturally preserved.

### 5.3 Inversion preservation
Cycling occurs **before** inversion, so inversion logic remains unchanged.

### 5.4 Scale independence
Cycling does **not** require the resulting chord to remain in the original scale.

This is intentional: pitch‑center cycling is a geometric transform, not a diatonic one.

---

## **6. Implementation Outline**

### 6.1 Extend HarmonySpec
- Add `"pitchCenterCycle"` as an optional integer field under `constraints`.

### 6.2 Add new transform class
Create:

`HarmonyTransform_PitchCenterCycle`

Responsibilities:
- Accept transformed triads/sevenths from PoC2.
- Apply uniform pitch‑class rotation.
- Return rotated chords.

### 6.3 Update transform pipeline
In `HarmonyGenerator`:

- After minSharedPitches:
  - If `pitchCenterCycle` exists, apply the new transform.
- Then apply inversion.

### 6.4 Update event generation
No changes required.

---

## **7. Test Cases**

Add unit tests for:

- Cycling triads by +1, +2, +5 semitones.
- Cycling seventh chords.
- Cycling chords with inversions.
- Cycling chords after minSharedPitches adjustments.
- Negative cycles (e.g., -3).
- No cycle (field omitted).
- Interaction with multiple scales (PoC5).

---

## **8. Example JSON for Pitch‑Center Cycling**

```json
{
  "meta": {
    "tempo": 100,
    "steps": 16,
    "bars": 2,
    "ppq": 480,
    "autoExtend": true
  },

  "harmony": {
    "scaleName": "major",
    "root": "C",

    "progression": [
      { "time": 0, "degree": 1, "type": "seventh", "inversion": 0 },
      { "time": 4, "degree": 4, "type": "triad", "inversion": 1 },
      { "time": 8, "degree": 5, "type": "seventh", "inversion": 2 },
      { "time": 12, "degree": 1, "type": "triad", "inversion": 0 }
    ],

    "constraints": {
      "minSharedPitches": 2,
      "pitchCenterCycle": 2
    },

    "channel": 0,
    "velocity": 90,
    "duration": 4
  }
}
```

---

## **9. Expected Behavior**

For a progression like:

- Cmaj7 → Fmaj → G7 → Cmaj  
- with `pitchCenterCycle = 2`

The engine should:

- Build the C major scale.
- Construct correct triads and sevenths.
- Apply minSharedPitches adjustments.
- Rotate all chords up by 2 semitones.
- Apply inversions.
- Output correct MIDI events.

The result is a harmonically coherent progression that has been globally “tilted” around the chromatic circle.

