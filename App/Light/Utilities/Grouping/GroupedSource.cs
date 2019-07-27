using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Light.Annotations;
using Light.Model;

namespace Light.Utilities.Grouping
{
    /// <summary>
    /// Implementation of grouped content source.
    /// </summary>
    /// <seealso cref="GroupedItems"/>
    /// <seealso cref="IGroupedViewSource"/>
    /// <exception cref="InvalidOperationException">Thrown when operation is called from non-UI thread.</exception>
    public class GroupedSource : IGroupedViewSource
    {
        /// <summary>
        /// Backend object of Items property.
        /// </summary>
        /// <seealso cref="Items"/>
        private ObservableCollection<IGroupedItems> _items;

        /// <summary>
        /// Backend items collection served for re-sorting and grouping.
        /// </summary>
        private readonly List<CommonViewItemModel> _entityCopies; 

        /// <summary>
        /// A hashtable of current groups for quick lookup.
        /// </summary>
        private readonly Dictionary<string, IGroupedItems> _itemsIndex;

        private bool _isEmpty;

        /// <summary>
        /// A observable collection of current groups.
        /// </summary>
        public ObservableCollection<IGroupedItems> Items
        {
            get { return _items; }
            private set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A value indicates whether the collection is empty or not.
        /// </summary>
        public bool IsEmpty
        {
            get { return _isEmpty; }
            private set
            {
                if (value == _isEmpty) return;
                _isEmpty = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Current entity indexer.
        /// </summary>
        public IEntityIndexer Indexer { get; private set; }

        /// <summary>
        /// Current entity index comparer.
        /// </summary>
        public IComparer<string> GroupComparer { get; private set; }

        /// <summary>
        /// Current entity index comparer.
        /// </summary>
        public IComparer<string> ItemComparer { get; private set; }

        /// <summary>
        /// Get sorted item list.
        /// </summary>
        public IList<CommonViewItemModel> Sorted
        {
            get
            {
                var ret = new List<CommonViewItemModel>();
                foreach (var group in Items)
                {
                    ret.AddRange(group);
                }
                return ret;
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="indexer">The desired entity indexer to use.</param>
        /// <param name="groupComparer">The desired group index comparer to use.</param>
        /// <param name="itemComparer">The desired item index comparer to use.</param>
        public GroupedSource(IEntityIndexer indexer, IComparer<string> groupComparer, IComparer<string> itemComparer)
        {
            Indexer = indexer;
            GroupComparer = groupComparer;
            ItemComparer = itemComparer;
            IsEmpty = true;
            _itemsIndex = new Dictionary<string, IGroupedItems>();
            _entityCopies = new List<CommonViewItemModel>();
            Items = new ObservableCollection<IGroupedItems>();
        }

        /// <summary>
        /// Index, group, sort and insert an entity to the collection.
        /// If group doesn't exist, it will be created and sorted.
        /// </summary>
        /// <param name="item">The item to insert into.</param>
        public void Add(CommonViewItemModel item)
        {
            AddInternal(item, false);
        }

        /// <summary>
        /// Index, group, sort and insert an entity to the collection.
        /// If group doesn't exist, it will be created and sorted.
        /// </summary>
        /// <param name="item">The item to insert into.</param>
        /// <param name="ignoreBackendFlatList">Whether ignore backend list or not.</param>
        private void AddInternal(CommonViewItemModel item, bool ignoreBackendFlatList = false)
        {
            if (item != null)
            {
                var index = Indexer.GetIndexForGroup(item);
                // If group exists
                if (_itemsIndex.ContainsKey(index))
                {
                    _itemsIndex[index].Add(item);
                }
                // Or create group
                else
                {
                    var newCreatedGroup = new GroupedItems(index, Indexer, ItemComparer);
                    _itemsIndex.Add(index, newCreatedGroup);
                    var processed = false;
                    if (Items.Count != 0)
                    {
                        for (int i = 0; i < Items.Count; i++)
                        {
                            if (GroupComparer.Compare(Items[i].Title, index) >= 0)
                            {
                                Items.Insert(i, newCreatedGroup);
                                processed = true;
                                break;
                            }
                        }
                    }

                    if (!processed)
                    {
                        Items.Add(newCreatedGroup);
                    }

                    // Add item
                    newCreatedGroup.Add(item);
                }

                // Add backend item (optional)
                if (!ignoreBackendFlatList)
                {
                    _entityCopies.Add(item);
                }

                // Notify change if required (precondition: IsEmpty == true)
                if (IsEmpty)
                {
                    IsEmpty = false;
                }
            }
        }

        /// <summary>
        /// Remove an entity from existing groups. Only first occurrence will be removed.
        /// If a group has size of zero after removal, it will be removed too.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void Remove(CommonViewItemModel item)
        {
            if (item != null)
            {
                var index = Indexer.GetIndexForGroup(item);
                // If group exists
                if (_itemsIndex.ContainsKey(index))
                {
                    var group = _itemsIndex[index];
                    group.Remove(item);
                    _entityCopies.Remove(item);
                    // Check group size. If group's size is 0, remove this group.
                    if (group.Count == 0)
                    {
                        _itemsIndex.Remove(index);
                        Items.Remove(group);
                    }
                }

                // Notify change if required (precondition: all removed)
                if (_entityCopies.Count == 0)
                {
                    IsEmpty = true;
                }
            }
        }

        /// <summary>
        /// Set indexer and regroup and resort all items.
        /// </summary>
        /// <param name="indexer">The indexer to be set.</param>
        public void SetIndexer(IEntityIndexer indexer)
        {
            _itemsIndex.Clear();
            Items.Clear();

            Indexer = indexer;

            foreach (var entity in _entityCopies)
            {
                AddInternal(entity, true);
            }
        }

        /// <summary>
        /// Set comparer and resort all items.
        /// </summary>
        /// <param name="groupComparer">The group comparer to be set.</param>
        /// <param name="itemComparer">The item comparer to be set.</param>
        public void SetComparer(IComparer<string> groupComparer, IComparer<string> itemComparer)
        {
            // Set base
            GroupComparer = groupComparer;
            ItemComparer = itemComparer;

            // Sort groups
            foreach (var group in Items)
            {
               group.SetComparer(ItemComparer);
            }

            // Sort groups
            for (int i = 1; i < Items.Count; i++)
            {
                var d = Items[i];
                int j;
                for (j = i - 1; j >= 0 && groupComparer.Compare(Items[j].Title, d.Title) >= 1; j--)
                {
                    Items[j + 1] = Items[j];
                }
                Items[j + 1] = d;
            }
        }

        /// <summary>
        /// Set both comparer and inexer, the index and regroup all items.
        /// </summary>
        /// <param name="indexer">The indexer to be set.</param>
        /// <param name="groupComparer">The group comparer to be set.</param>
        /// <param name="itemComparer">The item comparer to be set.</param>
        public void SetAll(IEntityIndexer indexer, IComparer<string> groupComparer, IComparer<string> itemComparer)
        {
            _itemsIndex.Clear();
            Items.Clear();

            Indexer = indexer;
            GroupComparer = groupComparer;
            ItemComparer = itemComparer;

            foreach (var entity in _entityCopies)
            {
                AddInternal(entity, true);
            }
        }

        /// <summary>
        /// Clear all existing groups and entities.
        /// </summary>
        public void Clear()
        {
            _itemsIndex.Clear();
            _entityCopies.Clear();
            Items.Clear();
            IsEmpty = true;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>The PropertyChanged event can indicate all properties on the object have changed by using either null or String.Empty as the property name in the PropertyChangedEventArgs.</remarks>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Used by class for raising PropertyChanged events.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        /// <seealso cref="PropertyChanged"/>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
