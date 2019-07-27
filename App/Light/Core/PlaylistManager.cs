using Light.Common;
using Light;
using Light.Managed.Database.Native;
using Light.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Playlists;
using Windows.Storage;
using SystemPlaylist = Windows.Media.Playlists.Playlist;
using Light.CueIndex;

namespace Light.Core
{
    public enum FavoriteChangeType
    {
        Removed,
        Added,
        Unknown
    }
    public class FavoriteChangedEventArgs : EventArgs
    {
        public PlaylistItem Item { get; set; }
        public FavoriteChangeType Change { get; set; }
    }
    public enum PlaylistChangeAction
    {
        Add,
        Remove,
        Rename,
        Content
    }
    public class PlaylistChangedEventArgs : EventArgs
    {
        public PlaylistChangeAction Action { get; set; }
        public Playlist Playlist { get; set; }
        public string NewTitle { get; set; }
        public string OldTitle { get; set; }
    }
    public class PlaylistManager
    {
        public static PlaylistManager Instance;
        static PlaylistManager()
        {
            Instance = new PlaylistManager();
        }

        private Dictionary<string, string> _nameToPathMap = new Dictionary<string, string>();
        private Dictionary<string, Playlist> _playlists = new Dictionary<string, Playlist>();
        private StorageFolder _playlistFolder;
        private Random _random = new Random();
        private string _favoriteListName;
        private bool _initialized = false;
        private AsyncLock _syncRoot = new AsyncLock();

        public event EventHandler<FavoriteChangedEventArgs> FavoriteChanged;
        public event EventHandler<PlaylistChangedEventArgs> PlaylistChanged;

        public Playlist FavoriteList => _playlists[_favoriteListName];

        public async Task ExportAsync(Playlist list, StorageFolder destFolder, string fileName)
        {
            if (fileName.ToLower().EndsWith(".wpl"))
                fileName = fileName.Remove(fileName.Length - 4, 4);
            var export = new SystemPlaylist();
            foreach (var item in list.Items)
            {
                var file = await NativeMethods.GetStorageFileFromPathAsync(item.Path) as StorageFile;
                if (file != null)
                    export.Files.Add(file);
            }
            await export.SaveAsAsync(destFolder, fileName, NameCollisionOption.ReplaceExisting, PlaylistFormat.WindowsMedia);
        }

        private async Task<StorageFolder> GetPlaylistFolder()
        {
            if (Directory.Exists(
                Path.Combine(ApplicationData.Current.LocalFolder.Path, "Playlists")))
            {
                return await ApplicationData.Current.LocalFolder.GetFolderAsync("Playlists");
            }
            else
            {
                return await ApplicationData.Current.LocalFolder.CreateFolderAsync("Playlists");
            }
        }

        public async Task InitializeAsync(string localizedFavoriteName)
        {
            using (await _syncRoot.LockAsync())
            {
                if (_initialized)
                    return;
                _favoriteListName = localizedFavoriteName;
                _playlistFolder = await GetPlaylistFolder();
                foreach (var file in await _playlistFolder.GetFilesAsync())
                {
                    if (string.Compare(file.FileType, ".json", true) == 0)
                    {
                        try
                        {
                            using (var s = await file.OpenReadAsync())
                            using (var stream = s.AsStreamForRead())
                            using (var sr = new StreamReader(stream))
                            {
                                var content = await sr.ReadToEndAsync();
                                var playlist = JsonConvert.DeserializeObject<Playlist>(content);
                                _nameToPathMap.Add(playlist.Title, file.Path);
                                _playlists.Add(playlist.Title, playlist);
                            }
                        }
                        catch { }
                    }
                }
                if (!_playlists.ContainsKey(localizedFavoriteName))
                {
                    await CreateBlankPlaylist(localizedFavoriteName);
                }
                FavoriteList.Items.CollectionChanged += OnFavoriteItemsChanged;
                _initialized = true;
            }
        }

        private void OnFavoriteItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (PlaylistItem item in e.NewItems)
                    {
                        FavoriteChanged?.Invoke(
                            this,
                            new FavoriteChangedEventArgs
                            {
                                Change = FavoriteChangeType.Added,
                                Item = item
                            });
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (PlaylistItem item in e.OldItems)
                    {
                        FavoriteChanged?.Invoke(
                            this,
                            new FavoriteChangedEventArgs
                            {
                                Change = FavoriteChangeType.Removed,
                                Item = item
                            });
                    }
                    break;
            }
        }

