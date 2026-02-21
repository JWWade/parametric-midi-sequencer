namespace ParametricMidiSequencer.Models
{
    public class PatternSpec
    {
        public string Name { get; set; }
        public int Note { get; set; }
        public string Type { get; set; }
        public int Interval { get; set; }
        // If >0, indicates number of hits to place per bar (e.g., 3 => 3 hits per bar)
        public int HitsPerBar { get; set; }
        // Mode: "global" or "bar" (global = every Nth global step; bar = distribute per bar)
        public string Mode { get; set; }
        // Per-pattern velocity (0-127)
        public int Velocity { get; set; }
        // Note duration as fraction of a step (1.0 = full step)
        public double Duration { get; set; }
        public string Expression { get; set; }
    }
}