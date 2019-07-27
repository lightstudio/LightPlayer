using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Core;
using Light.Managed.Settings;
using Light.Managed.Tools;
using Light.Utilities;
using Light.View.Core;
using Light.View.InitialExpeience;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Diagnostics;
using Windows.Media.Audio;
using Windows.Media.Devices;
using Windows.Media.Render;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Light
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        [DllImport("Light")]
        static extern void SetSystemSampleRate(int sampleRate);

        public static ResourceLoader ResourceLoader { get; private set; }

        private bool _isIsinitialization = true;
        private bool _hasInitialized = false;
        private bool _ignoreException = false;

        private const string InterfaceLangKey = "InterfaceLanguage";
        private const string InterfaceThemeKey = "InterfaceTheme";

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            TelemetryHelper.Initialize();

#if FORCE_ENABLE_ZH_CN
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "zh-CN";
#endif

            // Set language override.
            if (SupportedLanguages.Contains(SettingsManager.Instance.GetValue<string>(InterfaceLangKey)))
            {
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride =
                    SettingsManager.Instance.GetValue<string>(InterfaceLangKey);
            }
            // Otherwise, set as current system language.
            else
            {
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
            }

            // Set theme if available
            var theme = GetCurrentPreferredTheme();
            if (theme.HasValue)
            {
                Current.RequestedTheme = theme.Value;
            }

            InitializeComponent();
            Suspending += OnSuspending;
            Resuming += OnResuming;

            if (PlatformInfo.IsRedstoneRelease)
            {
                EnteredBackground += OnEnteringBackground;
                MemoryManager.AppMemoryUsageLimitChanging += OnAppMemroyUsageLimitChanging;
                MemoryManager.AppMemoryUsageIncreased += OnAppMemoryUsageIncreased;
            }

            MediaDevice.DefaultAudioRenderDeviceChanged += OnDefaultAudioDeviceChanged;
        }

        /// <summary>
        /// Get current preferred theme.
        /// </summary>
        /// <returns>Application preferred theme.</returns>
        /// <remarks>On Redstone+, if no preferred theme is set, null value will be returned.</remarks>
        public static ApplicationTheme? GetCurrentPreferredTheme()
        {
            if (PlatformInfo.CurrentPlatform == Platform.WindowsMobile)
            {
                return ApplicationTheme.Dark;
            }

            // Get theme, if platform is available
            if (PlatformInfo.IsRedstoneRelease)
            {
                var theme = SettingsManager.Instance.GetValue<string>(InterfaceThemeKey);
                switch (theme)
                {
                    case "Light":
                        return ApplicationTheme.Light;
                    case null:
                    case "Dark":
                        return ApplicationTheme.Dark;
                    default:
                        return null;
                }
            }
            else
            {
                // Get color for 10586- (force dark theme override)
                return ApplicationTheme.Dark;
            }
        }

        /// <summary>
        /// Attempts to reduce application memory usage.
        /// </summary>
        private void ReduceMemoryUsage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            TelemetryHelper.LogEvent();
        }

        /// <summary>
        /// Handles application memory usage increase.
        /// </summary>
        private void OnAppMemoryUsageIncreased(object sender, object e)
        {
            var level = MemoryManager.AppMemoryUsageLevel;
            if (level == AppMemoryUsageLevel.OverLimit ||
                level == AppMemoryUsageLevel.High)
            {
                TelemetryHelper.LogEvent();
                ReduceMemoryUsage();
            }
        }

        /// <summary>
        /// Handles application memory limit changes.
        /// </summary>
        /// <param name="e">Instance of <see cref="AppMemoryUsageLimitChangingEventArgs"/>.</param>
        private void OnAppMemroyUsageLimitChanging(object sender, AppMemoryUsageLimitChangingEventArgs e)
        {
            if (MemoryManager.AppMemoryUsage >= e.NewLimit)
            {
                var fields = new LoggingFields();
                fields.AddUInt64("CurrentUsage", MemoryManager.AppMemoryUsage);
                fields.AddUInt64("NewLimit", e.NewLimit);
                TelemetryHelper.LogEventWithParams("MemUsageChanging", fields);

                ReduceMemoryUsage();
            }
        }

        /// <summary>
        /// Handles app background entry lifecycle event.
        /// </summary>
        private void OnEnteringBackground(object sender, EnteredBackgroundEventArgs e)
        {
            TelemetryHelper.LogEvent();
            ReduceMemoryUsage();
        }

        /// <summary>
        /// Handles file activation.
        /// </summary>
        /// <param name="args">Instance of <see cref="FileActivatedEventArgs"/>.</param>
        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            OnActivated(args);

            base.OnFileActivated(args);
        }

        /// <summary>
        /// Handles application initialization after activation.
        /// </summary>
        /// <param name="args">Activation arguments.</param>
        protected override void OnActivated(IActivatedEventArgs args)
        {
            // Initialize base
            PerformBaseInitialize();

            // Handle other specificed launch purpose here.
            if (args.Kind != ActivationKind.Launch)
            {
                // Initialize core UI frame.
                var rootFrame = InitializeRootFrameAndResourceLoader(args.PreviousExecutionState);

                // Route to launch event(s).
                // But normal launch is not handled here.
                switch (args.Kind)
                {
                    case ActivationKind.File:
                        OnFileActivated(rootFrame, (FileActivatedEventArgs)args);
                        break;
                }
            }

            base.OnActivated(args);
        }

        /// <summary>
        /// Update system sample rate asynchronously.
        /// </summary>
        /// <returns>Task represents the asynchronous operation.</returns>
        public async Task<int> UpdateSampleRate()
        {
            try
            {
                var result = await AudioGraph.CreateAsync(
                    new AudioGraphSettings(AudioRenderCategory.Media));

                if (result.Status == AudioGraphCreationStatus.Success)
                {
                    var rate = (int) result.Graph.EncodingProperties.SampleRate;
                    SetSystemSampleRate(rate);
                    result.Graph.Dispose();
                    return rate;
                }
                else
                {
                    SetSystemSampleRate(0);
                }
            }
            catch
            {
                // Ignore
            }

            return 0;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Initialize base
            PerformBaseInitialize();

            var rootFrame = InitializeRootFrameAndResourceLoader(args.PreviousExecutionState);
            if (_isIsinitialization)
            {
                await UpdateSampleRate();
                var extendedSplashscreen = new ExtendedSplashScreen(args.SplashScreen, args.Arguments);
                rootFrame.Content = extendedSplashscreen;
                Window.Current.Activate();

                _isIsinitialization = false;
            }
            else
            {
                if (args.Arguments.StartsWith("light-jumplist:"))
                {
                    Messenger.Default.Send(new GenericMessage<string>(args.Arguments), CommonSharedStrings.ContentFrameNavigateToken);
                }
                // We only handle normal launch here.
                OnLaunchEvent(args.Arguments, rootFrame);
            }
        }

        /// <summary>
        /// Handles system default audio device changes.
        /// </summary>
        /// <param name="args">Instance of <see cref="DefaultAudioCaptureDeviceChangedEventArgs"/>.</param>
        private async void OnDefaultAudioDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            await UpdateSampleRate();
        }

        /// <summary>
        /// Initializes root frame and resource loader.
        /// </summary>
        /// <param name="state">Application execution state.</param>
        /// <returns>Instance of <see cref="Frame"/>.</returns>
        private Frame InitializeRootFrameAndResourceLoader(ApplicationExecutionState state)
        {
            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Background = new SolidColorBrush(Colors.Black);

                if (state == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            // Initialize resource loader then
            ResourceLoader = ResourceLoader.GetForCurrentView();

            return rootFrame;
        }

        /// <summary>
        /// Handles application initialization after file activation completes.
        /// </summary>
        /// <param name="rootFrame">Instance of <see cref="Frame"/>.</param>
        /// <param name="args">Instance of <see cref="FileActivatedEventArgs"/>.</param>
        private async void OnFileActivated(Frame rootFrame, FileActivatedEventArgs args)
        {
            if (args.Kind != ActivationKind.File) return;

            //Disable previous playlist restore
            PlaybackControl.Instance.SetRestore(false);

            // Initialize the root frame and wait for it completes loading.
            if (rootFrame.Content == null)
            {
                var task = new TaskCompletionSource<object>();
                var extendedSplashscreen = new ExtendedSplashScreen(args.SplashScreen, task);
                rootFrame.Content = extendedSplashscreen;
                _isIsinitialization = false;
                Window.Current.Activate();
                base.OnFileActivated(args);
                await task.Task;
            }
            else
            {
                Window.Current.Activate();
                base.OnFileActivated(args);
            }

            // Ensure the current window is active
            // Pass files to FileOpenFailure 
            await FileOpen.OpenFilesAsync(args.Files);
        }

        #region Stage 1 Initialization
        private void PerformBaseInitialize()
        {
            if (_hasInitialized) return;

            // Register Exception Handler.
            RegisterExceptionHandlingSynchronizationContext();
            UnhandledException += OnUnhandledException;

            _hasInitialized = true;
        }
        #endregion

        #region Non-initial startup
        private Type DeterminePageToNavigateToByRtCheckpoints()
        {
            var pageType = typeof (FrameView);

            var settings = SettingsManager.Instance;

            if (!settings.ContainsKey("InitialOOBEExperience.Settings.v3"))
                pageType = typeof (InitialSettings);

            return pageType;
        }

        private void OnLaunchEvent(string arg, Frame rootFrame)
        {
            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(DeterminePageToNavigateToByRtCheckpoints(), arg);
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }
        #endregion

        #region Application Lifecycle

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // Log event.
            TelemetryHelper.LogEvent();

            // Stop change tracking.
            LibraryService.StopChangeTracking();

            deferral.Complete();
        }

        /// <summary>
        /// Invoked when application execution is being resumed.
        /// </summary>
        /// <param name="sender">The source of the resume request.</param>
        /// <param name="e">Details about the resume request.</param>
        private async void OnResuming(object sender, object e)
        {
            await LibraryService.StartChangeTrackingAsync();
        }

        #endregion

        #region Language support
        /// <summary>
        /// A hashset of current supported languages.
        /// </summary>
        /// <remarks>DO Update this hashset when new language support is introduced.</remarks>
        private static readonly HashSet<string> SupportedLanguages = new HashSet<string>
        {
            "en-US",
            "en-GB",
            "zh-CN"
        };
        #endregion
    }
}
