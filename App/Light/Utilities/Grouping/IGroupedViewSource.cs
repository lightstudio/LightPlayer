using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Light.Model;

namespace Light.Utilities.Grouping
{
    /// <summary>
    /// Interface for grouped view source.
    /// </summary>
    public interface IGroupedViewSource : INotifyPropertyChanged
    {
        /// <summary>
        /// Observable collection of groups.
        /// </summary>
        ObservableCollection<IGroupedItems> Items { get; }

        /// <summary>
        /// A value indicates whether the collection is empty or not.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Current indexer.
        /// </summary>
        IEntityIndexer Indexer { get; }

        /// <summary>
        /// Current group comparer.
        /// </summary>
        IComparer<string> GroupComparer { get; }

        /// <summary>
        /// Current item comparer.
        /// </summary>
        IComparer<string> ItemComparer { get; }

        /// <summary>
        /// Index, group, sort and insert an entity to the collection.
        /// If group doesn't exist, it will be created and sorted.
        /// </summary>
        /// <param name="item">The item to insert into.</param>
        void Add(CommonViewItemModel item);

        /// <summary>
        /// Remove an entity from existing groups. Only first occurrence will be removed.
        /// If a group has size of zero after removal, it will be removed too.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        void Remove(CommonViewItemModel item);

        /// <summary>
        /// Clear all existing groups and entities.
        /// </summary>
        void Clear();

        /// <summary>
        /// Set comparer and resort all items.
        /// Do not perform other operations during sorting - that may affect data integrity.
        /// </summary>
        /// <param name="groupComparer">The group comparer to be set.</param>
        /// <param name="itemComparer">The item comparer to be set.</param>
        void SetComparer(IComparer<string> groupComparer, IComparer<string> itemComparer);

        /// <summary>
        /// Set both comparer and inexer, the index and regroup all items.
        /// </summary>
        /// <param name="indexer">The indexer to be set.</param>
        /// <param name="groupComparer">The group comparer to be set.</param>
        /// <param name="itemComparer">The item comparer to be set.</param>
        void SetAll(IEntityIndexer indexer, IComparer<string> groupComparer, IComparer<string> itemComparer);

        /// <summary>
        /// Set indexer and regroup and resort all items.
        /// </summary>
        /// <param name="indexer">The indexer to be set.</param>
        void SetIndexer(IEntityIndexer indexer);

        /// <summary>
        /// Get sorted item list.
        /// </summary>
        IList<CommonViewItemModel> Sorted { get; }
    }
}
