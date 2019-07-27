using System.Collections.Generic;
using Light.Managed.Settings;
using Light.NETCore.IO;

namespace Light.Common
{
    internal class PlaylistRequestCallbackParams
    {
        public List<string> Playlist { get; set; }
        public int Current { get; set; }
    }
    enum DetailedPageButton
    {
        Play,
        AddToList,
        Share,
        Edit,
        Delete
    }
    static class Shared
    {
        public static FutureAccessList FutureAccessList;
        public static string PlaylistIndex;

        static Shared()
        {
            FutureAccessList = new FutureAccessList();
            PlaylistIndex = "";
        }

        public static int GetGroupingOption(string scenario)
        {
            var key = string.Format(CommonSharedStrings.GroupOptionTemplate, scenario);

            return (SettingsManager.Instance.ContainsKey(key))
                ? SettingsManager.Instance.GetValue<int>(key)
                : 0;
        }
        public static void SetGroupingOption(string scenario,int value)
        {
            var key = string.Format(CommonSharedStrings.GroupOptionTemplate, scenario);
            SettingsManager.Instance.SetValue(value, key);
        }
    }
}
