
# **poc3_spec.md — Seventh Chord Extension for Harmony Engine**

A focused extension to the Harmony PoC series that adds support for **seventh chords** while preserving the architectural principles established in PoC1 and PoC2. This milestone expands harmonic richness without introducing new transform types or modifying the core scheduling engine.

---

## **1. Purpose**

This proof‑of‑concept adds the ability to generate **seventh chords** from scale degrees in the harmony module. Seventh chords must:

- Be parsed from JSON (`"type": "seventh"`).
- Be constructed using diatonic seventh‑chord qualities.
- Pass through the existing **minSharedPitches** transform layer unchanged in quality.
- Produce four‑note MIDI events using the existing scheduler.

This PoC demonstrates that the harmony engine can grow in expressive power while remaining modular and stable.

---

## **2. Scope**

### In Scope
- Seventh chord generation from scale degrees.
- Major 7, dominant 7, minor 7, and half‑diminished 7 qualities.
- JSON support for `"type": "seventh"`.
- Integration with the PoC2 transform layer.
- One harmony track.
- One scale (C major).
- Event generation for four‑note chords.

### Out of Scope
- Altered sevenths (e.g., ♭7♭9, ♯11).
- Fully diminished seventh chords.
- Inversions.
- Multiple scales.
- Additional transform layers.
- ML‑based chord transitions.
- Rhythm/harmony interaction.
- UI or DAW integration.

---

## **3. Seventh Chord Quality Rules**

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

## **4. JSON Format Update**

The `"type"` field in each progression entry may now be `"triad"` or `"seventh"`.

Example:

```json
{ "time": 4, "degree": 5, "type": "seventh" }
```

If `"type"` is omitted, default to `"triad"` for backward compatibility.

---

## **5. Integration With Transform Layer**

The PoC2 transform layer must operate on seventh chords exactly as it does on triads, with the following rules:

### Fixed elements
- Root pitch class.
- Seventh chord quality (major 7, dominant 7, minor 7, half‑diminished 7).
- Interval structure (0, X, 7, Y).
- Chord size (always 4 notes).

### Adjustable elements
Only the **third**, **fifth**, or **seventh** may be adjusted by ±1 semitone, and only if the adjustment preserves the chord’s quality.

### Adjustment order
To satisfy `minSharedPitches`, attempt adjustments in this order:

1. Lower the seventh.
2. Raise the seventh.
3. Lower the third.
4. Raise the third.
5. Lower the fifth.
6. Raise the fifth.

Stop as soon as the constraint is satisfied.

This ordering reflects musical practice: the seventh is the most flexible tone and least likely to break the chord’s identity.

---

## **6. Implementation Outline**

### 6.1 Extend HarmonySpec
- Add `"seventh"` as a valid chord type.
- Add a helper to map degree → seventh chord quality.

### 6.2 Extend HarmonyGenerator
- Add seventh chord construction logic.
- Ensure seventh chords pass through the transform pipeline.

### 6.3 Update HarmonyTransform_MinSharedPitches
- Add support for 4‑note chords.
- Ensure adjustments preserve seventh chord quality.
- Use the new adjustment order.

### 6.4 Update event generation
- Emit 4 MIDI note events per chord instead of 3.

---

## **7. Test Cases**

Add unit tests for:

- Imaj7 → IVmaj7 with `minSharedPitches = 2`
- V7 → vi7 with `minSharedPitches = 1`
- viiø7 → Imaj7 with `minSharedPitches = 2`
- Mixed triad + seventh sequences
- Cases where no adjustment is needed
- Cases where adjustment is impossible

---

## **8. Example JSON for Seventh Chords**

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

## **9. Expected Behavior**

For a progression like:

- Imaj7 → IVmaj7 → V7 → Imaj7  
- with `minSharedPitches = 2`

The transform layer should:

- Preserve all seventh chord qualities.
- Adjust only non‑root tones.
- Produce smooth, voice‑led seventh chords.
- Output four‑note MIDI events.
