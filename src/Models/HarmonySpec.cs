using System.Collections.Generic;

namespace ParametricMidiSequencer.Models
{
    public class HarmonySpec
    {
        // Custom pitch-class set for PoC8. If present, overrides Scale, ScaleName, Root, and BorrowMode.
        public List<int> CustomScale { get; set; } = null;
        
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

        // Geometric chord-shape transform (PoC9).
        public ShapeTransform ShapeTransform { get; set; } = null;

        // Enable global voice-leading optimization via DP (PoC20).
        // When true, the VoiceLeadingOptimizer selects the lowest-cost voicing
        // sequence across the entire progression before MIDI generation.
        public bool OptimizeVoiceLeading { get; set; } = false;
    }

    public class ShapeTransform
    {
        // Transform type: "rotate", "reflect", or "expand"
        public string Type { get; set; } = null;
        
        // Amount (for rotate and expand)
        public double Amount { get; set; } = 0;
        
        // Axis pitch class (for reflect, 0-11)
        public int Axis { get; set; } = 0;
    }

    public class ChordEvent
    {
        public int Time { get; set; }
        public int Degree { get; set; }
        public string Type { get; set; } = "triad";
        public int Inversion { get; set; } = 0;  // 0=root, 1=first, 2=second, 3=third (seventh only)
        // Optional modal borrow; if set, the chord will be constructed
        // using the parallel mode of the root instead of the primary scale.
        public string BorrowMode { get; set; } = null;
    }
}
