using System.Collections.Generic;

namespace ParametricMidiSequencer.Models
{
    public class MetaSpec
    {
        public int Tempo { get; set; } = 120;
        public int Steps { get; set; } = 16;
        public int Bars { get; set; } = 4;
        public int Ppq { get; set; } = 480;
        public bool AutoExtend { get; set; } = true; // auto-extend bars if notes overflow
    }

    public class TrackSpec
    {
        public string Name { get; set; }
        // MIDI channel (0-15). If null or -1, generator will choose (percussion auto-mapped).
        public int Channel { get; set; } = -1;
        public bool IsPercussion { get; set; } = false;
        public List<PatternSpecJson> Patterns { get; set; } = new List<PatternSpecJson>();
        public List<ManualEvent> Events { get; set; } = new List<ManualEvent>();
    }

    public class PatternSpecJson
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public int Note { get; set; }
        public int Interval { get; set; }
        public int HitsPerBar { get; set; }
        public string Mode { get; set; }
        public int Velocity { get; set; }
        public double Duration { get; set; }
        public string Expression { get; set; }
        public int Channel { get; set; } = -1;
        // Offset in steps; can be fractional (e.g., 0.5 = half a step)
        public double Offset { get; set; } = 0.0;
    }

    public class ManualEvent
    {
        // timeStep is absolute step index (0..)
        public int TimeStep { get; set; }
        public int Note { get; set; }
        public int Velocity { get; set; }
        public double Duration { get; set; }
        public int Channel { get; set; } = -1;
    }
}
