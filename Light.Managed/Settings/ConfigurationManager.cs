using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.System.Profile;
using Light.Managed.Settings.Model;
using Light.Managed.Tools;
using Newtonsoft.Json;

namespace Light.Managed.Settings
{
    internal sealed class ConfigurationManager : Dictionary<string, SettingEntry>
    {
        private readonly Guid _instanceGuid;
        private readonly bool _localMode;

        public static Guid CommonSettingsGuid = Guid.Parse("5F098859-2F79-4000-BC4A-429A1795D828");

        public ConfigurationManager(Guid instanceGuid, bool localMode = false)
        {
            _instanceGuid = instanceGuid;
            _localMode = localMode;
            LoadConfiguration();
        }
        public ConfigurationManager(Guid instanceGuid, int capacity, bool localMode = false) : base(capacity)
        {
            _instanceGuid = instanceGuid;
            _localMode = localMode;
            LoadConfiguration();
        }

        public void LoadConfiguration()
        {
            Clear();
            var settingsContainer = (!_localMode)
                ? ApplicationData.Current.RoamingSettings.Values
                : ApplicationData.Current.LocalSettings.Values;

            var containerId = $"SettingsContainer.{_instanceGuid}";
            if (settingsContainer.ContainsKey(containerId))
            {
                // Read content.
                var entities =
                    JsonConvert.DeserializeObject<Dictionary<string, SettingEntry>> (
                        (string)settingsContainer[containerId]);
                foreach(var e in entities) Add(e.Key,e.Value);
            }
        }
        public void InstallSchema(string schema)
        {
            var schemaEntity = JsonConvert.DeserializeObject<SettingsSchema>(schema);

            var schemaQuery = from c in schemaEntity.Schema
                where c.Platform.Contains(AnalyticsInfo.VersionInfo.DeviceFamily)
                select c; 

            PostUpdateSchmea(schemaQuery);
            SaveConfiguration(true, !_localMode);

            #region Update schema version
            // Write schema version
            AddOrUpdateKeyValuePair($"{schemaEntity.SchemaGuid}.Version", schemaEntity.Version, !_localMode);
            #endregion
        }
        public void SaveConfiguration(bool autoTriggered = false, bool isRoaming = false)
        {
            AddOrUpdateKeyValuePair($"SettingsContainer.{_instanceGuid}", JsonConvert.SerializeObject(this), isRoaming);
        }

        #region Private utils
        private void AddOrUpdateKeyValuePair(string key, object value, bool isRoaming = false)
        {
            var settingsContainer = (isRoaming)
                ? ApplicationData.Current.RoamingSettings.Values
                : ApplicationData.Current.LocalSettings.Values;

            if (settingsContainer.ContainsKey(key))
                settingsContainer[key] = value;
            else
            {
                settingsContainer.Add(key, value);
            }
        }
        private void PostUpdateSchmea(IEnumerable<SettingsKeyValueSchema> schemaQuery)
        {
            foreach (var kvPair in schemaQuery)
            {
                try
                {
                    var type = Type.GetType(kvPair.Type, true);
                    object defaultValue = null;
                    switch (type.FullName)
                    {
                        case "System.Boolean":
                            defaultValue = bool.Parse(kvPair.DefaultValue);
                            break;
                        case "System.String":
                            defaultValue = kvPair.DefaultValue;
                            break;
                        case "System.Int32":
                            defaultValue = int.Parse(kvPair.DefaultValue);
                            break;
                        case "System.Int64":
                            defaultValue = long.Parse(kvPair.DefaultValue);
                            break;
                    }

                    Add(kvPair.Key, new SettingEntry
                    {
                        Description = "",
                        Title = kvPair.Key,
                        Value = defaultValue,
                        ValueType = type
                    });
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                }
            }
        }
        #endregion
    }
}
