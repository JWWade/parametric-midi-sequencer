
---

# **poc10_spec.md — First UI Proof‑of‑Concept (Revised Based on UX Priorities)**

This milestone introduces the earliest version of a user interface for the Parametric MIDI Sequencer. The goal is to define **what the user should see**, **what the user should not see**, and **what minimal interactive slice** will validate the architecture and workflow. This version reflects your clarified preferences: a Windows Forms UI, a minimal surface area, and no exposure of cryptic MIDI internals.

---

## **1. Purpose**

The first UI PoC should:

- Provide a minimal, visual entry point into the system.
- Surface only the *highest‑level* musical concepts.
- Hide all low‑level or cryptic engine details (e.g., PPQ, raw MIDI values).
- Allow the user to load a JSON file, view a clean summary, and generate MIDI.
- Display a readable event summary without exposing internal structures.
- Establish the UI architecture for future PoCs.

This PoC is about **UX clarity and conceptual framing**, not feature completeness.

---

## **2. Scope**

### In Scope
- Windows Forms UI (chosen for speed and simplicity).
- File picker for JSON input.
- Display of parsed harmony structure (high‑level only).
- Display of transform layers (read‑only).
- “Generate MIDI” button.
- Output path selector.
- Human‑readable event summary (not raw MIDI).
- A single window.

### Out of Scope
- Editing JSON.
- Editing harmony or transforms.
- Real‑time playback.
- Visualizations (chromatic circle, spiral, etc.).
- Multi‑track editing.
- Any exposure of PPQ, ticks, raw MIDI bytes, or scheduler internals.

---

## **3. What the UI *Should* Surface**

### 3.1 High‑level musical summary
The UI should show only the information a composer cares about:

- **Scale / Mode / Custom Scale**  
  e.g., “Scale: C major” or “Custom scale: [0,2,3,7,8]”

- **Number of chords in the progression**  
  This replaces the ambiguous “Chords: 4 items.”  
  It now explicitly means: **the number of chord events in the progression**, not the number of notes per chord.

- **Transform layers detected**  
  e.g., “Transforms: minSharedPitches=2, pitchCenterCycle=1, shapeTransform=reflect(axis=0)”

- **Inversions used**  
  e.g., “Inversions: present (3 chords)”

### 3.2 Generation controls
- “Generate MIDI” button.
- Output path selector.

### 3.3 Output summary
A clean, readable summary such as:

- “Generated 48 musical events”
- “First 10 events (human‑readable):  
  - Time 0: Cmaj7  
  - Time 4: Fmaj7 (1st inversion)  
  - Time 8: G7 (2nd inversion)  
  - …”

No raw MIDI bytes, no PPQ, no ticks.

---

## **4. What the UI *Should NOT* Surface**

### 4.1 No cryptic MIDI internals
The UI must not show:

- PPQ  
- Ticks  
- Raw MIDI bytes  
- MIDI status codes  
- Note‑on/note‑off hex values  
- Scheduler internals  
- Engine debug logs

These remain internal and invisible.

### 4.2 No low‑level harmony details
The UI must not show:

- Pitch‑class math  
- Transform pipeline ordering  
- Raw JSON schemas  
- Internal chord objects  
- Debug printouts  

### 4.3 No editing capabilities (yet)
- No chord editing  
- No transform editing  
- No scale editing  
- No drag‑and‑drop  

### 4.4 No visualizations (yet)
- No chromatic circle  
- No geometric diagrams  
- No voice‑leading graphs  

These belong in later PoCs.

---

## **5. UI Layout (Windows Forms)**

### Left Panel
- “Load JSON File” button  
- File path display  
- Summary section:  
  - Scale / Mode / Custom Scale  
  - Number of chords  
  - Transform layers present  
  - Inversions present  

### Right Panel
- “Generate MIDI” button  
- Output path selector  
- Human‑readable event summary  
- Status messages  

### Bottom Panel
- Log output (only high‑level messages, not engine internals)

---

## **6. Implementation Outline**

- Create a new Windows Forms project.
- Add JSON loading using existing HarmonySpec models.
- Display parsed summary in the left panel.
- Call the existing sequencer pipeline.
- Display human‑readable event summaries.
- Save MIDI to user‑selected path.
- Handle errors gracefully.

---

## **7. Expected Behavior**

The PoC10 UI should:

- Load any PoC1–PoC9 JSON file.
- Display a clean, minimal musical summary.
- Run the full harmony engine.
- Export a MIDI file.
- Hide all internal complexity.
- Establish the foundation for future UI PoCs.
