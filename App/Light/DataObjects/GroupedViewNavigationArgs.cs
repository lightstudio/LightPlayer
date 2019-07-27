using System.Collections.Generic;
using Light.Model;
using Light.Utilities.EntityComparer;
using Light.Utilities.EntityIndexer;
using Light.Utilities.Grouping;

namespace Light.DataObjects
{
    /// <summary>
    /// Unified grouped view navigation args class. Contains Information required for grouping, sorting and page type.
    /// </summary>
    public class GroupedViewNavigationArgs
    {
        /// <summary>
        /// Indicates the desired page type.
        /// </summary>
        public CommonItemType PageType { get; }

        /// <summary>
        /// Contains search keywords.
        /// </summary>
        /// <remarks>If SearchWord is present, then PageType must be Search.</remarks>
        /// <seealso cref="PageType" />
        public string SearchWord { get; }

        /// <summary>
        /// Represents the default entity indexer used for viewmodel grouped data source.
        /// </summary>
        /// <seealso cref="EntityComparer"/>
        public IEntityIndexer EntityIndexer { get; }

        /// <summary>
        /// Represents the default item comparer used for viewmodel grouped data source.
        /// </summary>
        /// <seealso cref="GroupComparer"/>
        public IComparer<string> GroupComparer { get; }

        /// <summary>
        /// Represents the default group comparer used for viewmodel grouped data source.
        /// </summary>
        public IComparer<string> ItemComparer { get; }

        /// <summary>
        /// Class constructor for search pages.
        /// </summary>
        /// <param name="keyword">Keywords for searching.</param>
        public GroupedViewNavigationArgs(string keyword)
        {
            PageType = CommonItemType.Search;
            SearchWord = keyword;
            GroupComparer = new AlphabetAscendComparer();
            ItemComparer = new AlphabetAscendComparer();
            EntityIndexer = new EntityTypeIndexer();
        }

        /// <summary>
        /// Class constructor for other pages.
        /// </summary>
        /// <param name="type">Page type.</param>
        /// <param name="indexer">Entity indexer.</param>
        /// <param name="comparer">Entity compaerer.</param>
        public GroupedViewNavigationArgs(CommonItemType type, IEntityIndexer indexer, IComparer<string> groupComparer, IComparer<string> itemComparer)
        {
            PageType = type;
            SearchWord = string.Empty;
            EntityIndexer = indexer;
            GroupComparer = groupComparer;
            ItemComparer = itemComparer;
        }

        /// <summary>
        /// Class constructor for other pages.
        /// </summary>
        /// <param name="type">Page type.</param>
        /// <param name="pair">A pair of indexer and comparer.</param>
        public GroupedViewNavigationArgs(CommonItemType type, IndexerComparerPair pair)
        {
            PageType = type;
            SearchWord = string.Empty;
            EntityIndexer = pair.Indexer;
            GroupComparer = pair.GroupComparer;
            ItemComparer = pair.ItemComparer;
        }
    }
}
