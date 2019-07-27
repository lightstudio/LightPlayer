using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Core;
using Light.DataObjects;
using Light.Flyout;
using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using Light.Model;
using Light.Shell;
using Light.Utilities;
using Light.Utilities.Grouping;
using Light.Utilities.UserInterfaceExtensions;
using Light.View.Feedback;
using Light.View.Library;
using Light.View.Library.Detailed;
using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Light.View.Core
{
    /// <summary>
    /// Core Frame View which offers core UI framework page and XAML-based title bar on desktop devices.
    /// </summary>
    public sealed partial class FrameView
    {
        public enum CurrentPageType
        {
            Home = 0,
            Music = 1,
            Artist = 2,
            Album = 3,
            Playlist = 4
        }

        /// <summary>
        /// Current instance of hosted <see cref="FrameView"/>.
        /// </summary>
        public static FrameView Current;

        /// <summary>
        /// CoreApplicationView titlebar.
        /// </summary>
        private readonly CoreApplicationViewTitleBar m_coreTitleBar = CoreApplication.GetCurrentView().TitleBar;

        public Color SystemButtonBackgroundColor
        {
            get { return (Color)GetValue(SystemButtonBackgroundColorProperty); }
            set { SetValue(SystemButtonBackgroundColorProperty, value); }
        }
        
        public static readonly DependencyProperty SystemButtonBackgroundColorProperty =
            DependencyProperty.Register(nameof(SystemButtonBackgroundColor), 
                typeof(Color), typeof(FrameView), 
                new PropertyMetadata(Colors.Transparent, OnSystemButtonBackgroundColorChanged));

        private static void OnSystemButtonBackgroundColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameView fv = (FrameView)d;
            // XAML titlebar will be enabled _ONLY_ for desktop devices.
            if (PlatformInfo.CurrentPlatform == Platform.WindowsDesktop)
            {
                // Set titlebar integration mode.
                fv.m_coreTitleBar.ExtendViewIntoTitleBar = true;
                var title = ApplicationView.GetForCurrentView().TitleBar;
                var hover = ((SolidColorBrush)fv.Resources["AppBarToggleButtonBackgroundHighLightOverlayPointerOver"]).Color;
                var pressed = ((SolidColorBrush)fv.Resources["AppBarToggleButtonBackgroundHighLightOverlayPressed"]).Color;
                var foreground = (Color)fv.Resources["SystemBaseHighColor"];
                title.ButtonHoverBackgroundColor = hover;
                title.ButtonPressedBackgroundColor = pressed;
                title.ButtonForegroundColor = foreground;
                title.ButtonInactiveForegroundColor = foreground;
                title.ButtonHoverForegroundColor = foreground;
                title.ButtonPressedForegroundColor = foreground;
                title.ButtonBackgroundColor = (Color)e.NewValue;
                title.ButtonInactiveBackgroundColor = (Color)e.NewValue;
                // Set titlebar control.
                Window.Current.SetTitleBar(fv.BackgroundElement);
            }
        }

        private Brush TitleBarBackground
        {
            get
            {
                return ContentFrame.CurrentSourcePageType != typeof(NowPlayingView) || ApplicationView.GetForCurrentView().IsFullScreenMode ?
                    (Brush) Resources["TitlebarBackgroundBrush"] :
                    new SolidColorBrush(Colors.Transparent);
            }
        }
        
        private Brush TopCommandBarBackground
        {
            get
            {
                return ContentFrame.CurrentSourcePageType == typeof(NowPlayingView) ?
                    new SolidColorBrush(Colors.Transparent) :
                    (Brush)Resources["AppBarBackgroundWithOpacityBrush"];
            }
        }

        private bool BackgroundArtistVisibility
        {
            get
            {
                return ContentFrame.CurrentSourcePageType == typeof(NowPlayingView);
            }
        }

        #region State Variables
        private int m_sortingMethod = -1;
        private bool m_previousPlaylistState;
        private bool m_isScanActive;
        private List<Tuple<string, Exception>> m_unreadExceptions;

        /// <summary>
        /// Implementation of <see cref="IPageGroupingStateManager"/> that manages page grouping options.
        /// </summary>
        private IPageGroupingStateManager m_groupState;

        /// <summary>
        /// Value indicates the last navigated item type.
        /// </summary>
        private CommonItemType m_lastNavigated;

        private bool? IsHomePage
        {
            get
            {
                return ContentFrame.CurrentSourcePageType == typeof(HomeView);
            }
            set
            {
                if (value.HasValue && value.Value) PageType = CurrentPageType.Home;

                if (ContentFrame.CurrentSourcePageType != typeof(HomeView))
                {
                    ContentFrame.Navigate(typeof(HomeView));
                }
                else
                {
                    Bindings.Update();
                }
            }
        }

        private bool? IsPlaylistPage
        {
            get
            {
                return ContentFrame.CurrentSourcePageType == typeof(PlaylistsView);
            }
            set
            {
                if (value.HasValue && value.Value) PageType = CurrentPageType.Playlist;

                if (ContentFrame.CurrentSourcePageType != typeof(PlaylistsView))
                {
                    ContentFrame.Navigate(typeof(PlaylistsView));
                }
                else
                {
                    Bindings.Update();
                }
            }
        }

        private bool? IsAlbumPage
        {
            get
            {
                return ContentFrame.CurrentSourcePageType == typeof(CommonGroupedGridView) &&
                    m_lastNavigated == CommonItemType.Album;
            }
            set
            {
                if (value.HasValue && value.Value) PageType = CurrentPageType.Album;

                CheckNavigation(typeof(CommonGroupedGridView), CommonItemType.Album, value.Value);
            }
        }

        private bool? IsArtistPage
        {
            get
            {
                return ContentFrame.CurrentSourcePageType == typeof(CommonGroupedGridView) &&
                    m_lastNavigated == CommonItemType.Artist;
            }
            set
            {
                if (value.HasValue && value.Value) PageType = CurrentPageType.Artist;

                CheckNavigation(typeof(CommonGroupedGridView), CommonItemType.Artist, value.Value);
            }
        }

        private bool? IsMusicPage
        {
            get
            {
                return ContentFrame.CurrentSourcePageType == typeof(CommonGroupedListView) &&
                    m_lastNavigated == CommonItemType.Song;
            }
            set
            {
                if (value.HasValue && value.Value) PageType = CurrentPageType.Music;

                CheckNavigation(typeof(CommonGroupedListView), CommonItemType.Song, value.Value);
            }
        }

        private bool IsInNowPlaying
        {
            get
            {
                return ContentFrame.CurrentSourcePageType == typeof(NowPlayingView);
            }
        }

        public int SortingMethod
        {
            get
            {
                return m_sortingMethod;
            }
            set
            {
                if (m_sortingMethod == value)
                    return;
                m_sortingMethod = value;
                if (m_sortingMethod == -1)
                    return;
                var cp = m_groupState.PopulateAvailablePairs()[value];
                Messenger.Default.Send(new GenericMessage<IndexerComparerPair>(cp), CommonSharedStrings.GroupingChangeMessageToken);
                m_groupState.SetLastUsedPair(cp);
            }
        }

        private CurrentPageType PageType { get; set; }

        private bool GroupOptionsVisible
        {
            get
            {
                return ContentFrame.CurrentSourcePageType == typeof(CommonGroupedListView) ||
                    ContentFrame.CurrentSourcePageType == typeof(CommonGroupedGridView);
            }
        }
        #endregion

        public ThumbnailTag ArtistImageTag
        {
            get
            {
                return PlaybackControl.Instance.Current?.ArtistImageTag;
            }
        }

        public ObservableCollection<IndexerComparerPair> SortingOptions = new ObservableCollection<IndexerComparerPair>();

        /// <summary>
        /// Class constructor that creates instance of <see cref="FrameView"/>.
        /// </summary>
        public FrameView()
        {
            InitializeComponent();

            // Initialize player.
            PlaybackControl.Instance.Initialize(mediaElement);
            PlaylistControl.Playlist = PlaybackControl.Instance.Items;

            // Register events.
            KeyDown += OnFrameViewKeyDown;
            Window.Current.Activated += OnCurrentHostWindowActivated;

            PageType = CurrentPageType.Home;

            #region Messages
            Loaded += (sender, args) =>
            {
                EnableBuiltInTitleBar();

                Current = this;
                Messenger.Default.Register<GenericMessage<SplitViewDisplayMode>>(this, CommonSharedStrings.InnerSplitViewModeChangeToken,
                    OnInnerSplitViewModeChange);

                Messenger.Default.Register<MessageBase>(this, "SplitViewOpen", OnSplitViewOpen);

                Messenger.Default.Register<GenericMessage<bool>>(this, CommonSharedStrings.ShowNowPlayingViewToken, OnShowNowPlayingView);

                Messenger.Default.Register<GenericMessage<Tuple<Type, string>>>(this, CommonSharedStrings.FrameViewNavigationMessageToken,
                    OnFrameViewNavigationRequested);

                Messenger.Default.Register<GenericMessage<Tuple<Type, int>>>(this, CommonSharedStrings.FrameViewNavigationIntMessageToken,
                    OnFrameViewNavigationIntRequested);

                Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexStartedMessageToken, OnIndexStarted);
                Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexGettingFilesMessageToken, OnIndexGettingFiles);
                Messenger.Default.Register<GenericMessage<string>>(this, CommonSharedStrings.IndexItemAddedMessageToken, OnItemAdded);
                Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
                Messenger.Default.Register<GenericMessage<string>>(this, CommonSharedStrings.ContentFrameNavigateToken, OnNavigateRequested);
                Messenger.Default.Register<GenericMessage<List<Tuple<string, Exception>>>>(this, CommonSharedStrings.ShowLibraryScanExceptions, OnLibraryScanException);

                Messenger.Default.Register<GenericMessage<(string, string)>>(this, CommonSharedStrings.InternalToastMessage, OnToastRequested);
                
                Application.Current.EnteredBackground += OnEnteredBackground;
                Application.Current.LeavingBackground += OnLeavingBackground;
                PlaybackControl.Instance.NowPlayingChanged += OnNowPlayingChanged;
                PlaybackControl.Instance.ErrorOccurred += OnPlaybackErrorOccurred;

                Messenger.Default.Send(new MessageBase(), "InitializePlaylist");

                if (PlatformInfo.CurrentPlatform == Platform.WindowsDesktop)
                {
                    // When the app window moves to a different screen
                    m_coreTitleBar.LayoutMetricsChanged += OnTitleBarLayoutMetricsChanged;

                    // When in full screen mode, the title bar is collapsed by default.
                    m_coreTitleBar.IsVisibleChanged += OnTitleBarIsVisibleChanged;

                    // The SizeChanged event is raised when the view enters or exits full screen mode.
                    Window.Current.SizeChanged += OnWindowSizeChanged;

                    // When title changed, update XAML titlebar
                    DesktopTitleViewConfiguration.TitleChanged += DesktopTitleViewConfigurationOnTitleChanged;

                    // Perform some XAML titlebar initialization.
                    UpdateLayoutMetrics();
                    UpdatePositionAndVisibility();
                    UpdateTitle(ApplicationView.GetForCurrentView().Title);
                }
            };
            Unloaded += (sender, args) =>
            {
                Messenger.Default.Unregister<GenericMessage<(string, string)>>(this, CommonSharedStrings.InternalToastMessage, OnToastRequested);

                Messenger.Default.Unregister<GenericMessage<SplitViewDisplayMode>>(this, "InnerSplitViewModeChange",
                    OnInnerSplitViewModeChange);
                Messenger.Default.Unregister<GenericMessage<MessageBase>>(this, CommonSharedStrings.ShowNowPlayingViewToken);
                Messenger.Default.Unregister<GenericMessage<Tuple<Type, string>>>(this, CommonSharedStrings.FrameViewNavigationMessageToken,
                    OnFrameViewNavigationRequested);
                Messenger.Default.Unregister<GenericMessage<Tuple<Type, int>>>(this, CommonSharedStrings.FrameViewNavigationIntMessageToken,
                    OnFrameViewNavigationIntRequested);
                Messenger.Default.Unregister<MessageBase>(this, "SplitViewOpen", OnSplitViewOpen);
                Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexStartedMessageToken, OnIndexStarted);
                Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexGettingFilesMessageToken, OnIndexGettingFiles);
                Messenger.Default.Unregister<GenericMessage<string>>(this, CommonSharedStrings.IndexItemAddedMessageToken, OnItemAdded);
                Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
                Messenger.Default.Unregister<GenericMessage<string>>(this, CommonSharedStrings.ContentFrameNavigateToken, OnNavigateRequested);
                Messenger.Default.Unregister<GenericMessage<List<Tuple<string, Exception>>>>(this, CommonSharedStrings.ShowLibraryScanExceptions, OnLibraryScanException);

                Application.Current.EnteredBackground -= OnEnteredBackground;
                Application.Current.LeavingBackground -= OnLeavingBackground;
                PlaybackControl.Instance.NowPlayingChanged -= OnNowPlayingChanged;
                PlaybackControl.Instance.ErrorOccurred -= OnPlaybackErrorOccurred;

                if (PlatformInfo.CurrentPlatform == Platform.WindowsDesktop)
                {
                    m_coreTitleBar.LayoutMetricsChanged -= OnTitleBarLayoutMetricsChanged;
                    m_coreTitleBar.IsVisibleChanged -= OnTitleBarIsVisibleChanged;
                    Window.Current.SizeChanged -= OnWindowSizeChanged;
                    DesktopTitleViewConfiguration.TitleChanged -= DesktopTitleViewConfigurationOnTitleChanged;
                }
                Current = null;
            };
            #endregion
        }

        /// <summary>
        /// Handles host window activation events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Instance of the class <see cref="WindowActivatedEventArgs"/>.</param>
        private void OnCurrentHostWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            switch (e.WindowActivationState)
            {
                case CoreWindowActivationState.CodeActivated:
                case CoreWindowActivationState.PointerActivated:
                    ((Storyboard) this.Resources["HostBackDropOn"]).Begin();
                    break;
                case CoreWindowActivationState.Deactivated:
                    ((Storyboard) this.Resources["HostBackDropOff"]).Begin();
                    break;
            }
        }

        private async void OnPlaybackErrorOccurred(object sender, (string FileName, string ErrorMessage) e)
        {
            try
            {
                var dialog = new MessageDialog(
                    string.Format(CommonSharedStrings.PlaybackErrorFormat, e.FileName, e.ErrorMessage),
                    CommonSharedStrings.Failure);
                await dialog.ShowAsync();
            }
            catch { }
        }

        private void OnToastRequested(GenericMessage<(string, string)> message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            m_notificationToastHost.Popup(message.Content.Item1, message.Content.Item2);
        }

        private void CheckNavigation(Type pageType, CommonItemType itemType, bool v)
        {
            if ((ContentFrame.CurrentSourcePageType != pageType ||
                m_lastNavigated != itemType) && v)
            {
                if (pageType == typeof(CommonGroupedListView))
                {
                    m_groupState = new PageGroupingStateManager<CommonGroupedListView>(itemType);
                }
                else
                {
                    m_groupState = new PageGroupingStateManager<CommonGroupedGridView>(itemType);
                }

                // Reload options
                SortingOptions.Clear();
                var options = m_groupState.PopulateAvailablePairs();
                foreach (var option in options)
                {
                    SortingOptions.Add(option);
                }

                var lastUsedOption = m_groupState.GetLastUsedPair();
                var elem = SortingOptions.Where(i => i.Identifier == lastUsedOption.Identifier).ToList();
                if (elem.Any())
                {
                    SortingMethod = SortingOptions.IndexOf(elem.FirstOrDefault());
                }

                ContentFrame.Navigate(pageType, new GroupedViewNavigationArgs(itemType, m_groupState.GetLastUsedPair()));
            }
            else
            {
                Bindings.Update();
            }
        }

        private void OnNavigateRequested(GenericMessage<string> obj)
        {
            CheckJumplistParameter(obj.Content);
        }

        private void OnLeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            PlaybackControl.Instance.NowPlayingChanged += OnNowPlayingChanged;
            Bindings.Update();
        }

        private void OnEnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            PlaybackControl.Instance.NowPlayingChanged -= OnNowPlayingChanged;
        }

        /// <summary>
        /// Method that handles Xbox controller events.
        /// </summary>
        private void OnFrameViewKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.GamepadMenu:
                    // Toggle playlist in specific scenarios
                    if (IsInNowPlaying || PageType == CurrentPageType.Home)
                    {
                        e.Handled = true;
                        PlaylistControl.ScrollToNowPlaying();
                        ContentSplitView.IsPaneOpen = !ContentSplitView.IsPaneOpen;
                    }
                    break;
                case Windows.System.VirtualKey.GamepadView:
                    e.Handled = true;
                    // Toggle now playing view
                    Messenger.Default.Send(new GenericMessage<bool>(true), "ShowNowPlayingView");
                    break;
                case Windows.System.VirtualKey.GamepadLeftTrigger:
                    if (!IsInNowPlaying)
                    {
                        e.Handled = true;
                        // Rotate one to left
                        HandleXboxShoulderButton((int)PageType - 1);
                    }
                    break;
                case Windows.System.VirtualKey.GamepadRightTrigger:
                    if (!IsInNowPlaying)
                    {
                        e.Handled = true;
                        // Rotate one to right
                        HandleXboxShoulderButton((int)PageType + 1);
                    }
                    break;
            }
        }

        /// <summary>
        /// Method that handles Xbox controller trigger button events.
        /// </summary>
        /// <param name="index">Rotated index.</param>
        private void HandleXboxShoulderButton(int index)
        {
            // Edge case: -1 and 5.
            if (index <= -1) index = 4;
            if (index >= 5) index = 0;

            switch ((CurrentPageType)index)
            {
                case CurrentPageType.Home:
                    IsHomePage = true;
                    break;
                case CurrentPageType.Album:
                    IsAlbumPage = true;
                    break;
                case CurrentPageType.Artist:
                    IsArtistPage = true;
                    break;
                case CurrentPageType.Music:
                    IsMusicPage = true;
                    break;
                case CurrentPageType.Playlist:
                    IsPlaylistPage = true;
                    break;
            }
        }

        /// <summary>
        /// Method that enables built-in title bar in desktop platforms.
        /// </summary>
        private void EnableBuiltInTitleBar()
        {
            // XAML titlebar will be enabled _ONLY_ for desktop devices.
            if (PlatformInfo.CurrentPlatform == Platform.WindowsDesktop)
            {
                // Set titlebar integration mode.
                m_coreTitleBar.ExtendViewIntoTitleBar = true;
                var title = ApplicationView.GetForCurrentView().TitleBar;
                var hover = ((SolidColorBrush)Resources["AppBarToggleButtonBackgroundHighLightOverlayPointerOver"]).Color;
                var pressed = ((SolidColorBrush)Resources["AppBarToggleButtonBackgroundHighLightOverlayPressed"]).Color;
                var foreground = (Color)Resources["SystemBaseHighColor"];
                title.ButtonHoverBackgroundColor = hover;
                title.ButtonPressedBackgroundColor = pressed;
                title.ButtonForegroundColor = foreground;
                title.ButtonInactiveForegroundColor = foreground;
                title.ButtonHoverForegroundColor = foreground;
                title.ButtonPressedForegroundColor = foreground;
                title.ButtonBackgroundColor = Colors.Transparent;
                title.ButtonInactiveBackgroundColor = Colors.Transparent;
                // Set titlebar control.
                Window.Current.SetTitleBar(BackgroundElement);
            }
        }

        /// <summary>
        /// Event handler on current playback item changing.
        /// </summary>
        private async void OnNowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                Bindings.Update();
            });
        }

        /// <summary>
        /// Message handler for index finishing event.
        /// </summary>
        /// <param name="obj">Instance of <see cref="MessageBase"/>.</param>
        private async void OnIndexFinished(MessageBase obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
              {
                  m_isScanActive = false;
                  Bindings.Update();
                  IndexPrompt.Text = "";
              });
        }

        /// <summary>
        /// Message handler for getting files.
        /// </summary>
        /// <param name="obj">Instance of <see cref="MessageBase"/>.</param>
        private async void OnIndexGettingFiles(MessageBase obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => IndexPrompt.Text = "Getting all files");
        }

        /// <summary>
        /// Message handler for index starting event.
        /// </summary>
        /// <param name="obj">Instance of <see cref="MessageBase"/>.</param>
        private async void OnIndexStarted(MessageBase obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                WarningPanel.Visibility = Visibility.Collapsed;
                m_unreadExceptions = null;
                m_isScanActive = true;
                Bindings.Update();
            });
        }

        /// <summary>
        /// Message handler for adding items.
        /// </summary>
        /// <param name="obj">Instance of <see cref="GenericMessage{string}"/>.</param>
        private async void OnItemAdded(GenericMessage<string> obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (!m_isScanActive)
                {
                    m_isScanActive = true;
                    Bindings.Update();
                }
                IndexPrompt.Text = string.Format(CommonSharedStrings.LibraryFilesAddedFormat, obj.Content);
            });
        }

        /// <summary>
        /// Handler on open events of SplitView.
        /// </summary>
        /// <param name="obj">Instance of <see cref="MessageBase"/>.</param>
        private void OnSplitViewOpen(MessageBase obj)
        {
            PlaylistControl.ScrollToNowPlaying();
            ContentSplitView.IsPaneOpen = true;
        }

        #region XAML titlebar for desktop devices
        #region CoreTitleBarHeight dp

        public double CoreTitleBarHeight
        {
            get { return (double)GetValue(CoreTitleBarHeightProperty); }
            set { SetValue(CoreTitleBarHeightProperty, value); }
        }

        public static readonly DependencyProperty CoreTitleBarHeightProperty =
            DependencyProperty.Register("CoreTitleBarHeight", typeof(double), typeof(FrameView), new PropertyMetadata(32d));

        #endregion

        #region CoreTitleBarPadding db

        public Thickness CoreTitleBarPadding
        {
            get { return (Thickness)GetValue(CoreTitleBarPaddingProperty); }
            set { SetValue(CoreTitleBarPaddingProperty, value); }
        }

        public static readonly DependencyProperty CoreTitleBarPaddingProperty =
            DependencyProperty.Register("CoreTitleBarPadding", typeof(Thickness), typeof(FrameView), new PropertyMetadata(default(Thickness)));

        #endregion

        #region CoreTiteBar Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(FrameView),
                PropertyMetadata.Create(default(string)));

        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

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
            if (UIViewSettings.GetForCurrentView().UserInteractionMode
                == UserInteractionMode.Mouse)
                CoreTitleBarHeight = 32;
            else
                CoreTitleBarHeight = 0;

            // The SystemOverlayLeftInset and SystemOverlayRightInset values are
            // in terms of physical left and right. Therefore, we need to flip
            // then when our flow direction is RTL.
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CoreTitleBarPadding = new Thickness()
                {
                    Left = m_coreTitleBar.SystemOverlayLeftInset,
                    // Workaround for title bar.
                    Right = m_coreTitleBar.SystemOverlayRightInset == 0 ? 188 : m_coreTitleBar.SystemOverlayRightInset
                };
            }
            else
            {
                CoreTitleBarPadding = new Thickness()
                {
                    Left = m_coreTitleBar.SystemOverlayRightInset,
                    Right = m_coreTitleBar.SystemOverlayLeftInset
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
                XamlTitleBar.Visibility = m_coreTitleBar.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                Grid.SetRow(XamlTitleBar, 1);

                // As there's already a button for exiting full screen mode,
                // we simply hide our custom full screen button here.
                // Also, if you use this button to exit the full screen mode,
                // the three default buttons will stop working, this might be a bug...
                FullScreenModeToggle.Visibility = Visibility.Collapsed;
            }
            else
            {
                // When not in full screen mode, the title bar is visible and does not overlay content.
                XamlTitleBar.Visibility = Visibility.Visible;
                Grid.SetRow(XamlTitleBar, 0);

                FullScreenModeToggle.Visibility = Visibility.Visible;
            }
            Bindings.Update();
        }

        /// <summary>
        /// Event handler for full screen toggle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFullScreenModeToggleClick(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();

            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
            }
            else
            {
                view.TryEnterFullScreenMode();
            }
        }

        /// <summary>
        /// Event handler for feedback button clicks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnFeedbackButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get a current screenshot
                var imagePath = await this.GetImageAsync();

                // Launch SIUF interface
                await FeedbackView.LaunchFeedbackAsync(imagePath);
            }
            catch (OutOfMemoryException)
            {
                // Do nothing.
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        /// <summary>
        /// Event handler for title changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="s"></param>
        private void DesktopTitleViewConfigurationOnTitleChanged(object sender, string s)
        {
            UpdateTitle(s);
        }

        /// <summary>
        /// Actual method for title updates.
        /// </summary>
        /// <param name="title"></param>
        private void UpdateTitle(string title)
        {
            Title = !string.IsNullOrEmpty(title) ? title : "Light";
        }

        #endregion

        /// <summary>
        /// Handle now playing view navigation request.
        /// </summary>
        /// <param name="obj">Message param.</param>
        private void OnShowNowPlayingView(GenericMessage<bool> obj)
        {
            if (ContentFrame.CurrentSourcePageType != typeof(NowPlayingView))
            {
                ContentFrame.Navigate(
                    typeof(NowPlayingView),
                    null,
                    new DrillInNavigationTransitionInfo());
            }
            else if (obj.Content)
            {
                ContentFrame.GoBack();
            }
        }

        /// <summary>
        /// Handle general navigation request type1 (int).
        /// </summary>
        /// <param name="obj">Message param. See `CommonSharedStrings.FrameViewNavigationIntMessageToken`</param>
        private void OnFrameViewNavigationIntRequested(GenericMessage<Tuple<Type, int>> obj)
        {
            ContentFrame.Navigate(obj.Content.Item1, obj.Content.Item2, new DrillInNavigationTransitionInfo());
        }

        /// <summary>
        /// Handle general navigation request type2 (string).
        /// </summary>
        /// <param name="obj">Message param. See `CommonSharedStrings.FrameViewNavigationMessageToken`</param>
        private void OnFrameViewNavigationRequested(GenericMessage<Tuple<Type, string>> obj)
        {
            ContentFrame.Navigate(obj.Content.Item1, obj.Content.Item2 ?? string.Empty, new DrillInNavigationTransitionInfo());
        }

        /// <summary>
        /// Handle navigate to events and initialize variables like ViewModel.
        /// </summary>
        /// <param name="e">NavigationTo event arguments, including target data type.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SurfaceLoader.Initialize(ElementCompositionPreview.GetElementVisual(this).Compositor);

            if (e.Parameter is string)
            {
                // Load host view, with jumplist arg (if available)
                CheckJumplistParameter((string)e.Parameter);
            }
            else
            {
                ContentFrame.Navigate(typeof(HomeView), CommonItemType.Other);
                (e.Parameter as TaskCompletionSource<object>)?.SetResult(null);
            }

            PlaylistControl.IsPlaylistPinned = NowPlayingStateManager.NowPlayingListDisplayMode == SplitViewDisplayMode.Inline;

            await ThreadPool.RunAsync(callback => LiveTileHelper.CleanUpCacheAsync(), WorkItemPriority.Normal);
        }

        private void CheckJumplistParameter(string param)
        {
            switch (param)
            {
                case "light-jumplist:viewallalbums":
                    IsAlbumPage = true;
                    break;
                case "light-jumplist:viewallartists":
                    IsArtistPage = true;
                    break;
                case "light-jumplist:viewallsongs":
                    IsMusicPage = true;
                    break;
                case "light-jumplist:viewallplaylist":
                    IsPlaylistPage = true;
                    break;
                default:
                    IsHomePage = true;
                    break;
            }
        }

        /// <summary>
        /// Handler for SplitView mode changing.
        /// Pinning is implemented via data binding, Message is only used for prefs saving.
        /// </summary>
        /// <param name="obj"></param>
        private void OnInnerSplitViewModeChange(GenericMessage<SplitViewDisplayMode> obj)
        {
            NowPlayingStateManager.NowPlayingListDisplayMode = obj.Content;
        }

        private void Page_DragEnter(object sender, DragEventArgs e)
        {
            m_previousPlaylistState = ContentSplitView.IsPaneOpen;
            PlaylistControl.ScrollToNowPlaying();
            ContentSplitView.IsPaneOpen = true;
        }

        private void Page_DragLeave(object sender, DragEventArgs e)
        {
            ContentSplitView.IsPaneOpen = m_previousPlaylistState;
        }

        public async void RefreshMediaLibrary(object sender, RoutedEventArgs e)
        {
            await LibraryService.IndexAsync(new ThumbnailOperations());
        }

        private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
        {
            if ((e.SourcePageType == typeof(CommonGroupedGridView) ||
                e.SourcePageType == typeof(CommonGroupedListView)) &&
                e.Parameter is GroupedViewNavigationArgs)
            {
                m_lastNavigated = (e.Parameter as GroupedViewNavigationArgs).PageType;
            }

            if (e.SourcePageType == typeof(NowPlayingView))
            {
                PlaylistControl.IsInNowPlayingView = true;
                //NowPlayingStateTrigger.IsActive = true;
                //NormalStateTrigger.IsActive = false;
            }
            else
            {
                if (PlaylistControl.IsInNowPlayingView)
                    PlaylistControl.IsInNowPlayingView = false;
                //NormalStateTrigger.IsActive = true;
                //NowPlayingStateTrigger.IsActive = false;
            }

            Bindings.Update();
        }

        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(SettingsView));
        }

        public void SetTitleString(string title)
        {
            PageTitle.Text = title;
        }

        private async void ShowAbout(object sender, RoutedEventArgs e)
        {
            var flyout = new AboutFlyout();
            await flyout.ShowAsync();
        }

        private async void FindButtonTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            SearchPanel.Visibility = Visibility.Visible;
            // Workaround.
            await Task.Delay(200);
            AutoSuggestBox.Focus(FocusState.Keyboard);
            await LibrarySearchUtils.LoadLibraryCacheAsync();
        }

        private void OnPlaylistToggleButtonClick(object sender, RoutedEventArgs e)
        {
            PlaylistControl.ScrollToNowPlaying();
            var view = ApplicationView.GetForCurrentView();
            if (ContentSplitView.DisplayMode == SplitViewDisplayMode.Inline && (!view.IsFullScreenMode))
            {
                var length = ContentSplitView.OpenPaneLength;
                var bounds = view.VisibleBounds;
                if (ContentSplitView.IsPaneOpen)
                {
                    view.TryResizeView(new Size(bounds.Width + length, bounds.Height));
                }
                else
                {
                    view.TryResizeView(new Size(bounds.Width - length, bounds.Height));
                }
            }
        }

        private string FormatExceptionDesc(int count)
        {
            if (count == 1)
            {
                return CommonSharedStrings.SingleException;
            }
            else
            {
                return string.Format(CommonSharedStrings.MultipleExceptionsFormat, count);
            }
        }

        private void OnLibraryScanException(GenericMessage<List<Tuple<string, Exception>>> obj)
        {
            if (m_unreadExceptions == null)
            {
                m_unreadExceptions = obj.Content;
            }
            else
            {
                m_unreadExceptions.AddRange(obj.Content);
            }

            if (m_unreadExceptions.Count > 0)
            {
                WarningPrompt.Text = FormatExceptionDesc(m_unreadExceptions.Count);
                WarningPanel.Visibility = Visibility.Visible;
            }
        }

        private async void OnWarningPanelTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var dialog = new LibraryWarningDialog(m_unreadExceptions);
            WarningPanel.Visibility = Visibility.Collapsed;
            m_unreadExceptions = null;
            await dialog.ShowAsync();
        }

        private static IEnumerable<T> Shuffle<T>(IEnumerable<T> source, Random rng)
        {
            var buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }

        private async void OnShuffleAllClick(object sender, RoutedEventArgs e)
        {
            if (GlobalLibraryCache.CachedDbMediaFile == null)
            {
                await GlobalLibraryCache.LoadMediaAsync();
            }
            var s = from x in Shuffle(GlobalLibraryCache.CachedDbMediaFile, new Random())
                    select MusicPlaybackItem.CreateFromMediaFile(x);
            PlaybackControl.Instance.Clear();
            await PlaybackControl.Instance.AddAndSetIndexAt(s, 0);
        }

        private void OnRecentlyAddedClick(object sender, RoutedEventArgs e)
        {
            if (IsMusicPage.HasValue && IsMusicPage.Value)
            {
                var methods = m_groupState.PopulateAvailablePairs().ToList();
                var method = methods.Where(x => x.Identifier == "RecentAddedGroup").First();
                SortingMethod = methods.IndexOf(method);
                Bindings.Update();
            }
            else
            {
                var state = new PageGroupingStateManager<CommonGroupedListView>(CommonItemType.Song);
                var group = state.PopulateAvailablePairs().Where(x => x.Identifier == "RecentAddedGroup").First();
                state.SetLastUsedPair(group);
                IsMusicPage = true;
            }
        }

        ObservableCollection<SearchResultModel> SearchSuggestions = new ObservableCollection<SearchResultModel>();

        private void SubmitQuery(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return;
            }

            if (ContentFrame.SourcePageType != typeof(SearchView))
            {
                Messenger.Default.Send(
                    new GenericMessage<Tuple<Type, string>>(
                        new Tuple<Type, string>(typeof(SearchView), keyword)),
                            CommonSharedStrings.FrameViewNavigationMessageToken);
            }
            else
            {
                // Send message to SearchView
                Messenger.Default.Send(new GenericMessage<string>(keyword), "RequestSearch");
            }

            SearchPanel.Visibility = Visibility.Collapsed;
        }

        private void OnSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                return;
            }
            SubmitQuery(AutoSuggestBox.Text);
        }

        private void OnSearchBoxSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var item = args.SelectedItem as SearchResultModel;
            switch (item.ItemType)
            {
                case CommonItemType.Album:
                    var album = item.Entity as DbAlbum;
                    Messenger.Default.Send(
                        new GenericMessage<Tuple<Type, int>>(
                            new Tuple<Type, int>(typeof(AlbumDetailView), album.Id)),
                                CommonSharedStrings.FrameViewNavigationIntMessageToken);
                    break;
                case CommonItemType.Artist:
                    var artist = item.Entity as DbArtist;
                    Messenger.Default.Send(
                        new GenericMessage<Tuple<Type, int>>(
                            new Tuple<Type, int>(typeof(ArtistDetailView), artist.Id)),
                                CommonSharedStrings.FrameViewNavigationIntMessageToken);
                    break;
                case CommonItemType.Song:
                    SubmitQuery(item.Title);
                    break;
            }
        }

        private void OnSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var keyword = AutoSuggestBox.Text;

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput &&
                keyword != string.Empty)
            {
                LibrarySearchUtils.UpdateSuggestions(keyword, SearchSuggestions);
            }
            else
            {
                SearchSuggestions.Clear();
            }
        }
    }
}
