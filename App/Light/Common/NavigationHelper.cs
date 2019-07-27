using Light.Managed.Tools;
using System;
using System.Collections.Generic;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Light.Common
{
    /// <summary>
    /// NavigationHelper aids in navigation between pages.  It provides commands used to 
    /// navigate back and forward as well as registers for standard mouse and keyboard 
    /// shortcuts used to go back and forward in Windows and the hardware back button in
    /// Windows Phone.  In addition it integrates SuspensionManger to handle process lifetime
    /// management and state management when navigating between pages.
    /// </summary>
    /// <example>
    /// To make use of NavigationHelper, follow these two steps or
    /// start with a BasicPage or any other Page item template other than BlankPage.
    /// 
    /// 1) Create an instance of the NavigationHelper somewhere such as in the 
    ///     constructor for the page and register a callback for the LoadState and 
    ///     SaveState events.
    /// <code>
    ///     public MyPage()
    ///     {
    ///         this.InitializeComponent();
    ///         var navigationHelper = new NavigationHelper(this);
    ///         this.navigationHelper.LoadState += navigationHelper_LoadState;
    ///         this.navigationHelper.SaveState += navigationHelper_SaveState;
    ///     }
    ///     
    ///     private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
    ///     { }
    ///     private async void navigationHelper_SaveState(object sender, LoadStateEventArgs e)
    ///     { }
    /// </code>
    /// 
    /// 2) Register the page to call into the NavigationHelper whenever the page participates 
    ///     in navigation by overriding the <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedTo"/> 
    ///     and <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedFrom"/> events.
    /// <code>
    ///     protected override void OnNavigatedTo(NavigationEventArgs e)
    ///     {
    ///         navigationHelper.OnNavigatedTo(e);
    ///     }
    ///     
    ///     protected override void OnNavigatedFrom(NavigationEventArgs e)
    ///     {
    ///         navigationHelper.OnNavigatedFrom(e);
    ///     }
    /// </code>
    /// </example>
    [WebHostHidden]
    public class NavigationHelper : DependencyObject
    {
        private Page Page { get; }
        private Frame Frame => Page.Frame;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationHelper"/> class.
        /// </summary>
        /// <param name="page">A reference to the current page used for navigation.  
        /// This reference allows for frame manipulation and to ensure that keyboard 
        /// navigation requests only occur when the page is occupying the entire window.</param>
        public NavigationHelper(Page page)
        {
            Page = page;

            // When this page is part of the visual tree make two changes:
            // 1) Map application view state to visual state for the page
            // 2) Handle hardware navigation requests
            Page.Loaded += (sender, e) =>
            {
                SystemNavigationManager.GetForCurrentView().BackRequested += OnSystemNavigationBackRequested;
                // Listen to the window directly so focus isn't required
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated +=
                    CoreDispatcher_AcceleratorKeyActivated;
                Window.Current.CoreWindow.PointerReleased += OnCoreWindowPointerReleased;
            };

            // Undo the same changes when the page is no longer visible
            Page.Unloaded += (sender, e) =>
            {
                SystemNavigationManager.GetForCurrentView().BackRequested -= OnSystemNavigationBackRequested;
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -=
                    CoreDispatcher_AcceleratorKeyActivated;
                Window.Current.CoreWindow.PointerReleased -= OnCoreWindowPointerReleased;
            };
        }

        /// <summary>
        /// Handle XButton events.
        /// </summary>
        private void OnCoreWindowPointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.XButton1Released)
            {
                var popups = VisualTreeHelper.GetOpenPopups(Window.Current);
                if (popups.Count != 0)
                    return;
                args.Handled = true;
                GoBackCommand.Execute(null);
            }
            else if (args.CurrentPoint.Properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.XButton2Released)
            {
                var popups = VisualTreeHelper.GetOpenPopups(Window.Current);
                if (popups.Count != 0)
                    return;
                args.Handled = true;
                GoForwardCommand.Execute(null);
            }
        }

        #region Navigation support

        RelayCommand _goBackCommand;
        RelayCommand _goForwardCommand;

        /// <summary>
        /// <see cref="RelayCommand"/> used to bind to the back Button's Command property
        /// for navigating to the most recent item in back navigation history, if a Frame
        /// manages its own navigation history.
        /// 
        /// The <see cref="RelayCommand"/> is set up to use the virtual method <see cref="GoBack"/>
        /// as the Execute Action and <see cref="CanGoBack"/> for CanExecute.
        /// </summary>
        public RelayCommand GoBackCommand
        {
            get
            {
                return _goBackCommand ?? (_goBackCommand = new RelayCommand(
                    GoBack,
                    CanGoBack));
            }
            set
            {
                _goBackCommand = value;
            }
        }

        /// <summary>
        /// <see cref="RelayCommand"/> used for navigating to the most recent item in 
        /// the forward navigation history, if a Frame manages its own navigation history.
        /// 
        /// The <see cref="RelayCommand"/> is set up to use the virtual method <see cref="GoForward"/>
        /// as the Execute Action and <see cref="CanGoForward"/> for CanExecute.
        /// </summary>
        public RelayCommand GoForwardCommand => _goForwardCommand ?? (_goForwardCommand = new RelayCommand(
            GoForward,
            CanGoForward));

        /// <summary>
        /// Virtual method used by the <see cref="GoBackCommand"/> property
        /// to determine if the <see cref="Frame"/> can go back.
        /// </summary>
        /// <returns>
        /// true if the <see cref="Frame"/> has at least one entry 
        /// in the back navigation history.
        /// </returns>
        public virtual bool CanGoBack()
        {
            return Frame != null && Frame.CanGoBack;
        }

        /// <summary>
        /// Virtual method used by the <see cref="GoForwardCommand"/> property
        /// to determine if the <see cref="Frame"/> can go forward.
        /// </summary>
        /// <returns>
        /// true if the <see cref="Frame"/> has at least one entry 
        /// in the forward navigation history.
        /// </returns>
        public virtual bool CanGoForward()
        {
            return Frame != null && Frame.CanGoForward;
        }

        /// <summary>
        /// Virtual method used by the <see cref="GoBackCommand"/> property
        /// to invoke the <see cref="Windows.UI.Xaml.Controls.Frame.GoBack()"/> method.
        /// </summary>
        public virtual void GoBack()
        {
            if (Frame != null && Frame.CanGoBack)
            {
                FrameBackRequestedEventArgs eventArgs = new FrameBackRequestedEventArgs();
                OnBackRequested?.Invoke(this, eventArgs);

                if (eventArgs == null || !eventArgs.IsHandled)
                {
                    Frame.GoBack();
                }
            }
        }

        /// <summary>
        /// Virtual method used by the <see cref="GoForwardCommand"/> property
        /// to invoke the <see cref="Windows.UI.Xaml.Controls.Frame.GoForward"/> method.
        /// </summary>
        public virtual void GoForward()
        {
            if (Frame != null && Frame.CanGoForward) Frame.GoForward();
        }

        /// <summary>
        /// Handler of event that system navigation was requested.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="backRequestedEventArgs">Instance of <see cref="FrameBackRequestedEventArgs"/>.</param>
        private void OnSystemNavigationBackRequested(object sender, BackRequestedEventArgs backRequestedEventArgs)
        {
            if (backRequestedEventArgs.Handled)
            {
                return;
            }
            if (GoBackCommand.CanExecute(null))
            {
                GoBackCommand.Execute(null);
                backRequestedEventArgs.Handled = true;
            }
        }

        /// <summary>
        /// Invoked on every keystroke, including system keys such as Alt key combinations, when
        /// this page is active and occupies the entire window.  Used to detect keyboard navigation
        /// between pages even when the page itself doesn't have focus.
        /// </summary>
        /// <param name="coreDispatcher">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher coreDispatcher,
            AcceleratorKeyEventArgs e)
        {
            var virtualKey = e.VirtualKey;

            // Only investigate further when Left, Right, or the dedicated Previous or Next keys
            // are pressed
            if ((e.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
                e.EventType == CoreAcceleratorKeyEventType.KeyDown) &&
                (virtualKey == VirtualKey.Left || virtualKey == VirtualKey.Right ||
                (int)virtualKey == 166 || (int)virtualKey == 167))
            {
                var coreWindow = Window.Current.CoreWindow;
                var downState = CoreVirtualKeyStates.Down;
                bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
                bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
                bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
                bool noModifiers = !menuKey && !controlKey && !shiftKey;
                bool onlyAlt = menuKey && !controlKey && !shiftKey;

                if (((int)virtualKey == 166 && noModifiers) ||
                    (virtualKey == VirtualKey.Left && onlyAlt))
                {
                    // When the previous key or Alt+Left are pressed navigate back
                    e.Handled = true;
                    GoBackCommand.Execute(null);
                }
                else if (((int)virtualKey == 167 && noModifiers) ||
                    (virtualKey == VirtualKey.Right && onlyAlt))
                {
                    // When the next key or Alt+Right are pressed navigate forward
                    e.Handled = true;
                    GoForwardCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Event that fires when back navigation is requested.
        /// </summary>
        public event EventHandler<FrameBackRequestedEventArgs> OnBackRequested;

        #endregion

        #region Process lifetime management

        private String _pageKey;

        /// <summary>
        /// Register this event on the current page to populate the page
        /// with content passed during navigation as well as any saved
        /// state provided when recreating a page from a prior session.
        /// </summary>
        public event LoadStateEventHandler LoadState;
        /// <summary>
        /// Register this event on the current page to preserve
        /// state associated with the current page in case the
        /// application is suspended or the page is discarded from
        /// the navigaqtion cache.
        /// </summary>
        public event SaveStateEventHandler SaveState;

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.  
        /// This method calls <see cref="LoadState"/>, where all page specific
        /// navigation and process lifetime management logic should be placed.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property provides the group to be displayed.</param>
        public void OnNavigatedTo(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(Frame);
            _pageKey = "Page-" + Frame.BackStackDepth;

            if (e.NavigationMode == NavigationMode.New)
            {
                // Clear existing state for forward navigation when adding a new page to the
                // navigation stack
                var nextPageKey = _pageKey;
                int nextPageIndex = Frame.BackStackDepth;
                while (frameState.Remove(nextPageKey))
                {
                    nextPageIndex++;
                    nextPageKey = "Page-" + nextPageIndex;
                }

                // Pass the navigation parameter to the new page
                LoadState?.Invoke(this, new LoadStateEventArgs(e.Parameter, null));
            }
            else
            {
                // Pass the navigation parameter and preserved page state to the page, using
                // the same strategy for loading suspended state and recreating pages discarded
                // from cache
                LoadState?.Invoke(this, new LoadStateEventArgs(e.Parameter, (Dictionary<String, Object>)frameState[_pageKey]));
            }

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = CanGoBack() ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
        }

        /// <summary>
        /// Invoked when this page will no longer be displayed in a Frame.
        /// This method calls <see cref="SaveState"/>, where all page specific
        /// navigation and process lifetime management logic should be placed.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property provides the group to be displayed.</param>
        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(Frame);
            var pageState = new Dictionary<String, Object>();
            SaveState?.Invoke(this, new SaveStateEventArgs(pageState));
            frameState[_pageKey] = pageState;

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = CanGoBack() ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
        }

        #endregion
    }

    /// <summary>
    /// Represents the method that will handle the <see cref="NavigationHelper.LoadState"/>event
    /// </summary>
    public delegate void LoadStateEventHandler(object sender, LoadStateEventArgs e);
    /// <summary>
    /// Represents the method that will handle the <see cref="NavigationHelper.SaveState"/>event
    /// </summary>
    public delegate void SaveStateEventHandler(object sender, SaveStateEventArgs e);

    /// <summary>
    /// Class used to hold the event data required when a page attempts to load state.
    /// </summary>
    public class LoadStateEventArgs : EventArgs
    {
        /// <summary>
        /// The parameter value passed to <see cref="Frame.Navigate(Type, Object)"/> 
        /// when this page was initially requested.
        /// </summary>
        public Object NavigationParameter { get; private set; }
        /// <summary>
        /// A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.
        /// </summary>
        public Dictionary<string, Object> PageState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadStateEventArgs"/> class.
        /// </summary>
        /// <param name="navigationParameter">
        /// The parameter value passed to <see cref="Frame.Navigate(Type, Object)"/> 
        /// when this page was initially requested.
        /// </param>
        /// <param name="pageState">
        /// A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.
        /// </param>
        public LoadStateEventArgs(Object navigationParameter, Dictionary<string, Object> pageState)
        {
            NavigationParameter = navigationParameter;
            PageState = pageState;
        }
    }
    /// <summary>
    /// Class used to hold the event data required when a page attempts to save state.
    /// </summary>
    public class SaveStateEventArgs : EventArgs
    {
        /// <summary>
        /// An empty dictionary to be populated with serializable state.
        /// </summary>
        public Dictionary<string, Object> PageState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveStateEventArgs"/> class.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        public SaveStateEventArgs(Dictionary<string, Object> pageState)
        {
            PageState = pageState;
        }
    }

    /// <summary>
    /// Class used to hold the event data required when a back navigation is requested.
    /// </summary>
    public class FrameBackRequestedEventArgs : EventArgs
    {
        public bool IsHandled { get; set; }

        /// <summary>
        /// Class constructor that creates instance of <see cref="FrameBackRequestedEventArgs"/>.
        /// </summary>
        public FrameBackRequestedEventArgs()
        {
            IsHandled = false;
        }
    }
}
