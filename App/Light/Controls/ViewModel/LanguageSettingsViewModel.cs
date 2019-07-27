using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using Light.Common;
using Light.Managed.Settings;

namespace Light.Controls.ViewModel
{
    /// <summary>
    /// Language sub-viewmodel for common settings viewmodel.
    /// </summary>
    public class LanguageSettingsViewModel : ViewModelBase
    {
        private const string InterfaceLangKey = "InterfaceLanguage";
        private ObservableCollection<LanguageSettingsEntry> _entries;
        private int _selectedIndex;
        private readonly string _currentLanguageOverride;
        private string _currentSelection;
        private bool _isRestartPromptVisible;
        private bool _isOperationable;

        /// <summary>
        /// Collection of available languages.
        /// </summary>
        public ObservableCollection<LanguageSettingsEntry> Entries
        {
            get { return _entries; }
            set
            {
                _entries = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Currently selected language entry index.
        /// </summary>
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;

                if (Entries != null && _isOperationable)
                {
                    _currentSelection = Entries[_selectedIndex].LanguageTag;
                    SettingsManager.Instance.SetValue(_currentSelection, InterfaceLangKey);
                    IsRestartPromptVisible = _currentSelection != _currentLanguageOverride;
                }

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
        /// Class constructor.
        /// </summary>
        public LanguageSettingsViewModel()
        {
            SelectedIndex = -1;
            Entries = new ObservableCollection<LanguageSettingsEntry>();

            var overrideLang = SettingsManager.Instance.GetValue<string>(InterfaceLangKey);
            _currentSelection = _currentLanguageOverride = overrideLang;
        }

        /// <summary>
        /// Load Settings data.
        /// </summary>
        public void LoadData()
        {
            Entries.Add(new LanguageSettingsEntry(CommonSharedStrings.GetString("UseOSLanguageSettings"), string.Empty));
            Entries.Add(new LanguageSettingsEntry("English (United States)", "en-US"));
            Entries.Add(new LanguageSettingsEntry("English (United Kingdom)", "en-GB"));
            Entries.Add(new LanguageSettingsEntry("简体中文 (中华人民共和国)", "zh-CN"));
            
            var isOverrideLangSet = false;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (_currentSelection == Entries[i].LanguageTag)
                {
                    SelectedIndex = i;
                    isOverrideLangSet = true;
                    break;
                }
            }

            if (!isOverrideLangSet)
            {
                SelectedIndex = 0;
            }

            IsRestartPromptVisible = _currentSelection != _currentLanguageOverride;
            _isOperationable = true;
        }

        /// <summary>
        /// Clean up method.
        /// </summary>
        public override void Cleanup()
        {
            _isOperationable = false;
            SelectedIndex = -1;
            Entries.Clear();
            base.Cleanup();
        }
    }

    /// <summary>
    /// Language selection entry.
    /// </summary>
    public class LanguageSettingsEntry
    {
        public string LanguageTag { get; }
        public string Description { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="description">Language description.</param>
        /// <param name="languageTag">BCP-47 language tag.</param>
        public LanguageSettingsEntry(string description, string languageTag)
        {
            Description = description;
            LanguageTag = languageTag;
        }
    }
}
