using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Utilities.Grouping;
using Light.View.Core;
using Light.View.Library;

namespace Light.ViewModel.Core
{
    /// <summary>
    /// Hosted page type.
    /// </summary>
    public enum HostedPageType
    {
        Unknown = -1,
        Album = 0,
        Artist = 1,
        Song = 2,
        Playlist = 3
    }

    /// <summary>
    /// Viewmodel for page host.
    /// </summary>
    public class LibraryHostViewModel : ViewModelBase
    {
        private readonly CoreDispatcher _dispatcher;

        private HostedPageType _type;

        internal bool _isAlbumSelected;
        internal bool _isArtistSelected;
        internal bool _isSongSelected;
        internal bool _isPlaylistSelected;
        private bool _isButtonClicked;
        private bool _isScanActive;

        private string _indexPrompt = "";
        private string _autoSuggestBoxText;

        private ICommand _settingsCommand;
        private RelayCommand<RoutedEventArgs> _searchButtonClickedRelayCommand;
        private RelayCommand<AutoSuggestBoxQuerySubmittedEventArgs> _autosuggestboxQueryRelayCommand;
        private ObservableCollection<IndexerComparerPair> _sortingOptions;
        private IPageGroupingStateManager _stateManager;
        private bool _isNavigationOpsAvailable;
        private int _selectedItemIndex;

        /// <summary>
        /// Dual-way bindable string for auto suggest box.
        /// </summary>
        public string AutoSuggestBoxText
        {
            get { return _autoSuggestBoxText; }
            set
            {
                _autoSuggestBoxText = value;
                NotifyChange(nameof(AutoSuggestBoxText));
            }
        }

