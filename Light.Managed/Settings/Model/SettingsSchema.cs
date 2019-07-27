using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Light.Managed.Settings.Model
{
    internal class SettingsSchema
    {
        [JsonProperty("version")]
        public int Version { get; set; }
        [JsonProperty("schemaId")]
        public Guid SchemaGuid { get; set; }
        [JsonProperty("schema")]
        public List<SettingsKeyValueSchema> Schema { get; set; } 
    }

    internal class SettingsKeyValueSchema
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("platform")]
        public List<string> Platform { get; set; } 
    }
}
