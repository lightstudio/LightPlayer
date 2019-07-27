using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using Light.BuiltInCodec;
using Light.Common;
using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.Managed.Database.Native;
using Light.Managed.Tools;
using Light.Utilities;
using Newtonsoft.Json;

namespace Light.Core
{
    public class PlaybackControl
    {
        public const string DbMediaFileToken = "DbMediaFileEntity";
        public const string TrackReference = "TrackReferenceEntity";
        public const string StorageFileReference = "StorageFileReference";
        public const string MediaFileReference = "MediaFileReference";
        public const string SelfReference = "SelfReference";
        static public PlaybackControl Instance = new PlaybackControl();

        AsyncLock _asyncLock = new AsyncLock();
        private Random _random = new Random();
        private bool _restore = true;
        private bool _collectionUpdateEventEnabled = false;
        private UvcServices _uvc;
        private FfmpegCodec codec = new FfmpegCodec();
        private MediaElement _player;
        public MediaElement Player => _player;
        private PlaybackMode _mode = PlaybackMode.Sequential;
        public PlaybackMode MobileMode
        {
            get { return _mode; }
            set
            {
                if (value == PlaybackMode.Random || value == PlaybackMode.Sequential)
                {
                    _mode = PlaybackMode.ListLoop;
                }
                else
                {
                    _mode = value;
                }
                CheckAndUpdateNextItem().ConfigureAwait(false);
            }
        }
        public PlaybackMode Mode
        {
            get { return _mode; }
            set
            {
                if (_mode == PlaybackMode.Random && value != PlaybackMode.Random)
                {
                    _mode = value;
                    DisableShuffle();
                }
                else if (_mode != PlaybackMode.Random && value == PlaybackMode.Random)
                {
                    _mode = value;
                    EnableShuffle();
                }
                else
                {
                    _mode = value;
                }

                if (_mode == PlaybackMode.Sequential)
                {
                    var current = Current;
                    if (current == null)
                    {
                        _uvc.IsPrevEnabled = _uvc.IsNextEnabled = false;
                    }
                    else
                    {
                        _uvc.IsPrevEnabled = current.Node != _list.First;
                        _uvc.IsNextEnabled = current.Node != _list.Last;
                    }
                }
                else
                {
                    _uvc.IsPrevEnabled = true;
                    _uvc.IsNextEnabled = true;
                }
                CheckAndUpdateNextItem().ConfigureAwait(false);
            }
        }
        //stores current track and next track.
        private MediaPlaybackList _playlist = new MediaPlaybackList();
        //stores all items in now playing list.
        private LinkedList<MusicPlaybackItem> _list = new LinkedList<MusicPlaybackItem>();
        private ObservableCollection<MusicPlaybackItem> _items = new ObservableCollection<MusicPlaybackItem>();
        private List<MusicPlaybackItem> _seqBackup;
        //binds to UI.
        public ObservableCollection<MusicPlaybackItem> Items => _items;
        public MusicPlaybackItem Current => GetCurrentPlaybackItem();
        public bool ItemLoaded => _list != null && _list.Count > 0;
        public bool MediaLoadFailed { get; private set; } = false;
        public event EventHandler<NowPlayingChangedEventArgs> NowPlayingChanged;
        public event EventHandler NowPlayingRestored;
        public event EventHandler<(string FileName, string ErrorMessage)> ErrorOccurred;

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

        public async void EnableShuffle()
        {
            _seqBackup = _items.ToList();
            var current = Current;
            var shuffled = Shuffle(_seqBackup, new Random()).ToList();
            if (shuffled.Contains(current))
            {
                shuffled.Remove(current);
            }

            _items.Clear();
            _list.Clear();

            DisableCollectionUpdateEvent();
            if (current != null)
            {
                current.Node = _list.AddLast(current);
                _items.Add(current);
            }
            foreach (var item in shuffled)
            {
                item.Node = _list.AddLast(item);
            }
            foreach (var item in shuffled)
            {
                _items.Add(item);
            }
            EnableCollectionUpdateEvent();
            await CheckAndUpdateNextItem();
        }

