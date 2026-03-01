using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ParametricMidiSequencer.Models;
using ParametricMidiSequencer.UI.Models;
using ParametricMidiSequencer.UI.Rendering;

namespace ParametricMidiSequencer.UI
{
    /// <summary>
    /// A custom control that renders a 12-node chromatic circle.
    /// Active scale degrees are highlighted; inactive pitch classes are dimmed.
    /// 
    /// This control uses a clean rendering pipeline with separation of concerns:
    /// - RenderState encapsulates visualization data
    /// - GeometryEngine computes all geometric information
    /// - OnPaint uses cleaner, extracted methods
    /// - Double-buffering eliminates flicker
    /// - Redraw triggers are consistent and minimal
    /// </summary>
    public class ChromaticCircleView : UserControl
    {
        private static readonly string[] PitchNames =
            { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        private RenderState _renderState;

        /// <summary>
        /// The active pitch classes (0–11) to highlight on the circle.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<int> ActivePitchClasses
        {
            get => _renderState.ActiveScale;
            set { SetRenderState(_renderState with { ActiveScale = value ?? new List<int>() }); }
        }

        /// <summary>
        /// The chord pitch classes (0–11) to render as a polygon on the circle.
        /// Set to null to clear the chord shape.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<int>? ChordPitchClasses
        {
            get => _renderState.ChordA;
            set { SetRenderState(_renderState with { ChordA = value }); }
        }

        /// <summary>
        /// The pitch classes (0–11) of the next chord to render as a dimmed polygon.
        /// Set to null to hide voice-leading lines.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<int>? NextChordPitchClasses
        {
            get => _renderState.ChordB;
            set { SetRenderState(_renderState with { ChordB = value }); }
        }

        /// <summary>
        /// When true, active nodes display scale degree numbers (1, 2, 3…)
        /// instead of pitch-class names.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowDegrees
        {
            get => _renderState.ShowDegrees;
            set { SetRenderState(_renderState with { ShowDegrees = value }); }
        }

        public ChromaticCircleView()
        {
            _renderState = new RenderState(
                ActiveScale: new List<int>(),
                ChordA: null,
                ChordB: null,
                VoiceLeadingPairs: null,
                ChordLabel: null,
                ShowDegrees: false);

            // Initialize for smooth rendering
            DoubleBuffered = true;
            MinimumSize = new Size(80, 80);
            BackColor = SystemColors.Control;
        }

        /// <summary>
        /// Updates the render state and triggers a redraw only if the state changed.
        /// </summary>
        private void SetRenderState(RenderState newState)
        {
            if (!_renderState.Equals(newState))
            {
                _renderState = newState;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!_renderState.HasContent)
                return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Compute geometry
            float radius = GeometryEngine.ComputeRadius(Width, Height);
            if (radius <= 0)
                return;

            float nodeRadius = GeometryEngine.ComputeNodeRadius(Width, Height);
            var nodePositions = GeometryEngine.ComputeNodePositions(ClientRectangle, radius);

            // Draw in layers: back to front
            DrawNextChordPolygon(g, nodePositions, nodeRadius);
            DrawCurrentChordPolygon(g, nodePositions, nodeRadius);
            DrawVoiceLeadingLines(g, nodePositions);
            DrawNodes(g, nodePositions, nodeRadius);
            DrawLegend(g, radius, nodeRadius);
        }

        /// <summary>
        /// Draws the next chord as a dimmed polygon (background layer).
        /// </summary>
        private void DrawNextChordPolygon(Graphics g, List<PointF> nodePositions, float nodeRadius)
        {
            if (!_renderState.HasVoiceLeading || _renderState.ChordB == null || _renderState.ChordB.Count < 2)
                return;

            var vertices = GeometryEngine.ComputePolygonVertices(_renderState.ChordB, nodePositions);
            if (vertices.Length == 0)
                return;

            using var fillBrush = new SolidBrush(Color.FromArgb(25, 100, 200, 255));
            g.FillPolygon(fillBrush, vertices);

            using var polyPen = new Pen(Color.FromArgb(100, 100, 180, 220), 1.2f);
            g.DrawPolygon(polyPen, vertices);
        }

        /// <summary>
        /// Draws the current chord as a highlighted polygon.
        /// </summary>
        private void DrawCurrentChordPolygon(Graphics g, List<PointF> nodePositions, float nodeRadius)
        {
            if (!_renderState.HasChord || _renderState.ChordA == null)
                return;

            var vertices = GeometryEngine.ComputePolygonVertices(_renderState.ChordA, nodePositions);
            if (vertices.Length == 0)
                return;

            using var fillBrush = new SolidBrush(Color.FromArgb(60, 255, 165, 0));
            g.FillPolygon(fillBrush, vertices);

            using var polyPen = new Pen(Color.DarkGoldenrod, 2.5f);
            g.DrawPolygon(polyPen, vertices);
        }

        /// <summary>
        /// Draws voice-leading lines between current and next chord tones.
        /// </summary>
        private void DrawVoiceLeadingLines(Graphics g, List<PointF> nodePositions)
        {
            if (!_renderState.HasVoiceLeading)
                return;

            using var vlPen = new Pen(Color.FromArgb(200, 0, 220, 220), 1.5f);
            foreach (var (from, to) in _renderState.GetVoiceLeadingPairs())
            {
                if (from >= 0 && from < 12 && to >= 0 && to < 12)
                {
                    g.DrawLine(vlPen, nodePositions[from], nodePositions[to]);
                }
            }
        }

        /// <summary>
        /// Draws the 12 pitch-class nodes with state-dependent coloring.
        /// </summary>
        private void DrawNodes(Graphics g, List<PointF> nodePositions, float nodeRadius)
        {
            var activeSet = new HashSet<int>(_renderState.ActiveScale);
            var chordSet = _renderState.ChordA != null ? new HashSet<int>(_renderState.ChordA) : new HashSet<int>();

            for (int pc = 0; pc < 12; pc++)
            {
                var nodePos = nodePositions[pc];
                bool isActive = activeSet.Contains(pc);
                bool isChordTone = chordSet.Contains(pc);

                // Draw node circle
                DrawNodeCircle(g, nodePos, nodeRadius, isActive, isChordTone);

                // Draw node label
                DrawNodeLabel(g, nodePos, nodeRadius, pc, isActive, isChordTone);
            }
        }

        /// <summary>
        /// Draws a single pitch-class node circle with state-dependent colors.
        /// </summary>
        private void DrawNodeCircle(Graphics g, PointF position, float nodeRadius, bool isActive, bool isChordTone)
        {
            Color fillColor;
            Color borderColor;

            if (isChordTone)
            {
                fillColor = Color.Goldenrod;
                borderColor = Color.SaddleBrown;
            }
            else if (isActive)
            {
                fillColor = _renderState.HasChord ? Color.FromArgb(100, 70, 130, 180) : Color.SteelBlue;
                borderColor = Color.DarkBlue;
            }
            else
            {
                fillColor = SystemColors.Control;
                borderColor = Color.Gray;
            }

            using var fillBrush = new SolidBrush(fillColor);
            g.FillEllipse(fillBrush, position.X - nodeRadius, position.Y - nodeRadius, nodeRadius * 2, nodeRadius * 2);

            float penWidth = isChordTone ? 1.5f : 1f;
            using var borderPen = new Pen(borderColor, penWidth);
            g.DrawEllipse(borderPen, position.X - nodeRadius, position.Y - nodeRadius, nodeRadius * 2, nodeRadius * 2);
        }

        /// <summary>
        /// Draws the label (pitch name or scale degree) for a node.
        /// </summary>
        private void DrawNodeLabel(Graphics g, PointF position, float nodeRadius, int pc, bool isActive, bool isChordTone)
        {
            string label;
            Color textColor;

            if (_renderState.ShowDegrees && isActive)
            {
                int degree = _renderState.ActiveScale.IndexOf(pc) + 1;
                label = degree.ToString();
                textColor = isChordTone ? Color.Black : Color.White;
            }
            else
            {
                label = PitchNames[pc];
                if (isChordTone)
                {
                    textColor = Color.Black;
                }
                else if (isActive)
                {
                    textColor = _renderState.HasChord ? Color.FromArgb(160, 255, 255, 255) : Color.White;
                }
                else
                {
                    textColor = Color.Gray;
                }
            }

            float fontSize = Math.Max(6f, nodeRadius * 0.72f);
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(textColor);
            var textSize = g.MeasureString(label, font);
            g.DrawString(label, font, textBrush, position.X - textSize.Width / 2, position.Y - textSize.Height / 2);
        }

        /// <summary>
        /// Draws the legend explaining the visualization.
        /// </summary>
        private void DrawLegend(Graphics g, float radius, float nodeRadius)
        {
            float legendY = GeometryEngine.ComputeLegendY(ClientRectangle, radius, nodeRadius);
            if (!GeometryEngine.CanDisplayLegend(ClientRectangle, legendY))
                return;

            using var legendFont = new Font(SystemFonts.DefaultFont.FontFamily, 7f, FontStyle.Regular, GraphicsUnit.Pixel);
            using var legendBrush = new SolidBrush(SystemColors.GrayText);

            string legend = _renderState.HasChord && _renderState.HasVoiceLeading
                ? "● chord tones   ─ voice leading   ● next chord   ○ inactive"
                : _renderState.HasChord
                    ? "● chord tones   ● scale degrees   ○ inactive"
                    : "● active scale degrees   ○ inactive pitch classes";

            var legendSize = g.MeasureString(legend, legendFont);
            float cx = ClientRectangle.Width / 2f;
            g.DrawString(legend, legendFont, legendBrush, cx - legendSize.Width / 2, legendY);
        }
    }
}
