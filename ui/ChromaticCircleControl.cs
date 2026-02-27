using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ParametricMidiSequencer.UI
{
    /// <summary>
    /// A custom control that renders a 12-node chromatic circle.
    /// Active scale degrees are highlighted; inactive pitch classes are dimmed.
    /// </summary>
    public class ChromaticCircleControl : UserControl
    {
        private static readonly string[] PitchNames =
            { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        private List<int> _activePitchClasses = new();
        private List<int>? _chordPitchClasses;
        private bool _showDegrees;

        /// <summary>
        /// The active pitch classes (0–11) to highlight on the circle.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<int> ActivePitchClasses
        {
            get => _activePitchClasses;
            set { _activePitchClasses = value ?? new List<int>(); Invalidate(); }
        }

        /// <summary>
        /// The chord pitch classes (0–11) to render as a polygon on the circle.
        /// Set to null to clear the chord shape.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<int>? ChordPitchClasses
        {
            get => _chordPitchClasses;
            set { _chordPitchClasses = value; Invalidate(); }
        }

        /// <summary>
        /// When true, active nodes display scale degree numbers (1, 2, 3…)
        /// instead of pitch-class names.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowDegrees
        {
            get => _showDegrees;
            set { _showDegrees = value; Invalidate(); }
        }

        public ChromaticCircleControl()
        {
            DoubleBuffered = true;
            MinimumSize = new Size(80, 80);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int size = Math.Min(Width, Height) - 24;
            if (size < 40)
                return;

            float cx = Width / 2f;
            float cy = Height / 2f;
            float radius = size / 2f;
            float nodeRadius = Math.Max(9f, size / 14f);

            var activeSet = new HashSet<int>(_activePitchClasses);
            var chordSet = _chordPitchClasses != null ? new HashSet<int>(_chordPitchClasses) : new HashSet<int>();
            bool hasChord = chordSet.Count > 0;

            // Draw chord polygon before nodes so nodes appear on top
            if (hasChord && _chordPitchClasses!.Count >= 2)
            {
                var sortedPcs = _chordPitchClasses.OrderBy(pc => pc).ToList();
                var points = sortedPcs.Select(pc =>
                {
                    double angle = (pc * 30.0 - 90.0) * Math.PI / 180.0;
                    return new PointF(cx + radius * (float)Math.Cos(angle), cy + radius * (float)Math.Sin(angle));
                }).ToArray();

                using var fillBrush = new SolidBrush(Color.FromArgb(60, 255, 165, 0));
                g.FillPolygon(fillBrush, points);

                using var polyPen = new Pen(Color.DarkGoldenrod, 2.5f);
                g.DrawPolygon(polyPen, points);
            }

            for (int pc = 0; pc < 12; pc++)
            {
                // C at top (−90°), clockwise
                double angle = (pc * 30.0 - 90.0) * Math.PI / 180.0;
                float nx = cx + radius * (float)Math.Cos(angle);
                float ny = cy + radius * (float)Math.Sin(angle);

                bool isActive = activeSet.Contains(pc);
                bool isChordTone = chordSet.Contains(pc);

                if (isChordTone)
                {
                    using var fillBrush = new SolidBrush(Color.Goldenrod);
                    g.FillEllipse(fillBrush, nx - nodeRadius, ny - nodeRadius, nodeRadius * 2, nodeRadius * 2);
                    using var borderPen = new Pen(Color.SaddleBrown, 1.5f);
                    g.DrawEllipse(borderPen, nx - nodeRadius, ny - nodeRadius, nodeRadius * 2, nodeRadius * 2);
                }
                else if (isActive)
                {
                    Color fillColor = hasChord ? Color.FromArgb(100, 70, 130, 180) : Color.SteelBlue;
                    using var fillBrush = new SolidBrush(fillColor);
                    g.FillEllipse(fillBrush, nx - nodeRadius, ny - nodeRadius, nodeRadius * 2, nodeRadius * 2);
                    using var borderPen = new Pen(Color.DarkBlue, 1.5f);
                    g.DrawEllipse(borderPen, nx - nodeRadius, ny - nodeRadius, nodeRadius * 2, nodeRadius * 2);
                }
                else
                {
                    using var fillBrush = new SolidBrush(SystemColors.Control);
                    g.FillEllipse(fillBrush, nx - nodeRadius, ny - nodeRadius, nodeRadius * 2, nodeRadius * 2);
                    using var borderPen = new Pen(Color.Gray, 1f);
                    g.DrawEllipse(borderPen, nx - nodeRadius, ny - nodeRadius, nodeRadius * 2, nodeRadius * 2);
                }

                // Determine label and text color
                string label;
                Color textColor;
                if (_showDegrees && isActive)
                {
                    int degree = _activePitchClasses.IndexOf(pc) + 1;
                    label = degree.ToString();
                    textColor = isChordTone ? Color.Black : Color.White;
                }
                else
                {
                    label = PitchNames[pc];
                    if (isChordTone)
                        textColor = Color.Black;
                    else if (isActive)
                        textColor = hasChord ? Color.FromArgb(160, 255, 255, 255) : Color.White;
                    else
                        textColor = Color.Gray;
                }

                float fontSize = Math.Max(6f, nodeRadius * 0.72f);
                using var font = new Font(SystemFonts.DefaultFont.FontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                using var textBrush = new SolidBrush(textColor);
                var textSize = g.MeasureString(label, font);
                g.DrawString(label, font, textBrush, nx - textSize.Width / 2, ny - textSize.Height / 2);
            }

            // Draw a subtle legend at the bottom
            float legendY = cy + radius + nodeRadius + 4;
            if (legendY + 14 < Height)
            {
                using var legendFont = new Font(SystemFonts.DefaultFont.FontFamily, 7f, FontStyle.Regular, GraphicsUnit.Pixel);
                using var legendBrush = new SolidBrush(SystemColors.GrayText);
                string legend = hasChord
                    ? "● chord tones   ● scale degrees   ○ inactive"
                    : "● active scale degrees   ○ inactive pitch classes";
                var legendSize = g.MeasureString(legend, legendFont);
                g.DrawString(legend, legendFont, legendBrush, cx - legendSize.Width / 2, legendY);
            }
        }
    }
}
