using GalaSoft.MvvmLight;
using Light.Common;
using Light.Managed.Settings;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;

namespace Light.Controls.ViewModel
{
    public class SampleRateSettingsViewModel : ViewModelBase
    {
        const string SampleRateKey = "PreferredSampleRate";
        const string AlwaysResampleKey = "AlwaysResample";

        private ObservableCollection<SampleRateSettingsEntry> _entries;
        private int _selectedIndex;
        private readonly int _currentSampleRateOverride;
        private readonly bool _alwaysResampleOverride;
        private int _currentSelection;
        private bool _alwaysResample;
        private bool _isNextTrackPromptVisible;
        private bool _isOperationable;
        private string _currentSystemSampleRate;

        private void CheckNextTrackPromptVisibility()
        {
            if (_currentSelection == _currentSampleRateOverride &&
                _alwaysResample == _alwaysResampleOverride)
            {
                IsNextTrackPromptVisible = false;
            }
            else
            {
                IsNextTrackPromptVisible = true;
            }
        }

        public ObservableCollection<SampleRateSettingsEntry> Entries
        {
            get { return _entries; }
            set
            {
                _entries = value;
                RaisePropertyChanged();
            }
        }

        public bool AlwaysResample
        {
            get
            {
                return _alwaysResample;
            }
            set
            {
                _alwaysResample = value;

                SettingsManager.Instance.SetValue(_alwaysResample);
                CheckNextTrackPromptVisibility();
            }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;

                if (Entries != null && _isOperationable)
                {
                    _currentSelection = Entries[_selectedIndex].SampleRate;
                    SettingsManager.Instance.SetValue(_currentSelection, SampleRateKey);
                    CheckNextTrackPromptVisibility();
                }

                RaisePropertyChanged();
            }
        }

        public bool IsNextTrackPromptVisible
        {
            get { return _isNextTrackPromptVisible; }
            set
            {
                _isNextTrackPromptVisible = value;
                RaisePropertyChanged();
            }
        }

        public string CurrentSystemSampleRate
        {
            get
            {
                return _currentSystemSampleRate;
            }
            set
            {
                _currentSystemSampleRate = value;
                RaisePropertyChanged();
            }
        }

        public SampleRateSettingsViewModel()
        {
            SelectedIndex = -1;
            Entries = new ObservableCollection<SampleRateSettingsEntry>();

            _currentSelection = _currentSampleRateOverride = SettingsManager.Instance.GetValue<int>(SampleRateKey);
            _alwaysResample = _alwaysResampleOverride = SettingsManager.Instance.GetValue<bool>(AlwaysResampleKey);
        }

        public void LoadData()
        {
            Entries.Add(new SampleRateSettingsEntry(0, CommonSharedStrings.GetString("UseSystemSampleRate")));
            Entries.Add(new SampleRateSettingsEntry(44100, "44100 Hz"));
            Entries.Add(new SampleRateSettingsEntry(48000, "48000 Hz"));
            Entries.Add(new SampleRateSettingsEntry(88200, "88200 Hz"));
            Entries.Add(new SampleRateSettingsEntry(96000, "96000 Hz"));
            Entries.Add(new SampleRateSettingsEntry(192000, "192000 Hz"));

            var isCurrentSet = false;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (_currentSelection == Entries[i].SampleRate)
                {
                    SelectedIndex = i;
                    isCurrentSet = true;
                    break;
                }
            }

            if (!isCurrentSet)
            {
                SelectedIndex = 0;
            }
            RefreshSystemSampleRate();

            CheckNextTrackPromptVisibility();
            _isOperationable = true;
        }

        public async void RefreshSystemSampleRate()
        {
            var rate = await ((App)Application.Current).UpdateSampleRate();
            CurrentSystemSampleRate = $"{rate} Hz";
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

    public class SampleRateSettingsEntry
    {
        public int SampleRate { get; }
        public string Description { get; }

        public SampleRateSettingsEntry(int rate, string desc)
        {
            SampleRate = rate;
            Description = desc;
        }
    }
}
