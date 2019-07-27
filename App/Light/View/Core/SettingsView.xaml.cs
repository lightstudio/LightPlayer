using Light.Common;
using Light.Model;
using Light.Shell;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Navigation;

namespace Light.View.Core
{
    /// <summary>
    /// Settings View.
    /// </summary>
    public sealed partial class SettingsView : BaseContentPage
    {
        private readonly NavigationHelper m_helper;

        public ObservableCollection<SettingsSection> SettingsEntries { get; private set; }

        /// <summary>
        /// Initializes new instance of <see cref="SettingsView"/>.
        /// </summary>
        public SettingsView()
        {
            this.InitializeComponent();

            m_helper = new NavigationHelper(this);           
            SettingsEntries = new ObservableCollection<SettingsSection>();
        }

        /// <summary>
        /// Load settings entries.
        /// </summary>
        private void LoadEntries()
        {
            if (SettingsEntries.Count > 0) return;

            var resLoader = ResourceLoader.GetForViewIndependentUse();

            SettingsEntries.Add(new SettingsSection(resLoader.GetString("SecThemeAndLanguage"), "\xE2B1", SettingsSection.SettingsType.Interface));
            SettingsEntries.Add(new SettingsSection(resLoader.GetString("SecLibrary"), "\xE8B7", SettingsSection.SettingsType.Library));
            SettingsEntries.Add(new SettingsSection(resLoader.GetString("SecPlaybackAndLyrics"), "\xE7F6", SettingsSection.SettingsType.Playback));
            SettingsEntries.Add(new SettingsSection(resLoader.GetString("SecPrivacy"), "\xE72E", SettingsSection.SettingsType.Privacy));
#if DEBUG
            SettingsEntries.Add(new SettingsSection(resLoader.GetString("SecDebug"), "\xE98F", SettingsSection.SettingsType.Debug));
#endif
        }

        /// <summary>
        /// Handles page navigation.
        /// </summary>
        /// <param name="e">Instance of <see cref="NavigationEventArgs"/>.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Set title
            DesktopTitleViewConfiguration.SetTitleBarText(CommonSharedStrings.SettingsTitle);

            m_helper.OnNavigatedTo(e);
            base.OnNavigatedTo(e);
            
            LoadEntries();
        }

        /// <summary>
        /// Handles page exit.
        /// </summary>
        /// <param name="e">Instance of <see cref="NavigationEventArgs"/>.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            m_helper.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }
    }
}
