using Light.Common;
using Light.Core;
using Light.Core.Provision.Tasks;
using Light.Managed.Database.Native;
using Light.Managed.Servicing;
using Light.Managed.Settings;
using Light.Phone.View;
using Light.Shell;
using Light.View.Core;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Light.View.InitialExpeience
{
    /// <summary>
    /// Extended splashscreen view.
    /// </summary>
    public sealed partial class ExtendedSplashScreen : Page
    {
        internal Rect SplashImageRect; // Rect to store splash screen image coordinates.
        private readonly SplashScreen _splash; // Variable to hold the splash screen object.
        internal bool Dismissed = false; // Variable to track splash screen dismissal status.
        internal Frame RootFrame;
        private readonly object _arg;

        /// <summary>
        /// Initializes new instance of <see cref="ExtendedSplashScreen"/>.
        /// </summary>
        /// <param name="splashscreen">Instance of <see cref="Splashscreen"/>.</param>
        /// <param name="arg">Launch arguments.</param>
        public ExtendedSplashScreen(SplashScreen splashscreen, object arg)
        {
            this.InitializeComponent();
            DesktopTitleViewConfiguration.EnterSplashScreen();

            _arg = arg;
            _splash = splashscreen;

            Window.Current.SizeChanged += ExtendedSplashOnResize;

            if (_splash != null)
            {
                // Register an event handler to be executed when the splash screen has been dismissed.
                _splash.Dismissed += DismissedEventHandler;
            }

            // Create a Frame to act as the navigation context
            RootFrame = new Frame();
        }

        // Include code to be executed when the system has transitioned from the splash screen to the extended splash screen (application's first view).
        async void DismissedEventHandler(SplashScreen sender, object e)
        {
            Dismissed = true;

            await RunInitializationTasksAsync();

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                await PlaylistManager.Instance.InitializeAsync(CommonSharedStrings.FavoritesLocalizedText);
                if (RootFrame.Content == null)
                {
                    RootFrame.Background = new SolidColorBrush(Colors.Black);
                    // Set up transitions
                    RootFrame.ContentTransitions = new TransitionCollection();
                    RootFrame.ContentTransitions.Add(new NavigationThemeTransition());
                    // Navigate to FrameView
                    RootFrame.Navigate(DeterminePageToNavigateToByRtCheckpoints(), _arg);
                    // Place the frame in the current Window
                    Window.Current.Content = RootFrame;
                    // Activate current window
                    Window.Current.Activate();
                }

                // Mobile frame
                if (PlatformInfo.CurrentPlatform == Platform.WindowsMobile)
                {
                    var applicationView = ApplicationView.GetForCurrentView();
                    applicationView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
                }
            });
        }

        /// <summary>
        /// Perform startup initialization tasks asynchronously.
        /// </summary>
        /// <returns>Task represents asynchronous operation.</returns>
        private async Task RunInitializationTasksAsync()
        {
            var provisionManager = new ProvisionManager();
            
            if (provisionManager.IsProvisionRequired)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    ProvisionStatusTextBlock.Text = CommonSharedStrings.ProvisionPrompt;
                });
            }

            // Initialize ffmpeg
            NativeMethods.InitializeFfmpeg();

            // Trigger dependency injection here.
            await ApplicationServiceBase.App.ConfigureServicesAsync();

            // Do all provision tasks
            if (provisionManager.IsProvisionRequired)
            {
                await provisionManager.RunProvisionTasksAsync();
            }

            // Clean up SIUF screenshots
            try
            {
                var files = await ApplicationData.Current.TemporaryFolder.GetFilesAsync();
                foreach (var file in files)
                {
                    if (file.FileType == ".png")
                    {
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }
            }
            catch (COMException)
            {

            }

            // Enable library tracking if set, but not for first-run
            if (SettingsManager.Instance.ContainsKey("InitialOOBEExperience.Settings.v3") &&
                LibraryService.IsAutoTrackingEnabled)
            {
                // We are using DispatchTimer for periodic check
                await Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
                {
                    // Windows Runtime offers the ability to capture offline changes
                    // So for users that enable this option, an need-based incremental index
                    // will be triggered
                    await LibraryService.StartChangeTrackingAsync(true);
                });
            }

            await PlaybackHistoryManager.Instance.ClearHistoryAsync(PlaybackHistoryManager.HistoryEntryLimit);
        }

        /// <summary>
        /// Handles splashscreen resize events.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Instance of <see cref="WindowSizeChangedEventArgs"/>.</param>
        void ExtendedSplashOnResize(Object sender, WindowSizeChangedEventArgs e)
        {
            // Safely update the extended splash screen image coordinates. This function will be executed when a user resizes the window.
            if (_splash != null)
            {
                // Update the coordinates of the splash screen image.
                SplashImageRect = _splash.ImageLocation;

                if (PlatformInfo.CurrentPlatform != Platform.WindowsMobile)
                {
                    ResizeImage();
                }
            }
        }

        /// <summary>
        /// Resizes image to correct position.
        /// </summary>
        private void ResizeImage()
        {
            ExtendedSplashImage.Width = SplashImageRect.Width;
            ExtendedSplashImage.Height = SplashImageRect.Height;
        }

        /// <summary>
        /// Determines navigation page type.
        /// </summary>
        /// <returns>Destination page type.</returns>
        private Type DeterminePageToNavigateToByRtCheckpoints()
        {
            if (!SettingsManager.Instance.ContainsKey("InitialOOBEExperience.Settings.v3"))
            {
                return typeof(InitialSettings);
            }
            else if (OnlineServicingManager.IsServicingRequired)
            {
                return typeof(ServicingView);
            }
            else if (PlatformInfo.CurrentPlatform == Platform.WindowsMobile)
            {
                return typeof(MobileFrameView);
            }
            else
            {
                return typeof(FrameView);
            }
        }
    }
}
