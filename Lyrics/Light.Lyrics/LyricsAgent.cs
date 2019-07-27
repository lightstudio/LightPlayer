using LightLrcComponent;
using Light.Lyrics.External;
using Light.Lyrics.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Light.Managed.Settings;
using Light.Managed.Online;

namespace Light.Lyrics
{
    public class SimilarityComparer : IComparer<ExternalLrcInfo>
    {
        private string _originalTitle, _originalArtist;

        public SimilarityComparer(string originalTitle, string originalArtist)
        {
            _originalArtist = originalArtist;
            _originalTitle = originalTitle;
        }

        public int Compare(ExternalLrcInfo x, ExternalLrcInfo y)
        {
            double simx = _originalTitle.Similarity(x.Title) * _originalArtist.Similarity(x.Artist);
            double simy = _originalTitle.Similarity(y.Title) * _originalArtist.Similarity(y.Artist);

            if (simx > simy)
                return -1;
            if (Math.Abs(simx - simy) < double.Epsilon)
                return 0;

            return 1;
        }
    }

    public class LyricsAgent
    {
        public enum SaveOption
        {
            /// <summary>
            /// Don not save lrc fifles.
            /// </summary>
            Never = 0,

            /// <summary>
            /// Save lrc files to Local\Lyrics.
            /// </summary>
            ToAppData = 1,

            /// <summary>
            /// Save lrc files to selected folder. 
            /// </summary>
            ToSpecifiedFolder = 2,
        }

        static Task<StorageFolder> LyricsSaveFolderCache;

        static LyricsAgent()
        {
            SettingsManager.Instance.MapChanged += OnSaveOptionChanged;
        }

        static void OnSaveOptionChanged(
            IObservableMap<string, object> sender,
            IMapChangedEventArgs<string> @event)
        {
            if (@event.Key == "SaveLyrics" || @event.Key == "LyricsSaveFolder")
                LyricsSaveFolderCache = null;
        }

        static T GetSettingsValue<T>(string key, T defaultValue)
        {
            if (SettingsManager.Instance.ContainsKey(key))
                return SettingsManager.Instance.GetValue<T>(key);
            return defaultValue;
        }

        static Task<StorageFolder> GetLyricsSaveFolderAsync()
        {
            if (LyricsSaveFolderCache != null) return LyricsSaveFolderCache;

            var saveOption = (SaveOption)GetSettingsValue("SaveLyrics", 1);
            switch (saveOption)
            {
                case SaveOption.Never:
                    return null;

                case SaveOption.ToAppData:
                    return (LyricsSaveFolderCache =
                        ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(
                            "Lyrics", CreationCollisionOption.OpenIfExists).AsTask());

                case SaveOption.ToSpecifiedFolder:
                    var path = GetSettingsValue<string>("LyricsSaveFolder", null);
                    if (path == null) throw new DirectoryNotFoundException("Specified lyrics folder is null");
                    return (LyricsSaveFolderCache = StorageFolder.GetFolderFromPathAsync(path).AsTask());

                default:
                    goto case SaveOption.ToAppData;
            }
        }

        static public string BuildLyricsFilename(string title, string artist)
        {
            var builder = new StringBuilder(32).AppendFormat(
                "{0} - {1}.lrc", (artist ?? "Unknown"), (title ?? "Unknown"));

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                builder.Replace(c, ' ');
            }

            return builder.ToString();
        }

        /// <summary>
        /// Fetch lyrics by <see cref="ExternalLrcInfo"/> from 
        /// previous call of <see cref="FetchLyricsAsync(string, string, IList{ExternalLrcInfo})"/>.
        /// </summary>
        /// <param name="info">Obtained from previous call</param>
        /// <returns>Parsed lyrics</returns>
        public static async Task<ParsedLrc> FetchLyricsAsync(ExternalLrcInfo info, string cacheName)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            var source = info.Source;
            if (string.IsNullOrEmpty(source))
                throw new ArgumentNullException(nameof(info.Source), "Where do you come from?");

            var s = SourceScriptManager.GetScript(source);
            var lrcText = await s.DownloadLrcAsync(info);
            await SaveLyricsInternalAsync(
                await GetLyricsSaveFolderAsync(),
                cacheName, lrcText);

            if (!string.IsNullOrEmpty(lrcText))
            {
                try
                {
                    return LrcParser.Parse(lrcText);
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// Fetch lyrics from either local file cache or Internet.
        /// </summary>
        /// <param name="candidates">
        /// A list collection receiving online lyrics candidates.
        /// </param>
        /// <param name="ignoreCache">Do not read local cache.</param>
        /// <returns>Parsed lyrics</returns>
        public static async Task<ParsedLrc> FetchLyricsAsync(string title, string artist, IList<ExternalLrcInfo> candidates, string cacheName, bool ignoreCache = false)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));

            var lrcFolder = await GetLyricsSaveFolderAsync();
            if (lrcFolder != null)
            {
                var existentFile = (await lrcFolder.TryGetItemAsync(cacheName)) as IStorageFile;
                if (existentFile != null && !ignoreCache)
                    return LrcParser.Parse(await existentFile.OpenStreamForReadAsync(), false);
            }

            // No existent lrc file. Query for it on Internet.
            List<ExternalLrcInfo> sorted = new List<ExternalLrcInfo>();

            foreach (var s in SourceScriptManager.GetAllScripts())
            {
                var infos = (await s.LookupLrcAsync(title, artist));
                sorted.AddRange(infos);
            }
            
            sorted.Sort(new SimilarityComparer(title, artist));
            sorted.ForEach((info) => candidates.Add(info));

            return candidates.Count > 0 ?
                await FetchLyricsAsync(candidates[0], cacheName) : null;
        }

        public static async Task<ParsedLrc> ImportLyricsAsync(string title, string artist, StorageFile lrcFile)
        {
            if (lrcFile == null) throw new ArgumentNullException(nameof(lrcFile));
            var lrcFolder = await GetLyricsSaveFolderAsync();
            if (lrcFolder != null)
            {
                await lrcFile.CopyAsync(lrcFolder, 
                    BuildLyricsFilename(title, artist), 
                    NameCollisionOption.ReplaceExisting);
            }

            return LrcParser.Parse(await lrcFile.OpenStreamForReadAsync(), false);
        }

        // public static async Task SaveLyricsAsync(ExternalLrcInfo info, SaveOption option);

        static async Task SaveLyricsInternalAsync(StorageFolder folder, string filename, string lrcText)
        {
            if (folder == null || string.IsNullOrEmpty(lrcText)) return;

            var lrcFile = (await folder.
                CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting));

            await FileIO.WriteTextAsync(lrcFile, lrcText);
        }

        public static async Task RemoveLyricsAsync(string title, string artist)
        {
            var lrcFolder = await GetLyricsSaveFolderAsync();
            if (lrcFolder != null)
            {
                //Create an empty cache file to prevent the wrong lyrics from downloading again.
                await lrcFolder.CreateFileAsync(BuildLyricsFilename(title, artist), CreationCollisionOption.ReplaceExisting);
            }
        }
    }
}
