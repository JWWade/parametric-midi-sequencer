using ParametricMidiSequencer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParametricMidiSequencer.UI.Utilities
{
    /// <summary>
    /// Formats harmony data for human-readable display in the UI.
    /// </summary>
    public static class HarmonySummaryFormatter
    {
        /// <summary>
        /// Create a high-level summary of the harmony specification.
        /// </summary>
        public static HarmonySummary CreateSummary(HarmonySpec spec)
        {
            if (spec == null)
                return new HarmonySummary();

            return new HarmonySummary
            {
                ScaleDescription = GetScaleDescription(spec),
                ChordCount = spec.Progression?.Count ?? 0,
                TransformDescription = GetTransformDescription(spec.Constraints),
                HasInversions = HasInversions(spec.Progression),
                InversionCount = CountInversions(spec.Progression),
                Velocity = spec.Velocity,
                Channel = spec.Channel,
                Duration = spec.Duration
            };
        }

        /// <summary>
        /// Get a human-readable scale description.
        /// </summary>
        private static string GetScaleDescription(HarmonySpec spec)
        {
            if (spec.CustomScale != null && spec.CustomScale.Count > 0)
            {
                var scaleStr = string.Join(",", spec.CustomScale);
                return $"Custom scale: [{scaleStr}]";
            }

            var scalePart = !string.IsNullOrEmpty(spec.ScaleName) ? spec.ScaleName : "unknown";
            var rootPart = !string.IsNullOrEmpty(spec.Root) ? spec.Root : "C";
            return $"{rootPart} {scalePart}";
        }

        /// <summary>
        /// Get a human-readable transform description.
        /// </summary>
        private static string GetTransformDescription(HarmonyConstraints constraints)
        {
            if (constraints == null)
                return "None";

            var parts = new List<string>();

            parts.Add($"minSharedPitches={constraints.MinSharedPitches}");
            parts.Add($"pitchCenterCycle={constraints.PitchCenterCycle}");

            if (constraints.ShapeTransform != null && !string.IsNullOrEmpty(constraints.ShapeTransform.Type))
            {
                var type = constraints.ShapeTransform.Type.ToLowerInvariant();
                string transformPart = type switch
                {
                    "rotate" => $"rotate({constraints.ShapeTransform.Amount})",
                    "reflect" => $"reflect(axis={constraints.ShapeTransform.Axis})",
                    "expand" => $"expand({constraints.ShapeTransform.Amount})",
                    _ => type
                };
                parts.Add($"shapeTransform={transformPart}");
            }
            else
            {
                parts.Add("shapeTransform=none");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "None";
        }

        /// <summary>
        /// Check if any chord uses an inversion.
        /// </summary>
        private static bool HasInversions(List<ChordEvent>? progression)
        {
            if (progression == null)
                return false;

            return progression.Any(c => c.Inversion > 0);
        }

        /// <summary>
        /// Count how many chords use inversions.
        /// </summary>
        private static int CountInversions(List<ChordEvent>? progression)
        {
            if (progression == null)
                return 0;

            return progression.Count(c => c.Inversion > 0);
        }
    }

    /// <summary>
    /// Container for harmony summary information for display.
    /// </summary>
    public class HarmonySummary
    {
        public string ScaleDescription { get; set; } = "";
        public int ChordCount { get; set; } = 0;
        public string TransformDescription { get; set; } = "";
        public bool HasInversions { get; set; } = false;
        public int InversionCount { get; set; } = 0;
        public int Velocity { get; set; } = 90;
        public int Channel { get; set; } = 0;
        public double Duration { get; set; } = 4.0;
    }
}
