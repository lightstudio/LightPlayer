using Light.Model;

namespace Light.Utilities.Grouping
{
    /// <summary>
    /// Interface for general indexer.
    /// </summary>
    public interface IEntityIndexer
    {
        /// <summary>
        /// Entity indexer's identifier.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Get item index with indexer-specific logic for grouping.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndex"/>
        string GetIndexForGroup(CommonViewItemModel item);

        /// <summary>
        /// Get item index with indexer-specific logic.
        /// </summary>
        /// <param name="item">The item to be indexed.</param>
        /// <returns>Item's index.</returns>
        /// <seealso cref="GetIndexForGroup"/>
        string GetIndex(CommonViewItemModel item);
    }
}
