using System;
using System.Collections.Generic;
using System.Linq;

namespace ParametricMidiSequencer.UI.Models
{
    /// <summary>
    /// An immutable snapshot of the visualization state.
    /// This decouples the rendering pipeline from business logic.
    /// Supports record-based copying with the `with` expression.
    /// </summary>
    public record RenderState(
        List<int> ActiveScale,
        List<int>? ChordA = null,
        List<int>? ChordB = null,
        List<(int from, int to)>? VoiceLeadingPairs = null,
        string? ChordLabel = null,
        bool ShowDegrees = false)
    {
        /// <summary>
        /// Returns true if there is a valid current chord to display.
        /// </summary>
        public bool HasChord => ChordA != null && ChordA.Count >= 2;

        /// <summary>
        /// Returns true if there is a valid next chord for voice-leading visualization.
        /// </summary>
        public bool HasVoiceLeading => ChordB != null && ChordB.Count >= 2 && (VoiceLeadingPairs?.Count ?? 0) > 0;

        /// <summary>
        /// Returns true if the visualization has any content to display.
        /// </summary>
        public bool HasContent => ActiveScale.Count > 0 || HasChord;

        /// <summary>
        /// Get voice leading pairs, guaranteed non-null.
        /// </summary>
        public List<(int from, int to)> GetVoiceLeadingPairs() =>
            VoiceLeadingPairs ?? new List<(int, int)>();
    }
}
