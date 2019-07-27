using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Light.Controls.ViewModel;
using Windows.ApplicationModel;
using Windows.System;
using Light.Common;
using Light.Model;
using Windows.UI.Xaml.Documents;

namespace Light.Controls
{
    /// <summary>
    /// Control that hosts configurable settings sections.
    /// </summary>
    public sealed partial class CommonSettings : UserControl
    {
        private readonly CommonSettingsControlViewModel viewModel;

        #region Universal settings property
        public static readonly DependencyProperty SettingsTypesProperty =
            DependencyProperty.Register(nameof(SettingsTypes), typeof(SettingsSection.SettingsType?), typeof(CommonSettings),
                new PropertyMetadata(default(SettingsSection.SettingsType?), OnSettingsTypesChanged));

        /// <summary>
        /// Type of settings to shown.
        /// </summary>
        public SettingsSection.SettingsType? SettingsTypes
        {
            get
            {
                return (SettingsSection.SettingsType?) GetValue(SettingsTypesProperty);
            }
            set
            {
                SetValue(SettingsTypesProperty, value);
            }
        }

        #endregion

        #region Language and Theme
        public static readonly DependencyProperty IsInterfaceSettingsVisibleProperty =
            DependencyProperty.Register(nameof(IsInterfaceSettingsVisible), typeof(bool), typeof(CommonSettings),
                new PropertyMetadata(default(bool)));

        public bool IsInterfaceSettingsVisible
        {
            get
            {
                return (bool)GetValue(IsInterfaceSettingsVisibleProperty);
            }
            set
            {
                SetValue(IsInterfaceSettingsVisibleProperty, value);
            }
        }
        #endregion

        #region Debug
        public static readonly DependencyProperty IsDebugSettingsVisibleProperty =
            DependencyProperty.Register(nameof(IsDebugSettingsVisible), typeof(bool), typeof(CommonSettings),
                new PropertyMetadata(default(bool)));

        public bool IsDebugSettingsVisible
        {
            get
            {
                return (bool) GetValue(IsDebugSettingsVisibleProperty);
            }
            set
            {
                SetValue(IsDebugSettingsVisibleProperty, value);
            }
        }
        #endregion

        #region Playback and Lyrics
        public static readonly DependencyProperty IsExtensionSettingsVisibleProperty =
            DependencyProperty.Register(nameof(IsExtensionSettingsVisible), typeof(bool), typeof(CommonSettings),
                new PropertyMetadata(default(bool)));

        public bool IsExtensionSettingsVisible
        {
            get
            {
                return (bool)GetValue(IsExtensionSettingsVisibleProperty);
            }
            set
            {
                SetValue(IsExtensionSettingsVisibleProperty, value);
            }
        }

        public static readonly DependencyProperty IsPlaybackAndLyricsSettingsVisibleProperty =
            DependencyProperty.Register(nameof(IsPlaybackAndLyricsSettingsVisible), typeof(bool), typeof(CommonSettings),
                new PropertyMetadata(default(bool)));

        public bool IsPlaybackAndLyricsSettingsVisible
        {
            get
            {
                return (bool)GetValue(IsPlaybackAndLyricsSettingsVisibleProperty);
            }
            set
            {
                SetValue(IsPlaybackAndLyricsSettingsVisibleProperty, value);
            }
        }

        public static readonly DependencyProperty IsLyricSettingsVisibleProperty =
            DependencyProperty.Register(nameof(IsLyricSettingsVisible), typeof(bool), typeof(CommonSettings),
                new PropertyMetadata(default(bool)));

        /// <summary>
        /// Indicates whether lyrics settings is visible.
        /// </summary>
        public bool IsLyricSettingsVisible
        {
            get
            {
                return (bool)GetValue(IsLyricSettingsVisibleProperty);
            }
            set
            {
                SetValue(IsLyricSettingsVisibleProperty, value);
            }
        }
        #endregion

        #region Library
        public static readonly DependencyProperty IsFullMetadataSettingsVisibleProperty = 
            DependencyProperty.Register(nameof(IsFullMetadataSettingsVisibleProperty), typeof(bool), typeof(CommonSettings),
                new PropertyMetadata(default(bool)));

        /// <summary>
        /// Indicates whether full metadata settings is visible.
        /// </summary>
        public bool IsFullMetadataSettingsVisible
        {
            get
            {
                return (bool) GetValue(IsFullMetadataSettingsVisibleProperty);
            }
            set
            {
                SetValue(IsFullMetadataSettingsVisibleProperty, value);
            }
        }

        public static readonly DependencyProperty IsLibrarySettingsVisibleProperty =
            DependencyProperty.Register(nameof(IsLibrarySettingsVisibleProperty), typeof(bool), typeof(CommonSettings),
                new PropertyMetadata(default(bool)));