        public async void DisableShuffle()
        {
            _list.Clear();
            _items.Clear();
            var c = Current.Node;
            foreach (var item in _seqBackup)
            {
                item.Node = _list.AddLast(item);
            }
            DisableCollectionUpdateEvent();
            foreach (var item in _seqBackup)
            {
                _items.Add(item);
            }
            EnableCollectionUpdateEvent();

            _seqBackup = null;
            await CheckAndUpdateNextItem();
        }

        public async void Initialize(MediaElement me)
        {
            using (await _asyncLock.LockAsync())
            {
                // Workaround for SMTC not working on mobile devices.
                if (PlatformInfo.CurrentPlatform == Platform.WindowsMobile)
                {
                    _uvc = new UvcServices(new MediaPlayer());
                }
                else
                {
                    _uvc = new UvcServices(SystemMediaTransportControls.GetForCurrentView());
                }
                _uvc.IsPauseEnabled = true;
                _uvc.IsPlayEnabled = true;
                _player = me;
                _player.AutoPlay = false;
                _playlist.CurrentItemChanged += OnCurrentItemChanged;
                _player.SetPlaybackSource(_playlist);
                _player.CurrentStateChanged += OnPlaybackStateChanged;
                _player.MediaFailed += OnMediaFailed;
                EnableCollectionUpdateEvent();
                CoreApplication.Suspending += OnApplicationSuspending;
                me.Volume = NowPlayingStateManager.Volume / 100;
            }
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ErrorOccurred?.Invoke(this, (Current?.File?.Path, e.ErrorMessage));
        }

        private async Task<string> ReadCacheAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(
                    ApplicationData.Current.LocalCacheFolder.Path,
                    fileName);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var file = await ApplicationData.Current.LocalCacheFolder.GetFileAsync(fileName);
                using (var stream = await file.OpenReadAsync())
                using (var s = stream.AsStreamForRead())
                using (var sr = new StreamReader(s))
                {
                    return await sr.ReadToEndAsync();
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task RestoreAsync()
        {
            try
            {
                if (!_restore)
                {
                    return;
                }
                _restore = false;

                var nptext = await ReadCacheAsync("NowPlaying.json");
                var npindex = await ReadCacheAsync("NowPlayingIndex");
                if (nptext != null)
                {
                    var files = JsonConvert.DeserializeObject<DbMediaFile[]>(nptext);

                    if (!int.TryParse(npindex, out int idx) || idx < 0 || idx > files.Length)
                    {
                        idx = 0;
                    }

                    await AddAndSetIndexAt(
                        files.Where(f => f != null)
                             .Select(f => MusicPlaybackItem.CreateFromMediaFile(f)), 
                        idx,
                        false);
                }
            }
            catch
            {
            }
            finally
            {
                NowPlayingRestored?.Invoke(this, null);
            }
        }

        private void OnApplicationSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            SaveNowPlaying();
        }

        private void SaveNowPlaying()
        {
            var playing = (from item
                           in (IEnumerable<MusicPlaybackItem>)_seqBackup ?? _items
                           select item.File).ToArray();
            var json = JsonConvert.SerializeObject(playing);
            File.WriteAllText(
                Path.Combine(
                    ApplicationData.Current.LocalCacheFolder.Path,
                    "NowPlaying.json"),
                json);
            if (Current != null)
            {
                int index = 0;
                if (_seqBackup != null)
                {
                    index = _seqBackup.IndexOf(Current);
                }
                else
                {
                    index = _items.IndexOf(Current);
                }
                //var index = _items.IndexOf(Current);
                File.WriteAllText(
                    Path.Combine(
                        ApplicationData.Current.LocalCacheFolder.Path,
                        "NowPlayingIndex"),
                    index.ToString());
            }
        }

        private void OnPlaybackStateChanged(object sender, object args)
        {
            switch (_player.CurrentState)
            {
                case MediaElementState.Buffering:
                case MediaElementState.Playing:
                case MediaElementState.Opening:
                    _uvc.Status = MediaPlaybackStatus.Playing;
                    break;
                case MediaElementState.Closed:
                case MediaElementState.Stopped:
                case MediaElementState.Paused:
                    _uvc.Status = MediaPlaybackStatus.Paused;
                    break;
            }
        }

        private void DisableCollectionUpdateEvent()
        {
            if (_collectionUpdateEventEnabled)
            {
                _items.CollectionChanged -= OnItemsCollectionChanged;
                _collectionUpdateEventEnabled = false;
            }
        }

