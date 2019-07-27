using System.Collections.Generic;
using Windows.ApplicationModel.Resources;

namespace Light.Utilities.Grouping
{
    /// <summary>
    /// Indexer & Comparer pair. Intended for UI usage.
    /// </summary>
    public class IndexerComparerPair
    {
        private readonly ResourceLoader _resLoader;

        /// <summary>
        /// Indexer & Comparer pair for UI text.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Display name for the indexer and comparer pair.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// General description for a grouping pair.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Entity indexer.
        /// </summary>
        public IEntityIndexer Indexer { get; }

        /// <summary>
        /// Group index comparer.
        /// </summary>
        public IComparer<string> GroupComparer { get; }

        /// <summary>
        /// Entity index comparer.
        /// </summary>
        public IComparer<string> ItemComparer { get; }

        /// <summary>
        /// Indicates whether grouping is required.
        /// </summary>
        public bool RequireGrouping { get; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="identifier">Indexer & Comparer pair for UI text.</param>
        /// <param name="indexer">Entity indexer.</param>
        /// <param name="comparer">Entity index comparer.</param>
        /// <param name="isGroupingRequired">Indicates whether grouping is required.</param>
        /// <remarks>Due to a recent resource design change, the constructor must be called from UI thread.</remarks>
        public IndexerComparerPair(string identifier, IEntityIndexer indexer, IComparer<string> groupComparer, IComparer<string> itemComparer, bool isGroupingRequired = false)
        {
            Identifier = identifier;
            Indexer = indexer;
            GroupComparer = groupComparer;
            ItemComparer = itemComparer;
            RequireGrouping = isGroupingRequired;

            _resLoader = ResourceLoader.GetForCurrentView();

            LoadLocalizedStrings();
        }

        private void LoadLocalizedStrings()
        {
            DisplayName = _resLoader.GetString($"{Identifier}Name");
            Description = _resLoader.GetString($"{Identifier}Description");
        }
    }
}
