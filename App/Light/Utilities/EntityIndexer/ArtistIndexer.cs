using Light.Common;
using Light.Model;
using Light.Utilities.Grouping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Utilities.EntityIndexer
{
    class ArtistIndexer : IEntityIndexer
    {
        /// <summary>
        /// Entity indexer's identifier.
        /// </summary>
        public string Identifier => nameof(AlbumIndexer);

        /// <summary>
        /// Get item index with indexer-specific logic for grouping.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndex"/>
        public string GetIndex(CommonViewItemModel item)
        {
            return !string.IsNullOrWhiteSpace(item?.Title) ? item.Title : CommonSharedStrings.UnknownIndex;
        }

        /// <summary>
        /// Get item index with indexer-specific logic.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndexForGroup"/>
        public string GetIndexForGroup(CommonViewItemModel item)
        {
            return !string.IsNullOrWhiteSpace(item?.File?.Artist) ? item.File.Artist : CommonSharedStrings.UnknownIndex;
        }
    }
}
