using System.Threading.Tasks;
using Windows.UI.Xaml;
using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.DataObjects;
using Light.Model;
using Light.Shell;
#if ENABLE_CRAPPY_SORTING
using Light.Utilities.EntityComparer;
using Light.Utilities.EntityIndexer;
#endif
using Light.Utilities.Grouping;
using System.Threading;
using System.Collections.Generic;
using Light.View.Library;
using Light.Managed.Database;

namespace Light.ViewModel.Library
{
    /// <summary>
    /// Shared Library ViewModel for items.
    /// </summary>
    public class LibraryViewModel : CoreViewBase
    {
        /// <summary>
        /// Share message token.
        /// </summary>
        public const string ShareToken = "LibraryShareMessage";
        /// <summary>
        /// Increment scan index changed message token.
        /// </summary>
        public const string IndexChanged = "IndexChanged";

        #region Private Objects
        /// <summary>
        /// An boolean which indicates whether the source has been grouped.
        /// </summary>
        private readonly bool _isGrouped;
        #endregion
        #region Public Defs
        #region Event behavior 
        #endregion
        #region Loading Progress Ring
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                if (value == _isLoading) return;

                _isLoading = value;

                NotifyChange(nameof(IsLoading));
            }
        }
        private bool _isLoading;
        #endregion
        #region ViewModel properties
        public CommonItemType ViewType
        {
            get { return _viewType; }
            set
            {
                _viewType = value;
                NotifyChange(nameof(ViewType));
            }
        }
        private CommonItemType _viewType;

        public IEnumerable<CommonViewItemModel> Items
        {
            get
            {
                return _items;
            }
            set
            {
                if (_items == value)
                    return;
                _items = value;
                RaisePropertyChanged();
            }
        }
        private IEnumerable<CommonViewItemModel> _items;

        #endregion
        #endregion

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="type">Item type.</param>
        /// <param name="isGrouped">Indicates whether the value has been grouped.</param>
        public LibraryViewModel(CommonItemType type, bool isGrouped = false, string keyWord = "") : base(Window.Current.Dispatcher)
        {
            RegisterEventHandlers();

            _isGrouped = isGrouped;
            _viewType = type;

            // Fallback or default title
            DesktopTitleViewConfiguration.SetTitleBarText(
                CommonSharedStrings.LibraryTitle);
        }

        /// <summary>
        /// Class constructor for new-style navigation params.
        /// </summary>
        /// <param name="args">Navigation event args.</param>
        public LibraryViewModel(GroupedViewNavigationArgs args) : base(Window.Current.Dispatcher)
        {
            RegisterEventHandlers();

            _isGrouped = true;
            _viewType = args.PageType;

            // Fallback or default title
            DesktopTitleViewConfiguration.SetTitleBarText(
                CommonSharedStrings.LibraryTitle);

            // Always set last used comparer.
            if (_viewType == CommonItemType.Song)
            {
                var groupState = new PageGroupingStateManager<CommonGroupedListView>(_viewType);
                var comparer = groupState.GetLastUsedPair();
                GroupedItems = new GroupedSource(comparer.Indexer, comparer.GroupComparer, comparer.ItemComparer);
            }
            else if (_viewType == CommonItemType.Album || _viewType == CommonItemType.Artist)
            {
                var groupState = new PageGroupingStateManager<CommonGroupedGridView>(_viewType);
                var comparer = groupState.GetLastUsedPair();
                GroupedItems = new GroupedSource(comparer.Indexer, comparer.GroupComparer, comparer.ItemComparer);
            }
            else
            {
                GroupedItems = new GroupedSource(args.EntityIndexer, args.GroupComparer, args.ItemComparer);
            }
        }

        /// <summary>
        /// Register event and messages.
        /// </summary>
        private void RegisterEventHandlers()
        {
            // Grouping
            Messenger.Default.Register<GenericMessage<IndexerComparerPair>>(this, CommonSharedStrings.GroupingChangeMessageToken, OnRegroupAndSorting);
        }

        /// <summary>
        /// Message handler for re-grouping and sorting.
        /// </summary>
        /// <param name="obj"></param>
        private void OnRegroupAndSorting(GenericMessage<IndexerComparerPair> obj)
        {
            // Search page does not accept regrouping and sorting, by design.
            if (obj.Content != null && _viewType != CommonItemType.Search)
            {
                // Differental sorting works like a crap currently. Before some optimization, we will use a full grouping and sorting.
#if ENABLE_CRAPPY_SORTING
                if (GroupedItems.Indexer.Identifier == obj.Content.Indexer.Identifier)
                {
                    GroupedItems.SetComparer(obj.Content.Comparer);
                }
                // Otherwise, do all
                else
                {
                    GroupedItems.SetAll(obj.Content.Indexer, obj.Content.Comparer);
                }
#else
                GroupedItems.SetAll(obj.Content.Indexer, obj.Content.GroupComparer, obj.Content.ItemComparer);
#endif
                if (_viewType == CommonItemType.Song)
                {
                    Items = GroupedItems.Sorted;
                }
            }
        }

        /// <summary>
        /// Cleanup existing event and message registrations.
        /// </summary>
        public override void Cleanup()
        {
            Messenger.Default.Unregister<GenericMessage<IndexerComparerPair>>(this, CommonSharedStrings.GroupingChangeMessageToken, OnRegroupAndSorting);
        }

        /// <summary>
        /// Load data from data source.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task LoadDataAsync(CancellationToken cancellationToken)
        {
            IsLoading = true;

            GroupedItems.Clear();

            switch (_viewType)
            {
                case CommonItemType.Album:
                    await LoadAlbumData(cancellationToken);
                    break;
                case CommonItemType.Artist:
                    await LoadArtistData(cancellationToken);
                    break;
                case CommonItemType.Song:
                    Items = await LoadSongData(cancellationToken);
                    break;
                case CommonItemType.Search:
                    break;
            }

            IsLoading = false;
        }

        /// <summary>
        /// Load file data from database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Processed files.</returns>
        public async Task<IEnumerable<CommonViewItemModel>> LoadSongData(CancellationToken cancellationToken)
        {
            if (GlobalLibraryCache.CachedDbMediaFile == null)
            {
                await GlobalLibraryCache.LoadMediaAsync();
            }

            var result = new List<CommonViewItemModel>(GlobalLibraryCache.CachedDbMediaFile.Length);
            foreach (var item in GlobalLibraryCache.CachedDbMediaFile)
            {
                var e = new CommonViewItemModel(item);
                GroupedItems.Add(e);
                result.Add(e);
            }

            return result;
        }

        /// <summary>
        /// Load artist data from database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Processed artists.</returns>
        public async Task LoadArtistData(CancellationToken cancellationToken)
        {
            if (GlobalLibraryCache.CachedDbArtist == null)
            {
                await GlobalLibraryCache.LoadArtistAsync();
                if (cancellationToken.IsCancellationRequested)
                    return;
            }
            foreach (var item in GlobalLibraryCache.CachedDbArtist)
            {
                var e = CommonViewItemModel.CreateFromDbArtistAndCheck(item);
                GroupedItems.Add(e);
            }
        }

        /// <summary>
        /// Load album data from database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Processed albums.</returns>
        private async Task LoadAlbumData(CancellationToken cancellationToken)
        {
            if (GlobalLibraryCache.CachedDbAlbum == null)
            {
                await GlobalLibraryCache.LoadAlbumAsync();
                if (cancellationToken.IsCancellationRequested)
                    return;
            }
            foreach (var item in GlobalLibraryCache.CachedDbAlbum)
            {
                var e = CommonViewItemModel.CreateFromDbAlbumAndCheck(item);
                GroupedItems.Add(e);
            }
        }
    }
}
