using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Light.Model;

namespace Light.Utilities.Grouping
{
    /// <summary>
    /// Interface for grouped items.
    /// </summary>
    public interface IGroupedItems : IList<CommonViewItemModel>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <summary>
        /// Group title.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Current indexer.
        /// </summary>
        IEntityIndexer Indexer { get; }

        /// <summary>
        /// Current comparer.
        /// </summary>
        IComparer<string> Comparer { get; }

        /// <summary>
        /// Set comparer and resort all items.
        /// Do not perform other operations during sorting - that may affect data integrity.
        /// </summary>
        /// <param name="comparer">The comparer to be set.</param>
        void SetComparer(IComparer<string> comparer);
    }
}
