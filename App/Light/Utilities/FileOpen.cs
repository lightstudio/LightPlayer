using Light.Core;
using Light;
using Light.CueIndex;
using Light.Flyout;
using Light.Managed.Database.Entities;
using Light.Managed.Database.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace Light.Utilities
{
    public static class FileOpen
    {
        public static async Task<List<StorageFile>> GetAllFiles(IEnumerable<IStorageItem> items)
        {
            FutureAccessListHelper.Instance.AddTempItem(items);
            var files = new List<StorageFile>();
            var folders = new Queue<StorageFolder>();
            foreach (var item in items)
            {
                if (item is StorageFolder)
                    folders.Enqueue(item as StorageFolder);
                else if (item is StorageFile)
                    files.Add(item as StorageFile);
            }

            while (folders.Count != 0)
            {
                try
                {
                    var item = folders.Dequeue();
                    var subItems = await item.GetItemsAsync();

                    foreach (var i in subItems)
                    {
                        if (i is StorageFolder)
                            folders.Enqueue(i as StorageFolder);
                        else if (i is StorageFile)
                            files.Add(i as StorageFile);
                    }
                }
                catch
                {

                }
            }
            return files;
        }

        private static int ParseWithDefaultFallback(string s)
        {
            int r = default(int);
            int.TryParse(s, out r);
            return r;
        }

        private static async Task<StorageFile> PickMediaFileAsync()
        {
            //wma is not supported.
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".flac");
            picker.FileTypeFilter.Add(".ape");
            picker.FileTypeFilter.Add(".tta");
            picker.FileTypeFilter.Add(".tak");
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".m4a");
            picker.CommitButtonText = "Select";

            StorageFile file = null;
            using (ManualResetEvent ev = new ManualResetEvent(false))
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    file = await picker.PickSingleFileAsync();
                    ev.Set();
                });
                ev.WaitOne();
            }

            return file;
        }

        public static async Task<IEnumerable<MusicPlaybackItem>> HandleFileWithCue(StorageFile file, CueFile cue)
        {
            //Cue files will only have track number
            var items = cue.Indices
                .OrderBy(c => ParseWithDefaultFallback(
                    c.TrackInfo.TrackNumber));
            List<MusicPlaybackItem> files = new List<MusicPlaybackItem>();
            IMediaInfo info = null;
            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                NativeMethods.GetMediaInfoFromStream(
                    stream,
                    out info);
            }
            if (info == null)
                return files;
            var prop = await file.GetBasicPropertiesAsync();
            foreach (ManagedAudioIndexCue item in items)
            {
                if (item.TrackInfo.Duration == TimeSpan.Zero)
                {
                    (item.TrackInfo as CueMediaInfo).Duration
                        = item.Duration
                        = info.Duration - item.StartTime;
                }
                var internalEntity = DbMediaFile.FromMediaInfo(item.TrackInfo, prop.DateModified);
                internalEntity.IsExternal = true;
                internalEntity.StartTime = (int)item.StartTime.TotalMilliseconds;
                internalEntity.Path = file.Path;
                internalEntity.Id = -65535;
                files.Add(MusicPlaybackItem.CreateFromMediaFile(internalEntity));
            }
            return files;
        }

        public static async Task<string> HandleCueFileOpen(StorageFile cueFile, List<StorageFile> files, List<MusicPlaybackItem> add)
        {
            try
            {
                var parent = await cueFile.GetParentAsync();
                var cue = await CueFile.CreateFromFileAsync(cueFile, false);

                // Check user-opened files that are in the same directory
                var audioTrack = (from f
                                  in files
                                  where string.Compare(cue.FileName, f.Name, true) == 0
                                  select f).FirstOrDefault();

                StorageFile file = audioTrack;
                // Cannot find suitable audio track in user opened files.
                if (file == null)
                {
                    // Check parent directory
                    if (parent != null && !string.IsNullOrWhiteSpace(cue.FileName))
                    {
                        file = await parent.TryGetItemAsync(cue.FileName) as StorageFile;
                    }
                    // Try opening the file
                    else if (!string.IsNullOrWhiteSpace(cue.FileName))
                    {
                        var parentPath = cueFile.Path.Substring(0,
                            cueFile.Path.Length - Path.GetFileName(cueFile.Path).Length);
                        var audioTrackPath = Path.Combine(parentPath, cue.FileName);
                        file = await NativeMethods.GetStorageFileFromPathAsync(audioTrackPath) as StorageFile;
                    }
                    else
                    {
                        return null;
                    }
                }
                // Otherwise, remove that.
                else
                {
                    files.Remove(audioTrack);
                }

                if (file != null)
                {
                    add.AddRange(await HandleFileWithCue(file, cue));
                }
                else
                {
                    return Path.Combine(
                        Directory.GetParent(cueFile.Path).FullName,
                        cue.FileName);
                }
            }
            catch { }
            return null;
        }

        public static Tuple<List<StorageFile>, List<StorageFile>, List<StorageFile>> PickAndRemoveCueM3uWplFiles(List<StorageFile> allFiles)
        {
            var cue = new List<StorageFile>();
            var m3u = new List<StorageFile>();
            var wpl = new List<StorageFile>();
            foreach (var file in allFiles)
            {
                if (string.Compare(file.FileType, ".cue", true) == 0)
                {
                    cue.Add(file);
                }
                else if (string.Compare(file.FileType, ".m3u", true) == 0 ||
                    string.Compare(file.FileType, ".m3u8", true) == 0)
                {
                    m3u.Add(file);
                }
                else if (string.Compare(file.FileType, ".wpl", true) == 0)
                {
                    wpl.Add(file);
                }
            }
            foreach (var file in cue)
            {
                allFiles.Remove(file);
            }
            foreach (var file in m3u)
            {
                allFiles.Remove(file);
            }
            foreach (var file in wpl)
            {
                allFiles.Remove(file);
            }
            return new Tuple<List<StorageFile>, List<StorageFile>, List<StorageFile>>(cue, m3u, wpl);
        }

        public static async Task HandleWplAsync(StorageFile file, List<MusicPlaybackItem> addItem)
        {
            try
            {
                if (!ApiInformation.IsApiContractPresent("Windows.Media.Playlists.PlaylistsContract", 1))
                {
                    //TODO: Tell user the platform is not supported.
                    return;
                }
                var playlist = await Windows.Media.Playlists.Playlist.LoadAsync(file);
                foreach (var item in playlist.Files)
                {
                    IMediaInfo info = null;
                    using (var stream = await item.OpenAsync(FileAccessMode.Read))
                    {
                        NativeMethods.GetMediaInfoFromStream(stream, out info);
                    }
                    if (info == null) continue;
                    var prop = await item.GetBasicPropertiesAsync();
                    var internalEntity = DbMediaFile.FromMediaInfo(info, prop.DateModified);
                    internalEntity.IsExternal = true;

                    internalEntity.Path = item.Path;
                    internalEntity.Id = -65535;
                    if (string.IsNullOrWhiteSpace(internalEntity.Title)
                        && !string.IsNullOrWhiteSpace(internalEntity.Path))
                    {
                        internalEntity.Title = Path.GetFileNameWithoutExtension(internalEntity.Path);
                    }
                    addItem.Add(MusicPlaybackItem.CreateFromMediaFile(internalEntity));
                }
            }
            catch { }
        }

        public static IEnumerable<string> Lines(this StreamReader reader)
        {
            while (!reader.EndOfStream)
                yield return reader.ReadLine();
        }

        public static async Task HandleM3uAsync(StorageFile file, List<MusicPlaybackItem> add)
        {
            try
            {
                using (var stream = await file.OpenStreamForReadAsync())
                using (var sr = new StreamReader(stream))
                {
                    var musicItems = M3u.Parse(sr.Lines());
                    foreach (var music in musicItems)
                    {
                        try
                        {
                            var f = await NativeMethods.GetStorageFileFromPathAsync(music);
                            if (f == null)
                                continue;
                            IMediaInfo info = null;
                            using (var s = await f.OpenAsync(FileAccessMode.Read))
                            {
                                NativeMethods.GetMediaInfoFromStream(s, out info);
                            }
                            if (info == null) continue;
                            var prop = await file.GetBasicPropertiesAsync();
                            var internalEntity = DbMediaFile.FromMediaInfo(info, prop.DateModified);
                            internalEntity.IsExternal = true;

                            internalEntity.Path = f.Path;
                            internalEntity.Id = -65535;
                            if (string.IsNullOrWhiteSpace(internalEntity.Title)
                                && !string.IsNullOrWhiteSpace(internalEntity.Path))
                            {
                                internalEntity.Title = Path.GetFileNameWithoutExtension(internalEntity.Path);
                            }
                            add.Add(MusicPlaybackItem.CreateFromMediaFile(internalEntity));
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static async Task AddToPlayback(IEnumerable<MusicPlaybackItem> added, int insertAt)
        {
            var t = new TaskCompletionSource<object>();
            await Windows.ApplicationModel.Core
                .CoreApplication.MainView.CoreWindow
                .Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    try
                    {
                        await PlaybackControl.Instance.AddFile(added, insertAt);
                        PlaybackControl.Instance.Play();
                    }
                    catch { }
                    t.SetResult(null);
                });
            await t.Task;
        }

        public static async Task<List<MusicPlaybackItem>> GetPlaybackItemsFromFilesAsync(IReadOnlyList<IStorageItem> items)
        {
            var files = new List<StorageFile>();
            var added = new List<MusicPlaybackItem>();

            // [| (cue file, full file path of missing file) |]
            var failedCue = new List<Tuple<StorageFile, string>>();
            foreach (var file in await GetAllFiles(items))
            {
                files.Add(file);
            }
            var listFiles = PickAndRemoveCueM3uWplFiles(files);

            foreach (var cueFile in listFiles.Item1)
            {
                try
                {
                    var failedFileName = await HandleCueFileOpen(cueFile, files, added);
                    if (failedFileName != null)
                    {
                        failedCue.Add(new Tuple<StorageFile, string>(cueFile, failedFileName));
                    }
                }
                catch { }
            }

            foreach (var m3uFile in listFiles.Item2)
            {
                await HandleM3uAsync(m3uFile, added);
            }

            foreach (var wplFile in listFiles.Item3)
            {
                await HandleWplAsync(wplFile, added);
            }

            foreach (var file in files)
            {
                try
                {
                    IMediaInfo info = null;
                    using (var stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        NativeMethods.GetMediaInfoFromStream(stream, out info);
                    }
                    if (info == null) continue;
                    var cue = info.AllProperties["cuesheet"];
                    if (!string.IsNullOrWhiteSpace(cue))
                    {
                        var cueItems = await HandleFileWithCue(file,
                            CueFile.CreateFromString(cue));
                        added.AddRange(cueItems);
                        continue;
                    }
                    var prop = await file.GetBasicPropertiesAsync();
                    var internalEntity = DbMediaFile.FromMediaInfo(info, prop.DateModified);

                    internalEntity.IsExternal = true;

                    internalEntity.Path = file.Path;
                    internalEntity.Id = -65535;

                    if (string.IsNullOrWhiteSpace(internalEntity.Title)
                        && !string.IsNullOrWhiteSpace(internalEntity.Path))
                    {
                        internalEntity.Title = Path.GetFileNameWithoutExtension(internalEntity.Path);
                    }
                    added.Add(MusicPlaybackItem.CreateFromMediaFile(internalEntity));
                }
                catch { }
            }
            if (failedCue.Count > 0)
            {
                added.AddRange(
                    await FileOpenFailure.AddFailedFilePath(
                        failedCue));
            }
            return added;
        }

        public static async Task OpenFilesAsync(IReadOnlyList<IStorageItem> items, int insertAt = -1)
        {
            if (insertAt == -1)
            {
                var t = new TaskCompletionSource<object>();
                await Windows.ApplicationModel.Core
                    .CoreApplication.MainView.CoreWindow
                    .Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        PlaybackControl.Instance.Clear();
                        t.SetResult(null);
                    });
                await t.Task;
            }
            var added = await GetPlaybackItemsFromFilesAsync(items);
            await AddToPlayback(added, insertAt);
        }

    }
}
