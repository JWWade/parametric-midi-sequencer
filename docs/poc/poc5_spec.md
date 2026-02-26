
---

# **poc5_spec.md — Multiple Scale Support for Harmony Engine**

This proof‑of‑concept extends the harmony engine to support **multiple scales**, enabling harmonic generation in any major or minor key and laying the groundwork for modal interchange, custom pitch‑class sets, and non‑diatonic workflows in future milestones. The system continues to use the architecture established in PoC1–PoC4.

---

## **1. Purpose**

This milestone introduces the ability to:

- Specify **any major or natural minor scale** in the harmony JSON.
- Automatically derive scale degrees, triad qualities, and seventh‑chord qualities from the selected scale.
- Apply existing transform layers (minSharedPitches) and inversion logic to chords in any key.
- Maintain backward compatibility with earlier PoCs.

This PoC demonstrates that the harmony engine can operate in arbitrary keys without architectural changes.

---

## **2. Scope**

### In Scope
- Major scales in all 12 keys.
- Natural minor scales in all 12 keys.
- JSON support for `"scaleName"` and `"root"`.
- Automatic generation of scale pitch‑class sets.
- Automatic mapping of scale degrees to chord qualities.
- Integration with PoC2 transform layer.
- Integration with PoC4 inversion logic.
- One harmony track.

### Out of Scope
- Harmonic minor and melodic minor.
- Modes (Dorian, Phrygian, etc.).
- Custom pitch‑class sets.
- Non‑diatonic chords.
- Modal interchange.
- Scale modulation within a progression.
- UI or DAW integration.

These will be addressed in later PoCs.

---

## **3. JSON Format Update**

The harmony section now supports two new fields:

```json
"scaleName": "major",
"root": "D"
```

### Valid values:
- `"scaleName"`: `"major"` or `"minor"`
- `"root"`: `"C"`, `"C#"`, `"Db"`, `"D"`, `"D#"`, `"Eb"`, … (all 12 chromatic roots)

### Backward compatibility:
If `"scale"` (pitch‑class array) is provided, it overrides `"scaleName"` and `"root"`.

If neither is provided, default to C major.

---

## **4. Scale Construction Rules**

### 4.1 Major scale intervals
`[0, 2, 4, 5, 7, 9, 11]`

### 4.2 Natural minor intervals
`[0, 2, 3, 5, 7, 8, 10]`

### 4.3 Root transposition
Given root pitch class `R`, the scale is:

```
scale[i] = (R + interval[i]) % 12
```

Example: D major  
Root = 2  
Intervals = [0,2,4,5,7,9,11]  
Scale = [2,4,6,7,9,11,1]

---

## **5. Chord Quality Rules for Multiple Scales**

### 5.1 Major scale
Same as PoC1–PoC3.

### 5.2 Natural minor scale
Triads:

| Degree | Quality |
|--------|----------|
| 1 | minor |
| 2 | diminished |
| 3 | major |
| 4 | minor |
| 5 | minor |
| 6 | major |
| 7 | major |

Seventh chords:

| Degree | Quality | Intervals |
|--------|----------|-----------|
| 1 | minor 7 | 0,3,7,10 |
| 2 | half‑diminished 7 | 0,3,6,10 |
| 3 | major 7 | 0,4,7,11 |
| 4 | minor 7 | 0,3,7,10 |
| 5 | minor 7 | 0,3,7,10 |
| 6 | major 7 | 0,4,7,11 |
| 7 | dominant 7 | 0,4,7,10 |

These qualities must remain fixed during transform operations.

---

## **6. Integration With Existing Features**

### 6.1 Transform layer (PoC2)
The minSharedPitches transform operates on pitch classes after scale‑based chord construction.

### 6.2 Inversions (PoC4)
Inversions are applied after transforms.

### 6.3 Event generation
Unchanged.

### 6.4 Scheduler
Unchanged.

---

## **7. Implementation Outline**

### 7.1 Extend HarmonySpec
- Add `"scaleName"` and `"root"` fields.
- Add validation for scale names and roots.
- Add helper to convert root string → pitch class.

### 7.2 Add ScaleBuilder module
Responsibilities:
- Convert `"scaleName"` + `"root"` → pitch‑class array.
- Provide scale degree → pitch‑class lookup.
- Provide scale degree → chord quality lookup.

### 7.3 Update HarmonyGenerator
- Replace hardcoded C major logic with ScaleBuilder.
- Generate triads and sevenths using scale‑derived qualities.

### 7.4 Update transform and inversion modules
- No changes required; they operate on pitch classes.

---

## **8. Test Cases**

Add unit tests for:

- D major triads and sevenths.
- A minor triads and sevenths.
- Transform layer behavior in non‑C scales.
- Inversions in non‑C scales.
- Mixed triad + seventh sequences.
- Invalid scale names or roots.
- Backward compatibility with `"scale"` array.

---

## **9. Example JSON for Multiple Scales**

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
    "root": "D",

    "progression": [
      { "time": 0, "degree": 1, "type": "seventh", "inversion": 0 },
      { "time": 4, "degree": 4, "type": "triad", "inversion": 1 },
      { "time": 8, "degree": 5, "type": "seventh", "inversion": 2 },
      { "time": 12, "degree": 1, "type": "triad", "inversion": 0 }
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

## **10. Expected Behavior**

For a progression in D major:

- Dmaj7 → Gmaj → A7 → Dmaj  
- with inversions and minSharedPitches applied

The engine should:

- Build the correct D major scale.
- Construct correct triads and sevenths.
- Apply transform layer.
- Apply inversions.
- Output correct MIDI events.