        /// <summary>
        /// Indicates whether library settings is visible.
        /// </summary>
        public bool IsLibrarySettingsVisible
        {
            get
            {
                return (bool)GetValue(IsLibrarySettingsVisibleProperty);
            }
            set
            {
                SetValue(IsLibrarySettingsVisibleProperty, value);
            }
        }
        #endregion

        #region Privacy
        public static readonly DependencyProperty IsPrivacySettingsVisibleProperty =
            DependencyProperty.Register(nameof(IsPrivacySettingsVisibleProperty), typeof(bool), typeof(CommonSettings),
                new PropertyMetadata(default(bool)));

        /// <summary>
        /// Indicates whether privacy settings is visible.
        /// </summary>
        public bool IsPrivacySettingsVisible
        {
            get
            {
                return (bool)GetValue(IsPrivacySettingsVisibleProperty);
            }
            set
            {
                SetValue(IsPrivacySettingsVisibleProperty, value);
            }
        }
        #endregion

        /// <summary>
        /// Indicates whether frame rate counter is enabled.
        /// </summary>
        /// <remarks>Debug only.</remarks>
        public bool IsFramerateCounterEnabled
        {
            get { return Application.Current.DebugSettings.EnableFrameRateCounter; }
            set { Application.Current.DebugSettings.EnableFrameRateCounter = value; }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CommonSettings()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            viewModel = new CommonSettingsControlViewModel();

            // Default settings
            IsInterfaceSettingsVisible = true;
            IsPlaybackAndLyricsSettingsVisible = true;
            IsLibrarySettingsVisible = true;
            IsPrivacySettingsVisible = true;
            IsDebugSettingsVisible = false;
        }

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            viewModel.ExtensionVm.Cleanup();

            // Will not enable immediately for OOBE experience
            if (IsFullMetadataSettingsVisible)
            {
                viewModel.LibSettingsVm.PostNotifyExternalWorkers();
            }

            viewModel.LibSettingsVm.Cleanup();
            viewModel.LangSettingsVm.Cleanup();
            viewModel.SampleRateSettingsVm.Cleanup();
            viewModel.Cleanup();
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            viewModel.ExtensionVm.LoadLyricsSources();
            viewModel.LangSettingsVm.LoadData();
            viewModel.SampleRateSettingsVm.LoadData();
            await viewModel.LibSettingsVm.LoadFoldersAsync();
        }

        private async void HyperlinkButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var file = await Package.Current.InstalledLocation.GetFileAsync(CommonSharedStrings.OssLicenseFileName);
            await Launcher.LaunchFileAsync(file);
        }

        private void OnAuthorizeLearnMoreClick(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            AccessAuthHelpTextBlock.Visibility = Visibility.Visible;
        }

        private void OnRefreshSampleRateClick(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            viewModel.SampleRateSettingsVm.RefreshSystemSampleRate();
        }

        /// <summary>
        /// Handles settings types changes.
        /// </summary>
        /// <param name="d">Instance of <see cref="DependencyObject"/>.</param>
        /// <param name="e">Instance of <see cref="DependencyPropertyChangedEventArgs"/>.</param>
        private static void OnSettingsTypesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Sanity check (we will not handle if value is not set)
            if (((SettingsSection.SettingsType?) e?.NewValue) == null || d == null) return;

            var settingsHost = (CommonSettings) d;
            var enumTypesSelected = (SettingsSection.SettingsType) e.NewValue;

            settingsHost.IsInterfaceSettingsVisible = false;
            settingsHost.IsPlaybackAndLyricsSettingsVisible = false;
            settingsHost.IsPrivacySettingsVisible = false;
            settingsHost.IsDebugSettingsVisible = false;
            settingsHost.IsLibrarySettingsVisible = false;

            switch (enumTypesSelected)
            {
                case SettingsSection.SettingsType.Interface:
                    settingsHost.IsInterfaceSettingsVisible = true;
                    break;
                case SettingsSection.SettingsType.Library:
                    settingsHost.IsLibrarySettingsVisible = true;
                    settingsHost.IsFullMetadataSettingsVisible = true;
                    break;
                case SettingsSection.SettingsType.Playback:
                    settingsHost.IsPlaybackAndLyricsSettingsVisible = true;
                    settingsHost.IsExtensionSettingsVisible = true;
                    settingsHost.IsLyricSettingsVisible = true;
                    break;
                case SettingsSection.SettingsType.Privacy:
                    settingsHost.IsPrivacySettingsVisible = true;
                    break;
                case SettingsSection.SettingsType.Debug:
                    settingsHost.IsDebugSettingsVisible = true;
                    break;
            }
        }
    }
}
