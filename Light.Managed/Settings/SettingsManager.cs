using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System.Profile;
using Light.Managed.Settings.Model;

namespace Light.Managed.Settings
{
    /// <summary>
    /// Next-generation XML-based settings manager.
    /// </summary>
    public class SettingsManager : IObservableMap<string, object>
    {
        private static SettingsManager _instance;
        /// <summary>
        /// Settings manager instance.
        /// </summary>
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SettingsManager();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public SettingsManager()
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer(nameof(SettingsManager),
                ApplicationDataCreateDisposition.Always);
            _underlyingSet = container.Values;
        }

        /// <summary>
        /// All setting keys.
        /// </summary>
        public ICollection<string> Keys => _underlyingSet.Keys;

        /// <summary>
        /// All settings values.
        /// </summary>
        public ICollection<object> Values => _underlyingSet.Values;

        /// <summary>
        /// Property set (WinRT storage).
        /// </summary>
        private readonly IPropertySet _underlyingSet;

        /// <summary>
        /// Count of current entries.
        /// </summary>
        public int Count => _underlyingSet.Count;

        /// <summary>
        /// Indicates whether the current settings set is read-only.
        /// </summary>
        public bool IsReadOnly => _underlyingSet.IsReadOnly;

        [Obsolete("Reserved for XAML framework.", true)]
        public object this[string key]
        {
            get { return _underlyingSet[key]; }
            set { SetValue(value, key); }
        }

        public event MapChangedEventHandler<string, object> MapChanged;

        /// <summary>
        /// Deserialize settings provision file content.
        /// </summary>
        /// <param name="xmlProvisionFile">Provision file content.</param>
        /// <returns>An awaitable task. Upon finishing, the deserialized object will be returned.</returns>
        private async Task<SettingsProvisionFile> DeserializeFileAsync(StorageFile xmlProvisionFile)
        {
            using (var fileRasStream = await xmlProvisionFile.OpenAsync(FileAccessMode.Read))
            {
                using (var netFxStream = fileRasStream.AsStream())
                {
                    var xmlSerializer = new XmlSerializer(typeof (SettingsProvisionFile));
                    return (SettingsProvisionFile) xmlSerializer.Deserialize(netFxStream);
                }
            }
        }

        /// <summary>
        /// Deserialize settings provision file and write into settings storage.
        /// </summary>
        /// <param name="xmlProvisionFile"></param>
        /// <returns></returns>
        public async Task ApplyProvisionFileAsync(StorageFile xmlProvisionFile)
        {
            var provisionContent = await DeserializeFileAsync(xmlProvisionFile);

            // Query device family.
            var appliedSettingEntriesQuery =
                provisionContent.SettingsEntry.Where(
                    entry => DeviceFamilyHelper.IsAvailable(entry.Availability.DeviceFamily));
            var currentVersion = int.Parse(provisionContent.Version);

            // Key upgrade: if version control key is present, then version control key will be validated.
            // If currenVersion < pendingUpgradeVersion, then an upgrade will be performed.
            // Otherwise, the key will be ignored.

            // If this is a new key, then simply write it into database.
            foreach (var entry in appliedSettingEntriesQuery)
            {
                if (_underlyingSet.ContainsKey($"Vcs{entry.Key}"))
                {
                    var version = GetValue<int>($"Vcs{entry.Key}");
                    if (version >= currentVersion)
                    {
                        continue;
                    }
                }

                // Parse content type.
                switch (entry.Type)
                {
                    case "System.Int32":
                        SetValue(int.Parse(entry.Value), entry.Key);
                        break;
                    case "System.Int64":
                        SetValue(long.Parse(entry.Value), entry.Key);
                        break;
                    case "System.Boolean":
                        SetValue(bool.Parse(entry.Value), entry.Key);
                        break;
                    case "System.String":
                        SetValue(entry.Value, entry.Key);
                        break;
                }

                // Update Vcs Control information
                SetValue(currentVersion, $"Vcs{entry.Key}");
            }
        }

