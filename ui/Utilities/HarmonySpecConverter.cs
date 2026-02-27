using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.UI.Utilities
{
    /// <summary>
    /// Custom JSON converter to handle both array and string formats for the scale property.
    /// Converts "scale": "major" to ScaleName for backward compatibility.
    /// </summary>
    public class HarmonySpecConverter : JsonConverter<HarmonySpec>
    {
        public override HarmonySpec? ReadJson(JsonReader reader, Type objectType, HarmonySpec? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jsonObject = JObject.Load(reader);
            var harmonySpec = existingValue ?? new HarmonySpec();

            // Handle scale property - can be array or string
            if (jsonObject.TryGetValue("scale", out var scaleToken) && scaleToken != null)
            {
                if (scaleToken.Type == JTokenType.Array)
                {
                    // Standard array format: "scale": [0, 2, 4, 5, 7, 9, 11]
                    harmonySpec.Scale = scaleToken.ToObject<List<int>>(serializer) ?? new List<int>();
                }
                else if (scaleToken.Type == JTokenType.String)
                {
                    // String format: "scale": "major" -> map to ScaleName
                    harmonySpec.ScaleName = scaleToken.ToString();
                    harmonySpec.Scale = new List<int>(); // Empty scale list
                }
            }

            // Handle other properties normally
            if (jsonObject.TryGetValue("customScale", out var customScaleToken) && customScaleToken != null)
                harmonySpec.CustomScale = customScaleToken.ToObject<List<int>>(serializer);
            
            if (jsonObject.TryGetValue("scaleName", out var scaleNameToken) && scaleNameToken != null)
                harmonySpec.ScaleName = scaleNameToken.ToString();
            
            if (jsonObject.TryGetValue("root", out var rootToken) && rootToken != null)
                harmonySpec.Root = rootToken.ToString();
            
            if (jsonObject.TryGetValue("progression", out var progressionToken) && progressionToken != null)
                harmonySpec.Progression = progressionToken.ToObject<List<ChordEvent>>(serializer) ?? new List<ChordEvent>();
            
            if (jsonObject.TryGetValue("constraints", out var constraintsToken) && constraintsToken != null)
                harmonySpec.Constraints = constraintsToken.ToObject<HarmonyConstraints>(serializer) ?? new HarmonyConstraints();
            
            if (jsonObject.TryGetValue("channel", out var channelToken) && channelToken != null)
                harmonySpec.Channel = channelToken.Value<int?>() ?? harmonySpec.Channel;
            
            if (jsonObject.TryGetValue("velocity", out var velocityToken) && velocityToken != null)
                harmonySpec.Velocity = velocityToken.Value<int?>() ?? harmonySpec.Velocity;
            
            if (jsonObject.TryGetValue("duration", out var durationToken) && durationToken != null)
                harmonySpec.Duration = durationToken.Value<double?>() ?? harmonySpec.Duration;

            return harmonySpec;
        }

        public override void WriteJson(JsonWriter writer, HarmonySpec? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // Use default serialization for writing
            var jsonObject = JObject.FromObject(value);
            jsonObject.WriteTo(writer);
        }
    }
}
