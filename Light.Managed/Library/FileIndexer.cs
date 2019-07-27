using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.BulkAccess;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Microsoft.EntityFrameworkCore;
using GalaSoft.MvvmLight.Messaging;

namespace Light.Managed.Library
{

    using Database;
    using Database.Entities;
    using Database.Native;
    using Light.CueIndex;
    using Settings;
    using Tools;

    //Tuple<IMediaInfo, string>
    //Item1: Media information
    //Item2: File path
    using MediaMetadata = Tuple<IMediaInfo, string, DateTimeOffset>;

    public class FileIndexer
    {
        private readonly MedialibraryDbContext m_dbContext;

        private int m_scannedCount = 0;

        /// <summary>
        /// Class constructor that creates instance of <see cref="FileIndexer"/>.
        /// </summary>
        /// <param name="dbContext"></param>
        public FileIndexer(MedialibraryDbContext dbContext)
        {
            m_dbContext = dbContext;
        }

        /// <summary>
        /// Internal Use Only - File diff result class.
        /// </summary>
        class FileDiffResult
        {
            public FileInformation[] Discovered { get; }

            public DbMediaFile[] Removed { get; }

            public FileDiffResult(FileInformation[] filesToIndex, DbMediaFile[] filesToDelete)
            {
                Discovered = filesToIndex;
                Removed = filesToDelete;
            }
        }

        class MetadataCollection : List<MediaMetadata>
        {
            string _albumArtist;

            public string AlbumArtist
            {
                get
                {
                    return _albumArtist ?? (_albumArtist = GetMostPossibleArtist(this));
                }
            }

            static string GetMostPossibleArtist(IList<MediaMetadata> metadatas)
            {
                var dict = new SortedDictionary<string, int>();
                KeyValuePair<string, int> mostItem;

                foreach (var item in metadatas)
                {
                    var artist = !string.IsNullOrEmpty(item.Item1.AlbumArtist) ?
                        item.Item1.AlbumArtist : item.Item1.Artist;

                    if (!dict.ContainsKey(artist))
                        dict[artist] = 0;

                    dict[artist]++;
                }

                foreach (var item in dict)
                {
                    if (item.Value >= mostItem.Value)
                        mostItem = item;
                }

                return mostItem.Key;
            }

            public MetadataCollection() : base(16) { }
        }

        static bool AutoIgnoreDrmProtectedFiles
        {
            get { return SettingsManager.Instance.GetValue<bool>("AutoIgnoreDrmProtectedFiles"); }
        }

        public static HashSet<string>
            SupportedFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".tta",
                ".tak",
                ".ape",
                ".mp3",
                ".wma",
                ".flac",
                ".wav",
                ".ogg",
                ".m4a"
            };

        /// <summary>
        /// Fires when index changes. Intended for communication between LibraryService and FileIndexer only.
        /// </summary>
        public event EventHandler<IndexChangeArgs> IndexChanged;

        FileDiffResult CalculateDiff(IReadOnlyList<FileInformation> queryFiles)
        {
            // Ignore indexed files.
            var queryDirs = new HashSet<string>();
            var queryFilesSet = new HashSet<string>();
            var queryFilesDict = new Dictionary<string, FileInformation>();
            FileInformation f;
            for (int i = 0; i < queryFiles.Count; i++)
            {
                f = queryFiles[i];
                queryFilesDict[f.Path] = f;
                queryFilesSet.Add(f.Path);
                queryDirs.Add(Path.GetDirectoryName(f.Path));
            }


            var dbFilesSet = new HashSet<string>();
            var dbFilesDict = new Dictionary<string, DbMediaFile>();


            var dbMedias = from dir in queryDirs
                           from m in m_dbContext.MediaFiles
                           where m.Path.StartsWith(dir)
                           select m;

            foreach (var file in dbMedias)
            {
                if (!dbFilesDict.ContainsKey(file.Path))
                    dbFilesDict.Add(file.Path, file);

                dbFilesSet.Add(file.Path);
            }

            dbFilesSet.SymmetricExceptWith(queryFilesSet);
            // Now dbFilesSet contains symmetric delta.
            queryFilesSet.IntersectWith(dbFilesSet);
            // Now queryFilesSet contains discovery.
            dbFilesSet.ExceptWith(queryFilesSet);
            // Now dbFilesSet contains removed files.

            // Files to be indexed
            var discoveredFiles = new List<FileInformation>(queryFilesSet.Count);
            // Files about to be removed
            var removedFiles = new List<DbMediaFile>(dbFilesSet.Count);

            foreach (var item in queryFilesSet)
            {
                discoveredFiles.Add(queryFilesDict[item]);
            }

            foreach (var item in dbFilesSet)
            {
                removedFiles.Add(dbFilesDict[item]);
            }

            return new FileDiffResult(discoveredFiles.ToArray(), removedFiles.ToArray());
        }

