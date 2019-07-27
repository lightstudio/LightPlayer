using Light.Managed.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Managed.Tools
{
    static public class PathExclusion
    {
        static HashSet<string> ExcludedPath;
        static PathExclusion()
        {
            var excluded = SettingsManager.Instance.GetValue<string[]>("PathExclusion");
            ExcludedPath = new HashSet<string>(excluded?? new string[0]);
        }

        static private void Save()
        {
            if (ExcludedPath.Count == 0)
            {
                SettingsManager.Instance.SetValue(null, "PathExclusion");
            }
            else
            {
                SettingsManager.Instance.SetValue(ExcludedPath.ToArray(), "PathExclusion");
            }
        }

        static public string[] GetExcludedPath()
        {
            return ExcludedPath.ToArray();
        }

        static public void RemoveExcludedPath(string path)
        {
            ExcludedPath.Remove(path);
            Save();
        }

        static public void AddExcludedPath(string path)
        {
            ExcludedPath.Add(path);
            Save();
        }
    }
}
