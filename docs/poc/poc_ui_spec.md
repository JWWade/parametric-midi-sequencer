
# **poc_ui_cleanup.md — UI Cleanup and Layout Refinement (Non‑Functional Refactor)**

**Purpose**  
Improve the clarity, consistency, and maintainability of the Parametric MIDI Sequencer UI without changing any functional behavior. This cleanup should make the PoC15 UI easier to read, easier to extend in future PoCs, and more visually coherent, while preserving the musical‑first, high‑level UX philosophy of the project.

---

## **1. Scope**

### **In Scope**
- Visual cleanup of the Windows Forms layout.
- Improved spacing, padding, and alignment.
- Grouping related controls into clearly labeled sections.
- Renaming controls for clarity and consistency.
- Improving readability of `.Designer.cs` files.
- Ensuring consistent fonts, margins, and control sizes.
- Reorganizing panels for better visual hierarchy.
- Ensuring the chromatic circle panel has clean borders and padding.
- Ensuring the progression editor grid columns are sized and labeled consistently.

### **Out of Scope**
- Any functional changes to the harmony engine.
- Any changes to JSON parsing or serialization.
- Any changes to transform logic.
- Any changes to MIDI generation.
- Any new UI features.
- Any removal of existing UI features.
- Any changes to PoC10–15 behavior.
- Any introduction of cryptic MIDI internals (PPQ, ticks, raw bytes).

---

## **2. Cleanup Goals**

### **2.1 Visual and Layout Improvements**
- Ensure each major conceptual area is grouped into a labeled section:
  - Scale / Mode  
  - Progression Editor  
  - Voice‑Leading Constraint  
  - Pitch‑Center Cycling  
  - Geometric Transform  
  - MIDI Generation  
  - Summary  
  - Status Log  
  - Chromatic Circle Visualization  
- Improve spacing and alignment so the UI feels intentional and readable.
- Ensure consistent padding around group boxes and panels.
- Ensure consistent label alignment and control spacing.

### **2.2 Naming and Maintainability**
- Rename controls to meaningful identifiers (e.g., `btnGenerateMIDI`, `panelScaleMode`, `gridProgressionEditor`).
- Ensure naming conventions follow PascalCase for controls and camelCase for private fields.
- Remove unused controls or dead designer artifacts.
- Organize the designer file so related controls are grouped logically.

### **2.3 UX Clarity**
- Ensure all labels use musical, high‑level language.
- Ensure no technical or cryptic terms appear in the UI.
- Ensure the chromatic circle panel is visually distinct and clean.
- Ensure the progression editor grid is readable and consistent.

---

## **3. Constraints**

### **3.1 Functional Behavior Must Not Change**
- All event handlers must remain wired to the same logic.
- All control values must continue to map to the same HarmonySpec fields.
- All transforms must behave identically.
- MIDI generation must behave identically.
- JSON loading and saving must behave identically.

### **3.2 No New Features**
- Do not add new controls.
- Do not remove existing controls.
- Do not add new editing capabilities.
- Do not modify the transform pipeline.
- Do not modify the chromatic circle drawing logic.

### **3.3 No Breaking Changes**
- Do not change public method signatures.
- Do not change the structure of HarmonySpec.
- Do not change the progression editor’s data binding.

---

## **4. Deliverables**

- Updated `.Designer.cs` files with improved layout and naming.
- Updated `.cs` files reflecting renamed controls.
- No functional changes.
- No new features.
- No removed features.
- A cleaner, more maintainable UI ready for PoC16–18 visual expansions.
