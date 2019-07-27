using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Light.Common;
using Light.Managed.Database.Native;
using Light.Managed.Tools;
using Light.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Light.ViewModel.Library
{
    /// <summary>
    /// View model for media file properties.
    /// </summary>
    public class MediaFilePropertiesViewModel : ViewModelBase
    {
        #region Metadata mapping
        /// <summary>
        /// Property mapping for general metadata entries.
        /// </summary>
        private static readonly Dictionary<string, string> PropertyNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "title", CommonSharedStrings.TitleText },
            { "album", CommonSharedStrings.AlbumText },
            { "album_artist", CommonSharedStrings.AlbumArtistText },
            { "artist", CommonSharedStrings.ArtistText },
            { "composer", CommonSharedStrings.Composer },
            { "date", CommonSharedStrings.Date },
            { "track", CommonSharedStrings.Track },
            { "disc", CommonSharedStrings.Disc },
            { "totaldiscs", CommonSharedStrings.TotalDiscs },
            { "totaltracks", CommonSharedStrings.TotalTracks },
            { "genre", CommonSharedStrings.GenreText },
            { "performer", CommonSharedStrings.Performer },
            { "grouping", CommonSharedStrings.Grouping },
            { "comment", CommonSharedStrings.Comment },
            { "copyright", CommonSharedStrings.CopyrightText },
            { "description", CommonSharedStrings.Description },
            { "time", CommonSharedStrings.Time },
            // Empty string for properties that should be ignored.
            { "cuesheet", string.Empty },
            // Some new ID3v2 tags
            { "album-sort", CommonSharedStrings.SortedAlbumText },
            { "artist-sort", CommonSharedStrings.SortedArtistText },
            { "encoded_by", CommonSharedStrings.EncoderText },
            { "publisher", CommonSharedStrings.PublisherText },
            { "title-sort", CommonSharedStrings.SortedTitleText },
            { "tso2", CommonSharedStrings.SortedArtistText },
            { "tsrc", CommonSharedStrings.TsrcText }
        };

        /// <summary>
        /// Unknown property string template.
        /// </summary>
        private static readonly string UnknownPropertyStringTemplate = CommonSharedStrings.GetString("UnknownProperty");
        #endregion

        private readonly StorageFile m_file;
        private BasicProperties m_fileProperties;
        private IMediaInfo m_info;
        private RelayCommand<string> m_copy;

        /// <summary>
        /// All file properties.
        /// </summary>
        public ObservableCollection<MediaPropertyItem> MediaProperties { get; set; }

        /// <summary>
        /// File name.
        /// </summary>
        public string FileName => m_file?.DisplayName;

        /// <summary>
        /// File type.
        /// </summary>
        public string FileExtension => m_file?.FileType;

        /// <summary>
        /// File path.
        /// </summary>
        public string FilePath => m_file?.Path ?? string.Empty;

        /// <summary>
        /// File size, in string.
        /// </summary>
        public string FileSize => m_fileProperties.Size.GetFormattedFileLengthString();

        /// <summary>
        /// Command for copying property value.
        /// </summary>
        public RelayCommand<string> Copy
        {
            get
            {
                return m_copy ?? (m_copy = new RelayCommand<string>(x =>
                {
                    var package = new DataPackage();
                    package.SetText(x);
                    Clipboard.SetContent(package);
                }));
            }
        }

        /// <summary>
        /// Class constructor that creates instance of <see cref="MediaFilePropertiesViewModel"/>.
        /// </summary>
        /// <param name="file">Instance of <see cref="StorageFile"/> that represents file to get metadata properties from.</param>
        public MediaFilePropertiesViewModel(StorageFile file)
        {
            m_file = file;
            MediaProperties = new ObservableCollection<MediaPropertyItem>();
        }

        /// <summary>
        /// Loading properties asynchronously.
        /// </summary>
        /// <returns>Task represents metadata retrieval operation.</returns>
        public async Task LoadDataAsync()
        {
            if (m_fileProperties == null)
            {
                // Get file properties.
                m_fileProperties = await m_file.GetBasicPropertiesAsync();
            }

            // Read file.
            using (var stream = await m_file.OpenReadAsync())
            {
                await Task.Run(() => NativeMethods.GetMediaInfoFromStream(stream, out m_info));

                // Dictionary used to prevent duplicate property pairs
                // One case: TSO2 w/ SORTED-ARTIST
                var metadataDict = new Dictionary<string, string>();

                // Get properties.
                if (MediaProperties.Count < 1 && m_info?.AllProperties != null)
                {
                    foreach (var property in m_info.AllProperties)
                    {
                        var metadataKey = PropertyNameMap.TryGetValue(property.Key.ToLower(), out string value) ?
                            value : string.Format(UnknownPropertyStringTemplate, property.Key);

                        if (!metadataDict.ContainsKey(metadataKey)) metadataDict.Add(metadataKey, property.Value);
                    }

                    var properties = metadataDict.Select(i => new MediaPropertyItem(i.Key, i.Value, Copy));
                    foreach (var property in properties) MediaProperties.Add(property);
                }
            }
        }
    }
}