        /// <summary>
        /// Get given typed value of item with given key.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="key">The key of item to retrieve.</param>
        /// <returns>Value of the item in given type, or default value of the type given if the key was not found.</returns>
        public T GetValue<T>([CallerMemberName] string key = null)
        {
            if (_underlyingSet.ContainsKey(key))
            {
                return (T) _underlyingSet[key];
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Set value of item with given key. If key doesn't exist, a new entry will be created.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="key">Key of the item to set.</param>
        public void SetValue(object value, [CallerMemberName] string key = null)
        {
            if (_underlyingSet.ContainsKey(key))
            {
                _underlyingSet[key] = value;
            }
            else
            {
                _underlyingSet.Add(key, value);
            }

            MapChanged?.Invoke(this, 
                new SettingsManagerItemChanedEventArgs(
                key, 
                CollectionChange.ItemChanged));
        }

        #region IDictionary<string, object> members

        /// <summary>
        /// Add entry to the dictionary with given key and value.
        /// </summary>
        /// <param name="key">Key of the entry.</param>
        /// <param name="value">Value of the entry.</param>
        public void Add(string key, object value)
        {
            _underlyingSet.Add(key, value);

            MapChanged?.Invoke(this,
                new SettingsManagerItemChanedEventArgs(
                key,
                CollectionChange.ItemInserted));
        }

        public bool ContainsKey(string key)
        {
            if (_underlyingSet.ContainsKey(key))
                return true;

            return false;
        }

        /// <summary>
        /// Remove entry with the given key.
        /// </summary>
        /// <param name="key">Key of the entry to be removed.</param>
        /// <returns>Value indicates the operation result.</returns>
        public bool Remove(string key)
        {
            var opResult = _underlyingSet.Remove(key);

            if (opResult)
            {
                MapChanged?.Invoke(this,
                  new SettingsManagerItemChanedEventArgs(
                  key,
                  CollectionChange.ItemRemoved));
            }

            return opResult;
        }

        public bool TryGetValue(string key, out object value)
        {
            return _underlyingSet.TryGetValue(key, out value);
        }

        /// <summary>
        /// Add entry to the dictionary.
        /// </summary>
        /// <param name="item">Instance of <see cref="KeyValuePair{string, object}"/></param>
        public void Add(KeyValuePair<string, object> item)
        {
            _underlyingSet.Add(item);

            MapChanged?.Invoke(this,
                new SettingsManagerItemChanedEventArgs(
                item.Key,
                CollectionChange.ItemInserted));
        }

        /// <summary>
        /// Clear the dictionary.
        /// </summary>
        public void Clear()
        {
            _underlyingSet.Clear();

            MapChanged?.Invoke(this,
                new SettingsManagerItemChanedEventArgs(
                null,
                CollectionChange.Reset));
        }

        public bool Contains(KeyValuePair<string, object> item) => _underlyingSet.Contains(item);

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) =>
            _underlyingSet.CopyTo(array, arrayIndex);

        /// <summary>
        /// Remove an entry from the dictionary.
        /// </summary>
        /// <param name="item">Instance of <see cref="KeyValuePair{string, object}"/></param>
        /// <returns>Value indicates the operation result.</returns>
        public bool Remove(KeyValuePair<string, object> item)
        {
            var opResult = _underlyingSet.Remove(item);

            if (opResult)
            {
                MapChanged?.Invoke(this,
                  new SettingsManagerItemChanedEventArgs(
                  item.Key,
                  CollectionChange.ItemRemoved));
            }

            return opResult;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _underlyingSet.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _underlyingSet.GetEnumerator();

        #endregion

        /// <summary>
        /// Internal nested device helper class.
        /// </summary>
        private static class DeviceFamilyHelper
        {
            /// <summary>
            /// Indicates whether the current setting entry is available for current device family.
            /// </summary>
            /// <param name="targetDeviceFamily">The target device family.</param>
            /// <returns>A boolean indicates whether the specificed setting entry is available for current device family.</returns>
            public static bool IsAvailable(string targetDeviceFamily)
            {
                if (string.IsNullOrEmpty(targetDeviceFamily))
                {
                    return false;
                }
                else
                {
                    if (targetDeviceFamily == "Windows.Universal")
                    {
                        return true;
                    }

                    return AnalyticsInfo.VersionInfo.DeviceFamily == targetDeviceFamily;
                }
            }
        }
    }

    /// <summary>
    /// Event arguments class for item changed events of <see cref="SettingsManager"/>.
    /// </summary>
    public class SettingsManagerItemChanedEventArgs : IMapChangedEventArgs<string>
    {
        /// <summary>
        /// Type of the change.
        /// </summary>
        public CollectionChange CollectionChange { get; }

        /// <summary>
        /// Key of the item changed.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Class constructor that creates instance of <see cref="SettingsManagerItemChanedEventArgs"/>.
        /// </summary>
        /// <param name="key">Key of the item changed.</param>
        /// <param name="change">Type of the change.</param>
        public SettingsManagerItemChanedEventArgs(string key,
            CollectionChange change)
        {
            Key = key;
            CollectionChange = change;
        }
    }
}
