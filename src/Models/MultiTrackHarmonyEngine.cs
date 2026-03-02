using System;
using System.Collections.Generic;
using System.Linq;

namespace ParametricMidiSequencer.Models
{
    /// <summary>
    /// PoC21: Multi-Track Harmony Engine.
    /// Processes an ordered list of <see cref="HarmonySpec"/> tracks independently,
    /// then merges all resulting <see cref="ManualEvent"/> objects into a unified
    /// event timeline for MIDI generation.
    /// </summary>
    public static class MultiTrackHarmonyEngine
    {
        /// <summary>
        /// Generates MIDI events for every track in <paramref name="tracks"/> and
        /// returns them as a single merged list ordered by time-step then channel.
        /// Each track's transform pipeline and voice-leading optimizer run independently.
        /// </summary>
        public static List<ManualEvent> GenerateAllEvents(IEnumerable<HarmonySpec> tracks)
        {
            var all = new List<ManualEvent>();
            if (tracks == null)
                return all;

            foreach (var track in tracks)
            {
                if (track == null)
                    continue;

                var events = HarmonyGenerator.GenerateHarmonyEvents(track);
                all.AddRange(events);
            }

            // Sort by time-step, then channel so the output is deterministic
            all.Sort((a, b) =>
            {
                int t = a.TimeStep.CompareTo(b.TimeStep);
                return t != 0 ? t : a.Channel.CompareTo(b.Channel);
            });

            return all;
        }

        /// <summary>
        /// Validates a multi-track harmony specification and returns a list of
        /// human-readable error messages.  An empty list means the spec is valid.
        /// </summary>
        /// <param name="tracks">The harmony tracks to validate.</param>
        /// <param name="allowChannelSharing">
        /// When <c>false</c> (default) every track must use a distinct MIDI channel.
        /// Pass <c>true</c> to suppress channel-collision warnings.
        /// </param>
        public static List<string> Validate(IEnumerable<HarmonySpec> tracks, bool allowChannelSharing = false)
        {
            var errors = new List<string>();
            if (tracks == null)
                return errors;

            var trackList = tracks.ToList();
            var seenChannels = new Dictionary<int, string>(); // channel → first track name

            for (int i = 0; i < trackList.Count; i++)
            {
                var track = trackList[i];
                string label = !string.IsNullOrEmpty(track?.Name) ? track.Name : $"Track {i + 1}";

                if (track == null)
                {
                    errors.Add($"{label}: track definition is null.");
                    continue;
                }

                // Scale / custom set
                bool hasScale = (track.CustomScale != null && track.CustomScale.Count > 0)
                    || (track.Scale != null && track.Scale.Count > 0)
                    || !string.IsNullOrEmpty(track.ScaleName);

                if (!hasScale)
                    errors.Add($"{label}: no scale definition found (set Scale, ScaleName, or CustomScale).");

                // Progression
                if (track.Progression == null || track.Progression.Count == 0)
                    errors.Add($"{label}: progression is empty.");

                // Channel uniqueness
                if (!allowChannelSharing)
                {
                    if (seenChannels.TryGetValue(track.Channel, out var firstTrack))
                        errors.Add($"{label}: MIDI channel {track.Channel} is already used by '{firstTrack}'. Set allowChannelSharing=true to permit shared channels.");
                    else
                        seenChannels[track.Channel] = label;
                }
            }

            return errors;
        }
    }
}
