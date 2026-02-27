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
        public override HarmonySpec ReadJson(JsonReader reader, Type objectType, HarmonySpec existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var harmonySpec = new HarmonySpec();

            // Handle scale property - can be array or string
            if (jsonObject["scale"] != null)
            {
                var scaleToken = jsonObject["scale"];
                if (scaleToken.Type == JTokenType.Array)
                {
                    // Standard array format: "scale": [0, 2, 4, 5, 7, 9, 11]
                    harmonySpec.Scale = scaleToken.ToObject<List<int>>();
                }
                else if (scaleToken.Type == JTokenType.String)
                {
                    // String format: "scale": "major" -> map to ScaleName
                    harmonySpec.ScaleName = scaleToken.ToString();
                    harmonySpec.Scale = new List<int>(); // Empty scale list
                }
            }

            // Handle other properties normally
            if (jsonObject["customScale"] != null)
                harmonySpec.CustomScale = jsonObject["customScale"].ToObject<List<int>>();
            
            if (jsonObject["scaleName"] != null)
                harmonySpec.ScaleName = jsonObject["scaleName"].ToString();
            
            if (jsonObject["root"] != null)
                harmonySpec.Root = jsonObject["root"].ToString();
            
            if (jsonObject["progression"] != null)
                harmonySpec.Progression = jsonObject["progression"].ToObject<List<ChordEvent>>();
            
            if (jsonObject["constraints"] != null)
                harmonySpec.Constraints = jsonObject["constraints"].ToObject<HarmonyConstraints>();
            
            if (jsonObject["channel"] != null)
                harmonySpec.Channel = (int)jsonObject["channel"];
            
            if (jsonObject["velocity"] != null)
                harmonySpec.Velocity = (int)jsonObject["velocity"];
            
            if (jsonObject["duration"] != null)
                harmonySpec.Duration = (double)jsonObject["duration"];

            return harmonySpec;
        }

        public override void WriteJson(JsonWriter writer, HarmonySpec value, JsonSerializer serializer)
        {
            // Use default serialization for writing
            var jsonObject = JObject.FromObject(value);
            jsonObject.WriteTo(writer);
        }
    }
}
