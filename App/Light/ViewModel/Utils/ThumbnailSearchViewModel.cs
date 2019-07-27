using GalaSoft.MvvmLight;
using Light.Common;
using Light.Core;
using Light.Managed.Online;
using Light.Managed.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Light.ViewModel.Utils
{
    public enum ThumbnailType
    {
        Album,
        Artist
    }

    public class ThumbnailInfo
    {
        public Uri ThumbnailAddress { get; set; }
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
    }

    public class ThumbnailSearchViewModel : ViewModelBase
    {
        private ThumbnailType _type;
        private bool _isBusy;
        private int _selectedIndex = -1;

        public ObservableCollection<IEntityInfo> SearchCandidates { get; } = new ObservableCollection<IEntityInfo>();

        public bool AlbumTextBoxVisible => _type == ThumbnailType.Album;

        public bool PrimaryButtonEnabled => !(_isBusy || _selectedIndex == -1);

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy == value)
                    return;
                _isBusy = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PrimaryButtonEnabled));
            }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex == value)
                    return;
                _selectedIndex = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PrimaryButtonEnabled));
            }
        }

        private string _artist;
        public string Artist
        {
            get { return _artist; }
            set
            {
                if (_artist == value)
                    return;
                _artist = value;
                RaisePropertyChanged();
            }
        }

        private string _album;
        public string Album
        {
            get { return _album; }
            set
            {
                if (_album == value)
                    return;
                _album = value;
                RaisePropertyChanged();
            }
        }

        private string _resultText;
        private string _originalArtist;
        private string _originalAlbum;

        public string ResultText
        {
            get { return _resultText; }
            set
            {
                if (_resultText == value)
                    return;
                _resultText = value;
                RaisePropertyChanged();
            }
        }

        public Task ImportAsync(byte[] content)
        {
            if (_type == ThumbnailType.Album)
            {
                return ThumbnailManager.AddAsync(_originalArtist, _originalAlbum, content, true);
            }
            else
            {
                return ThumbnailManager.AddAsync(_originalArtist, content, true);
            }
        }

        static string FileNameEscape(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c, '-'));
        }

        public ThumbnailSearchViewModel(string artistName)
        {
            _type = ThumbnailType.Artist;
            _artist = artistName;
            _originalArtist = artistName;
        }

        public ThumbnailSearchViewModel(string artistName, string albumName)
        {
            _type = ThumbnailType.Album;
            _artist = artistName;
            _album = albumName;
            _originalArtist = artistName;
            _originalAlbum = albumName;
        }

        public async Task<bool> DownloadSelected()
        {
            var selected = SearchCandidates[SelectedIndex].Thumbnail;
            if (selected == null)
            {
                ResultText = CommonSharedStrings.NoValidThumbnailPrompt;
                return false;
            }
            IsBusy = true;
            try
            {
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(selected))
                    {
                        response.EnsureSuccessStatusCode();
                        response.Content.Headers.TryGetValues("Content-Length", out var ctlength);
                        var lengthValue = ctlength.FirstOrDefault();

                        if (lengthValue == null ||
                            !ulong.TryParse(lengthValue, out ulong length))
                        {
                            length = 0;
                        }

                        if (length <= 10 * 1024 * 1024)
                        {
                            var content = await response.Content.ReadAsByteArrayAsync();

                            if (content.Length >= (int)length)
                            {
                                if (_type == ThumbnailType.Album)
                                {
                                    await ThumbnailManager.AddAsync(_originalArtist, _originalAlbum, content, true);
                                }
                                else
                                {
                                    await ThumbnailManager.AddAsync(_originalArtist, content, true);
                                }
                                return true;
                            }
                            else
                            {
                                ResultText = CommonSharedStrings.DownloadFailedPrompt;
                                return false;
                            }
                        }
                        else
                        {
                            ResultText = CommonSharedStrings.FileSizeLimitPrompt;
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ResultText = string.Format(CommonSharedStrings.SearchError, ex.Message);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void LoadData(IEntityInfo[] result)
        {
            try
            {
                SelectedIndex = -1;
                SearchCandidates.Clear();
                if (result == null)
                {
                    return;
                }
                foreach (var candidate in result)
                {
                    SearchCandidates.Add(candidate);
                }
                switch (result.Length)
                {
                    case 0:
                        ResultText = CommonSharedStrings.NoResultText;
                        break;
                    case 1:
                        ResultText = CommonSharedStrings.OneResultText;
                        break;
                    default:
                        ResultText = string.Format(CommonSharedStrings.MultipleResultsText, result.Length);
                        break;
                }
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        public async Task SearchAsync()
        {
            try
            {
                if (_isBusy)
                    return;
                IsBusy = true;

                if (_type == ThumbnailType.Album)
                {
                    LoadData(
                        await AggreatedOnlineMetadata.GetAlbumsAsync(Album, Artist));
                }
                else
                {
                    LoadData(
                        await AggreatedOnlineMetadata.GetArtistsAsync(Artist));
                }
            }
            catch (Exception ex)
            {
                ResultText = string.Format(CommonSharedStrings.SearchError, ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ClearAsync()
        {
            if (_type == ThumbnailType.Album)
            {
                await ThumbnailManager.RemoveAsync(_originalArtist, _originalAlbum, true);
            }
            else
            {
                await ThumbnailManager.RemoveAsync(_originalArtist, true);
            }
        }
    }
}
