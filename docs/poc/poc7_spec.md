
---

# **poc7_spec.md — Modal Interchange for Harmony Engine**

This proof‑of‑concept introduces **modal interchange**, allowing the harmony engine to borrow chords from **parallel modes** of the current key. This expands harmonic color while preserving the architecture established in PoC1–PoC6. Modal interchange is implemented as a controlled, explicit mechanism in the JSON spec, ensuring predictable behavior and compatibility with all existing transform layers.

---

## **1. Purpose**

Modal interchange enables the harmony engine to:

- Borrow chords from parallel modes (e.g., C major borrowing from C minor, C Dorian, C Phrygian, etc.).
- Preserve the existing scale-degree workflow.
- Preserve chord quality rules within each borrowed mode.
- Integrate cleanly with the minSharedPitches transform (PoC2).
- Integrate with pitchCenterCycle (PoC6).
- Integrate with inversions (PoC4).
- Maintain backward compatibility with PoC5 multiple-scale support.

This PoC demonstrates that the harmony engine can operate across multiple modal layers while keeping the core architecture stable.

---

## **2. Scope**

### In Scope
- Borrowing chords from **parallel modes**:
  - Ionian (major)
  - Aeolian (natural minor)
  - Dorian
  - Phrygian
  - Lydian
  - Mixolydian
  - Locrian
- JSON support for `"borrowMode"` per chord.
- Automatic chord-quality derivation from the borrowed mode.
- Integration with all previous transform layers.
- One harmony track.

### Out of Scope
- Borrowing from harmonic/melodic minor.
- Borrowing from non-parallel modes (e.g., modal modulation).
- Borrowing entire progressions.
- Automatic or ML-based mode selection.
- Geometric modal blending.
- UI or DAW integration.

These will be addressed in later PoCs.

---

## **3. JSON Format Update**

Each progression entry may now include a `"borrowMode"` field:

```json
{ "time": 4, "degree": 4, "type": "triad", "borrowMode": "phrygian" }
```

### Valid values:
- `"ionian"` (major)
- `"aeolian"` (natural minor)
- `"dorian"`
- `"phrygian"`
- `"lydian"`
- `"mixolydian"`
- `"locrian"`

### Behavior:
- If `"borrowMode"` is present:
  - Construct the chord using the **parallel mode** of the current root.
- If omitted:
  - Use the primary scale defined in PoC5.

### Backward compatibility:
- `"scale"` or `"scaleName"` + `"root"` still define the primary mode.
- `"borrowMode"` overrides only for that chord.

---

## **4. Modal Scale Definitions**

All modes are defined as rotations of the major scale:

| Mode | Intervals (semitones) |
|------|------------------------|
| Ionian | 0,2,4,5,7,9,11 |
| Dorian | 0,2,3,5,7,9,10 |
| Phrygian | 0,1,3,5,7,8,10 |
| Lydian | 0,2,4,6,7,9,11 |
| Mixolydian | 0,2,4,5,7,9,10 |
| Aeolian | 0,2,3,5,7,8,10 |
| Locrian | 0,1,3,5,6,8,10 |

Given root pitch class `R`, the modal scale is:

```
scale[i] = (R + interval[i]) % 12
```

---

## **5. Chord Quality Rules for Modal Interchange**

Chord qualities are derived from the borrowed mode’s scale degrees.

### Example: C Phrygian triads
- i: minor
- II♭: major
- III♭: major
- iv: minor
- v°: diminished
- VI♭: major
- VII♭: major

### Seventh chords follow the same pattern:
- Minor 7 for minor degrees
- Major 7 for major degrees
- Dominant 7 for Mixolydian V
- Half-diminished 7 for Locrian ii°
- Diminished 7 is out of scope for this PoC

---

## **6. Integration With Existing Features**

### 6.1 Transform layer ordering
Modal interchange fits into the existing pipeline:

1. Build chord from primary or borrowed mode.
2. Apply minSharedPitches (PoC2).
3. Apply pitchCenterCycle (PoC6).
4. Apply inversion (PoC4).
5. Generate events.

### 6.2 Multiple scales (PoC5)
The primary scale still defines:
- default chord qualities,
- default scale degrees,
- default pitch-class set.

Borrowed chords override only for the specific progression entry.

### 6.3 Inversions
Inversions apply after modal interchange and transforms.

### 6.4 Event generation
Unchanged.

---

## **7. Implementation Outline**

### 7.1 Extend HarmonySpec
- Add `"borrowMode"` as an optional string field.
- Validate mode names.

### 7.2 Add ModeBuilder module
Responsibilities:
- Convert `"borrowMode"` + `"root"` → modal pitch-class array.
- Provide scale-degree → pitch-class lookup.
- Provide scale-degree → chord-quality lookup.

### 7.3 Update HarmonyGenerator
- For each chord:
  - If `"borrowMode"` exists:
    - Use ModeBuilder instead of ScaleBuilder.
  - Else:
    - Use primary scale (PoC5).

### 7.4 Update transform pipeline
No changes required.

---

## **8. Test Cases**

Add unit tests for:

- Borrowing iv from parallel minor in a major key.
- Borrowing ♭II from Phrygian.
- Borrowing ♭VII from Mixolydian.
- Borrowing chords with inversions.
- Borrowing chords with minSharedPitches.
- Borrowing chords with pitchCenterCycle.
- Mixed triad + seventh sequences.
- Invalid mode names.

---

## **9. Example JSON for Modal Interchange**

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
      { "time": 0, "degree": 1, "type": "seventh" },
      { "time": 4, "degree": 4, "type": "triad", "borrowMode": "lydian" },
      { "time": 8, "degree": 2, "type": "triad", "borrowMode": "phrygian" },
      { "time": 12, "degree": 7, "type": "triad", "borrowMode": "mixolydian" }
    ],

    "constraints": {
      "minSharedPitches": 2,
      "pitchCenterCycle": 1
    },

    "channel": 0,
    "velocity": 90,
    "duration": 4
  }
}
```

---

## **10. Expected Behavior**

For a progression like:

- Cmaj7 (Ionian)
- F Lydian (borrowed #4)
- D♭ major (borrowed ♭II from Phrygian)
- B♭ major (borrowed ♭VII from Mixolydian)

The engine should:

- Build the correct modal scales.
- Construct correct triads and sevenths from borrowed modes.
- Apply minSharedPitches adjustments.
- Apply pitchCenterCycle.
- Apply inversions.
- Output correct MIDI events.

This produces a harmonically rich, modal‑interchange progression consistent with modern film scoring, jazz, and contemporary classical practice.