        async Task<List<DbArtist>> IndexArtistsAsync(
            IEnumerable<string> artists,
            Dictionary<string, DbArtist> artistsCache)
        {
            var artistsDelta = new List<DbArtist>();
            foreach (var a in artists)
            {
                // Empty Artist: Ignore artist relationship
                if (string.IsNullOrEmpty(a) || artistsCache.ContainsKey(a)) continue;

                var dbA = new DbArtist { Name = a };
                m_dbContext.Artists.Add(dbA);
                artistsCache.Add(a, dbA);
                artistsDelta.Add(dbA);
            }
            await m_dbContext.SaveChangesAsync();

            return artistsDelta;
        }

        async Task<List<DbAlbum>> IndexAlbumsAsync(
            Dictionary<string, MetadataCollection> albums,
            Dictionary<string, DbArtist> artistsCache,
            Dictionary<string, DbAlbum> albumsCache)
        {
            var albumsDelta = new List<DbAlbum>();
            foreach (var a in albums)
            {
                // Empty Album: Ignore album relationship
                if (string.IsNullOrEmpty(a.Key) || albumsCache.ContainsKey(a.Key)) continue;

                var artist = a.Value.AlbumArtist;
                var firstItem = a.Value.FirstOrDefault();

                var e = new DbAlbum
                {
                    Title = a.Key,
                    Artist = a.Value.AlbumArtist,
                    Genre = firstItem.Item1.Genre,
                    Date = firstItem.Item1.Date,
                    RelatedArtistId = artistsCache.ContainsKey(artist) ?
                    artistsCache[artist].Id : (int?)null,
                    FirstFileInAlbum = firstItem.Item2
                };

                m_dbContext.Albums.Add(e);
                albumsCache.Add(a.Key, e);
                albumsDelta.Add(e);
            }

            await m_dbContext.SaveChangesAsync();

            return albumsDelta;
        }

        async Task<List<DbMediaFile>> IndexSongsAsync(
            MediaMetadata[] songItems,
            Dictionary<string, DbArtist> artistsCache,
            Dictionary<string, DbAlbum> albumsCache)
        {
            var songsDelta = new List<DbMediaFile>();

            foreach (var s in songItems)
            {
                var e = DbMediaFile.FromMediaInfo(s.Item1, s.Item3);
                e.Path = s.Item2;
                // Empty AlbumArtist: Use the most frequently artist in album instead
                if (string.IsNullOrEmpty(e.AlbumArtist))
                {
                    if (albumsCache.ContainsKey(e.Album))
                        e.AlbumArtist = albumsCache[e.Album].Artist;
                }

                e.RelatedAlbumId = albumsCache.ContainsKey(e.Album) ?
                    albumsCache[e.Album].Id : (int?)null;

                // Here, the relationship is song -> album's artist. (Same behavior as Groove)
                e.RelatedArtistId = artistsCache.ContainsKey(e.AlbumArtist) ?
                    artistsCache[e.AlbumArtist].Id : (int?)null;

                m_dbContext.MediaFiles.Add(e);
                songsDelta.Add(e);
            }
            await m_dbContext.SaveChangesAsync();

            return songsDelta;
        }

