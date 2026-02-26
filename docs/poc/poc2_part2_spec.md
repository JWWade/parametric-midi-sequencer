
# **POC2 Part 2 — Transform Layer Implementation Details (Triad Quality Preserved)**

## **1. Goal**
Extend PoC2 by implementing the **minimum shared‑pitch voice‑leading constraint** while **preserving triad quality** (major, minor, diminished) and **preserving the root**. The transform layer adjusts only the *voicing* of the third and fifth to satisfy the constraint.

---

## **2. Rules for Preserving Triad Quality**
These elements must remain fixed:
- Root pitch class (derived from scale degree).
- Chord quality (major, minor, diminished).
- Interval structure:
  - Major: 0, 4, 7
  - Minor: 0, 3, 7
  - Diminished: 0, 3, 6
- Chord size (always 3 notes).

Only the **third** and **fifth** may be adjusted by ±1 semitone, but must remain recognizable as a third and fifth relative to the root.

---

## **3. Constraint Logic**
For each chord after the first:

1. Compute shared pitch classes with the previous chord.
2. If shared ≥ `minSharedPitches`, keep the chord unchanged.
3. If shared < required:
   - Attempt minimal adjustments to the third or fifth.
   - Try adjustments in this order:
     1. Lower the third by 1 semitone.
     2. Raise the third by 1 semitone.
     3. Lower the fifth by 1 semitone.
     4. Raise the fifth by 1 semitone.
4. After each adjustment:
   - Check if triad quality is preserved.
   - Check if shared pitch count increases.
   - Prefer adjustments that remain inside the scale.
5. Stop as soon as the constraint is satisfied.

If no adjustment satisfies the constraint, fall back to the original triad.

---

## **4. Adjustment Validity Rules**
An adjustment is valid only if:
- The root stays unchanged.
- The adjusted third remains within ±1 semitone of its original interval.
- The adjusted fifth remains within ±1 semitone of its original interval.
- The chord still contains exactly 3 distinct pitch classes.
- The adjustment increases shared pitch count.

---

## **5. Implementation Outline**

### **5.1 New Class**
Create a new class:

`HarmonyTransform_MinSharedPitches`

Responsibilities:
- Accept initial triads.
- Apply the constraint.
- Return transformed triads.

### **5.2 Required Helpers**
Add small helper functions:
- `IntersectPitchClasses(chordA, chordB)`
- `AdjustPitchClass(pc, semitoneDelta)`
- `IsScaleTone(pc, scale)`
- `IsValidTriad(root, third, fifth, quality)`

### **5.3 Transform Pipeline**
In `HarmonyGenerator` (or equivalent):
- After generating initial triads:
  - If `constraints.minSharedPitches` exists:
    - Pass triads through the transform.
  - Then convert to events.

---

## **6. Debugging Support**
Add an optional debug flag (e.g., `--debug-harmony`) that prints:

```
Before: [0,4,7]
After:  [0,5,7]
Shared: 2
```

This helps verify transform behavior.

---

## **7. Test Cases**
Add unit tests for:

- I → IV with `minSharedPitches = 2`
- V → vi with `minSharedPitches = 1`
- vii° → I with `minSharedPitches = 2`
- Cases where no adjustment is needed
- Cases where adjustment is impossible

---

## **8. Expected Behavior Example**
Input:
- C major → F major → G major → C major
- `minSharedPitches = 2`

Output (example):
- C major: {0,4,7}
- F major adjusted: {0,5,7}
- G major adjusted: {0,2,7}
- C major unchanged: {0,4,7}
