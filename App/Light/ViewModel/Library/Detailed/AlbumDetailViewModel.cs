using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Managed.Database.Entities;
using Light.Model;
using Light.Shell;
using Light.Utilities;
using Windows.UI.Xaml;
using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;

namespace Light.ViewModel.Library.Detailed
{
    /// <summary>
    /// View model for detailed page view.
    /// </summary>
    public class AlbumDetailViewModel : ViewModelBase
    {
        #region Private Definitions for Albums
        private readonly int _itemId;
        private string _title;
        private string _genre;
        private string _year;
        private string _intro;
        private string _artist;
        private DbAlbum _backendAlbum;
        private ThumbnailTag _coverImageTag;
        #endregion

        #region Album Detail Fields
        /// <summary>
        /// Album title.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaisePropertyChanged();
            }
        }


        private ObservableCollection<DbMediaFile> _viewItems = new ObservableCollection<DbMediaFile>();
        public ObservableCollection<DbMediaFile> ViewItems
        {
            get { return _viewItems; }
            set
            {
                _viewItems = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Album genre.
        /// </summary>
        public string Genre
        {
            get { return _genre; }
            set
            {
                _genre = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Album release year.
        /// </summary>
        public string Year
        {
            get { return _year; }
            set
            {
                _year = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Album intro.
        /// </summary>
        public string Intro
        {
            get { return _intro; }
            set
            {
                _intro = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Album artist.
        /// </summary>
        public string Artist
        {
            get { return _artist; }
            set
            {
                _artist = value;
                RaisePropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Cover image tag.
        /// </summary>
        public ThumbnailTag CoverImageTag
        {
            get { return _coverImageTag; }
            set
            {
                _coverImageTag = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        /// <summary>
        /// DbAlbum entity id in database.
        /// </summary>
        public int EntityId => _itemId;
        
        /// <summary>
        /// Standard class constructor.
        /// </summary>
        /// <param name="entityId">DbAlbum ID in database.</param>
        public AlbumDetailViewModel(int entityId)
        {
            _itemId = entityId;

            IsLoading = true;

            RegisterMessages();
        }

        /// <summary>
        /// Cleanup method.
        /// </summary>
        public override void Cleanup()
        {
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.ControlPageShareClickedEventMessageToken, OnControlPageShareClickedReceived);

            _backendAlbum = null;
            ViewItems = null;

            base.Cleanup();
        }

        public async Task LoadTitleAsync()
        {
            _backendAlbum = await EntityRetrievalExtensions.GetAlbumByIdAsync(_itemId);
            if (_backendAlbum != null)
            {
                CoverImageTag = new ThumbnailTag
                {
                    Fallback = "Album,AlbumPlaceholder",
                    ArtistName = _backendAlbum.Artist,
                    AlbumName = _backendAlbum.Title,
                    ThumbnailPath = _backendAlbum.FirstFileInAlbum,
                };
                
                // Get album detailed information
                Title = _backendAlbum.Title;
                Artist = _backendAlbum.Artist;
                Year = DateTimeHelper.GetItemDateYearString(_backendAlbum.Date);
                Intro = _backendAlbum.Intro;
                Genre = _backendAlbum.Genre;
                DesktopTitleViewConfiguration.SetTitleBarText(Title);
            }
        }

        public async Task LoadContentAsync()
        {
            if (_backendAlbum!= null)
            {
                var tempOrderedItem = new List<Tuple<int, int, DbMediaFile>>();
                var dbFileAsyncEnumerator = _backendAlbum.MediaFiles.ToAsyncEnumerable().GetEnumerator();
                if (dbFileAsyncEnumerator != null)
                {
                    while (await dbFileAsyncEnumerator.MoveNext())
                    {
                        var item = dbFileAsyncEnumerator.Current;
                        int discNum = 0, trackNum = 0;
                        int.TryParse(item.DiscNumber, out discNum);
                        int.TryParse(item.TrackNumber, out trackNum);
                        tempOrderedItem.Add(new Tuple<int, int, DbMediaFile>(discNum, trackNum, item));
                    }
                }

                // Disk and Track
                var sortedItem = tempOrderedItem.OrderBy(i => i.Item1)
                    .ThenBy(i => i.Item2);

                // Add subitems to the ListView data entity set
                foreach (var s in sortedItem)
                {
                    if (s == null) continue;
                    ViewItems.Add(s.Item3);
                }
            }

            IsLoading = false;
        }

        /// <summary>
        /// Load data from database.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task LoadDataAsync()
        {
            var elem = await EntityRetrievalExtensions.GetAlbumByIdAsync(_itemId);
            if (elem != null)
            {
                _backendAlbum = elem;

                CoverImageTag = new ThumbnailTag
                {
                    Fallback = "Album,AlbumPlaceholder",
                    ArtistName = _backendAlbum.Artist,
                    AlbumName = _backendAlbum.Title,
                    ThumbnailPath = _backendAlbum.FirstFileInAlbum,
                };

                #region Basic Info
                // Get album detailed information
                Title = elem.Title;
                Artist = elem.Artist;
                Year = DateTimeHelper.GetItemDateYearString(elem.Date);
                Intro = elem.Intro;
                Genre = elem.Genre;
                DesktopTitleViewConfiguration.SetTitleBarText(Title);
                #endregion

                #region Items

                var tempOrderedItem = new List<Tuple<int, int, DbMediaFile>>();
                var dbFileAsyncEnumerator = elem.MediaFiles.ToAsyncEnumerable().GetEnumerator();
                if (dbFileAsyncEnumerator != null)
                {
                    while (await dbFileAsyncEnumerator.MoveNext())
                    {
                        var item = dbFileAsyncEnumerator.Current;
                        int discNum = 0, trackNum = 0;
                        int.TryParse(item.DiscNumber, out discNum);
                        int.TryParse(item.TrackNumber, out trackNum);
                        tempOrderedItem.Add(new Tuple<int, int, DbMediaFile>(discNum, trackNum, item));
                    }
                }

                // Disk and Track
                var sortedItem = tempOrderedItem.OrderBy(i => i.Item1)
                    .ThenBy(i => i.Item2);

                // Add subitems to the ListView data entity set
                foreach (var s in sortedItem)
                {
                    if (s == null) continue;
                    ViewItems.Add(s.Item3);
                }
                #endregion
            }

            // Mark complete
            IsLoading = false;
        }

        #region Share
        public void RegisterMessages()
        {
            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.ControlPageShareClickedEventMessageToken, OnControlPageShareClickedReceived);
        }

        /// <summary>
        /// Event handler for share button clicked.
        /// </summary>
        /// <param name="obj"></param>
        private async void OnControlPageShareClickedReceived(MessageBase obj)
        {
            await _backendAlbum.ShareAsync();
        }

        #endregion
    }
}
