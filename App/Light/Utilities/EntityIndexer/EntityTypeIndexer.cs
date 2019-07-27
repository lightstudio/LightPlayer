using Light.Common;
using Light.Model;
using Light.Utilities.Grouping;

namespace Light.Utilities.EntityIndexer
{
    /// <summary>
    /// Entity indexer using entity type.
    /// </summary>
    public class EntityTypeIndexer : IEntityIndexer
    {
        /// <summary>
        /// Entity indexer's identifier.
        /// </summary>
        public string Identifier => nameof(EntityTypeIndexer);

        /// <summary>
        /// Get item index with indexer-specific logic for grouping.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndex"/>
        public string GetIndexForGroup(CommonViewItemModel item)
        {
            var ret = CommonSharedStrings.UnknownIndex;

            if (item != null)
            {
                switch (item.Type)
                {
                    case CommonItemType.Album:
                        ret = CommonSharedStrings.DefaultAlbumName;
                        break;
                    case CommonItemType.Artist:
                        ret = CommonSharedStrings.DefaultArtistName;
                        break;
                }
            }

            return ret;
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
