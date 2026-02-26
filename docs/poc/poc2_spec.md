
# **poc2_spec.md — Transform Layer: Minimum Shared‑Pitch Voice‑Leading Constraint**

A minimal extension to the Harmony PoC that introduces the first *transform layer* in the Parametric MIDI Sequencer. This layer modifies an initial chord progression so that each chord shares at least **N pitch classes** with the previous chord. The goal is to demonstrate how post‑processing transforms can be layered onto the harmony module without modifying the core scheduling engine.

---

## **1. Purpose of This PoC**

This PoC adds a small but meaningful generative behavior:

- Start with a scale‑degree‑based triad progression (from PoC #1).
- Apply a **voice‑leading constraint** requiring each chord to share at least **N** pitch classes with the previous chord.
- Adjust chords minimally to satisfy the constraint.
- Output the transformed chords as MIDI events using the existing scheduler.

This proves that the system can support **transform layers** that operate on harmony before event generation.

---

## **2. Scope**

### In Scope
- One harmony track.
- One scale (C major).
- Triads only.
- Scale‑degree progression as input.
- A single transform layer: **minSharedPitches**.
- Minimal chord adjustments (add, remove, or replace one pitch).
- JSON → harmony → transform → events → scheduler → MIDI.

### Out of Scope
- Seventh chords.
- Multiple scales.
- Multiple transform layers.
- ML‑based transitions.
- Rhythm/harmony interaction.
- Geometry, FSMs, spiral logic.
- UI or DAW integration.

This PoC is intentionally narrow.

---

## **3. JSON Format (Extended Harmony Section)**

A new optional field is added under `harmony`:

```json
"harmony": {
  "scale": [0, 2, 4, 5, 7, 9, 11],
  "progression": [
    { "time": 0, "degree": 1, "type": "triad" },
    { "time": 4, "degree": 4, "type": "triad" },
    { "time": 8, "degree": 5, "type": "triad" },
    { "time": 12, "degree": 1, "type": "triad" }
  ],
  "constraints": {
    "minSharedPitches": 2
  },
  "channel": 0,
  "velocity": 90,
  "duration": 4
}
```

### Field Definitions

| Field | Type | Description |
|-------|------|-------------|
| `constraints` | object | Optional transform layer configuration. |
| `constraints.minSharedPitches` | int | Minimum number of shared pitch classes between consecutive chords. |

If `constraints` is omitted, the harmony module behaves exactly like PoC #1.

---

## **4. Transform Layer Behavior**

The transform layer operates **after** the initial triads are generated but **before** event creation.

### 4.1 Initial Chord Generation
Same as PoC #1:

- Degree → scale index → root pitch class.
- Triad quality determined by major scale degree.
- Triad = root + third + fifth.

### 4.2 Constraint Enforcement
For each chord after the first:

1. Compute the intersection of pitch classes with the previous chord.
2. If `sharedCount >= minSharedPitches`, keep the chord unchanged.
3. If not, apply **minimal adjustments** until the constraint is satisfied.

### 4.3 Adjustment Rules
Adjustments must be:

- Minimal (one pitch change at a time).
- Local (±1 or ±2 semitone changes preferred).
- Scale‑aware (prefer notes inside the scale).
- Structure‑preserving (keep triad size = 3 notes).

Allowed operations:

- Replace one chord tone with a neighboring pitch class.
- Raise or lower a chord tone by a semitone.
- Swap one chord tone for a pitch from the previous chord.

Forbidden operations:

- Changing the root.
- Changing the chord size.
- Changing the timing.

### 4.4 Example

Input progression (C major):

- I → IV → V → I  
- C major → F major → G major → C major

With `minSharedPitches = 2`:

- C major = {0,4,7}
- F major = {5,9,0} → shares only {0} → adjust to {0,5,7}
- G major = {7,11,2} → shares only {7} → adjust to {7,0,2}
- C major = {0,4,7} → shares {0,7} → OK

The output progression is smoother and more voice‑leading‑friendly.

---

## **5. Integration With Existing Sequencer**

### 5.1 Harmony Module Flow

1. Parse `harmony` JSON.
2. Generate initial triads.
3. Apply transform layer if `constraints` is present.
4. Convert final chords into event objects.
5. Pass events to existing scheduler.

### 5.2 No Changes to Core Engine
The scheduler remains untouched.

The transform layer is purely a **pre‑event** operation.

---

## **6. Example PoC JSON File**

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
      { "time": 0, "degree": 1, "type": "triad" },
      { "time": 4, "degree": 4, "type": "triad" },
      { "time": 8, "degree": 5, "type": "triad" },
      { "time": 12, "degree": 1, "type": "triad" }
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

## **7. Success Criteria**

The PoC is successful if:

- The sequencer accepts a JSON file with a `constraints` section.
- The harmony module generates initial triads.
- The transform layer adjusts chords to satisfy `minSharedPitches`.
- The final chords are converted to events.
- The scheduler renders them correctly.
- The output MIDI file reflects the transformed progression.

This demonstrates that the system can support **post‑processing transforms** without modifying the core engine.

---

## **8. Next Steps After PoC #2**

Once this transform layer is working, the next logical steps are:

- Add seventh chords.
- Add multiple scales.
- Add a second transform layer (e.g., pitch‑center cycling).
- Add rhythm/harmony interaction.
- Add geometric transforms (rotation, reflection).
- Add ML‑guided chord transitions.
