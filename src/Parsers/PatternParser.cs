using System;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Parsers
{
    public class PatternParser
    {
        public static PatternSpec ParseLine(string line)
        {
            var parts = line.Split(':');
            var id = parts[0].Trim();
            var rule = parts[1].Trim();

            return id switch
            {
                "K" => ParseModulo("kick", 36, rule),
                "S" => ParseModulo("snare", 38, rule),
                "H" => ParseFunction("hihat", 42, rule),
                _ => throw new Exception("Unknown pattern ID")
            };
        }

        private static PatternSpec ParseModulo(string name, int note, string rule)
        {
            // rule may contain modifiers separated by spaces, e.g. "1/3@bar v=100 d=0.5"
            var tokens = rule.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var main = tokens.Length > 0 ? tokens[0] : rule;
            var parts = main.Split('/');
            if (parts.Length < 2)
                throw new Exception($"Invalid modulo rule: {rule}");

            if (!int.TryParse(parts[1], out var interval))
                throw new Exception($"Invalid interval in rule: {rule}");

            var spec = new PatternSpec
            {
                Name = name,
                Note = note,
                Type = "modulo",
                Interval = interval,
                HitsPerBar = 0,
                Mode = "global",
                Velocity = 100,
                Duration = 1.0
            };

            if (tokens.Length > 1)
            {
                ApplyModifiers(spec, tokens, 1);
            }

            return spec;
        }

        private static PatternSpec ParseFunction(string name, int note, string rule)
        {
            // function rule may contain modifiers after a space
            var tokens = rule.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var main = tokens.Length > 0 ? tokens[0] : rule;
            var expression = main.Replace("sin", "sin(x)");

            var spec = new PatternSpec
            {
                Name = name,
                Note = note,
                Type = "function",
                Expression = expression,
                Mode = "global",
                Velocity = 100,
                Duration = 1.0
            };

            if (tokens.Length > 1)
            {
                ApplyModifiers(spec, tokens, 0);
            }

            return spec;
        }

        private static void ApplyModifiers(PatternSpec spec, string[] tokens, int mainTokenIndex)
        {
            // tokens include main token at mainTokenIndex; modifiers start after that
            for (int i = mainTokenIndex + 1; i < tokens.Length; i++)
            {
                var t = tokens[i].Trim();
                if (string.Equals(t, "@bar", StringComparison.OrdinalIgnoreCase))
                {
                    spec.Mode = "bar";
                    if (spec.Interval > 0)
                        spec.HitsPerBar = spec.Interval;
                    continue;
                }

                if (string.Equals(t, "@global", StringComparison.OrdinalIgnoreCase))
                {
                    spec.Mode = "global";
                    spec.HitsPerBar = 0;
                    continue;
                }

                // velocity e.g. v=100
                if (t.StartsWith("v=", StringComparison.OrdinalIgnoreCase))
                {
                    var vv = t.Substring(2);
                    if (int.TryParse(vv, out var v))
                        spec.Velocity = Math.Max(0, Math.Min(127, v));
                    continue;
                }

                // duration e.g. d=0.5 (fraction of a step)
                if (t.StartsWith("d=", StringComparison.OrdinalIgnoreCase))
                {
                    var dv = t.Substring(2);
                    if (double.TryParse(dv, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
                        spec.Duration = Math.Max(0.0, d);
                    continue;
                }
            }
        }
    }
}