        /// <summary>
        /// Radio button group - Indicates whether album button is selected.
        /// </summary>
        /// <seealso cref="IsArtistSelected"/>
        /// <seealso cref="IsSongSelected"/>
        /// <seealso cref="IsPlaylistSelected"/>
        public bool IsAlbumSelected
        {
            get
            {
                return _isAlbumSelected;
            }
            set
            {
                if (value) Type = HostedPageType.Album;
                if (_isAlbumSelected == value) return;
                _isAlbumSelected = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Radio button group - Indicates whether artist button is selected.
        /// </summary>
        /// <seealso cref="IsAlbumSelected"/>
        /// <seealso cref="IsSongSelected"/>
        /// <seealso cref="IsPlaylistSelected"/>
        public bool IsArtistSelected
        {
            get { return _isArtistSelected; }
            set
            {
                if (value) Type = HostedPageType.Artist;
                if (_isArtistSelected == value) return;
                _isArtistSelected = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// A value indicates whether the search button is clicked.
        /// </summary>
        public bool IsButtonClicked
        {
            get { return _isButtonClicked; }
            set
            {
                _isButtonClicked = value;

                NotifyChange(nameof(IsButtonClicked));
            }
        }

        /// <summary>
        /// A value indicates whether navigation ops is available at this moment.
        /// </summary>
        public bool IsNavigationOpsAvailable
        {
            get { return _isNavigationOpsAvailable; }
            set
            {
                _isNavigationOpsAvailable = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Radio button group - Indicates whether song button is selected.
        /// </summary>
        /// <seealso cref="IsAlbumSelected"/>
        /// <seealso cref="IsArtistSelected"/>
        /// <seealso cref="IsPlaylistSelected"/>
        public bool IsSongSelected
        {
            get { return _isSongSelected; }
            set
            {
                if (value) Type = HostedPageType.Song;
                if (_isSongSelected == value) return;
                _isSongSelected = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Radio button group - Indicates whether playlist button is selected.
        /// </summary>
        /// <seealso cref="IsAlbumSelected"/>
        /// <seealso cref="IsArtistSelected"/>
        /// <seealso cref="IsSongSelected"/>
        public bool IsPlaylistSelected
        {
            get { return _isPlaylistSelected; }
            set
            {
                if (value) Type = HostedPageType.Playlist;
                if (_isPlaylistSelected == value) return;
                _isPlaylistSelected = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Indicates current page type.
        /// </summary>
        /// <remarks>If this value is set, it will navigate to the desired page.</remarks>
        public HostedPageType Type
        {
            get { return _type; }
            set
            {
                if (_type == value) return;
                _type = value;
                RaisePropertyChanged();

                Messenger.Default.Send(new GenericMessage<HostedPageType>(value), CommonSharedStrings.HostedPageNavigationMessage);
            }
        }

        /// <summary>
        /// Settings button ICommand.
        /// </summary>
        public ICommand SettingsCommand
        {
            get { return _settingsCommand; }
            set
            {
                if (_settingsCommand == value) return;
                _settingsCommand = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Page grouping state manager for frontend view.
        /// </summary>
        public IPageGroupingStateManager StateManager => _stateManager;

        /// <summary>
        /// Indicates whether a indexing activity is in progress.
        /// </summary>
        public bool IsScanActive
        {
            get { return _isScanActive; }
            set
            {
                if (_isScanActive == value) return;
                _isScanActive = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Index prompt on Hosted UI view toolbar.
        /// </summary>
        public string IndexPrompt
        {
            get
            {
                return _indexPrompt;
            }
            set
            {
                if (_indexPrompt == value) return;
                _indexPrompt = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// A collection of sorting options for hosted page.
        /// </summary>
        public ObservableCollection<IndexerComparerPair> SortingOptions
        {
            get { return _sortingOptions; }
            set
            {
                _sortingOptions = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// A value indicates the current selected item.
        /// </summary>
        public int SelectedItemIndex
        {
            get { return _selectedItemIndex; }
            set
            {
                _selectedItemIndex = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Bindable relay command for search button clicks.
        /// </summary>
        public RelayCommand<RoutedEventArgs> SearchButtonClickedRelayCommand
        {
            get { return _searchButtonClickedRelayCommand; }
            set
            {
                _searchButtonClickedRelayCommand = value;
                NotifyChange(nameof(SearchButtonClickedRelayCommand));
            }
        }

        /// <summary>
        /// Bindable relay command for auto suggest box queries.
        /// </summary>
        public RelayCommand<AutoSuggestBoxQuerySubmittedEventArgs> AutoSuggestBoxQueryRelayCommand
        {
            get { return _autosuggestboxQueryRelayCommand; }
            set
            {
                if (value == _autosuggestboxQueryRelayCommand) return;
                _autosuggestboxQueryRelayCommand = value;

                NotifyChange(nameof(AutoSuggestBoxQueryRelayCommand));
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="dispatcher">The dispatcher for current UI thread.</param>
        public LibraryHostViewModel(CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            Type = HostedPageType.Unknown;
            AutoSuggestBoxQueryRelayCommand = new RelayCommand<AutoSuggestBoxQuerySubmittedEventArgs>(AutoSuggestBoxQuerySubmittedEventHandler);
            SearchButtonClickedRelayCommand = new RelayCommand<RoutedEventArgs>(SearchButtonClickedEventHandler);
            SettingsCommand = new SettingsButtonCommand();
            SortingOptions = new ObservableCollection<IndexerComparerPair>();
            IsNavigationOpsAvailable = false;
            SelectedItemIndex = -1;

            // Reg scan event
            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexStartedMessageToken, OnIndexStarted);
            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexGettingFilesMessageToken, OnIndexGettingFiles);
            Messenger.Default.Register<GenericMessage<string>>(this, CommonSharedStrings.IndexItemAddedMessageToken, OnItemAdded);
            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
        }

        /// <summary>
        /// Event command handler for auto suggest box queries.
        /// </summary>
        /// <param name="autoSuggestBoxQuerySubmittedEventArgs"></param>
        private void AutoSuggestBoxQuerySubmittedEventHandler(AutoSuggestBoxQuerySubmittedEventArgs autoSuggestBoxQuerySubmittedEventArgs)
        {
            if (IsNavigationOpsAvailable)
            {
                Messenger.Default.Send(
                   new GenericMessage<Tuple<Type, string>>(
                       new Tuple<Type, string>(typeof(CommonGroupedGridView), autoSuggestBoxQuerySubmittedEventArgs.QueryText)),
                   CommonSharedStrings.FrameViewNavigationMessageToken);

                // Hide search box
                IsButtonClicked = !IsButtonClicked;
                AutoSuggestBoxText = string.Empty;
            }
        }

        /// <summary>
        /// Clean up view model and unregister all message subscriptions.
        /// </summary>
        public override void Cleanup()
        {
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexStartedMessageToken, OnIndexStarted);
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexGettingFilesMessageToken, OnIndexGettingFiles);
            Messenger.Default.Unregister<GenericMessage<string>>(this, CommonSharedStrings.IndexItemAddedMessageToken, OnItemAdded);
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
            base.Cleanup();
        }

        /// <summary>
        /// Internal utility for notifying property changes.
        /// </summary>
        /// <param name="name">The name of the changed property.</param>
        protected async void NotifyChange(string name)
        {
            if (!_dispatcher.HasThreadAccess)
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => NotifyChange(name));
                return;
            }
            // ReSharper disable once ExplicitCallerInfoArgument
            RaisePropertyChanged(name);
        }

        /// <summary>
        /// Message handler for index finishing event.
        /// </summary>
        /// <param name="obj"></param>
        private async void OnIndexFinished(MessageBase obj)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    IsScanActive = false;
                    IndexPrompt = "";
                });
        }

        /// <summary>
        /// Message handler for getting files.
        /// </summary>
        /// <param name="obj"></param>
        private async void OnIndexGettingFiles(MessageBase obj)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => IndexPrompt = "Getting all files");
        }

        /// <summary>
        /// Message handler for index starting event.
        /// </summary>
        /// <param name="obj"></param>
        private async void OnIndexStarted(MessageBase obj)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => IsScanActive = true);
        }

        /// <summary>
        /// Message handler for adding items.
        /// </summary>
        /// <param name="obj"></param>
        private async void OnItemAdded(GenericMessage<string> obj)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    if (!IsScanActive)
                        IsScanActive = true;
                    IndexPrompt = string.Format("{0} files added", obj.Content);
                });
        }

        /// <summary>
        /// Update all grouping options. This method is intended to be called only from parent view.
        /// </summary>
        /// <param name="stateManager">The new state manager for the hosted page.</param>
        public void ReloadGroupingOptions(IPageGroupingStateManager stateManager)
        {
            IsNavigationOpsAvailable = false;

            _stateManager = stateManager;
            SortingOptions.Clear();

            // Reload options
            var options = _stateManager.PopulateAvailablePairs();
            foreach (var option in options)
            {
                SortingOptions.Add(option);
            }

            // Set default option
            // We don't need to set it - it is already passed to view
            var lastUsedOption = _stateManager.GetLastUsedPair();
            var elem = SortingOptions.Where(i => i.Identifier == lastUsedOption.Identifier).ToList();
            if (elem.Any())
            {
                SelectedItemIndex = SortingOptions.IndexOf(elem.FirstOrDefault());
            }

            // Enable selection
            IsNavigationOpsAvailable = true;
        }

        /// <summary>
        /// Event command handler for search button click events.
        /// </summary>
        /// <param name="routedEventArgs"></param>
        private void SearchButtonClickedEventHandler(RoutedEventArgs routedEventArgs)
        {
            IsButtonClicked = !IsButtonClicked;
            if (!IsButtonClicked) AutoSuggestBoxText = string.Empty;
        }

        /// <summary>
        /// Settings button nested class for event handling.
        /// </summary>
        public class SettingsButtonCommand : ICommand
        {
            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                Messenger.Default.Send(
                    new GenericMessage<Tuple<Type, string>>(
                        new Tuple<Type, string>(typeof(SettingsView), string.Empty)),
                    CommonSharedStrings.FrameViewNavigationMessageToken);
            }

#pragma warning disable CS0067
            public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
        }
    }
}
