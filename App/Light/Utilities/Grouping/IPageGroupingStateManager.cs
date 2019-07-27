using System;
using System.Collections.Generic;
using Light.Model;

namespace Light.Utilities.Grouping
{
    /// <summary>
    /// Interface for page grouping and sorting state manager.
    /// </summary>
    public interface IPageGroupingStateManager
    {
        /// <summary>
        /// Read only - Current view's page type.
        /// </summary>
        Type PageType { get; }

        /// <summary>
        /// Read only - Current view's item type.
        /// </summary>
        CommonItemType ItemType { get; }

        /// <summary>
        /// Populate all available pairs for the page.
        /// </summary>
        /// <returns>A read-only list, contains all available pairs.</returns>
        IReadOnlyList<IndexerComparerPair> PopulateAvailablePairs();

        /// <summary>
        /// Set last used pair for further use.
        /// </summary>
        /// <param name="pair">The pair to save.</param>
        void SetLastUsedPair(IndexerComparerPair pair);

        /// <summary>
        /// Get last used pair. If no suitable page found, default pair will be loaded and used.
        /// </summary>
        /// <returns>An available pair.</returns>
        IndexerComparerPair GetLastUsedPair();
    }
}
