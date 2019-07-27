using GalaSoft.MvvmLight;
using Light.Managed.Database;
using Light.Managed.Tools;
using Light.Managed.Online;
using Light.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Light.Utilities;

namespace Light.ViewModel.Library
{
    public class SearchViewModel : ViewModelBase
    {
        public ObservableCollection<SearchResultModel> AlbumResult { get; } = new ObservableCollection<SearchResultModel>();
        public ObservableCollection<SearchResultModel> ArtistResult { get; } = new ObservableCollection<SearchResultModel>();
        public ObservableCollection<CommonViewItemModel> MusicResult { get; } = new ObservableCollection<CommonViewItemModel>();

        public ObservableCollection<SearchResultModel> Suggestions { get; } = new ObservableCollection<SearchResultModel>();
        
        private bool _isBusy = false;
        private bool _noResult = false;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsAvailable));
                }
            }
        }

        public bool IsAvailable => !_isBusy;

        public bool NoResult
        {
            get => _noResult;
            set
            {
                if (_noResult != value)
                {
                    _noResult = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsAlbumAvailable => AlbumResult.Count > 0;

        public bool IsArtistAvailable => ArtistResult.Count > 0;

        public bool IsMusicAvailable => MusicResult.Count > 0;

        public string SearchKeyword { get; set; }

        private void ClearResults()
        {
            AlbumResult.Clear();
            ArtistResult.Clear();
            MusicResult.Clear();
        }

        public async void DoQuery()
        {
            var keyword = SearchKeyword?.ToLower().Trim();
            if (string.IsNullOrEmpty(keyword) || IsBusy)
            {
                return;
            }
            ClearResults();
            NoResult = false;
            RaisePropertyChanged(nameof(IsAlbumAvailable));
            RaisePropertyChanged(nameof(IsArtistAvailable));
            RaisePropertyChanged(nameof(IsMusicAvailable));
            IsBusy = true;
            await Task.Delay(200);

            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                var albumQuery = context.Albums
                    .Include(album => album.MediaFiles)
                    .Where(album => album.Title.Like(keyword) ||
                                    album.Artist.Like(keyword) ||
                                    album.Genre.Like(keyword) ||
                                    album.MediaFiles.Any(
                                        file => file.Title.Like(keyword)));

                var artistQuery = context.Artists
                    .Include(artist => artist.MediaFiles)
                    .Include(artist => artist.Albums)
                    .Where(artist => artist.Name.Like(keyword) ||
                                     artist.MediaFiles.Any(
                                         file => file.Title.Like(keyword)) ||
                                     artist.Albums.Any(
                                         album => album.Title.Like(keyword)));

                var musicQuery = context.MediaFiles
                    .Where(file => file.Title.Like(keyword) ||
                                   file.Artist.Like(keyword) ||
                                   file.AlbumArtist.Like(keyword) ||
                                   file.Album.Like(keyword));

                var albumResults = albumQuery.AsEnumerable().Select(x => new { data = x, sim = (int)(x.Title.ToLower().Similarity(keyword) * 100) }).ToList();
                albumResults.Sort((album1, album2) => album2.sim - album1.sim);
                var artistResults = artistQuery.AsEnumerable().Select(x => new { data = x, sim = (int)(x.Name.ToLower().Similarity(keyword) * 100) }).ToList();
                albumResults.Sort((artist1, artist2) => artist2.sim - artist1.sim);
                var musicResults = musicQuery.AsEnumerable().Select(x => new { data = x, sim = (int)(x.Title.ToLower().Similarity(keyword) * 100) }).ToList();
                musicResults.Sort((music1, music2) => music2.sim - music1.sim);

                foreach (var album in albumResults)
                {
                    AlbumResult.Add(new SearchResultModel(album.data));
                }

                foreach (var artist in artistResults)
                {
                    ArtistResult.Add(new SearchResultModel(artist.data));
                }

                foreach (var music in musicResults)
                {
                    MusicResult.Add(new CommonViewItemModel(music.data));
                }
            }

            NoResult = (!IsAlbumAvailable) && (!IsArtistAvailable) && (!IsMusicAvailable);
            RaisePropertyChanged(nameof(IsAlbumAvailable));
            RaisePropertyChanged(nameof(IsArtistAvailable));
            RaisePropertyChanged(nameof(IsMusicAvailable));
            IsBusy = false;
        }

        public void UpdateSuggestions(string keyword)
        {
            LibrarySearchUtils.UpdateSuggestions(keyword, Suggestions);
        }

        public void ClearSuggestions()
        {
            Suggestions.Clear();
        }
    }
}
