using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.View.InitialExpeience;
using Light.ViewModel.Core;
using System;
using System.Windows.Input;

namespace Light.Controls.ViewModel
{
    /// <summary>
    /// ViewModel for most settings view.
    /// </summary>
    public class CommonSettingsControlViewModel : ViewModelBase
    {
        private ICommand _commonButtonCommand;
        private ThemeSettingsViewModel _themeSettingsSubVm;
        private LanguageSettingsViewModel _langSettingsVm;
        private LibrarySettingsViewModel _libSettingsVm;
        private ExtensionSettingsViewModel _extensionVm;
        private OnlineMetadataSettingsViewModel _onlineMetadataSettingsVm;
        private PrivacyViewModel _privacySettingsVm;
        private SampleRateSettingsViewModel _sampleRateSettingsVm;

        /// <summary>
        /// Current ETW channel ID for debugging.
        /// </summary>
        public string EtwChannelId => ApplicationServiceBase.App.EtwChannelId.ToString();

        /// <summary>
        /// Common button command handler.
        /// </summary>
        public ICommand CommonButtonCommand
        {
            get { return _commonButtonCommand; }
            set
            {
                if (_commonButtonCommand == value) return;
                _commonButtonCommand = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Exension settings sub viewmodel.
        /// </summary>
        public ExtensionSettingsViewModel ExtensionVm
        {
            get { return _extensionVm; }
            set
            {
                _extensionVm = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Language settings sub viewmodel.
        /// </summary>
        public LanguageSettingsViewModel LangSettingsVm
        {
            get { return _langSettingsVm; }
            set
            {
                _langSettingsVm = value;
                RaisePropertyChanged();
            }
        }

        public SampleRateSettingsViewModel SampleRateSettingsVm
        {
            get { return _sampleRateSettingsVm; }
            set
            {
                _sampleRateSettingsVm = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Library settings sub viewmodel.
        /// </summary>
        public LibrarySettingsViewModel LibSettingsVm
        {
            get { return _libSettingsVm; }
            set
            {
                _libSettingsVm = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Online metadata settings viewmodel (partially loaded).
        /// </summary>
        public OnlineMetadataSettingsViewModel OnlineMetadataSettingsVm
        {
            get { return _onlineMetadataSettingsVm; }
            set
            {
                _onlineMetadataSettingsVm = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Privacy settings viewmodel.
        /// </summary>
        public PrivacyViewModel PrivacySettingsVm
        {
            get { return _privacySettingsVm; }
            set
            {
                _privacySettingsVm = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Theme settings sub viewmodel.
        /// </summary>
        public ThemeSettingsViewModel ThemeSettingsSubVm
        {
            get { return _themeSettingsSubVm; }
            set
            {
                _themeSettingsSubVm = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommonSettingsControlViewModel()
        {
            // Public commands
            CommonButtonCommand = new CommonButtonCommand();

            // Sub viewmodels
            ExtensionVm = new ExtensionSettingsViewModel();
            LangSettingsVm = new LanguageSettingsViewModel();
            SampleRateSettingsVm = new SampleRateSettingsViewModel();
            LibSettingsVm = new LibrarySettingsViewModel();
            OnlineMetadataSettingsVm = new OnlineMetadataSettingsViewModel();
            ThemeSettingsSubVm = new ThemeSettingsViewModel();
            PrivacySettingsVm = new PrivacyViewModel();
        }
    }

    /// <summary>
    /// Common Button Command for Settings View.
    /// Currently it handles IAP(test) and Metadata mgmt.
    /// </summary>
    public class CommonButtonCommand : ICommand
    {
        public bool CanExecute(object parameter) => parameter is string;

        /// <summary>
        /// Event route entry point.
        /// </summary>
        /// <param name="parameter">Requested route.</param>
        public void Execute(object parameter)
        {
            if (parameter == null)
            {
                return;
            }

            switch ((string) parameter)
            {
                case "MetadataMgmt":
                    GoToMetadataMgmtPage();
                    break;
            }
        }

        /// <summary>
        /// Handle metadata mgmt navigation.
        /// </summary>
        private void GoToMetadataMgmtPage()
        {
            // Call FrameView to finish navigation.
            Messenger.Default.Send(new GenericMessage<Tuple<Type, int>>(
                new Tuple<Type, int>(typeof(OnlineMetadataSettingsView), 0)), CommonSharedStrings.FrameViewNavigationIntMessageToken);
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }
}
