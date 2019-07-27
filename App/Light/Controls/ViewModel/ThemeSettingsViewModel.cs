using Light.Managed.Settings;
using GalaSoft.MvvmLight;
using Light.Common;

namespace Light.Controls.ViewModel
{
    /// <summary>
    /// Theme settings sub-viewmodel for common settings control viewmodel.
    /// </summary>
    public class ThemeSettingsViewModel : ViewModelBase
    {
        /// <summary>
        /// Settings key.
        /// </summary>
        const string InterfaceThemeKey = "InterfaceTheme";
        private readonly string _currentSetting;

        private bool _isDarkThemeSettingsChecked;
        private bool _isLightThemeSettingsChecked;
        private bool _isUseOsThemeSettingsChecked;
        private bool _isRestartPromptVisible;
        private bool _isThemeSettingsAvailable;

        /// <summary>
        /// Indicates whether dark theme settings radio button is checked.
        /// </summary>
        public bool IsDarkThemeSettingsChecked
        {
            get { return _isDarkThemeSettingsChecked; }
            set
            {
                _isDarkThemeSettingsChecked = value;
                SettingsManager.Instance.SetValue("Dark", InterfaceThemeKey);
                IsRestartPromptVisible = _currentSetting != "Dark";
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Indicates whether light theme settings radio button is checked.
        /// </summary>
        public bool IsLightThemeSettingsChecked
        {
            get { return _isLightThemeSettingsChecked; }
            set
            {
                _isLightThemeSettingsChecked = value;
                SettingsManager.Instance.SetValue("Light", InterfaceThemeKey);
                IsRestartPromptVisible = _currentSetting != "Light";
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Indicates whether OS theme settings radio button is checked.
        /// </summary>
        public bool IsUseOsThemeSettingsChecked
        {
            get { return _isUseOsThemeSettingsChecked; }
            set
            {
                _isUseOsThemeSettingsChecked = value;
                SettingsManager.Instance.SetValue(string.Empty, InterfaceThemeKey);
                IsRestartPromptVisible = _currentSetting != string.Empty;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Indicates whether the restart app prompt is visible.
        /// </summary>
        public bool IsRestartPromptVisible
        {
            get { return _isRestartPromptVisible; }
            set
            {
                _isRestartPromptVisible = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Indicates whether theme settings is available on this OS release.
        /// </summary>
        public bool IsThemeSettingsAvailable
        {
            get { return _isThemeSettingsAvailable; }
            set
            {
                _isThemeSettingsAvailable = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public ThemeSettingsViewModel()
        {
            // OS release specific logic
            IsThemeSettingsAvailable = PlatformInfo.IsRedstoneRelease;

            var theme = SettingsManager.Instance.GetValue<string>(InterfaceThemeKey);
            _currentSetting = theme;
            switch (theme)
            {
                case "Dark":
                    _isDarkThemeSettingsChecked = true;
                    break;
                case "Light":
                    _isLightThemeSettingsChecked = true;
                    break;
                default:
                    _isUseOsThemeSettingsChecked = true;
                    break;
            }
            IsRestartPromptVisible = false;
        }
    }
}
