using System;
using System.Threading;
using Light.Common;
using Light.DataObjects;
using Light.Model;
using Light.ViewModel.Library;
using GalaSoft.MvvmLight.Messaging;
using Windows.UI.Core;
using Windows.UI.Xaml.Navigation;

namespace Light.View.Library
{
    /// <summary>
    /// Common Grouped GridView page.
    /// </summary>
    public sealed partial class CommonGroupedGridView
    {
        /// <summary>
        /// Common navigation helper for state save and navigation integration.
        /// </summary>
        private readonly NavigationHelper _navigationHelper;

        /// <summary>
        /// Common ViewModel for data.
        /// </summary>
        private LibraryViewModel _vm;

        /// <summary>
        /// Cancellation token source.
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// Page class constructor.
        /// </summary>
        public CommonGroupedGridView()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
        }

        /// <summary>
        /// Handle navigate to events and initialize variables like ViewModel.
        /// </summary>
        /// <param name="e">NavigationTo event arguments, including target data type.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);
            base.OnNavigatedTo(e);

            // Register message.
            RegisterMessage();

            // Set up cancellation token.
            _cts = new CancellationTokenSource();

            if (e.Parameter != null)
            {
                if (e.Parameter is CommonItemType)
                {
                    DataContext = _vm
                        = new LibraryViewModel((CommonItemType)e.Parameter, true);
                }
                else if (e.Parameter is string)
                {
                    DataContext = _vm
                        = new LibraryViewModel(new GroupedViewNavigationArgs((string)e.Parameter));
                }
                else if (e.Parameter is GroupedViewNavigationArgs)
                {
                    DataContext = _vm
                        = new LibraryViewModel((GroupedViewNavigationArgs) e.Parameter);
                }
            }
            else
            {
                DataContext = _vm
                    = new LibraryViewModel(CommonItemType.Album, true);
            }

            GroupedCvs.Source = _vm.GroupedItems.Items;
            GroupedCvs.IsSourceGrouped = true;
            await _vm.LoadDataAsync(_cts.Token);
        }

        /// <summary>
        /// Handle navigate from events and cleanup all used variables.
        /// </summary>
        /// <param name="e">NavigateFrom event arguments, used by common navigation helper.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Register message.
        /// </summary>
        private void RegisterMessage()
        {
            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
        }

        /// <summary>
        /// Unregister message.
        /// </summary>
        private void UnregisterMessage()
        {
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
        }

        /// <summary>
        /// Event handler after media library is indexed.
        /// This is a temporary solution during refactoring.
        /// </summary>
        /// <param name="obj"></param>
        private async void OnIndexFinished(MessageBase obj)
        {
            // Dispatcher is required here
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => OnIndexFinished(obj));
                return;
            }
            await _vm.LoadDataAsync(_cts.Token);
        }

        private void OnUnloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // Unreg message.
            UnregisterMessage();

            Bindings.StopTracking();

            // Cancel current event.
            _cts?.Cancel();
            _vm.Cleanup();
            _cts.Dispose();
        }
    }
}