        async Task RecordMetadatasAsync(MediaMetadata[] metadatas,
            Dictionary<string, DbArtist> artistsCache, Dictionary<string, DbAlbum> albumsCache)
        {
            var albums = new Dictionary<string, MetadataCollection>();
            var artists = new HashSet<string>();

            string artist;
            string albumTitle;

            // Aggreate data.
            foreach (var meta in metadatas)
            {
                artist = meta.Item1.Artist;
                if (artists.Add(artist) && !artistsCache.ContainsKey(artist))
                {
                    var artistEntity = (from a in m_dbContext.Artists
                                        where a.Name == artist
                                        select new DbArtist { Name = a.Name, Id = a.Id });
                    if (artistEntity.Any()) artistsCache.Add(artist, artistEntity.Single());
                }

                albumTitle = meta.Item1.Album;
                if (!albums.ContainsKey(albumTitle))
                {
                    albums[albumTitle] = new MetadataCollection();

                    if (!albumsCache.ContainsKey(albumTitle))
                    {
                        var albumEntity = from a in m_dbContext.Albums
                                          where a.Title == albumTitle
                                          select new DbAlbum { Artist = a.Artist, Id = a.Id };
                        if (albumEntity.Any()) albumsCache.Add(albumTitle, albumEntity.Single());
                    }
                }

                albums[albumTitle].Add(meta);
            }

            // Weird metadata workaround:
            // Empty Album: Ignore album relationship
            // Empty AlbumArtist: Use the most frequently artist in album instead
            // Empty Artist: Ignore artist relationship

            var artistsDelta = await IndexArtistsAsync(artists, artistsCache);
            IndexChanged?.Invoke(this, new IndexChangeArgs(IndexChangeType.Add, artistsDelta));

            var albumsDelta = await IndexAlbumsAsync(albums, artistsCache, albumsCache);
            IndexChanged?.Invoke(this, new IndexChangeArgs(IndexChangeType.Add, albumsDelta));

            var songsDelta = await IndexSongsAsync(metadatas, artistsCache, albumsCache);
            IndexChanged?.Invoke(this, new IndexChangeArgs(IndexChangeType.Add, songsDelta));
        }

        async Task ArchiveMetadatasAsync(FileInformation[] inputFiles)
        {
            var artistsCache = new Dictionary<string, DbArtist>();
            var albumsCache = new Dictionary<string, DbAlbum>();

            // TODO: Only initialize ffmpeg from background operation. @RsIncubator
            NativeMethods.InitializeFfmpeg();

            // Index new files together.
            var metadataBag = new ConcurrentBag<MediaMetadata>();

            // await Task.Run(() => Parallel.ForEach(inputFiles, (f) => FillMetadataBagStub(f, metadataBag)));
            Messenger.Default.Send(new GenericMessage<string>(this, m_scannedCount.ToString()), "IndexItemAdded");
            foreach (var f in inputFiles)
            {
                FillMetadataBagStub(f, metadataBag);
                m_scannedCount++;
                if (m_scannedCount % 30 == 0)
                {
                    Messenger.Default.Send(new GenericMessage<string>(this, m_scannedCount.ToString()), "IndexItemAdded");
                }
            }
            Messenger.Default.Send(new GenericMessage<string>(this, m_scannedCount.ToString()), "IndexItemAdded");
            m_scannedCount = 0;
            await RecordMetadatasAsync(metadataBag.ToArray(), artistsCache, albumsCache);
        }

        async Task ArchiveMetadatasAsync(FileInformationFactory fileInfoFactory)
        {
            var artistsCache = new Dictionary<string, DbArtist>();
            var albumsCache = new Dictionary<string, DbAlbum>();

            // Ensure ffmpeg is initialized.
            // TODO: Only initialize ffmpeg from background operation. @RsIncubator
            NativeMethods.InitializeFfmpeg();

            // Index new files together.
            var metadataBag = new ConcurrentBag<MediaMetadata>();
            var fileInfos = await fileInfoFactory.GetFilesAsync();
            Messenger.Default.Send(new GenericMessage<string>(this, m_scannedCount.ToString()), "IndexItemAdded");
            foreach (var f in fileInfos)
            {
                FillMetadataBagStub(f, metadataBag);
                m_scannedCount++;
                if (m_scannedCount % 30 == 0)
                {
                    Messenger.Default.Send(new GenericMessage<string>(this, m_scannedCount.ToString()), "IndexItemAdded");
                }
            }
            Messenger.Default.Send(new GenericMessage<string>(this, m_scannedCount.ToString()), "IndexItemAdded");
            m_scannedCount = 0;
            await RecordMetadatasAsync(metadataBag.ToArray(), artistsCache, albumsCache);
        }

