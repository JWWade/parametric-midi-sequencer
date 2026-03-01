
---

# **poc19_spec.md — Preparing the UI for Animated Geometry (Render Pipeline Stabilization)**

This milestone prepares the Parametric MIDI Sequencer UI for future animated geometric features. It does **not** introduce animation yet. Instead, it stabilizes the rendering pipeline, improves performance, eliminates flicker, and ensures the chromatic‑circle visualization can support smooth transitions in PoC20+.

The goal is to make the UI *animation‑ready* while preserving all existing PoC behaviors.

---

## **1. Purpose**

The rendering pipeline should be upgraded so that:

- The chromatic circle, chord polygons, and voice‑leading lines redraw smoothly.
- The UI uses double‑buffering and optimized paint routines.
- Redraw triggers are consistent and minimal.
- The visualization layer is isolated from business logic.
- The system can support animation in PoC20 without architectural changes.

This PoC is about **render stability**, not visual effects.

---

## **2. Scope**

### In Scope
- Enabling double‑buffering for all custom‑drawn controls.
- Refactoring the chromatic‑circle control into a clean rendering component.
- Ensuring redraws occur only when necessary.
- Cleaning up paint logic for clarity and maintainability.
- Introducing a lightweight render‑state model (immutable snapshot of what to draw).
- Ensuring consistent coordinate transforms and scaling.
- Preparing the control for time‑based animation loops (but not implementing them).

### Out of Scope
- Any actual animation.
- Any new UI features.
- Any changes to the harmony engine.
- Any changes to transform logic.
- Any new visualization types.
- Any user interaction on the circle.
- Any performance optimizations that require unsafe code or GPU acceleration.

These belong in PoC20+.

---

## **3. Rendering Improvements**

### 3.1 Double‑buffering
Enable double‑buffering on the chromatic‑circle control to eliminate flicker:

- Set `DoubleBuffered = true` in the control constructor.
- Ensure no manual `BufferedGraphics` unless necessary.
- Ensure no forced `Invalidate()` loops.

### 3.2 Clean paint pipeline
Refactor the control so that `OnPaint`:

- Reads from a **RenderState** object.
- Draws the circle.
- Draws scale nodes.
- Draws chord polygon (PoC17).
- Draws voice‑leading lines (PoC18).
- Draws labels.

No business logic should run inside `OnPaint`.

### 3.3 RenderState model
Introduce a simple immutable struct/class:

```csharp
class RenderState {
    public List<int> ActiveScale { get; }
    public List<int> ChordA { get; }
    public List<int> ChordB { get; }
    public List<(int from, int to)> VoiceLeadingPairs { get; }
    public string ChordLabel { get; }
}
```

The UI layer updates this state; the control only draws it.

### 3.4 Redraw triggers
Redraw only when:

- Scale changes.
- Selected chord changes.
- Transform values change.
- Progression changes.
- Window resizes.

Avoid redundant redraws.

### 3.5 Coordinate system cleanup
Ensure:

- Circle radius is computed once per resize.
- Node positions are cached.
- Polygon vertices are computed outside the paint loop.
- Text rendering uses consistent alignment.

---

## **4. UI Architecture Improvements**

### 4.1 Separation of concerns
Move all geometry calculations into a helper class:

```csharp
class GeometryEngine {
    public static List<PointF> ComputeNodePositions(Rectangle bounds);
    public static List<PointF> ComputePolygonVertices(List<int> pcs, List<PointF> nodePositions);
    public static List<(PointF, PointF)> ComputeVoiceLeadingLines(...);
}
```

The chromatic‑circle control becomes a pure renderer.

### 4.2 Consistent naming and structure
- Rename the control to `ChromaticCircleView`.
- Move all drawing code into methods like:
  - `DrawNodes`
  - `DrawChordPolygon`
  - `DrawVoiceLeading`
  - `DrawLabels`

### 4.3 Prepare for animation
Add a placeholder method:

```csharp
public void SetAnimationFrame(RenderState state);
```

This will be used in PoC20 for interpolation.

---

## **5. Constraints**

### 5.1 No functional changes
- Harmony engine must behave identically.
- Transform pipeline must behave identically.
- JSON loading must behave identically.
- MIDI generation must behave identically.

### 5.2 No new features
- No animation.
- No new UI controls.
- No new editing capabilities.

### 5.3 No breaking changes
- Public APIs must remain stable.
- Designer layout must remain intact.

---

## **6. Deliverables**

- Updated chromatic‑circle control with double‑buffering.
- Cleaned and refactored paint logic.
- New `RenderState` model.
- New `GeometryEngine` helper.
- Updated UI code that sets render state and triggers redraws.
- No changes to engine logic or UI features.

---

## **7. Test Cases**

- Resize the window; verify no flicker.
- Select chords rapidly; verify smooth redraws.
- Change transforms; verify redraws occur once per change.
- Change scale; verify redraws occur once per change.
- Load new JSON; verify visualization resets cleanly.
- Stress test with rapid UI interactions; verify stable rendering.

---

## **8. Expected Behavior**

PoC19 should produce a UI that:

- Redraws smoothly and flicker‑free.
- Has a clean, maintainable rendering pipeline.
- Is ready for animation in PoC20.
- Preserves all PoC1–18 behaviors.
- Requires no architectural changes when animation is added.
