using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Light.Common;
using Light.ViewModel.Feedback;

namespace Light.View.Feedback
{
    /// <summary>
    /// Feedback view for SIUF feedbacks.
    /// </summary>
    public sealed partial class FeedbackView : Page
    {
        /// <summary>
        /// CoreApplicationView titlebar.
        /// </summary>
        private readonly CoreApplicationViewTitleBar _coreTitleBar = CoreApplication.GetCurrentView().TitleBar;

        /// <summary>
        /// Feedback image path.
        /// </summary>
        private readonly string _currentFeedbackImagePath;

        /// <summary>
        /// Feedback view model.
        /// </summary>
        private readonly FeedbackViewModel _viewModel;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public FeedbackView(string currentImagePath)
        {
            this.InitializeComponent();

            _currentFeedbackImagePath = currentImagePath;
            _viewModel = new FeedbackViewModel(currentImagePath);

            // XAML titlebar will be enabled _ONLY_ for desktop devices.
            if (PlatformInfo.CurrentPlatform == Platform.WindowsDesktop)
            {
                // Set titlebar integration mode.
                _coreTitleBar.ExtendViewIntoTitleBar = true;
                // Set titlebar control.
                Window.Current.SetTitleBar(BackgroundElement);
            }

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Event handler for window unloading.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (PlatformInfo.CurrentPlatform == Platform.WindowsDesktop)
            {
                _coreTitleBar.LayoutMetricsChanged -= OnTitleBarLayoutMetricsChanged;
                _coreTitleBar.IsVisibleChanged -= OnTitleBarIsVisibleChanged;
                Window.Current.SizeChanged -= OnWindowSizeChanged;
            }
        }

        /// <summary>
        /// Event handler for window loading.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (PlatformInfo.CurrentPlatform == Platform.WindowsDesktop)
            {
                // When the app window moves to a different screen
                _coreTitleBar.LayoutMetricsChanged += OnTitleBarLayoutMetricsChanged;

                // When in full screen mode, the title bar is collapsed by default.
                _coreTitleBar.IsVisibleChanged += OnTitleBarIsVisibleChanged;

                // The SizeChanged event is raised when the view enters or exits full screen mode.
                Window.Current.SizeChanged += OnWindowSizeChanged;

                // Perform some XAML titlebar initialization.
                UpdateLayoutMetrics();
                UpdatePositionAndVisibility();
            }

            if (_currentFeedbackImagePath != null)
            {
                // Set image source.
                FeedbackImagePreview.Source = new BitmapImage(new Uri(_currentFeedbackImagePath));
            }

            // Validate internet connection
            await _viewModel.ValidateInternetConnectionAsync();
        }

        #region XAML based title bar
        #region CoreTitleBarHeight dp

        public double CoreTitleBarHeight
        {
            get { return (double)GetValue(CoreTitleBarHeightProperty); }
            set { SetValue(CoreTitleBarHeightProperty, value); }
        }

        public static readonly DependencyProperty CoreTitleBarHeightProperty =
            DependencyProperty.Register("CoreTitleBarHeight", typeof(double), typeof(FeedbackView), new PropertyMetadata(0d));

        #endregion

        #region CoreTitleBarPadding db

        public Thickness CoreTitleBarPadding
        {
            get { return (Thickness)GetValue(CoreTitleBarPaddingProperty); }
            set { SetValue(CoreTitleBarPaddingProperty, value); }
        }

        public static readonly DependencyProperty CoreTitleBarPaddingProperty =
            DependencyProperty.Register("CoreTitleBarPadding", typeof(Thickness), typeof(FeedbackView), new PropertyMetadata(default(Thickness)));

        #endregion

        /// <summary>
        /// Event handler for titlebar metrics changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            UpdateLayoutMetrics();
        }

        /// <summary>
        /// Event handler for titlebar visibility changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnTitleBarIsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {
            UpdatePositionAndVisibility();
        }

        /// <summary>
        /// Event handler for window size changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            UpdatePositionAndVisibility();
        }

        /// <summary>
        /// Actual method processing layout metrics changes.
        /// </summary>
        private void UpdateLayoutMetrics()
        {
            CoreTitleBarHeight = _coreTitleBar.Height;

            // The SystemOverlayLeftInset and SystemOverlayRightInset values are
            // in terms of physical left and right. Therefore, we need to flip
            // then when our flow direction is RTL.
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CoreTitleBarPadding = new Thickness()
                {
                    Left = _coreTitleBar.SystemOverlayLeftInset,
                    Right = _coreTitleBar.SystemOverlayRightInset
                };
            }
            else
            {
                CoreTitleBarPadding = new Thickness()
                {
                    Left = _coreTitleBar.SystemOverlayRightInset,
                    Right = _coreTitleBar.SystemOverlayLeftInset
                };
            }
        }

        // We wrap the normal contents of the MainPage inside a grid
        // so that we can place a title bar on top of it.
        //
        // When not in full screen mode, the grid looks like this:
        //
        //      Row 0: Custom-rendered title bar
        //      Row 1: Rest of content
        //
        // In full screen mode, the the grid looks like this:
        //
        //      Row 0: (empty)
        //      Row 1: Custom-rendered title bar
        //      Row 1: Rest of content
        //
        // The title bar is either Visible or Collapsed, depending on the value of
        // the IsVisible property.
        private void UpdatePositionAndVisibility()
        {
            if (ApplicationView.GetForCurrentView().IsFullScreenMode)
            {
                // In full screen mode, the title bar overlays the content.
                // and might or might not be visible.
                XamlTitleBar.Visibility = _coreTitleBar.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                Grid.SetRow(XamlTitleBar, 1);
            }
            else
            {
                // When not in full screen mode, the title bar is visible and does not overlay content.
                XamlTitleBar.Visibility = Visibility.Visible;
                Grid.SetRow(XamlTitleBar, 0);
            }
        }

        #endregion

        /// <summary>
        /// Helper method for launching feedback in new window.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static async Task LaunchFeedbackAsync(string imagePath)
        {
            var feedbackView = CoreApplication.CreateNewView();
            int viewId = 0;

            await feedbackView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                
                viewId = ApplicationView.GetForCurrentView().Id;
                var frame = new Frame {Content = new FeedbackView(imagePath)};

                // Set theme if available.
                var theme = App.GetCurrentPreferredTheme();
                if (theme.HasValue)
                {
                    switch (theme.Value)
                    {
                        case ApplicationTheme.Dark:
                            frame.RequestedTheme = ElementTheme.Dark;
                            break;
                        case ApplicationTheme.Light:
                            frame.RequestedTheme = ElementTheme.Light;
                            break;
                    }
                }

                Window.Current.Content = frame;
                Window.Current.Activate();

                // Use separated method (prevent calling main view)
                var appView = ApplicationView.GetForCurrentView();
                appView.Title = "Feedback";
            });

            var result = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
                viewId,
                ViewSizePreference.UseLess,
                ApplicationView.GetForCurrentView().Id,
                ViewSizePreference.UseLess);
        }

        /// <summary>
        /// Event handler for feedback button clicks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFeedbackSubmitButtonClicked(object sender, RoutedEventArgs e)
        {
            // Go back to top so user can see status
            FeedbackControlHostViewer.ChangeView(FeedbackControlHostViewer.HorizontalOffset, 
                0.0, FeedbackControlHostViewer.ZoomFactor);
        }
    }
}
