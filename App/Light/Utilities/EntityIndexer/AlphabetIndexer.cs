using Light.Common;
using Light.Model;
using Light.Utilities.Grouping;

namespace Light.Utilities.EntityIndexer
{
    /// <summary>
    /// Simple alphabet indexer without multi-language support.
    /// </summary>
    public class AlphabetIndexer : IEntityIndexer
    {
        /// <summary>
        /// Entity indexer's identifier.
        /// </summary>
        public string Identifier => nameof(AlphabetIndexer);

        /// <summary>
        /// Get item index with indexer-specific logic for grouping.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndex"/>
        public string GetIndexForGroup(CommonViewItemModel item)
        {
            return !string.IsNullOrEmpty(item?.Title) ? item.Title.Substring(0, 1).ToUpper() : CommonSharedStrings.UnknownIndex;
        }

        /// <summary>
        /// Get item index with indexer-specific logic.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndexForGroup"/>
        public string GetIndex(CommonViewItemModel item)
        {
            return !string.IsNullOrEmpty(item?.Title) ? item.Title : CommonSharedStrings.UnknownIndex;
        }
    }
}
