
---

# **poc4_spec.md — Chord Inversions for Harmony Engine**

This proof‑of‑concept extends the harmony engine by adding support for **chord inversions**. Inversions increase voice‑leading flexibility and musical realism while preserving the core architecture established in PoC1–PoC3. The system continues to use scale‑degree–based harmony, seventh‑chord support, and the PoC2 transform layer.

---

## **1. Purpose**

This milestone introduces **first**, **second**, and (for seventh chords) **third** inversions. The goal is to allow the user to specify inversions in the JSON progression and to ensure the harmony engine:

- Parses inversion requests.
- Applies inversion logic after chord construction.
- Preserves chord quality and root identity.
- Integrates with the existing minSharedPitches transform layer.
- Produces correct MIDI note events with reordered pitch classes.

This PoC demonstrates that the harmony engine can support more realistic harmonic textures without altering the core scheduler.

---

## **2. Scope**

### In Scope
- Inversions for triads: root, first, second.
- Inversions for seventh chords: root, first, second, third.
- JSON support for `"inversion"` field.
- Integration with PoC2 transform layer.
- One harmony track.
- One scale (C major).
- Event generation for reordered chord tones.

### Out of Scope
- Drop‑2, drop‑3 voicings.
- Spread voicings.
- Octave‑doubling logic.
- Voice‑leading–optimized inversion selection.
- ML‑based inversion prediction.
- Geometry‑based inversion rules.
- UI or DAW integration.

---

## **3. JSON Format Update**

Each progression entry may now include an `"inversion"` field:

```json
{ "time": 4, "degree": 5, "type": "seventh", "inversion": 2 }
```

### Valid values:
- Triads: `0` (root), `1` (first), `2` (second)
- Seventh chords: `0`, `1`, `2`, `3`

If omitted, default to `0` (root position).

---

## **4. Inversion Rules**

### 4.1 Triads
Given a triad `[root, third, fifth]`:

- **Root position (0):** `[root, third, fifth]`
- **First inversion (1):** `[third, fifth, root+12]`
- **Second inversion (2):** `[fifth, root+12, third+12]`

### 4.2 Seventh Chords
Given a seventh chord `[root, third, fifth, seventh]`:

- **Root position (0):** `[root, third, fifth, seventh]`
- **First inversion (1):** `[third, fifth, seventh, root+12]`
- **Second inversion (2):** `[fifth, seventh, root+12, third+12]`
- **Third inversion (3):** `[seventh, root+12, third+12, fifth+12]`

### 4.3 Pitch‑class preservation
Inversions reorder chord tones but do **not** change:
- pitch classes,
- chord quality,
- root identity.

---

## **5. Integration With Transform Layer**

The PoC2 transform layer must operate on **pitch classes before inversion**.

### Processing order:
1. Generate chord tones (triad or seventh).
2. Apply minSharedPitches transform to pitch classes.
3. Apply inversion to the transformed chord.
4. Convert to MIDI events.

This ensures:
- Transform logic remains consistent.
- Inversions do not interfere with constraint satisfaction.
- Output voicings reflect the transformed chord.

---

## **6. Implementation Outline**

### 6.1 Extend HarmonySpec
- Add `"inversion"` as an optional integer field.
- Validate inversion range based on chord type.

### 6.2 Extend HarmonyGenerator
- After constructing the chord and applying transforms:
  - Apply inversion logic.
  - Ensure octave adjustments maintain ascending order.

### 6.3 Update event generation
- Emit MIDI notes in the inverted order.
- Preserve velocity, duration, and channel.

### 6.4 Add helper functions
- `ApplyTriadInversion(chord, inversion)`
- `ApplySeventhInversion(chord, inversion)`
- `NormalizeAscending(chord)` (ensures correct octave stacking)

---

## **7. Test Cases**

Add unit tests for:

- Triad inversions: I → IV → V with inversions 0,1,2.
- Seventh inversions: Imaj7 → V7 → ii7 with inversions 0–3.
- Mixed triad + seventh sequences.
- Interaction with minSharedPitches transform.
- Invalid inversion values (should default to 0 or error gracefully).
- Cases where inversion does not change pitch classes but changes voicing.

---

## **8. Example JSON for Inversions**

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
      { "time": 0, "degree": 1, "type": "seventh", "inversion": 0 },
      { "time": 4, "degree": 4, "type": "seventh", "inversion": 1 },
      { "time": 8, "degree": 5, "type": "seventh", "inversion": 2 },
      { "time": 12, "degree": 1, "type": "seventh", "inversion": 3 }
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

- Imaj7 (root)  
- IVmaj7 (first inversion)  
- V7 (second inversion)  
- Imaj7 (third inversion)  

The engine should:

- Construct correct seventh chords.
- Apply minSharedPitches transform.
- Apply inversions.
- Output four‑note MIDI events with correct octave stacking.
- Preserve harmonic identity and smooth voice‑leading.
