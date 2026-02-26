using System.Collections.Generic;

namespace ParametricMidiSequencer.Models
{
    public class HarmonySpec
    {
        public List<int> Scale { get; set; } = new List<int>();
        // Optional scale specification by name/root. If `Scale` is provided (non-empty), it takes precedence.
        public string ScaleName { get; set; } = "major";
        public string Root { get; set; } = "C";
        public List<ChordEvent> Progression { get; set; } = new List<ChordEvent>();
        public HarmonyConstraints Constraints { get; set; } = new HarmonyConstraints();
        public int Channel { get; set; } = 0;
        public int Velocity { get; set; } = 90;
        public double Duration { get; set; } = 4;
    }

    public class HarmonyConstraints
    {
        // Minimum number of shared pitch classes between consecutive chords.
        public int MinSharedPitches { get; set; } = 0;
        
        // Control the depth of multi-voice search in transform.
        // 1 = single voices only; 2 = single+pairs; 3+ = single+pairs+triples, etc.
        // 0 or negative = unlimited depth (try all combinations).
        // Default: 2 (pairs allowed for good balance).
        public int TransformDepth { get; set; } = 2;

        // Additional transform: uniform semitone shift of every chord.
        // Valid range: -11..+11. 0 means no cycling.
        public int PitchCenterCycle { get; set; } = 0;
    }

    public class ChordEvent
    {
        public int Time { get; set; }
        public int Degree { get; set; }
        public string Type { get; set; } = "triad";
        public int Inversion { get; set; } = 0;  // 0=root, 1=first, 2=second, 3=third (seventh only)
    }
}