        private string RandomFileNamePath()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for (;;)
            {
                var str = Path.Combine(
                    _playlistFolder.Path,
                    new string(Enumerable.Repeat(chars, 10)
                    .Select(s => s[_random.Next(s.Length)]).ToArray()) + ".json");
                if (!_nameToPathMap.ContainsValue(str))
                {
                    return str;
                }
            }
        }

        private async Task WriteToDisk(string Title)
        {
            await Task.Run(() =>
            {
                var path = _nameToPathMap[Title];
                var list = _playlists[Title];
                File.WriteAllText(path, JsonConvert.SerializeObject(list));
            });
        }

        private void ReplaceFavoriteList(Playlist newList)
        {
            FavoriteList.Items.CollectionChanged -= OnFavoriteItemsChanged;
            newList.Items.CollectionChanged += OnFavoriteItemsChanged;
            _playlists[_favoriteListName] = newList;
            FavoriteChanged?.Invoke(this,
                new FavoriteChangedEventArgs { Change = FavoriteChangeType.Unknown });
        }

        private async Task AddOrUpdatePlaylist(Playlist list)
        {
            if (_playlists.ContainsKey(list.Title)) //Update
            {
                if (_playlists[list.Title] != list)
                {
                    if (list.Title == _favoriteListName)
                    {
                        ReplaceFavoriteList(list);
                    }
                    else
                    {
                        _playlists[list.Title] = list;
                    }
                }

                PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                {
                    Action = PlaylistChangeAction.Content,
                    NewTitle = list.Title,
                    Playlist = list,
                });
            }
            else //Add
            {
                _nameToPathMap.Add(list.Title, RandomFileNamePath());
                _playlists.Add(list.Title, list);

                PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                {
                    Action = PlaylistChangeAction.Add,
                    NewTitle = list.Title,
                    Playlist = list,
                });
            }
            await WriteToDisk(list.Title);
        }

        public async Task AddOrUpdatePlaylistAsync(Playlist list)
        {
            using (await _syncRoot.LockAsync())
            {
                await AddOrUpdatePlaylist(list);
            }
        }

        private async Task CreateBlankPlaylist(string name, bool _override = false)
        {
            if (_playlists.ContainsKey(name))
            {
                if (_override)
                {
                    if (name == _favoriteListName)
                    {
                        ReplaceFavoriteList(new Playlist { Title = name });
                    }
                    else
                    {
                        _playlists[name] = new Playlist { Title = name };
                    }

                    PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                    {
                        Action = PlaylistChangeAction.Content,
                        NewTitle = name,
                        Playlist = _playlists[name],
                    });
                }
                else
                    throw new ArgumentException(string.Format(
                        CommonSharedStrings.PlaylistAlreadyExistPrompt, name));
            }
            else
            {
                _nameToPathMap.Add(name, RandomFileNamePath());
                _playlists.Add(name, new Playlist { Title = name });

                PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                {
                    Action = PlaylistChangeAction.Add,
                    NewTitle = name,
                    Playlist = _playlists[name],
                });
            }
            await WriteToDisk(name);
        }

        public async Task CreateBlankPlaylistAsync(string name, bool _override = false)
        {
            using (await _syncRoot.LockAsync())
            {
                await CreateBlankPlaylist(name, _override);
            }
        }

        public async Task AddToListAsync(string name, IEnumerable<PlaylistItem> items)
        {
            using (await _syncRoot.LockAsync())
            {
                var list = _playlists[name];
                foreach (var item in items)
                {
                    list.Items.Add(item);
                }
                PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                {
                    Action = PlaylistChangeAction.Content,
                    NewTitle = name,
                    Playlist = list,
                });
                await WriteToDisk(name);
            }
        }

        public async Task AddToListAsync(string name, PlaylistItem item)
        {
            using (await _syncRoot.LockAsync())
            {
                var list = _playlists[name];
                list.Items.Add(item);

                PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                {
                    Action = PlaylistChangeAction.Content,
                    NewTitle = name,
                    Playlist = list,
                });
                await WriteToDisk(name);
            }
        }

        public async Task RemoveFromListAtAsync(string name, int at)
        {
            using (await _syncRoot.LockAsync())
            {
                var list = _playlists[name];
                list.Items.RemoveAt(at);

                PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                {
                    Action = PlaylistChangeAction.Content,
                    NewTitle = name,
                    Playlist = list,
                });
                await WriteToDisk(name);
            }
        }

        public async Task RemoveFromListAsync(string name, PlaylistItem item)
        {
            using (await _syncRoot.LockAsync())
            {
                var list = _playlists[name];
                var rm = list.Items.Where(s => s.Equals(item)).FirstOrDefault();
                if (rm != null)
                    list.Items.Remove(rm);

                PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                {
                    Action = PlaylistChangeAction.Content,
                    NewTitle = name,
                    Playlist = list,
                });
                await WriteToDisk(name);
            }
        }

        public Playlist GetPlaylist(string name) => _playlists[name];

        public Playlist[] GetAllPlaylists() => _playlists.Values.ToArray();

        public async Task RenameAsync(string oldName, string newName, bool _override = false)
        {
            using (await _syncRoot.LockAsync())
            {
                if (oldName == newName)
                    return;
                if (oldName == _favoriteListName)
                    throw new ArgumentException(CommonSharedStrings.CannotRenameFavoritePrompt);
                else if (_playlists.ContainsKey(newName))
                {
                    if (!_override)
                    {
                        throw new ArgumentException(string.Format(
                            CommonSharedStrings.PlaylistAlreadyExistPrompt, newName));
                    }
                }

                var list = _playlists[oldName];
                var path = _nameToPathMap[oldName];
                list.Title = newName;
                await WriteToDisk(oldName);
                _playlists.Remove(oldName);
                _playlists.Add(newName, list);
                _nameToPathMap.Remove(oldName);
                _nameToPathMap.Add(newName, path);

                PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                {
                    Action = PlaylistChangeAction.Rename,
                    OldTitle = oldName,
                    NewTitle = newName,
                    Playlist = _playlists[newName],
                });
            }
        }

        public async Task DuplicateListAsync(string oldName, string newName, bool _override = false)
        {
            using (await _syncRoot.LockAsync())
            {
                if (oldName == newName)
                    return;
                else if (_playlists.ContainsKey(newName))
                {
                    if (!_override)
                    {
                        throw new ArgumentException(string.Format(
                            CommonSharedStrings.PlaylistAlreadyExistPrompt, newName));
                    }
                }
                var newList = _playlists[oldName].Duplicate(newName);
                await AddOrUpdatePlaylist(newList);
            }
        }

        public async Task RemoveListAsync(string name)
        {
            using (await _syncRoot.LockAsync())
            {
                if (name == _favoriteListName)
                {
                    FavoriteList.Items.Clear();
                    await AddOrUpdatePlaylist(FavoriteList);
                    PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                    {
                        Action = PlaylistChangeAction.Content,
                        NewTitle = name,
                        Playlist = FavoriteList,
                    });
                    FavoriteChanged?.Invoke(this, new FavoriteChangedEventArgs
                    {
                        Change = FavoriteChangeType.Unknown
                    });
                }
                else
                {
                    PlaylistChanged?.Invoke(this, new PlaylistChangedEventArgs
                    {
                        Action = PlaylistChangeAction.Remove,
                        OldTitle = name,
                        Playlist = _playlists[name],
                    });
                    await Task.Run(() =>
                    {
                        File.Delete(_nameToPathMap[name]);
                        _nameToPathMap.Remove(name);
                        _playlists.Remove(name);
                    });
                }
            }
        }

        public async Task AddToFavoriteAsync(PlaylistItem item)
        {
            await AddToListAsync(_favoriteListName, item);
        }

        public async Task RemoveFromFavoriteAsync(PlaylistItem item)
        {
            await RemoveFromListAsync(_favoriteListName, item);
        }

        public bool IsInFavorite(string path, ManagedAudioIndexCue cue)
        {
            return FavoriteList.Items.Any(s =>
            {
                if (s.Path != path)
                    return false;
                if ((s.Cue == null) != (cue == null))
                    return false;
                if (s.Cue != null && (
                    s.Cue.StartTime != cue.StartTime ||
                    s.Cue.Duration != cue.Duration))
                    return false;
                return true;
            });
        }

        public async Task SaveNowPlayingList(string name)
        {
            using (await _syncRoot.LockAsync())
            {
                var items = from item
                        in PlaybackControl.Instance.Items
                            select PlaylistItem.FromMediaFile(item.File);
                var list = new Playlist { Title = name };
                foreach (var item in items)
                {
                    list.Items.Add(item);
                }
                await AddOrUpdatePlaylist(list);
            }
        }
    }
}
