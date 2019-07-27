using System;
using Windows.Globalization.Collation;
using Light.Common;
using Light.Model;
using Light.Utilities.Grouping;

namespace Light.Utilities.EntityIndexer
{
    /// <summary>
    /// Multi-language character grouping entity indexer.
    /// </summary>
    public class CharacterGroupingIndexer : IEntityIndexer
    {
        private readonly CharacterGroupings _slg;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public CharacterGroupingIndexer()
        {
            _slg = new CharacterGroupings();
        }

        /// <summary>
        /// Entity indexer's identifier.
        /// </summary>
        public string Identifier => nameof(CharacterGroupingIndexer);

        /// <summary>
        /// Get item index with indexer-specific logic for grouping.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndex"/>
        public string GetIndexForGroup(CommonViewItemModel item)
        {
            if (string.IsNullOrEmpty(item?.Title))
            {
                return CommonSharedStrings.UnknownIndex;
            }

            // Lookup table
            return _slg.Lookup(item.Title[0].ToString());
        }

        /// <summary>
        /// Get item index with indexer-specific logic.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndexForGroup"/>
        public string GetIndex(CommonViewItemModel item)
        {
            // We do not that for full-text grouping, so return all
            return !string.IsNullOrEmpty(item?.Title) ? item.Title : CommonSharedStrings.UnknownIndex;
        }
    }
}
