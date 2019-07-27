using Light.Common;
using Light.Model;
using Light.Utilities.Grouping;

namespace Light.Utilities.EntityIndexer
{
    /// <summary>
    /// Indexer based on album release year. Not available for artists.
    /// </summary>
    public class ReleaseYearIndexer : IEntityIndexer
    {
        /// <summary>
        /// Entity indexer's identifier.
        /// </summary>
        public string Identifier => nameof(ReleaseYearIndexer);

        /// <summary>
        /// Get item index with indexer-specific logic for grouping.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndex"/>
        public string GetIndexForGroup(CommonViewItemModel item)
        {
            if (string.IsNullOrEmpty(item?.ReleaseDate))
            {
                return CommonSharedStrings.UnknownDate;
            }

            // Parse date.
            return DateTimeHelper.GetItemDateYearString(item);
        }

        /// <summary>
        /// Get item index with indexer-specific logic.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndexForGroup"/>
        public string GetIndex(CommonViewItemModel item)
        {
            // Inner content will still follow alphabet-based sort.
            return !string.IsNullOrEmpty(item?.Title) ? item.Title : CommonSharedStrings.UnknownIndex;
        }
    }
}