        private void EnableCollectionUpdateEvent()
        {
            if (!_collectionUpdateEventEnabled)
            {
                _items.CollectionChanged += OnItemsCollectionChanged;
                _collectionUpdateEventEnabled = true;
            }
        }

        private async void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            using (await _asyncLock.LockAsync())
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        await InsertItems(e.NewItems.Cast<MusicPlaybackItem>(), e.NewStartingIndex, true, false);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        await InternalRemoveAsync(e.OldItems.Cast<MusicPlaybackItem>(), true);
                        break;
                        //These action are not likely to happen in WinRT.
                        //case NotifyCollectionChangedAction.Move:

                        //    break;
                        //case NotifyCollectionChangedAction.Replace:

                        //    break;
                        //case NotifyCollectionChangedAction.Reset:

                        //    break;
                }
            }
        }

        private bool UseSystemCodec(string fileType)
        {
            return string.Compare(fileType, ".wma", true) == 0;
        }

        private async Task<MediaPlaybackItem> AddToMediaPlaybackList(LinkedListNode<MusicPlaybackItem> item)
        {
            try
            {
                // Null check!
                if (item == null)
                {
                    return null;
                }

                MediaPlaybackItem mediaPlaybackItem = null;

                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.Value.File.Path);
                    if (UseSystemCodec(file.FileType))
                    {
                        mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file));
                    }
                    else
                    {
                        var mediaFile = codec.LoadFromFile(file);
#if !EFCORE_MIGRATION
                        var singleTrack = mediaFile.LoadTrack(item.Value.File.MediaCue);
                        mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromMediaStreamSource(singleTrack));
                        // Hold reference to prevent GC.
                        mediaPlaybackItem.Source.CustomProperties.Add(MediaFileReference, mediaFile);
                        mediaPlaybackItem.Source.CustomProperties.Add(TrackReference, singleTrack);
