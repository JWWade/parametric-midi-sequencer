
---

# **poc11_spec.md — Editable Progression UI**

This milestone introduces the first editable component in the Parametric MIDI Sequencer UI: the **harmony progression**. The goal is to let the user modify the sequence of chords (degree, type, inversion, borrowMode) in a controlled, high‑level, musically meaningful way—without exposing low‑level engine details or overwhelming the interface.

This PoC establishes the pattern for future editable UI elements (transforms, scales, geometry), so the design emphasizes clarity, constraints, and guardrails.

---

## **1. Purpose**

The editable progression UI should:

- Allow users to view and modify the chord progression in a structured, musical way.
- Keep the editing surface intentionally small and safe.
- Prevent invalid or nonsensical edits.
- Maintain the separation between *musical intent* (UI) and *technical detail* (engine).
- Preserve all PoC1–PoC10 behaviors.
- Provide a foundation for future editing features.

This PoC is about **controlled editing**, not full composition.

---

## **2. Scope**

### In Scope
- A progression editor panel in the UI.
- Ability to:
  - Add a chord event.
  - Remove a chord event.
  - Edit an existing chord event’s:
    - time
    - degree
    - type (triad/seventh)
    - inversion
    - borrowMode (optional)
- Validation of edits.
- Automatic re‑rendering of the summary panel.
- Regeneration of MIDI using the edited progression.

### Out of Scope
- Editing transforms.
- Editing scales or custom pitch‑class sets.
- Editing meta fields (tempo, steps, bars, ppq).
- Drag‑and‑drop timeline editing.
- Graphical chord visualization.
- Real‑time playback.
- Multi‑track editing.

These belong in PoC12+.

---

## **3. What the UI *Should* Surface for Progression Editing**

### 3.1 A simple, table‑like editor
Each chord event appears as a row with the following editable fields:

- **Time** — integer (step index)
- **Degree** — integer (1–7)
- **Type** — dropdown: triad / seventh
- **Inversion** — dropdown: valid inversions based on type
- **Borrow Mode** — dropdown: none / ionian / dorian / phrygian / lydian / mixolydian / aeolian / locrian

### 3.2 Add / Remove controls
- “Add chord” button (appends a new row).
- “Remove” button on each row.

### 3.3 High‑level musical validation
- Time must be within the sequence length.
- Degree must be valid for the current scale or custom set.
- Inversion must match chord type.
- borrowMode must be a valid mode name.
- No duplicate times unless explicitly allowed.

### 3.4 Automatic summary updates
When the user edits the progression:

- The summary panel updates immediately.
- The event preview updates after clicking “Generate MIDI”.

---

## **4. What the UI *Should NOT* Surface**

### 4.1 No low‑level engine details
- No pitch‑class arrays.
- No raw chord objects.
- No transform pipeline ordering.
- No MIDI internals (PPQ, ticks, bytes).

### 4.2 No advanced editing
- No editing of transform layers.
- No editing of scale or customScale.
- No editing of meta fields.
- No geometric editing.

### 4.3 No free‑form JSON editing
The user interacts only through structured UI controls.

---

## **5. UI Layout Additions (Windows Forms)**

### New Center Panel: “Progression Editor”
A grid with columns:

- Time (numeric up/down)
- Degree (numeric up/down)
- Type (dropdown)
- Inversion (dropdown)
- Borrow Mode (dropdown)
- Remove (button)

Below the grid:

- “Add Chord” button

### Updated Right Panel
- “Generate MIDI” button
- Output path selector
- Event summary

### Updated Left Panel
- File path
- Scale summary
- Transform summary
- Chord count (auto‑updated)

---

## **6. Implementation Outline**

### 6.1 Extend the UI project
- Add a new UserControl or panel for the progression editor.
- Bind the grid to an in‑memory representation of the progression.

### 6.2 Add validation logic
- Validate each field on edit.
- Highlight invalid entries.
- Prevent MIDI generation if errors exist.

### 6.3 Update HarmonySpec integration
- After editing, rebuild the HarmonySpec object from the grid.
- Pass the updated spec to the engine.

### 6.4 Update summary panel
- Recompute chord count.
- Recompute inversion presence.
- Recompute transform summary (unchanged).

### 6.5 Update event generation
- Use the edited progression.
- Display updated event summary.

---

## **7. Test Cases**

- Load a JSON file, edit a chord, generate MIDI.
- Add a new chord at a valid time.
- Add a chord at an invalid time (should show error).
- Change a triad to a seventh and verify inversion options update.
- Add a chord with a borrowMode and verify it is applied.
- Remove a chord and verify the summary updates.
- Generate MIDI after multiple edits.
- Load a new file and verify the editor resets.

---

## **8. Example User Flow**

1. User loads a JSON file.
2. UI displays:
   - Scale: G major
   - Chords: 6
   - Transforms: minSharedPitches=1, shapeTransform=expand(1.5)
3. User edits the progression:
   - Changes degree 3 → degree 6
   - Adds a new chord at time 14
   - Sets borrowMode to “phrygian”
4. User clicks “Generate MIDI”.
5. UI displays:
   - “Generated 32 musical events”
   - First 10 events (human‑readable)

---

## **9. Expected Behavior**

The PoC11 UI should:

- Allow safe, structured editing of the progression.
- Keep the UI musical and high‑level.
- Prevent invalid edits.
- Regenerate MIDI using the updated progression.
- Maintain all PoC1–PoC10 behaviors.
- Establish the pattern for future editable UI components.