        void FillMetadataBagStub(FileInformation fileInfo, ConcurrentBag<MediaMetadata> bag)
        {
            // Prep metadata and cover.
            IRandomAccessStreamWithContentType stream = null;

            try
            {
                IMediaInfo info;
                // Workaround for mojibake in id3v2 tags: use system code page.
                if (string.Compare(fileInfo.FileType, ".mp3", true) == 0)
                {
                    var fInfo = MusicPropertiesMediaInfo.Create(fileInfo.MusicProperties);
                    IMediaInfo extraInfo;
                    using (stream = fileInfo.OpenReadAsync().AsTask().Result)
                    {
                        var result =
                            NativeMethods.GetMediaInfoFromStream(stream, out extraInfo);
                    }
                    fInfo.TrackNumber = extraInfo.TrackNumber;
                    fInfo.TotalTracks = extraInfo.TotalTracks;
                    fInfo.DiscNumber = extraInfo.DiscNumber;
                    fInfo.TotalDiscs = extraInfo.TotalDiscs;
                    fInfo.Date = extraInfo.Date;
                    fInfo.Genre = extraInfo.Genre;
                    fInfo.AllProperties = extraInfo.AllProperties;
                    info = fInfo;
                }
                else
                {
                    using (stream = fileInfo.OpenReadAsync().AsTask().Result)
                    {
                        var result =
                            NativeMethods.GetMediaInfoFromStream(stream, out info);
                    }
                }
                // Ignore all entities with empty title field.
                if (string.IsNullOrEmpty(info?.Title)) return;
                bag.Add(new MediaMetadata(info, fileInfo.Path, fileInfo.BasicProperties.DateModified));
            }
            catch (Exception ex)
            {
                // TODO: Handle exceptions
                Debug.WriteLine(ex);
            }
        }

        void MarkMediaFileDeletion(DbMediaFile[] files)
        {
            // Perform deletion.
            foreach (var fileEntity in files)
            {
                m_dbContext.Entry(fileEntity).State = EntityState.Deleted;
            }
        }

        public void RemoveOrphanedEntities()
        {
            var artistDeletion = new List<DbArtist>();

            var range = from a in m_dbContext.Artists
                        where !a.MediaFiles.Any()
                        select a;

            // Prevent the deletion of StubArtist/StubAlbum.
            foreach (var artist in range)
            {
                // if (artist.MediaFiles == null || artist.MediaFiles.Count < 1)
                {
                    m_dbContext.Entry(artist).State = EntityState.Deleted;
                    artistDeletion.Add(artist);

                    // Performing deletion will also delete empty albums. Notify UI.
                    IndexChanged?.Invoke(this, new IndexChangeArgs(IndexChangeType.Delete, artist.Albums));
                }
            }

            // Save database
            m_dbContext.SaveChangesAsync();

            // Notify deletion
            IndexChanged?.Invoke(this, new IndexChangeArgs(IndexChangeType.Delete, artistDeletion));
        }

        public async Task TriggerScanAsync(IReadOnlyList<FileInformation> queryFiles)
        {
            if (queryFiles.Count < 1) return;

            // Calculate diff.
            var diff = CalculateDiff(queryFiles);

            await ArchiveMetadatasAsync(diff.Discovered);

            // After deleting files, check all artist and albums.
            MarkMediaFileDeletion(diff.Removed);

            // Notify deletion
            IndexChanged?.Invoke(this, new IndexChangeArgs(IndexChangeType.Delete, diff.Removed));

            // Save database
            await m_dbContext.SaveChangesAsync();
        }

        public async Task InitialScanAsync(FileInformationFactory fileInfoFactory)
        {
            await ArchiveMetadatasAsync(fileInfoFactory);

            // Save database
            await m_dbContext.SaveChangesAsync();
        }

        private int ParseWithFallback(string s)
        {
            if (int.TryParse(s, out int result))
            {
                return result;
            }
            return 0;
        }

        private static QueryOptions BuildQueryOptions()
        {
            var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery,
                SupportedFormats)
            {
                FolderDepth = FolderDepth.Deep,
                IndexerOption = IndexerOption.UseIndexerWhenAvailable,
            };

            if (AutoIgnoreDrmProtectedFiles)
                queryOptions.ApplicationSearchFilter =
                    "(protected:no OR protected:[])";