#endif
                    }
                    mediaPlaybackItem.Source.CustomProperties.Add(StorageFileReference, file);
                }
                catch (Exception ex)
                {
                    // Create MSS stub to defer the error.
                    MediaStreamSource source = new MediaStreamSource(
                        new AudioStreamDescriptor(
                            Windows.Media.MediaProperties.AudioEncodingProperties.CreatePcm(44100, 2, 16)))
                    {
                        CanSeek = true,
                        Duration = TimeSpan.FromSeconds(1)
                    };
                    source.MusicProperties.Album = item.Value.File.Album;
                    source.MusicProperties.Artist = item.Value.File.Artist;
                    source.MusicProperties.Title = item.Value.File.Title;
                    source.MusicProperties.Year = 0;
                    source.MusicProperties.TrackNumber = 0;
                    source.Starting += async (s, e) =>
                    {
                        s.NotifyError(MediaStreamSourceErrorStatus.FailedToOpenFile);
                        await _player.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                        {
                            _player.Pause();
                            ErrorOccurred?.Invoke(this, (item.Value.File.Path, ex.Message));
                        });
                    };
                    source.Closed += (s, e) => { };
                    source.SampleRequested += (s, e) => { };
                    mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromMediaStreamSource(source));
                    MediaLoadFailed = true;
                }
                // Hold metadata for playlist.
                mediaPlaybackItem.Source.CustomProperties.Add("Item", item.Value);
                mediaPlaybackItem.Source.CustomProperties.Add(DbMediaFileToken, item.Value.File);
                // Push it to current item.
                _playlist.Items.Add(mediaPlaybackItem);
                return mediaPlaybackItem;
            }
            catch
            {
                MediaLoadFailed = true;
                return null;
            }
        }

        private MusicPlaybackItem GetCurrentPlaybackItem()
        {
            if (_playlist.CurrentItem == null)
                return null;
            _playlist.CurrentItem.Source.CustomProperties.TryGetValue("Item", out object current);
            return current as MusicPlaybackItem;
        }

        private async void OnCurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            using (await _asyncLock.LockAsync())
            {
                if (args.NewItem != null)
                {
                    args.NewItem.Source.CustomProperties.TryGetValue("Item", out object current);
                    var item = current as MusicPlaybackItem;
                    _uvc.UpdateInfo(item.File);
                    if (Mode == PlaybackMode.Sequential)
                    {
                        _uvc.IsPrevEnabled = item.Node != _list.First;
                        _uvc.IsNextEnabled = item.Node != _list.Last;
                    }
                    else
                    {
                        _uvc.IsPrevEnabled = true;
                        _uvc.IsNextEnabled = true;
                    }
                    // Workaround: MediaPlaybackList dead lock.
                    await _player.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
                    {
                        await CheckAndUpdateNextItem();
                    });
                    NowPlayingChanged?.Invoke(this, new NowPlayingChangedEventArgs { NewItem = Current });
                    await PlaybackHistoryManager.Instance.AddHistoryAsync(item);
                }
            }
        }

        private async Task AddNextTrack(bool end)
        {
            var current = Current;
            LinkedListNode<MusicPlaybackItem> _c = null;
            switch (_mode)
            {
                case PlaybackMode.Random:
                case PlaybackMode.ListLoop:
                    if (end)
                    {
                        _c = _list.First;
                    }
                    else
                    {
                        _c = current.Node.Next;
                    }
                    while (_c != null &&
                        await AddToMediaPlaybackList(_c) == null)
                    {
                        _c = _c.Next;
                    }
                    break;
                case PlaybackMode.Sequential:
                    if (!end)
                    {
                        _c = current.Node.Next;
                        while (_c != null &&
                            await AddToMediaPlaybackList(_c) == null)
                        {
                            _c = _c.Next;
                        }
                    }
                    break;
                case PlaybackMode.SingleTrackLoop:
                    await AddToMediaPlaybackList(current.Node);
                    break;
                    //int n = 0,
                    //    count = 0;//In case all files cannot be read, the app should not go into a infinite loop.
                    //do
                    //{
                    //    n = _random.Next(_items.Count);
                    //    count++;
                    //}
                    //while (await AddToMediaPlaybackList(_items[n].Node) == null && count < _items.Count);
                    //break;
            }
        }

        private bool CheckNextTrack(
            MusicPlaybackItem current,
            MusicPlaybackItem inListNext)
        {
            switch (_mode)
            {
                case PlaybackMode.Random:
                case PlaybackMode.ListLoop:
                    if (current.Node.Next != null)
                    {
                        if (current.Node.Next.Value == inListNext)
                            return true;
                        else return false;
                    }
                    else
                    {
                        if (inListNext == _list.First?.Value)
                            return true;
                        else return false;
                    }
                case PlaybackMode.Sequential:
                    if (current.Node.Next != null)
                    {
                        if (current.Node.Next.Value == inListNext)
                            return true;
                        else return false;
                    }
                    else return true;
                case PlaybackMode.SingleTrackLoop:
                    if (current == inListNext)
                        return true;
                    else return false;
                //case PlaybackMode.Random:
                //    return true;
                default://which should never happen.
                    return true;
            }
        }

        private async Task CheckAndUpdateNextItem()
        {
            int currentIndex = -1;
            //Clear all tracks except current and next
            if (_playlist.CurrentItem != null &&
                (currentIndex = _playlist.Items.IndexOf(_playlist.CurrentItem)) != 0)
            {
                for (int i = 0; i < currentIndex; i++)
                {
                    _playlist.Items.RemoveAt(0);
                }
                for (int i = 2; i < _playlist.Items.Count; i++)
                {
                    _playlist.Items.RemoveAt(2);
                }
            }

            if (_playlist.Items.Count == 0 &&
                _list.Count != 0)
            {
                await AddToMediaPlaybackList(_list.First);
                if (_list.First.Next != null)
                {
                    await AddNextTrack(true);
                }
            }
            else if (_playlist.Items.Count == 1)
            {
                var n = Current?.Node;
                if (n?.Next != null)
                {
                    await AddNextTrack(false);
                }
                else if (n?.List != null)
                {
                    //This is the last node.
                    await AddNextTrack(true);
                }
                else
                {
                    //This node is removed from list.
                    //replay from the first track.
                    if (_list.Count != 0)
                        await AddToMediaPlaybackList(_list.First);
                }
            }
            else if (_playlist.Items.Count >= 2 &&
                _list.Count != 0)
            {
                if (_playlist.CurrentItem == null)
                {
                    //TODO: Check reason.
                    return;
                }
                _playlist.CurrentItem.Source.CustomProperties.TryGetValue("Item", out object current);
                _playlist.Items[1].Source.CustomProperties.TryGetValue("Item", out object next);
                var n = (current as MusicPlaybackItem).Node;
                if (n.Next != null)
                {
                    if (!CheckNextTrack(current as MusicPlaybackItem, next as MusicPlaybackItem))
                    {
                        _playlist.Items.RemoveAt(1);
                        await AddNextTrack(false);
                    }
                }
                else if (n.List != null)
                {
                    _playlist.Items.RemoveAt(1);
                    //This is the last node.
                    await AddNextTrack(true);
                }
                else
                {
                    //This node is removed from list.
                    //Do nothing.
                }
            }
        }

        private async Task AddItemsToLast(IEnumerable<MusicPlaybackItem> items, bool preserveObservableCollection = false, bool play = true)
        {
            var newitems = items.ToList();
            if (_mode == PlaybackMode.Random)
            {
                _seqBackup.AddRange(newitems);
            }
            foreach (var item in _mode == PlaybackMode.Random ? Shuffle(newitems, new Random()) : newitems)
            {
                item.Node = _list.AddLast(item);
                if (!preserveObservableCollection)
                    _items.Add(item);
            }
            if (_playlist.Items.Count == 0)
            {
                _playlist.Items.Clear();
                await AddToMediaPlaybackList(_list.First);
                if (_playlist.Items.Count > 0 && play)
                    InternalPlay();
            }
            else
            {
                await CheckAndUpdateNextItem();
            }
        }

        private async Task AddItemsToCurrent(IEnumerable<MusicPlaybackItem> items, bool preserveObservableCollection = false, bool play = true)
        {
            var current = GetCurrentPlaybackItem();
            if (current?.Node.Next == null)
            {
                await AddItemsToLast(items, preserveObservableCollection, play);
                return;
            }
            var previous = current.Node;
            var currentIndex = _items.IndexOf(current) + 1;
            foreach (var item in items)
            {
                previous = item.Node = _list.AddAfter(previous, item);
                if (!preserveObservableCollection)
                    _items.Insert(currentIndex, item);
                currentIndex++;
            }
            if (_playlist.Items.Count == 0)
            {
                await AddToMediaPlaybackList(_list.First);
                if (play)
                    InternalPlay();
            }
            else
            {
                await CheckAndUpdateNextItem();
            }
        }

        private async Task InsertItems(IEnumerable<MusicPlaybackItem> items, int insertAt, bool preserveObservableCollection = false, bool play = true)
        {
            if (insertAt >= _list.Count)
            {
                await AddItemsToLast(items, preserveObservableCollection, play);
                return;
            }
            LinkedListNode<MusicPlaybackItem> previous = null;
            var currentIndex = insertAt;
            if (insertAt == 0)
            {
                foreach (var item in items)
                {
                    if (previous == null)
                        previous = item.Node = _list.AddFirst(item);
                    else
                        previous = item.Node = _list.AddAfter(previous, item);
                    if (!preserveObservableCollection)
                        _items.Insert(currentIndex, item);
                    currentIndex++;
                }
            }
            else
            {
                previous = _items[insertAt - 1].Node;
                foreach (var item in items)
                {
                    previous = item.Node = _list.AddAfter(previous, item);
                    if (!preserveObservableCollection)
                        _items.Insert(currentIndex, item);
                    currentIndex++;
                }
            }
            if (_playlist.Items.Count == 0)
            {
                await AddToMediaPlaybackList(_list.First);
                if (play)
                    InternalPlay();
            }
            else
            {
                await CheckAndUpdateNextItem();
            }
        }

        /// <summary>
        /// Add single file to the now playing list.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="index">The insert position. Use -1 to append to the list, -2 to add as next track.</param>
        public async Task AddFile(MusicPlaybackItem item, int index = -1)
        {
            using (await _asyncLock.LockAsync())
            {
                DisableCollectionUpdateEvent();
                try
                {
                    switch (index)
                    {
                        case -1:
                            await AddItemsToLast(new MusicPlaybackItem[] { item });
                            break;
                        case -2:
                            await AddItemsToCurrent(new MusicPlaybackItem[] { item });
                            break;
                        default:
                            await InsertItems(new MusicPlaybackItem[] { item }, index);
                            break;
                    }
                }
                finally
                {
                    EnableCollectionUpdateEvent();
                }
            }
        }

        public async Task AddToNextAndPlay(MusicPlaybackItem item)
        {
            using (await _asyncLock.LockAsync())
            {
                DisableCollectionUpdateEvent();
                try
                {
                    var current = GetCurrentPlaybackItem();
                    if (current?.Node.Next == null)
                    {
                        await AddItemsToLast(new[] { item }, false, true);
                        return;
                    }
                    var previous = current.Node;
                    var currentIndex = _items.IndexOf(current) + 1;
                    item.Node = _list.AddAfter(previous, item);

                    _playlist.Items.Clear();
                    await AddToMediaPlaybackList(item.Node);
                    await CheckAndUpdateNextItem();
                    _items.Insert(currentIndex, item);
                    InternalPlay();
                }
                finally
                {
                    EnableCollectionUpdateEvent();
                }
            }
        }

        public async Task AddAndSetIndexAt(IEnumerable<MusicPlaybackItem> items, int playAt, bool play = true)
        {
            using (await _asyncLock.LockAsync())
            {
                DisableCollectionUpdateEvent();
                try
                {
                    if (_items.Count > 0)
                    {
                        _items.Clear();
                        _list.Clear();
                        _seqBackup?.Clear();
                    }
                    if (_playlist.Items.Count > 0)
                    {
                        _playlist.Items.Clear();
                    }

                    if (_mode == PlaybackMode.Random)
                    {
                        var newItems = items.ToList();
                        if (newItems.Count == 0)
                        {
                            return;
                        }
                        var playAtItem = newItems[playAt];
                        _seqBackup.AddRange(newItems);
                        foreach (var item in Shuffle(newItems, new Random()))
                        {
                            item.Node = _list.AddLast(item);
                            _items.Add(item);
                        }

                        if (_items.Count == 0 || playAt < 0 || playAt >= _items.Count)
                            return;

                        await AddToMediaPlaybackList(playAtItem.Node);
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            item.Node = _list.AddLast(item);
                            _items.Add(item);
                        }

                        if (_items.Count == 0 || playAt < 0 || playAt >= _items.Count)
                            return;

                        await AddToMediaPlaybackList(_items[playAt].Node);
                    }

                    if (play)
                        InternalPlay();
                }
                finally
                {
                    EnableCollectionUpdateEvent();
                }
            }
        }

        /// <summary>
        /// Add multiple files to the now playing list.
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <param name="index">The insert position. Use -1 to append to the list, -2 to add as next track.</param>
        public async Task AddFile(IEnumerable<MusicPlaybackItem> items, int index = -1)
        {
            using (await _asyncLock.LockAsync())
            {
                DisableCollectionUpdateEvent();
                try
                {
                    switch (index)
                    {
                        case -1:
                            await AddItemsToLast(items);
                            break;
                        case -2:
                            await AddItemsToCurrent(items);
                            break;
                        default:
                            await InsertItems(items, index);
                            break;
                    }
                }
                finally
                {
                    EnableCollectionUpdateEvent();
                }
            }
        }

        public async Task AddFile(int Id, int index = -1)
        {
            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                var entity = context.MediaFiles.First(i => i.Id == Id);
                if (entity != null)
                    await AddFile(MusicPlaybackItem.CreateFromMediaFile(entity), index);
            }
        }

        /// <summary>
        /// Clear the playlist.
        /// </summary>
        public async void Clear()
        {
            using (await _asyncLock.LockAsync())
            {
                DisableCollectionUpdateEvent();
                try
                {
                    //_player.Pause();
                    _playlist.Items.Clear();
                    _list.Clear();
                    _items.Clear();
                    _seqBackup?.Clear();
                }
                finally
                {
                    EnableCollectionUpdateEvent();
                }
            }
        }

        public async void PlayOrPause()
        {
            using (await _asyncLock.LockAsync())
            {
                if (_player.CurrentState == MediaElementState.Playing ||
                _player.CurrentState == MediaElementState.Buffering)
                {
                    _player.Pause();
                }
                else
                {
                    _player.Play();
                }
            }
        }

        private void InternalPlay()
        {
            if (_player.CurrentState == MediaElementState.Opening)
            {
                RoutedEventHandler h = null;
                h = new RoutedEventHandler((s, e) =>
                {
                    _player.MediaOpened -= h;
                    _player.Play();
                });
                _player.MediaOpened += h;
            }
            else if (_player.CurrentState != MediaElementState.Playing ||
                _player.CurrentState != MediaElementState.Buffering)
                _player.Play();
        }

        public async void Play()
        {
            using (await _asyncLock.LockAsync())
            {
                InternalPlay();
            }
        }

        public async void Pause()
        {
            using (await _asyncLock.LockAsync())
            {
                _player.Pause();
            }
        }

        private async Task InternalPlayAt(int index)
        {
            if (_list.Count == 0)
            {
                return;
            }
            else if (index > _list.Count)
            {
                await PlayAt(0);
                return;
            }
            _playlist.Items.Clear();
            await AddToMediaPlaybackList(_items[index].Node);
            InternalPlay();
        }

        public async Task PlayAt(int index)
        {
            using (await _asyncLock.LockAsync())
            {
                await InternalPlayAt(index);
            }
        }

        private async Task InternalPlayAt(MusicPlaybackItem item)
        {
            var index = _items.IndexOf(item);
            if (index != -1)
            {
                await InternalPlayAt(index);
                return;
            }
            _playlist.Items.Clear();
            await AddToMediaPlaybackList(item.Node);
            InternalPlay();

        }

        public async Task PlayAt(MusicPlaybackItem item)
        {
            using (await _asyncLock.LockAsync())
            {
                await InternalPlayAt(item);
            }
        }

        public async void Stop()
        {
            using (await _asyncLock.LockAsync())
            {
                _player.Pause();
                _playlist.Items.Clear();
                await CheckAndUpdateNextItem();
            }
        }

        public async void Prev()
        {
            using (await _asyncLock.LockAsync())
            {
                var current = GetCurrentPlaybackItem();
                if (current == null)
                {
                    if (_list.Count != 0)
                        await AddToMediaPlaybackList(_list.First);
                }
                else
                {
                    var previous = current.Node.Previous;
                    _playlist.Items.Clear();
                    if (previous == null)
                    {
                        if (_list.Count != 0)
                            await AddToMediaPlaybackList(_list.Last);
                    }
                    else
                    {
                        await AddToMediaPlaybackList(previous);
                    }
                }
            }
        }

        public async void Next()
        {
            using (await _asyncLock.LockAsync())
            {
                if (_playlist.Items.Count < 2)
                {
                    await AddToMediaPlaybackList(_list.First);
                }
                _playlist.MoveNext();
            }
        }

        public async void SetIndex(int index)
        {
            using (await _asyncLock.LockAsync())
            {
                _playlist.Items.Clear();
                await AddToMediaPlaybackList(_items[index].Node);
                //CheckAndUpdateNextItem();
            }
        }

        public async Task RemoveAsync(MusicPlaybackItem item, bool preserveObservableCollection = false)
        {
            using (await _asyncLock.LockAsync())
            {
                _list.Remove(item);
                if (!preserveObservableCollection)
                    _items.Remove(item);
                await CheckAndUpdateNextItem();
            }
        }

        private async Task InternalRemoveAsync(IEnumerable<MusicPlaybackItem> items, bool preserveObservableCollection = false)
        {
            foreach (var item in items)
                _list.Remove(item);
            if (!preserveObservableCollection)
                foreach (var item in items)
                    _items.Remove(item);
            await CheckAndUpdateNextItem();
        }

        public async Task RemoveAsync(IEnumerable<MusicPlaybackItem> items, bool preserveObservableCollection = false)
        {
            using (await _asyncLock.LockAsync())
            {
                await InternalRemoveAsync(items, preserveObservableCollection);
            }
        }

        public async void SetPosition(TimeSpan position)
        {
            using (await _asyncLock.LockAsync())
            {
                _player.Position = position;
            }
        }

        public async void SetVolume(double volume)
        {
            using (await _asyncLock.LockAsync())
            {
                _player.Volume = volume;
            }
        }

        public void SetRestore(bool restore)
        {
            _restore = restore;
        }
    }
}
