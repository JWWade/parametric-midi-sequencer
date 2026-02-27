
---

# **poc9_spec.md — Geometric Chord‑Shape Transforms**

This proof‑of‑concept introduces **geometric chord‑shape transforms**, enabling the harmony engine to manipulate chords using geometric operations on pitch‑class sets. These transforms operate on the *shape* of a chord rather than its functional identity, allowing for non‑functional harmonic motion, synthetic transformations, and the beginnings of your geometric harmonic language.

---

## **1. Purpose**

Geometric chord‑shape transforms allow the harmony engine to:

- Treat chords as **geometric objects** on the chromatic circle.
- Apply **shape‑preserving transformations** such as rotation, reflection, and expansion.
- Operate independently of scale, mode, or functional harmony.
- Integrate with all previous transform layers (minSharedPitches, pitchCenterCycle).
- Support custom pitch‑class sets (PoC8).
- Produce harmonic motion that reflects your geometric diagrams.

This PoC demonstrates that the harmony engine can manipulate harmonic material using **geometric rules**, not just functional or modal rules.

---

## **2. Scope**

### In Scope
- A new `"shapeTransform"` field under `constraints`.
- Three geometric transforms:
  - `"rotate"` — rotate the chord shape by N semitones.
  - `"reflect"` — reflect the chord shape across a chosen axis.
  - `"expand"` — expand or contract the chord shape around its centroid.
- Integration with:
  - minSharedPitches (PoC2)
  - pitchCenterCycle (PoC6)
  - inversions (PoC4)
  - modal interchange (PoC7)
  - custom pitch‑class sets (PoC8)
- One harmony track.

### Out of Scope
- Continuous geometric morphing.
- Spiral‑based transforms.
- Multi‑layer geometric blending.
- ML‑guided geometric transforms.
- UI visualization of geometric shapes.

These will be addressed in later PoCs.

---

## **3. JSON Format Update**

A new `"shapeTransform"` object is added under `constraints`:

```json
"constraints": {
  "minSharedPitches": 1,
  "pitchCenterCycle": 2,
  "shapeTransform": {
    "type": "rotate",
    "amount": 3
  }
}
```

### Valid transform types:
- `"rotate"` — rotate all chord tones by N semitones.
- `"reflect"` — reflect chord tones across an axis.
- `"expand"` — expand or contract chord intervals.

### Parameters:
- `"amount"` — integer (for rotate, expand).
- `"axis"` — pitch class (for reflect).

### Backward compatibility:
If `"shapeTransform"` is omitted, no geometric transform is applied.

---

## **4. Transform Definitions**

### 4.1 Rotate
Uniform rotation of the chord shape:

```
pc' = (pc + amount) % 12
```

This is similar to pitchCenterCycle but applied **after** all other transforms, and intended to preserve **shape**, not functional identity.

### 4.2 Reflect
Reflection across an axis pitch class A:

```
pc' = (2*A - pc) % 12
```

This mirrors the chord across a vertical line on the chromatic circle.

### 4.3 Expand
Expansion or contraction around the chord’s centroid C:

1. Compute centroid:
   ```
   C = average(pitchClasses)
   ```
2. Expand each pitch class:
   ```
   pc' = C + (pc - C) * amount
   ```
3. Wrap into 0–11.

`amount > 1` expands the shape;  
`0 < amount < 1` contracts it.

This is the first transform that changes **intervallic structure** while preserving **shape ratios**.

---

## **5. Transform Layer Ordering**

Geometric transforms occur **after** all functional transforms:

1. Build chord (triad/seventh/custom).
2. Apply minSharedPitches (PoC2).
3. Apply pitchCenterCycle (PoC6).
4. Apply modal interchange (PoC7).
5. Apply geometric shapeTransform (PoC9).
6. Apply inversion (PoC4).
7. Generate events.

This ordering ensures:
- Functional harmony is established first.
- Geometric transforms modify the chord shape.
- Inversions reorder the final shape.

---

## **6. Implementation Outline**

### 6.1 Extend HarmonySpec
- Add `"shapeTransform"` as an optional object.
- Validate `"type"` and parameters.

### 6.2 Add new transform class
Create:

`HarmonyTransform_GeometricShape`

Responsibilities:
- Accept chords after functional transforms.
- Apply rotate, reflect, or expand.
- Return transformed pitch‑class sets.

### 6.3 Update transform pipeline
In `HarmonyGenerator`:

- After pitchCenterCycle and modal interchange:
  - If `"shapeTransform"` exists, apply geometric transform.
- Then apply inversion.

### 6.4 Update event generation
No changes required.

---

## **7. Test Cases**

Add unit tests for:

- Rotate triads and sevenths by various amounts.
- Reflect chords across axes 0, 3, 6.
- Expand and contract chords.
- Interaction with custom pitch‑class sets.
- Interaction with minSharedPitches.
- Interaction with pitchCenterCycle.
- Interaction with modal interchange.
- Inversions applied after geometric transforms.
- Invalid transform types or parameters.

---

## **8. Example JSON for Geometric Chord‑Shape Transforms**

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
    "customScale": [0, 2, 3, 7, 8],

    "progression": [
      { "time": 0, "degree": 1, "type": "triad" },
      { "time": 4, "degree": 3, "type": "seventh" },
      { "time": 8, "degree": 5, "type": "triad", "inversion": 1 },
      { "time": 12, "degree": 2, "type": "seventh", "inversion": 2 }
    ],

    "constraints": {
      "minSharedPitches": 1,
      "pitchCenterCycle": -2,
      "shapeTransform": {
        "type": "reflect",
        "axis": 0
      }
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
[0, 2, 3, 7, 8]
```

And a transform like:

```
shapeTransform: { type: "reflect", axis: 0 }
```

The engine should:

- Build chords from the custom set.
- Apply minSharedPitches.
- Apply pitchCenterCycle.
- Apply modal interchange (if present).
- Reflect the chord shape across axis 0.
- Apply inversions.
- Output correct MIDI events.

This produces harmonic motion that reflects your geometric diagrams—triangles flipping, shapes rotating, and chord structures transforming in a spatially meaningful way.
