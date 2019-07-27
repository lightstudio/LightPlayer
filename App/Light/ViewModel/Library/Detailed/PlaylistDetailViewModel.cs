using System;
using System.Linq;
using Windows.UI.Core;
using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using Light.Model;
using Light.Core;
using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Command;
using System.Collections.Specialized;
using Light.Shell;
using Light.Managed.Database.Native;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Light.ViewModel.Library.Detailed
{
    internal class PlaylistDetailViewModel : ViewModelBase
    {
        Playlist _tempPlaylist;
        public string ViewTitle
        {
            get { return Playlist.Title; }
        }
        public ObservableCollection<PlaylistItem> ViewItems
        {
            get { return _tempPlaylist.Items; }
        }

        private ObservableCollection<string> _groupOptions;
        public ObservableCollection<string> GroupOptions
        {
            get { return _groupOptions; }
            set
            {
                _groupOptions = value;
                RaisePropertyChanged();
            }
        }

        public Playlist TempPlaylist
        {
            get { return _tempPlaylist; }
            set
            {
                if (_tempPlaylist == value)
                    return;
                _tempPlaylist = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ViewItems));
            }
        }

        public bool IsEmpty => ViewItems.Count == 0;

        private Playlist _playlist;
        public Playlist Playlist
        {
            get { return _playlist; }
            set
            {
                if (_playlist == value)
                    return;
                _playlist = value;
                RaisePropertyChanged();
            }
        }

        private bool _playlistUnsaved;
        public bool PlaylistUnsaved
        {
            get { return _playlistUnsaved; }
            set
            {
                if (_playlistUnsaved == value)
                    return;
                _playlistUnsaved = value;
                RaisePropertyChanged();
            }
        }

        private bool _isEditToggleButtonChecked = false;

        public bool IsEditToggleButtonChecked
        {
            get => _isEditToggleButtonChecked;
            set
            {
                if (_isEditToggleButtonChecked == value)
                {
                    return;
                }
                _isEditToggleButtonChecked = value;
                RaisePropertyChanged();
            }
        }

        private ListViewSelectionMode _playlistListViewSelectionMode;
        public ListViewSelectionMode PlaylistListViewSelectionMode
        {
            get => _playlistListViewSelectionMode;
            set
            {
                if (_playlistListViewSelectionMode == value)
                {
                    return;
                }
                _playlistListViewSelectionMode = value;
                RaisePropertyChanged();
            }
        }

        public IList<object> SelectedItems { get; set; }

        public RelayCommand PlayAllCommand { get; }

        public RelayCommand AddToListCommand { get; }

        public RelayCommand ShareCommand { get; }

        public RelayCommand SaveCommand { get; }

        public RelayCommand DeleteCommand { get; }

        private async void AddToList()
        {
            await PlaybackControl.Instance.AddFile(
                from item
                in ViewItems
                select MusicPlaybackItem.CreateFromMediaFile(
                    item.ToMediaFile()));
        }

        private async void PlayAll()
        {
            PlaybackControl.Instance.Stop();
            PlaybackControl.Instance.Clear();
            await PlaybackControl.Instance.AddFile(
                from item
                in ViewItems
                select MusicPlaybackItem.CreateFromMediaFile(
                    item.ToMediaFile()));
        }

        private async void Save()
        {
            Playlist.Items.CollectionChanged -= OnItemsCollectionChanged;
            TempPlaylist.Items.CollectionChanged -= OnTempCollectionChanged;
            await PlaylistManager.Instance.AddOrUpdatePlaylistAsync(_tempPlaylist);
            Playlist = _tempPlaylist;
            TempPlaylist = Playlist.Duplicate(Playlist.Title);
            Playlist.Items.CollectionChanged += OnItemsCollectionChanged;
            TempPlaylist.Items.CollectionChanged += OnTempCollectionChanged;
            PlaylistUnsaved = false;
        }

        private async void Share()
        {
            List<StorageFile> files = new List<StorageFile>();
            foreach (var file in _playlist.Items)
            {
                var f = await NativeMethods.GetStorageFileFromPathAsync(file.Path) as StorageFile;
                if (f != null)
                    files.Add(f);
            }
            if (files.Count == 0)
                return;
            var shareService = ShareServices.GetForCurrentView();
            shareService.PrecleanForSession();
            shareService.Title = ViewTitle;
            shareService.Description = string.Empty;
            shareService.AddFiles(files);
            shareService.ShowShareUI();
        }

        public PlaylistDetailViewModel(string playlistTitle)
        {
            AddToListCommand = new RelayCommand(AddToList);
            PlayAllCommand = new RelayCommand(PlayAll);
            ShareCommand = new RelayCommand(Share);
            SaveCommand = new RelayCommand(Save);
            DeleteCommand = new RelayCommand(Delete);
            Playlist = PlaylistManager.Instance.GetPlaylist(playlistTitle);
            TempPlaylist = Playlist.Duplicate(Playlist.Title);
            Playlist.Items.CollectionChanged += OnItemsCollectionChanged;
            TempPlaylist.Items.CollectionChanged += OnTempCollectionChanged;
        }

        private void Delete()
        {
            if (SelectedItems != null)
            {
                foreach (PlaylistItem item in SelectedItems.ToArray())
                {
                    TempPlaylist.Items.Remove(item);
                }
            }
            IsEditToggleButtonChecked = false;
            PlaylistListViewSelectionMode = ListViewSelectionMode.None;
        }

        public override void Cleanup()
        {
            Playlist.Items.CollectionChanged -= OnItemsCollectionChanged;
            TempPlaylist.Items.CollectionChanged -= OnTempCollectionChanged;
            base.Cleanup();
        }

        private void OnTempCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            PlaylistUnsaved = true;
            RaisePropertyChanged("IsEmpty");
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TempPlaylist.Items.CollectionChanged -= OnTempCollectionChanged;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (PlaylistItem item in e.NewItems)
                        if (!_tempPlaylist.Items.Contains(item))
                            _tempPlaylist.Items.Add(item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (PlaylistItem item in e.OldItems)
                        if (_tempPlaylist.Items.Contains(item))
                            _tempPlaylist.Items.Remove(item);
                    break;
            }
            TempPlaylist.Items.CollectionChanged += OnTempCollectionChanged;
            RaisePropertyChanged("IsEmpty");
        }
    }
}
