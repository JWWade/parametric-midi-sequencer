using System;
using System.Collections.Generic;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Models
{
    public class HarmonyGenerator
    {
        public static List<ManualEvent> GenerateHarmonyEvents(HarmonySpec harmony)
        {
            var events = new List<ManualEvent>();

            if (harmony == null || harmony.Progression == null || harmony.Scale == null)
                return events;

            foreach (var chord in harmony.Progression)
            {
                var notes = BuildTriad(harmony.Scale, chord.Degree);
                foreach (var note in notes)
                {
                    events.Add(new ManualEvent
                    {
                        TimeStep = chord.Time,
                        Note = note + 60, // C4 as baseline
                        Velocity = harmony.Velocity,
                        Duration = harmony.Duration,
                        Channel = harmony.Channel
                    });
                }
            }

            return events;
        }

        private static List<int> BuildTriad(List<int> scale, int degree)
        {
            var triad = new List<int>();

            // Validate degree (1-7)
            if (degree < 1 || degree > 7 || scale.Count == 0)
                return triad;

            // Root
            int root = scale[degree - 1];
            triad.Add(root);

            // Third and fifth based on scale degree quality in major scale
            int[] intervals = GetTriadIntervals(degree);
            triad.Add(root + intervals[1]); // major/minor third
            triad.Add(root + intervals[2]); // perfect/diminished fifth

            return triad;
        }

        private static int[] GetTriadIntervals(int degree)
        {
            // Returns [root, third, fifth] intervals in semitones
            return degree switch
            {
                1 => new[] { 0, 4, 7 },  // major
                2 => new[] { 0, 3, 7 },  // minor
                3 => new[] { 0, 3, 7 },  // minor
                4 => new[] { 0, 4, 7 },  // major
                5 => new[] { 0, 4, 7 },  // major
                6 => new[] { 0, 3, 7 },  // minor
                7 => new[] { 0, 3, 6 },  // diminished
                _ => new[] { 0, 4, 7 }   // default major
            };
        }
    }
}
