using ChakraBridge;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Light.Lyrics.External
{
    //TODO: store serialized scripts.
    public static class SourceScriptManager
    {
        static string _scriptFolder = null;
        static string _infoFilePath = null;
        //static Dictionary<string, string> _scriptFiles = null;//path, script content
        static Dictionary<string, string> _scriptInfo = null;//name, path
        static Dictionary<string, JsDownloadSource> _scriptSource = null;//name, source
        static SourceScriptManager()
        {
            var local = ApplicationData.Current.LocalFolder.Path;
            _scriptFolder = Path.Combine(local, "Script");
            _infoFilePath = Path.Combine(local, "Script.json");
            try
            {
                if (File.Exists(_infoFilePath))
                {
                    var json = File.ReadAllText(_infoFilePath);
                    _scriptInfo = JsonConvert.DeserializeObject<Dictionary<string,string>>(json);
                }
                else
                    _scriptInfo = new Dictionary<string, string>();
            }
            catch
            {
                _scriptInfo = new Dictionary<string, string>();
            }
            if (!Directory.Exists(_scriptFolder))
            {
                Directory.CreateDirectory(_scriptFolder);
            }
            var files = Directory.GetFiles(_scriptFolder);
            _scriptSource = new Dictionary<string, JsDownloadSource>(_scriptInfo.Count);
            foreach (var kvp in _scriptInfo)
            {
                try
                {
                    var scriptContent = File.ReadAllText(kvp.Value, Encoding.UTF8);
                    var source = new JsDownloadSource(scriptContent, kvp.Key);
                    _scriptSource.Add(kvp.Key, source);
                }
                catch
                {

                }
            }
        }
        private static void SaveInfo()
        {
            File.WriteAllText(_infoFilePath, JsonConvert.SerializeObject(_scriptInfo), Encoding.UTF8);
        }
        /// <summary>
        /// Remove unreferenced script files from disk,
        /// remove scripts that does not exist on disk from json.
        /// </summary>
        public static void Cleanup()
        {
            var _toremove = from info in _scriptInfo where !File.Exists(info.Value) select info.Key;
            foreach (var item in _toremove)
                _scriptInfo.Remove(item);
            SaveInfo();
            var files = Directory.GetFiles(_scriptFolder);
            foreach (var file in files)
            {
                if (!_scriptInfo.ContainsValue(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
            }
        }
        /// <summary>
        /// Add a script.
        /// </summary>
        /// <param name="name">Unique script name</param>
        /// <param name="content">Script content string</param>
        /// <param name="overwrite">Script with the same name will be replaced by new script.</param>
        /// <exception cref="InvalidOperationException">parameter "overwrite" is false and parameter "name" already exists.</exception>
        /// <exception cref="JavaScriptException">An exception is thrown during JsDownloadSource creation.</exception>
        public static void AddScript(string name, string content, bool overwrite=true)
        {
            lock (_scriptInfo)
            {
                _scriptSource.Add(name, new JsDownloadSource(content, name));
                if (_scriptInfo.ContainsKey(name) && !overwrite)
                    throw new InvalidOperationException($"script {name} already exists.");
                string path = null;
                if (_scriptInfo.ContainsKey(name))
                    path = _scriptInfo[name];
                else
                {
                    generate:
                    path = Path.Combine(_scriptFolder, Path.GetRandomFileName());
                    if (File.Exists(path)) goto generate;
                    _scriptInfo.Add(name, path);
                    SaveInfo();
                }
                File.WriteAllText(path, content, Encoding.UTF8);
            }
        }
        /// <summary>
        /// Remove a script.
        /// </summary>
        /// <param name="name">Unique script name</param>
        /// <exception cref="InvalidOperationException">parameter "name" does not exist.</exception>
        public static void RemoveScript(string name)
        {
            lock (_scriptInfo)
            {
                if (!_scriptInfo.ContainsKey(name))
                    throw new InvalidOperationException($"{name} does not exist.");
                var path = _scriptInfo[name];
                _scriptInfo.Remove(name);
                _scriptSource[name].Dispose();
                _scriptSource.Remove(name);
                SaveInfo();
                File.Delete(path);
            }
        }
        /// <summary>
        /// Read and return a script
        /// </summary>
        /// <param name="name">Unique script name</param>
        /// <returns>Script content string</returns>
        /// <exception cref="InvalidOperationException">parameter "name" doesn not exist.</exception>
        public static JsDownloadSource GetScript(string name)
        {
            lock (_scriptInfo)
            {
                if (!_scriptSource.ContainsKey(name))
                    throw new InvalidOperationException($"{name} does not exist.");
                return _scriptSource[name];
            }
        }
        /// <summary>
        /// Rename a script.
        /// </summary>
        /// <param name="oldName">Old unique script name</param>
        /// <param name="newName">New unique script name</param>
        /// <exception cref="InvalidOperationException">oldName does not exist or newName already exists.</exception>
        public static void RenameScript(string oldName, string newName)
        {
            lock (_scriptInfo)
            {
                if (!_scriptInfo.ContainsKey(oldName))
                    throw new InvalidOperationException($"{oldName} does not exist.");
                if (_scriptInfo.ContainsKey(newName))
                    throw new InvalidOperationException($"{newName} already exists.");
                _scriptInfo.Add(newName, _scriptInfo[oldName]);
                _scriptInfo.Remove(oldName);
                var source = _scriptSource[oldName];
                source.Name = newName;
                _scriptSource.Remove(oldName);
                _scriptSource.Add(newName, source);
            }
        }
        /// <summary>
        /// Get all scripts in a dictionary.
        /// </summary>
        /// <returns>name-content dictionary of all scripts.</returns>
        public static JsDownloadSource[] GetAllScripts()
        {
            return _scriptSource.Values.ToArray();
        } 
    }
}
