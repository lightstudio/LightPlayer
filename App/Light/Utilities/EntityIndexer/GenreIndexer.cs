using Light.Common;
using Light.Model;
using Light.Utilities.Grouping;

namespace Light.Utilities.EntityIndexer
{
    /// <summary>
    /// Indexer based on album genre. Not available for artists.
    /// </summary>
    public class GenreIndexer : IEntityIndexer
    {
        /// <summary>
        /// Entity indexer's identifier.
        /// </summary>
        public string Identifier => nameof(GenreIndexer);

        /// <summary>
        /// Get item index with indexer-specific logic for grouping.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndex"/>
        public string GetIndexForGroup(CommonViewItemModel item)
        {
            if (string.IsNullOrEmpty(item?.Genre))
            {
                return CommonSharedStrings.UnknownIndex;
            }

            return item.Genre;
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
