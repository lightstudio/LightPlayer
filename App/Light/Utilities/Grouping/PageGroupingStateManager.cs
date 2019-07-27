using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Light.Model;
using Light.Annotations;
using Light.Utilities.EntityComparer;
using Light.Utilities.EntityIndexer;
using Light.Common;

namespace Light.Utilities.Grouping
{
    /// <summary>
    /// Page grouping and sorting state manager.
    /// </summary>
    /// <typeparam name="T">The desired page type.</typeparam>
    public class PageGroupingStateManager<T> : INotifyPropertyChanged, IPageGroupingStateManager
    {
        private const string UsedSortingGroupKey = "UsedSortingGroup";
        static readonly Dictionary<CommonItemType, string> NoGroupTitleString = new Dictionary<CommonItemType, string>
        {
            { CommonItemType.Album, CommonSharedStrings.GetString("AllAlbums") },
            { CommonItemType.Artist, CommonSharedStrings.GetString("AllArtists") },
            { CommonItemType.Song, CommonSharedStrings.GetString("AllMusic") }
        };

        /// <summary>
        /// Read only - Current view's page type.
        /// </summary>
        public Type PageType => typeof (T);

        /// <summary>
        /// Read only - Current view's item type.
        /// </summary>
        public CommonItemType ItemType { get; }

        /// <summary>
        /// Backend value storage support class.
        /// </summary>
        private readonly ApplicationDataContainer _container;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="type">View's item type.</param>
        public PageGroupingStateManager(CommonItemType type)
        {
            ItemType = type;
            _container = ApplicationData.Current.LocalSettings.CreateContainer($"{typeof(T).FullName}.{ItemType}",
                ApplicationDataCreateDisposition.Always);
        }

        /// <summary>
        /// Populate all available pairs for the page.
        /// </summary>
        /// <returns>A read-only list, contains all available pairs.</returns>
        public IReadOnlyList<IndexerComparerPair> PopulateAvailablePairs()
        {
            var rel = new List<IndexerComparerPair>();

            if (ItemType != CommonItemType.Search)
            {
                // Group by A-Z and Z-A is always available in all scenarios
                rel.Add(new IndexerComparerPair("AtoZTitleGroup", new CharacterGroupingIndexer(),
                    new AlphabetAscendComparer(), new AlphabetAscendComparer()));
                rel.Add(new IndexerComparerPair("ZtoATitleGroup", new CharacterGroupingIndexer(),
                    new AlphabetDescendComparer(), new AlphabetAscendComparer()));

                // Genre and release grouping is available in album and song scenario
                if (ItemType == CommonItemType.Album || ItemType == CommonItemType.Song)
                {
                    rel.Add(new IndexerComparerPair("GenreGroup", new GenreIndexer(), new AlphabetAscendComparer(), new AlphabetAscendComparer()));
                    rel.Add(new IndexerComparerPair("ReleaseYearGroup", new ReleaseYearIndexer(),
                        new AlphabetAscendComparer(), new AlphabetAscendComparer()));
                }

                if (ItemType == CommonItemType.Song)
                {
                    rel.Add(new IndexerComparerPair("AlbumNameGroup", new AlbumIndexer(), new AlphabetAscendComparer(), new TrackIndexComparer()));
                    rel.Add(new IndexerComparerPair("ArtistNameGroup", new ArtistIndexer(), new AlphabetAscendComparer(), new AlphabetAscendComparer()));
                }

                rel.Add(new IndexerComparerPair("RecentAddedGroup", new ItemAddedTimeIndexer(), new AlphabetDescendComparer(), new UnixTimestampComparer(false)));
                rel.Add(new IndexerComparerPair("EarlierAddedGroup", new ItemAddedTimeIndexer(), new AlphabetAscendComparer(), new UnixTimestampComparer(true)));

                if (NoGroupTitleString.TryGetValue(ItemType, out string groupTitle))
                {
                    rel.Add(new IndexerComparerPair("NoGrouping", new NoGroupingIndexer(groupTitle), new AlphabetAscendComparer(), new AlphabetAscendComparer()));
                }
            }
            else
            {
                // The only type supported by search is by entity type. But the name, AtoZTitleGroup, is still used.
                rel.Add(new IndexerComparerPair("AtoZTitleGroup", new EntityTypeIndexer(),
                    new AlphabetAscendComparer(), new AlphabetAscendComparer()));
            }

            return rel;
        }

        /// <summary>
        /// Set last used pair for further use.
        /// </summary>
        /// <param name="pair">The pair to save.</param>
        public void SetLastUsedPair(IndexerComparerPair pair)
        {
            if (_container.Values.ContainsKey(UsedSortingGroupKey))
            {
                _container.Values[UsedSortingGroupKey] = pair.Identifier;
            }
            else
            {
                _container.Values.Add(UsedSortingGroupKey, pair.Identifier);
            }
        }

        /// <summary>
        /// Get last used pair. If no suitable page found, default pair will be loaded and used.
        /// </summary>
        /// <returns>An available pair.</returns>
        public IndexerComparerPair GetLastUsedPair()
        {
            // The default settings for all pages is AtoZTitleGroup.
            var groups = PopulateAvailablePairs();
            // Always present
            var rel = groups[0];
            if (_container.Values.ContainsKey(UsedSortingGroupKey))
            {
                var key = (string) _container.Values[UsedSortingGroupKey];
                var query = groups.Where(group => group.Identifier == key);
                rel = query.FirstOrDefault() ?? rel;
            }

            return rel;
        }

        /// <summary>
        /// Event for property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Internal method used to raise property changed event.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
