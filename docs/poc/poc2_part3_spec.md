
# **POC2 Part 3 — Seventh Chords Extension (Post‑Transform Harmony Expansion)**

## **1. Goal**
Extend the harmony engine to support **seventh chords** in addition to triads, while keeping all PoC2 transform behavior intact. This milestone expands harmonic richness without introducing new transform types or altering the core scheduling engine.

The system must:
- Parse `"type": "seventh"` in the harmony progression.
- Generate correct seventh chords from scale degrees.
- Preserve chord quality (major 7, dominant 7, minor 7, half‑diminished 7).
- Apply the existing **minSharedPitches** transform *after* seventh chord generation.
- Convert the resulting chords into MIDI events using the existing scheduler.

---

## **2. Seventh Chord Quality Rules**
Seventh chords are derived from the major scale using standard diatonic qualities:

| Degree | Quality | Intervals (semitones) |
|--------|----------|------------------------|
| 1 | major 7 | 0, 4, 7, 11 |
| 2 | minor 7 | 0, 3, 7, 10 |
| 3 | minor 7 | 0, 3, 7, 10 |
| 4 | major 7 | 0, 4, 7, 11 |
| 5 | dominant 7 | 0, 4, 7, 10 |
| 6 | minor 7 | 0, 3, 7, 10 |
| 7 | half‑diminished 7 | 0, 3, 6, 10 |

These interval structures must remain fixed during transform operations.

---

## **3. JSON Format Update**
The `"type"` field in each progression entry may now be `"triad"` or `"seventh"`.

Example:

```json
{
  "time": 4,
  "degree": 5,
  "type": "seventh"
}
```

If `"type"` is omitted, default to `"triad"` for backward compatibility.

---

## **4. Integration With Transform Layer**
The transform layer from PoC2 Part 2 must operate on seventh chords exactly as it does on triads, with the following rules:

### Fixed elements:
- Root pitch class.
- Seventh chord quality (major 7, dominant 7, minor 7, half‑diminished 7).
- Interval structure (0, X, 7, Y).
- Chord size (always 4 notes).

### Adjustable elements:
- Only the **third**, **fifth**, or **seventh** may be adjusted by ±1 semitone.
- Adjustments must preserve the chord quality’s interval pattern.

### Constraint logic:
- Apply the same `minSharedPitches` rule.
- Try adjustments in this order:
  1. Lower the seventh.
  2. Raise the seventh.
  3. Lower the third.
  4. Raise the third.
  5. Lower the fifth.
  6. Raise the fifth.
- Stop as soon as the constraint is satisfied.

This ordering prioritizes the seventh because it is the most flexible tone in functional harmony and least likely to break the chord’s identity.

---

## **5. Implementation Outline**

### 5.1 Extend HarmonySpec
- Add `"seventh"` as a valid chord type.
- Add a helper to map degree → seventh chord quality.

### 5.2 Extend HarmonyGenerator
- Add seventh chord construction logic.
- Ensure seventh chords pass through the transform pipeline unchanged.

### 5.3 Update HarmonyTransform_MinSharedPitches
- Add support for 4‑note chords.
- Ensure adjustments preserve seventh chord quality.
- Use the new adjustment order.

### 5.4 Update event generation
- Emit 4 MIDI note events per chord instead of 3.

---

## **6. Test Cases**
Add unit tests for:

- Imaj7 → IVmaj7 with `minSharedPitches = 2`
- V7 → vi7 with `minSharedPitches = 1`
- viiø7 → Imaj7 with `minSharedPitches = 2`
- Mixed triad + seventh sequences
- Cases where no adjustment is needed
- Cases where adjustment is impossible

---

## **7. Example JSON for Seventh Chords**

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
    "scale": [0, 2, 4, 5, 7, 9, 11],

    "progression": [
      { "time": 0, "degree": 1, "type": "seventh" },
      { "time": 4, "degree": 4, "type": "seventh" },
      { "time": 8, "degree": 5, "type": "seventh" },
      { "time": 12, "degree": 1, "type": "seventh" }
    ],

    "constraints": {
      "minSharedPitches": 2
    },

    "channel": 0,
    "velocity": 90,
    "duration": 4
  }
}
```

---

## **8. Expected Behavior**
For a progression like:

- Imaj7 → IVmaj7 → V7 → Imaj7  
- with `minSharedPitches = 2`

The transform layer should:
- Preserve all seventh chord qualities.
- Adjust only non‑root tones.
- Produce smooth, voice‑led seventh chords.
