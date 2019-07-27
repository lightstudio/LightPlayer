using Light.Managed.Settings;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace Light.Managed.Tools
{
    /// <summary>
    /// Now playing page state manager.
    /// </summary>
    public static class NowPlayingStateManager
    {
        public const double DefaultVolume = 50.0;
        public const int DefaultMode = 0;

        /// <summary>
        /// Indicates whether to show now playing list or not.
        /// </summary>
        public static bool ShowNowPlayingList
        {
            get
            {
                if (SettingsManager.Instance.ContainsKey(nameof(ShowNowPlayingList)))
                    return SettingsManager.Instance.GetValue<bool>();
                return false;
            }
            set
            {
                SettingsManager.Instance.SetValue(value);
            }
        }

        /// <summary>
        /// Indicates current now playing list display mode.
        /// </summary>
        public static SplitViewDisplayMode NowPlayingListDisplayMode
        {
            get
            {
                if (SettingsManager.Instance.ContainsKey(nameof(NowPlayingListDisplayMode)))
                    return SettingsManager.Instance.GetValue<SplitViewDisplayMode>();
                return SplitViewDisplayMode.Overlay;
            }
            set
            {
                SettingsManager.Instance.SetValue((int) value);
            }
        }

        /// <summary>
        /// App volume.
        /// </summary>
        /// <remarks>This value ranges from 0 - 100.</remarks>
        public static double Volume
        {
            get
            {
                if (SettingsManager.Instance.ContainsKey(nameof(Volume)))
                    return SettingsManager.Instance.GetValue<double>();

                SettingsManager.Instance.SetValue(DefaultVolume);
                return DefaultVolume;
            }
            set
            {
                SettingsManager.Instance.SetValue(value);
            }
        }

        public static int PlaybackMode
        {
            get
            {
                if (SettingsManager.Instance.ContainsKey(nameof(PlaybackMode)))
                    return SettingsManager.Instance.GetValue<int>();

                SettingsManager.Instance.SetValue(DefaultMode);
                return DefaultMode;                
            }
            set
            {
                SettingsManager.Instance.SetValue(value);
            }
        }

        /// <summary>
        /// Indicates whether shuffle playback is enabled.
        /// </summary>
        public static bool IsShuffleEnabled
        {
            get
            {
                if (SettingsManager.Instance.ContainsKey(nameof(IsShuffleEnabled)))
                    return SettingsManager.Instance.GetValue<bool>();
                return false;
            }
            set
            {
                SettingsManager.Instance.SetValue(value);
            }
        }
    }

    /// <summary>
    /// General purpose page state manager.
    /// </summary>
    public static class PageStateManger
    {
        public static object GetPageStatus(string pageId)
        {
            if (SettingsManager.Instance.ContainsKey($"{pageId}.NavigateStatus"))
            {
                return SettingsManager.Instance.GetValue<object>($"{pageId}.NavigateStatus");
            }
            throw new KeyNotFoundException();
        }

        public static void SetPageStatus(string pageId, object status) =>
            SettingsManager.Instance.SetValue(status, $"{pageId}.NavigateStatus");

        public static bool HasStatus(string pageId) =>
            SettingsManager.Instance.ContainsKey($"{pageId}.NavigateStatus");
    }
}
