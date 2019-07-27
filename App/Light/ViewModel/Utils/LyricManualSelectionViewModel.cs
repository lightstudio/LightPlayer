using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using GalaSoft.MvvmLight;
using Light.Lyrics;
using LightLrcComponent;
using Light.Lyrics.Model;
using Light.Managed.Tools;
using Light.Common;

namespace Light.ViewModel.Utils
{
    class LyricManualSelectionViewModel : ViewModelBase
    {
        public ObservableCollection<ExternalLrcInfo> LyricCandidates { get; }

        public bool PrimaryButtonEnabled
        {
            get
            {
                return !(_isBusy || _selectedIndex == -1);
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value)
                    return;
                _title = value;
                RaisePropertyChanged();
            }
        }

        private int _selectedIndex = -1;
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

        private string _resultText;
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

        public bool _isBusy;
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

        public RelayCommand SearchCommand { get; }

        private string _originalArtist, _originalTitle, _cacheName;

        public LyricManualSelectionViewModel(string title, string artist, IList<ExternalLrcInfo> candidates)
        {
            SearchCommand = new RelayCommand(Search);
            LyricCandidates = new ObservableCollection<ExternalLrcInfo>();
            _cacheName = LyricsAgent.BuildLyricsFilename(title, artist);
            _originalTitle = title;
            _originalArtist = artist;
            Title = title;
            Artist = artist;
            LoadData(candidates);
        }

        private async void Search()
        {
            try
            {
                if (_isBusy)
                    return;
                IsBusy = true;
                var list = new List<ExternalLrcInfo>();
                await LyricsAgent.FetchLyricsAsync(Title, Artist, list, _cacheName, true);
                LoadData(list);
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

        public async Task<ParsedLrc> DownloadAsync()
        {
            var candidate = LyricCandidates[SelectedIndex];
            return await LyricsAgent.FetchLyricsAsync(candidate, LyricsAgent.BuildLyricsFilename(_originalTitle, _originalArtist));
        }

        public void LoadData(IList<ExternalLrcInfo> lrc)
        {
            // Acquire title & artist and search it first.
            if (string.IsNullOrEmpty(Title)) return;
            try
            {
                if (LyricCandidates == null) return;
                SelectedIndex = -1;
                LyricCandidates.Clear();
                if (lrc == null)
                    return;
                foreach (var candidate in lrc)
                    LyricCandidates.Add(candidate);
                switch (lrc.Count)
                {
                    case 0:
                        ResultText = CommonSharedStrings.NoResultText;
                        break;
                    case 1:
                        ResultText = CommonSharedStrings.OneResultText;
                        break;
                    default:
                        ResultText = string.Format(CommonSharedStrings.MultipleResultsText, lrc.Count);
                        break;
                }
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        public async Task<ParsedLrc> ImportAsync(StorageFile file)
        {
            return await LyricsAgent.ImportLyricsAsync(_originalTitle, _originalArtist, file);
        }

        public async Task DeleteAsync()
        {
            await LyricsAgent.RemoveLyricsAsync(_originalTitle, _originalArtist);
        }
    }
}
