using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Light.Model;

namespace Light.Utilities.Grouping
{
    /// <summary>
    /// Implementation of item group.
    /// </summary>
    /// <seealso cref="GroupedSource"/>
    /// <seealso cref="IGroupedItems"/>
    /// <exception cref="InvalidOperationException">Thrown when operation is called from non-UI thread.</exception>
    public class GroupedItems : ObservableCollection<CommonViewItemModel>, IGroupedItems
    {
        private string _title;

        /// <summary>
        /// Group title(header).
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        /// <summary>
        /// Current entity indexer.
        /// </summary>
        public IEntityIndexer Indexer { get; private set; }

        /// <summary>
        /// Current entity index comparer.
        /// </summary>
        public IComparer<string> Comparer { get; private set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="title">Group title.</param>
        /// <param name="indexer">Indexer of items.</param>
        /// <param name="comparer">Comparer of items.</param>
        public GroupedItems(string title, IEntityIndexer indexer, IComparer<string> comparer)
        {
            Title = title;
            Indexer = indexer;
            Comparer = comparer;
        }

        /// <summary>
        /// Sort and put item into group. If the item to put doesn't belong to this group, it will not be added.
        /// </summary>
        /// <param name="item">The item to put into the group.</param>
        public new void Add(CommonViewItemModel item)
        {
            if (Count == 0)
            {
                base.Add(item);
            }
            else
            {
                var processed = false;
                if (Indexer.GetIndexForGroup(item) != Title) return;
                var index = Indexer.GetIndex(item);

                for (int i = 0; i < Items.Count; i++)
                {
                    if (Comparer.Compare(Indexer.GetIndex(Items[i]), index) >= 0)
                    {
                        Insert(i, item);
                        processed = true;
                        break;
                    }
                }
                if (!processed)
                {
                    base.Add(item);
                }
            }
        }

        /// <summary>
        /// Set comparer and resort all items.
        /// Do not perform other operations during sorting - that may affect data integrity.
        /// </summary>
        /// <param name="comparer">The comparer to be set.</param>
        public void SetComparer(IComparer<string> comparer)
        {
            Comparer = comparer;

            for (int i = 1; i < Count; i++)
            {
                var d = base[i];
                int j;
                for (j = i - 1; j >= 0 && comparer.Compare(Indexer.GetIndex(base[j]), Indexer.GetIndex(d)) >= 1; j--)
                {
                    base[j + 1] = base[j];
                }
                base[j + 1] = d;
            }
        }

        /// <summary>
        /// Get group title.
        /// </summary>
        /// <returns>A string represents the title of the group.</returns>
        public override string ToString()
        {
            return Title;
        }
    }
}