            return queryOptions;
        }

        private async Task<(string, IMediaInfo, DateTimeOffset)> ReadMediaInfoAsync(
            IStorageFile file, bool ignoreDrm)
        {
            using (var stream = await file.OpenReadAsync())
            {
                IMediaInfo info = null;
                int result = -1;
                // Workaround for mojibake in id3v2 tags: use system code page.
                if (string.Compare(file.FileType, ".mp3", true) == 0)
                {
                    if (file is StorageFile)
                    {
                        var props = await ((StorageFile)file).Properties.GetMusicPropertiesAsync();
                        var fInfo = MusicPropertiesMediaInfo.Create(props);
                        result = NativeMethods.GetMediaInfoFromStream(stream, out IMediaInfo extraInfo);
                        fInfo.TrackNumber = extraInfo.TrackNumber;
                        fInfo.TotalTracks = extraInfo.TotalTracks;
                        fInfo.DiscNumber = extraInfo.DiscNumber;
                        fInfo.TotalDiscs = extraInfo.TotalDiscs;
                        fInfo.Date = extraInfo.Date;
                        fInfo.Genre = extraInfo.Genre;
                        fInfo.AllProperties = extraInfo.AllProperties;
                        info = fInfo;
                    }
                    else
                    {
                        result =
                            NativeMethods.GetMediaInfoFromStream(stream, out info);
                    }
                }
                else
                {
                    result = NativeMethods.GetMediaInfoFromStream(
                        stream,
                        out info);
                }
                if (result == 0 && info != null)
                {
                    if (ignoreDrm && !string.IsNullOrWhiteSpace(info.AllProperties["encryption"]))
                    {
                        // Throw DRM exception for upstream reference
                        throw new DrmProtectedException();
                    }

                    var prop = await file.GetBasicPropertiesAsync();
                    return (file.Path, info, prop.DateModified);
                }
                return default((string, IMediaInfo, DateTimeOffset));
            }
        }

        private async void UpdateDb(
            BlockingCollection<(string, IMediaInfo, DateTimeOffset, ManagedAudioIndexCue)> infoCollection,
            BlockingCollection<DbMediaFile> removeCollection,
            ConcurrentBag<Tuple<string, Exception>> exceptions,
            IThumbnailOperations thumbnail,
            TaskCompletionSource<int> taskCompletionSource,
            bool incremental = false)
        {
            Dictionary<string, DbArtist> artistsCache;
            Dictionary<string, DbAlbum> albumsCache;
            if (incremental)
            {
                artistsCache = await m_dbContext.Artists.ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);
                albumsCache = await m_dbContext.Albums.ToDictionaryAsync(x => x.Title, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                artistsCache = new Dictionary<string, DbArtist>(StringComparer.OrdinalIgnoreCase);
                albumsCache = new Dictionary<string, DbAlbum>(StringComparer.OrdinalIgnoreCase);
            }

            var send = new List<DbMediaFile>(30);
            foreach (var (path, info, date, cue) in infoCollection.GetConsumingEnumerable())
            {
                try
                {
                    var dbFile = DbMediaFile.FromMediaInfo(info, date);
                    if (string.IsNullOrWhiteSpace(info.Title))
                    {
                        dbFile.Title = Path.GetFileName(path);
                    }
                    dbFile.Path = path;
                    if (cue != null)
                    {
                        dbFile.StartTime = (int)cue.StartTime.TotalMilliseconds;
                    }

                    bool emptyAlbumArtist;
                    //Check AlbumArtist
                    if (emptyAlbumArtist = string.IsNullOrWhiteSpace(info.AlbumArtist))
                    {
                        dbFile.AlbumArtist = info.Artist;
                    }
                    var emptyArtist = string.IsNullOrWhiteSpace(dbFile.AlbumArtist);
                    var emptyAlbum = string.IsNullOrWhiteSpace(dbFile.Album);

                    //Check and create artist
                    if (!emptyArtist)
                    {
                        if (!artistsCache.ContainsKey(dbFile.AlbumArtist))
                        {
                            var dbArtist = new DbArtist
                            {
                                Name = dbFile.AlbumArtist,
                                FileCount = 1,
                                AlbumCount = 0,
                                DatabaseItemAddedDate = DateTimeOffset.Now
                            };
                            await m_dbContext.Artists.AddAsync(dbArtist);
                            artistsCache.Add(dbFile.AlbumArtist, dbArtist);
                            //dbFile.RelatedArtist = dbArtist;
                            dbFile.RelatedArtistId = dbArtist.Id;
                        }
                        else
                        {
                            var artist = artistsCache[dbFile.AlbumArtist];
                            artist.FileCount++;
                            //dbFile.RelatedArtist = artist;
                            dbFile.RelatedArtistId = artist.Id;
                        }
                    }
                    else
                    {
                        dbFile.RelatedArtistId = null;
                    }

                    //Check and create album
                    if (!emptyAlbum)
                    {
                        if (!albumsCache.ContainsKey(dbFile.Album))
                        {
                            var dbAlbum = new DbAlbum
                            {
                                Title = dbFile.Album,
                                Artist = dbFile.AlbumArtist,
                                Date = dbFile.Date,
                                Genre = dbFile.Genre,
                                RelatedArtistId = emptyArtist ? (int?)null : artistsCache[dbFile.AlbumArtist].Id,
                                FirstFileInAlbum = path,
                                FirstFileTrackNumber = ParseWithFallback(dbFile.TrackNumber),
                                FirstFileDiscNumber = ParseWithFallback(dbFile.DiscNumber),
                                FileCount = 1,
                                DatabaseItemAddedDate = DateTimeOffset.Now
                            };
                            if (thumbnail != null)
                            {
                                await thumbnail.FetchAlbumAsync(dbAlbum.Artist, dbAlbum.Title, path);
                            }
                            Messenger.Default.Send(new GenericMessage<DbAlbum>(this, dbAlbum), "NewAlbumAdded");
                            await m_dbContext.Albums.AddAsync(dbAlbum);
                            //dbFile.RelatedAlbum = dbAlbum;
                            dbFile.RelatedAlbumId = dbAlbum.Id;
                            var artistDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            if (!emptyArtist)
                            {
                                artistDict.Add(dbFile.AlbumArtist, 1);
                            }
                            albumsCache.Add(
                                dbFile.Album,
                                dbAlbum);
                        }
                        else
                        {
                            var album = albumsCache[dbFile.Album];
                            album.FileCount++;
                            dbFile.RelatedAlbumId = album.Id;
                            if (emptyAlbumArtist)
                            {
                                dbFile.AlbumArtist = album.Artist;
                            }

                            int discNum = ParseWithFallback(dbFile.DiscNumber);
                            int trackNum = ParseWithFallback(dbFile.TrackNumber);
                            if (discNum < album.FirstFileDiscNumber ||
                               (discNum == album.FirstFileDiscNumber &&
                                trackNum < album.FirstFileTrackNumber))
                            {
                                album.FirstFileTrackNumber = trackNum;
                                album.FirstFileDiscNumber = discNum;
                                album.FirstFileInAlbum = path;
                                album.Genre = dbFile.Genre;
                                album.Date = dbFile.Date;
                            }
                        }
                    }
                    else
                    {
                        dbFile.RelatedAlbumId = null;
                    }
                    send.Add(dbFile);
                    await m_dbContext.MediaFiles.AddAsync(dbFile);
                    m_scannedCount++;
                    if (m_scannedCount % 30 == 0)
                    {
                        Messenger.Default.Send(new GenericMessage<DbMediaFile[]>(this, send.ToArray()), "NewItemAdded");
                        send.Clear();
                        Messenger.Default.Send(
                            new GenericMessage<string>(
                                this,
                                m_scannedCount.ToString()),
                            "IndexItemAdded");
                        await m_dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(new Tuple<string, Exception>(path, ex));
                }
            }

            try
            {
                if (incremental)
                {
                    foreach (var artist in artistsCache.Values)
                    {
                        artist.AlbumCount = 0;
                    }
                }

                foreach (var album in albumsCache.Values)
                {
                    if (artistsCache.TryGetValue(album.Artist, out DbArtist artist))
                    {
                        artist.AlbumCount++;
                    }
                }

                Messenger.Default.Send(new GenericMessage<string>(this, m_scannedCount.ToString()), "IndexItemAdded");
                await m_dbContext.SaveChangesAsync();

                if (incremental)
                {
                    foreach (var remove in removeCollection.GetConsumingEnumerable())
                    {
                        m_dbContext.PlaybackHistory.RemoveRange(
                            m_dbContext.PlaybackHistory.Where(
                                history => history.RelatedMediaFileId == remove.Id));

                        remove.RelatedAlbumId = null;
                        var removeAlbum = remove.RelatedAlbum;
                        if (removeAlbum != null)
                        {
                            removeAlbum.FileCount--;
                            removeAlbum.MediaFiles.Remove(remove);

                            removeAlbum.RelatedArtistId = null;
                            if (removeAlbum.FileCount == 0)
                            {
                                albumsCache.Remove(removeAlbum.Title);
                                m_dbContext.Albums.Remove(removeAlbum);

                                var artist = removeAlbum.RelatedArtist;
                                if (artist != null)
                                {
                                    artist.AlbumCount--;
                                    artist.Albums.Remove(removeAlbum);

                                    if (artist.AlbumCount == 0 &&
                                        artist.FileCount == 0)
                                    {
                                        artistsCache.Remove(removeAlbum.Artist);
                                        m_dbContext.Artists.Remove(artist);
                                    }
                                }
                            }
                        }

                        remove.RelatedArtistId = null;
                        var removeArtist = remove.RelatedArtist;
                        if (removeArtist != null)
                        {
                            removeArtist.FileCount--;
                            removeArtist.MediaFiles.Remove(remove);

                            if (removeArtist.AlbumCount == 0 &&
                                removeArtist.FileCount == 0)
                            {
                                artistsCache.Remove(removeArtist.Name);
                                m_dbContext.Artists.Remove(removeArtist);
                            }
                        }

                        m_dbContext.MediaFiles.Remove(remove);
                    }
                }
            }
            catch { }

            var scanned = m_scannedCount;
            m_scannedCount = 0;
            await m_dbContext.SaveChangesAsync();
            taskCompletionSource.TrySetResult(scanned);
        }

        class FileTrackInfo
        {
            public string FilePath { get; }
            public DateTimeOffset LastModifiedTime { get; }
            public List<string> TrackIdentifiers { get; } = new List<string>();
            public FileTrackInfo(string filePath, DateTimeOffset lastModifiedTime)
            {
                FilePath = filePath;
                LastModifiedTime = lastModifiedTime;
            }
        }

        public async Task<Tuple<int, List<Tuple<string, Exception>>>> ScanAsync(
            IEnumerable<StorageFolder> folders,
            IThumbnailOperations thumbnail)
        {
            bool ignoreDrm = AutoIgnoreDrmProtectedFiles;
            var infoCollection = new BlockingCollection<(string, IMediaInfo, DateTimeOffset, ManagedAudioIndexCue)>(1000);
            var exceptions = new ConcurrentBag<Tuple<string, Exception>>();

            // Due to the usage of asynchronous operations, awaiting the database thread action to complete
            // is not a good idea. (https://docs.microsoft.com/en-us/windows/uwp/threading-async/best-practices-for-using-the-thread-pool)
            // We use a custom TaskCompletionSource to represent database worker thread's status.
            IAsyncAction dbTask;
            TaskCompletionSource<int> dbCompletionSource = new TaskCompletionSource<int>();

            NativeMethods.InitializeFfmpeg();
            var metadata = new List<MediaMetadata>();
            Messenger.Default.Send(new GenericMessage<string>(this, m_scannedCount.ToString()), "IndexItemAdded");

            var excluded = PathExclusion.GetExcludedPath();

            var removeCollection = new BlockingCollection<DbMediaFile>();
            var trackIdentifierDict = new Dictionary<string, DbMediaFile>(StringComparer.OrdinalIgnoreCase);// await m_dbContext.MediaFiles.ToDictionaryAsync(x => x.ToString());
            var fileInfoDict = new Dictionary<string, FileTrackInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in m_dbContext.MediaFiles)
            {
                var id = file.ToString();
                trackIdentifierDict.Add(id, file);
                if (!fileInfoDict.TryGetValue(file.Path, out var fileItem))
                {
                    fileInfoDict.Add(file.Path, fileItem = new FileTrackInfo(file.Path, file.FileLastModifiedDate));
                }
                fileItem.TrackIdentifiers.Add(id);
            }
            dbTask = ThreadPool.RunAsync(handler => UpdateDb(infoCollection, removeCollection, exceptions, thumbnail, dbCompletionSource, true));

            var s = new Stack<StorageFolder>(folders);

            //iterate over all files and add all metadata to the list.
            StorageFolder current = null;
            while (s.Count != 0)
            {
                current = s.Pop();
                try
                {
                    var subDirectories = await current.GetFoldersAsync();
                    foreach (var dir in subDirectories)
                    {
                        s.Push(dir);
                    }

                    var fileList = (await current.GetFilesAsync()).ToList();

                    var cueFiles = fileList.Where(file => file.FileType.ToLower() == ".cue").ToList();

                    foreach (var cue in cueFiles)
                    {
                        try
                        {
                            var idx = await CueFile.CreateFromFileAsync(cue, false);
                            var props = await cue.GetBasicPropertiesAsync();

                            if (string.IsNullOrWhiteSpace(idx.FileName) ||
                                idx.Indices.Count == 0)
                            {
                                throw new Exception($"{LocalizedStrings.CueInvalid}: {cue.Name}");
                            }

                            var audioTrack = (from f
                                              in fileList
                                              where string.Compare(idx.FileName, f.Name, true) == 0
                                              select f).FirstOrDefault();

                            if (audioTrack == null)
                            {
                                throw new Exception($"{LocalizedStrings.AudioFileAccessFailure}: {idx.FileName}");
                            }

                            var (_, audioTrackInfo, _) = await ReadMediaInfoAsync(audioTrack, true);

                            if (audioTrackInfo == null)
                            {
                                throw new Exception($"{LocalizedStrings.AudioFileMetadataFailure}: {idx.FileName}");
                            }

                            var totalDuration = audioTrackInfo.Duration;

                            foreach (var track in idx.Indices)
                            {
                                if (track.StartTime + track.Duration > totalDuration)
                                {
                                    // Invalid track
                                    continue;
                                }

                                if (track.Duration == TimeSpan.Zero)
                                {
                                    (track.TrackInfo as CueMediaInfo).Duration
                                        = track.Duration
                                        = totalDuration - track.StartTime;
                                }

                                var fileIdentifier = $"{audioTrack.Path}|{track.StartTime.TotalMilliseconds}|{track.Duration.TotalMilliseconds}";
                                if (trackIdentifierDict.TryGetValue(fileIdentifier, out DbMediaFile f))
                                {
                                    if (props.DateModified > f.FileLastModifiedDate)
                                    {
                                        infoCollection.Add((audioTrack.Path, track.TrackInfo, props.DateModified, track));
                                    }
                                    else
                                    {
                                        trackIdentifierDict.Remove(fileIdentifier);
                                    }
                                }
                                else
                                {
                                    infoCollection.Add((audioTrack.Path, track.TrackInfo, props.DateModified, track));
                                }
                            }
                            fileList.Remove(audioTrack);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(new Tuple<string, Exception>(cue.Path, ex));
                        }
                    }


                    var files = fileList
                        .Where(f => !excluded.Any(x => f.Path.ToLower().IsSubPathOf(x)) &&
                                    SupportedFormats.Contains(f.FileType));
                    foreach (var file in files)
                    {
                        try
                        {
                            async Task AddFile()
                            {
                                var (path, info, date) = await ReadMediaInfoAsync(file, ignoreDrm);
                                if (path != null)
                                {
                                    var cue = info.AllProperties["cuesheet"];
                                    CueFile idx;

                                    if (string.IsNullOrWhiteSpace(cue) ||
                                        (idx = CueFile.CreateFromString(cue)).Indices.Count == 0)
                                    {
                                        infoCollection.Add((path, info, date, null));
                                    }
                                    else
                                    {
                                        var totalDuration = info.Duration;
                                        var basicProp = await file.GetBasicPropertiesAsync();
                                        foreach (var track in idx.Indices)
                                        {
                                            if (track.StartTime + track.Duration > totalDuration)
                                            {
                                                // Invalid track
                                                continue;
                                            }

                                            if (track.Duration == TimeSpan.Zero)
                                            {
                                                (track.TrackInfo as CueMediaInfo).Duration
                                                    = track.Duration
                                                    = totalDuration - track.StartTime;
                                            }

                                            infoCollection.Add((path, track.TrackInfo, basicProp.DateModified, track));
                                        }
                                    }
                                }
                                else
                                {
                                    throw new Exception(LocalizedStrings.AudioFileMetadataFailureExceptionMessage);
                                }
                            }

                            // Check modified time per file.
                            if (fileInfoDict.TryGetValue(file.Path, out var fileTrackInfo))
                            {
                                var basicProp = await file.GetBasicPropertiesAsync();
                                if (basicProp.DateModified > fileTrackInfo.LastModifiedTime)
                                {
                                    // Read info from the file again since it may have been modified.
                                    await AddFile();
                                }
                                else
                                {
                                    // The file is not modified.
                                    // Remove all identifiers from trackIdentifierDict
                                    foreach (var item in fileTrackInfo.TrackIdentifiers)
                                    {
                                        trackIdentifierDict.Remove(item);
                                    }
                                }
                            }
                            else
                            {
                                await AddFile();
                            }
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(new Tuple<string, Exception>(file.Path, ex));
                        }
                    }
                }
                catch { }
            }
            infoCollection.CompleteAdding();

            foreach (var file in trackIdentifierDict)
            {
                removeCollection.Add(file.Value);
            }
            removeCollection.CompleteAdding();

            try
            {
                await dbCompletionSource.Task;
            }
            catch
            {
                // Ignore
            }

            SettingsManager.Instance.SetValue(
               PropertyValue.CreateDateTime(DateTime.UtcNow),
               "LastAutoRefereshTime");
            GlobalLibraryCache.Invalidate();

            return new Tuple<int, List<Tuple<string, Exception>>>((dbCompletionSource.Task.Status == TaskStatus.RanToCompletion) ?
                dbCompletionSource.Task.Result :
                -1, exceptions.ToList());
        }
    }
}
