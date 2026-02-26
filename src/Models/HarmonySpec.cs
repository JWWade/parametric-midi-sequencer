using System.Collections.Generic;

namespace ParametricMidiSequencer.Models
{
    public class HarmonySpec
    {
        public List<int> Scale { get; set; } = new List<int>();
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
    }

    public class ChordEvent
    {
        public int Time { get; set; }
        public int Degree { get; set; }
        public string Type { get; set; } = "triad";
    }
}
