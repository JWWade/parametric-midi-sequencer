using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ParametricMidiSequencer.UI.Rendering
{
    /// <summary>
    /// Computes geometric data for chromatic circle visualization.
    /// All calculations are pure functions with no side effects.
    /// </summary>
    public static class GeometryEngine
    {
        /// <summary>
        /// Computes the position of each pitch-class node on the chromatic circle.
        /// Index 0 is C at the top (−90°), arranged clockwise.
        /// </summary>
        public static List<PointF> ComputeNodePositions(Rectangle bounds, float radius)
        {
            var positions = new List<PointF>(12);
            float cx = bounds.Width / 2f;
            float cy = bounds.Height / 2f;

            for (int pc = 0; pc < 12; pc++)
            {
                // C at top (−90°), clockwise
                double angle = (pc * 30.0 - 90.0) * Math.PI / 180.0;
                float x = cx + radius * (float)Math.Cos(angle);
                float y = cy + radius * (float)Math.Sin(angle);
                positions.Add(new PointF(x, y));
            }

            return positions;
        }

        /// <summary>
        /// Computes the radius of the chromatic circle based on control dimensions.
        /// </summary>
        public static float ComputeRadius(int width, int height)
        {
            int size = Math.Min(width, height) - 24;
            return size < 40 ? 0 : size / 2f;
        }

        /// <summary>
        /// Computes the radius of individual pitch-class nodes.
        /// </summary>
        public static float ComputeNodeRadius(int width, int height)
        {
            int size = Math.Min(width, height) - 24;
            return Math.Max(9f, size / 14f);
        }

        /// <summary>
        /// Converts a pitch class (0–11) to a position on the circle.
        /// </summary>
        public static PointF GetNodePosition(int pitchClass, Rectangle bounds, float radius)
        {
            if (pitchClass < 0 || pitchClass > 11)
                throw new ArgumentOutOfRangeException(nameof(pitchClass), "Pitch class must be 0–11.");

            float cx = bounds.Width / 2f;
            float cy = bounds.Height / 2f;
            double angle = (pitchClass * 30.0 - 90.0) * Math.PI / 180.0;
            return new PointF(cx + radius * (float)Math.Cos(angle), cy + radius * (float)Math.Sin(angle));
        }

        /// <summary>
        /// Computes polygon vertices for a chord in order around the circle.
        /// Returns an empty array if pcs has fewer than 2 elements.
        /// </summary>
        public static PointF[] ComputePolygonVertices(List<int> pitchClasses, List<PointF> nodePositions)
        {
            if (pitchClasses == null || pitchClasses.Count < 2)
                return Array.Empty<PointF>();

            return pitchClasses
                .OrderBy(pc => pc)
                .Select(pc => nodePositions[pc])
                .ToArray();
        }

        /// <summary>
        /// Computes the visual center of the control for text alignment.
        /// </summary>
        public static PointF ComputeCenter(Rectangle bounds)
        {
            return new PointF(bounds.Width / 2f, bounds.Height / 2f);
        }

        /// <summary>
        /// Computes the legend position (y-coordinate) below the circle.
        /// </summary>
        public static float ComputeLegendY(Rectangle bounds, float radius, float nodeRadius)
        {
            float cy = bounds.Height / 2f;
            return cy + radius + nodeRadius + 4;
        }

        /// <summary>
        /// Determines if the legend fits within the control bounds.
        /// </summary>
        public static bool CanDisplayLegend(Rectangle bounds, float legendY)
        {
            return legendY + 14 < bounds.Height;
        }
    }
}
