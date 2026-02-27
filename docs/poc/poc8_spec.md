
---

# **poc8_spec.md — Custom Pitch‑Class Sets for Harmony Engine**

This proof‑of‑concept extends the harmony engine to support **custom pitch‑class sets**, allowing users to define arbitrary collections of pitch classes instead of relying solely on predefined scales or modes. This milestone is foundational for geometric harmony, non‑functional systems, synthetic scales, and user‑defined harmonic spaces.

---

## **1. Purpose**

Custom pitch‑class sets enable the harmony engine to:

- Accept any set of pitch classes as the harmonic universe.
- Derive chord tones directly from the custom set.
- Support triads and seventh chords built from arbitrary pitch collections.
- Integrate with all existing transform layers (minSharedPitches, pitchCenterCycle).
- Integrate with inversions and modal interchange.
- Maintain backward compatibility with PoC1–PoC7.

This PoC demonstrates that the harmony engine can operate in **non‑diatonic**, **non‑modal**, and **non‑functional** harmonic spaces.

---

## **2. Scope**

### In Scope
- A new `"customScale"` field accepting an array of pitch classes.
- Chord construction using custom pitch‑class sets.
- Automatic derivation of chord tones using interval stacking.
- Integration with:
  - minSharedPitches (PoC2)
  - pitchCenterCycle (PoC6)
  - inversions (PoC4)
  - modal interchange (PoC7)
- One harmony track.

### Out of Scope
- Microtonal pitch classes (non‑12‑TET).
- Dynamic or time‑varying pitch‑class sets.
- Automatic chord‑quality naming.
- Geometric chord‑shape transforms.
- ML‑based pitch‑class set inference.
- UI or DAW integration.

These will be addressed in later PoCs.

---

## **3. JSON Format Update**

A new field is added under `harmony`:

```json
"customScale": [0, 1, 5, 7, 8]
```

### Behavior:
- If `"customScale"` is present:
  - It overrides `"scale"`, `"scaleName"`, `"root"`, and `"borrowMode"`.
- If omitted:
  - The system behaves exactly as in PoC7.

### Valid values:
- Any array of integers from 0–11.
- Length ≥ 3 (minimum for triads).
- Duplicates are ignored.

### Backward compatibility:
- `"scale"` (pitch‑class array) still works.
- `"scaleName"` + `"root"` still work.
- `"borrowMode"` still works unless `"customScale"` is present.

---

## **4. Chord Construction Rules for Custom Pitch‑Class Sets**

### 4.1 Degree interpretation
Scale degrees map to indices in the custom set:

```
degree 1 → customScale[0]
degree 2 → customScale[1]
degree 3 → customScale[2]
...
```

### 4.2 Triad construction
Triads are built by **stacking scale steps**, not fixed intervals:

```
triad = [
  customScale[i],        // root
  customScale[i+2],      // third (scale step)
  customScale[i+4]       // fifth (scale step)
]
```

Indices wrap around modulo the length of the custom set.

### 4.3 Seventh chord construction
Sevenths are built by stacking four scale steps:

```
seventh = [
  customScale[i],
  customScale[i+2],
  customScale[i+4],
  customScale[i+6]
]
```

### 4.4 No chord‑quality labels
Chord qualities (major/minor/diminished/etc.) are not used for custom sets.  
The engine simply stacks scale steps.

This is essential for supporting:
- synthetic scales,
- non‑functional harmony,
- geometric chord shapes.

---

## **5. Integration With Existing Features**

### 5.1 Transform layer ordering
Custom pitch‑class sets fit into the existing pipeline:

1. Build chord from customScale (if present).
2. Apply minSharedPitches (PoC2).
3. Apply pitchCenterCycle (PoC6).
4. Apply inversion (PoC4).
5. Generate events.

### 5.2 Modal interchange (PoC7)
Disabled when `"customScale"` is present.

### 5.3 Multiple scales (PoC5)
Disabled when `"customScale"` is present.

### 5.4 Inversions
Unchanged.

### 5.5 Event generation
Unchanged.

---

## **6. Implementation Outline**

### 6.1 Extend HarmonySpec
- Add `"customScale"` as an optional array of integers.
- Validate:
  - integers 0–11,
  - length ≥ 3,
  - no duplicates.

### 6.2 Add CustomScaleBuilder
Responsibilities:
- Normalize pitch classes.
- Provide degree → pitch‑class lookup.
- Provide stacked‑step chord construction.

### 6.3 Update HarmonyGenerator
- If `"customScale"` exists:
  - Use CustomScaleBuilder.
- Else:
  - Use ScaleBuilder or ModeBuilder (PoC5–PoC7).

### 6.4 Update transform pipeline
No changes required.

---

## **7. Test Cases**

Add unit tests for:

- Custom pentatonic scale (e.g., [0,2,4,7,9]).
- Custom synthetic scale (e.g., [0,1,5,7,8]).
- Custom symmetric scale (e.g., [0,3,6,9]).
- Triads and sevenths built from custom sets.
- Interaction with minSharedPitches.
- Interaction with pitchCenterCycle.
- Inversions applied to custom chords.
- Invalid customScale arrays.

---

## **8. Example JSON for Custom Pitch‑Class Sets**

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
    "customScale": [0, 1, 5, 7, 8],

    "progression": [
      { "time": 0, "degree": 1, "type": "triad" },
      { "time": 4, "degree": 3, "type": "seventh" },
      { "time": 8, "degree": 5, "type": "triad", "inversion": 1 },
      { "time": 12, "degree": 2, "type": "seventh", "inversion": 2 }
    ],

    "constraints": {
      "minSharedPitches": 1,
      "pitchCenterCycle": -3
    },

    "channel": 0,
    "velocity": 90,
    "duration": 4
  }
}
```

---

## **9. Expected Behavior**

For a custom pitch‑class set like:

```
[0, 1, 5, 7, 8]
```

The engine should:

- Build chords by stacking scale steps.
- Apply minSharedPitches adjustments.
- Apply pitchCenterCycle.
- Apply inversions.
- Output correct MIDI events.

This produces harmonic material that cannot be generated by traditional scales or modes, enabling synthetic, geometric, and experimental harmonic systems.
