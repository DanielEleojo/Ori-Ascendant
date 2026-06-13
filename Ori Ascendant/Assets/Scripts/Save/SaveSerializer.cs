using System;
using Newtonsoft.Json;

namespace OriAscendant.Save
{
    /// <summary>
    /// Pure serialization core for SaveData (testable without file IO).
    /// Newtonsoft over JsonUtility per TECH_DESIGN §1: nested classes, lists and
    /// proper null handling. Unknown JSON members are ignored and missing members
    /// fall back to field defaults, so add-only schema changes are forward-safe.
    /// </summary>
    public static class SaveSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
        };

        public static string ToJson(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return JsonConvert.SerializeObject(data, Settings);
        }

        /// <summary>Returns null on null/empty/corrupt input — callers treat that
        /// as "no usable save" and fall back to a fresh SaveData.</summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonConvert.DeserializeObject<SaveData>(json, Settings);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
