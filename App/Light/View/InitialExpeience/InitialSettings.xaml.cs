using Light.Common;
using Light.Core;
using Light.Lyrics.External;
using Light.Managed.Servicing;
using Light.Managed.Settings;
using Light.Phone.View;
using Light.View.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Light.View.InitialExpeience
{
    /// <summary>
    /// Initial SettingsExperience Page.
    /// </summary>
    public sealed partial class InitialSettings : Page
    {
        private readonly NavigationHelper _helper;
        private HashSet<Guid> _filePassThru = null;

        public InitialSettings()
        {
            this.InitializeComponent();
            _helper = new NavigationHelper(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _helper.OnNavigatedTo(e);
            base.OnNavigatedTo(e);

            if (PlatformInfo.CurrentPlatform == Platform.WindowsDesktop)
            {
                var title = ApplicationView.GetForCurrentView().TitleBar;
                if (!ApplicationView.GetForCurrentView().IsFullScreenMode)
                {
                    ContentScrollViewer.Margin = new Thickness(0, 30, 0, 0);
                }
                title.ButtonBackgroundColor = Colors.Transparent;
                title.ButtonInactiveBackgroundColor = Colors.Transparent;
            }

            if (e.Parameter is HashSet<Guid>) _filePassThru = (HashSet<Guid>)e.Parameter;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _helper.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }

        async void OnFinishedSettingsButtonClicked(object sender, RoutedEventArgs e)
        {
            if (EnableThirdPartyLyricsToggle.IsOn)
            {
                var assembly = GetType().GetTypeInfo().Assembly;
                var predefinedResources = new List<string> { "netease", "qqmusic", "ttlyrics", "xiami" };
                foreach (var res in predefinedResources)
                {
                    try
                    {
                        using (var resource = assembly.GetManifestResourceStream($"Light.Resource.{res}.js"))
                        using (var sr = new StreamReader(resource))
                        {
                            var content = await sr.ReadToEndAsync();
                            SourceScriptManager.AddScript(res, content);
                        }
                    }
                    catch { }
                }
            }

            // Provision OOBE Experience Step 2.
            if (!SettingsManager.Instance.ContainsKey("InitialOOBEExperience.Settings.v3"))
                SettingsManager.Instance.Add("InitialOOBEExperience.Settings.v3", 1);

            if (PlatformInfo.CurrentPlatform == Platform.WindowsMobile)
            {
                Frame.Navigate(typeof(MobileFrameView), _filePassThru);
            }
            else
            {
                Frame.Navigate(typeof(FrameView), _filePassThru);
            }

            // Set flag
            OnlineServicingManager.Commit();

            // Start indexing library
            await LibraryService.IndexAsync(new ThumbnailOperations());
        }
    }
}
