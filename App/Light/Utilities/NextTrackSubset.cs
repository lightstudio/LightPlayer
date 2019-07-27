using Light.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Windows.UI.Core;

namespace Light.Utilities
{
    public class NextTrackSubset : ObservableCollection<MusicPlaybackItem>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private int _lastKnownIndex;
        private readonly int _limit;
        private CoreDispatcher _dispatcher;

        public NextTrackSubset(CoreDispatcher dispatcher, int limit)
        {
            _limit = limit;
            PlaybackControl.Instance.Items.CollectionChanged += OnCollectionChanged;
            PlaybackControl.Instance.NowPlayingChanged += NowPlayingChanged;
            _lastKnownIndex = NowPlayingIndex;
            if (_lastKnownIndex == -1)
                _lastKnownIndex = 0;
            _dispatcher = dispatcher;
            foreach (var item in
                Subset(_lastKnownIndex,
                Math.Min(_lastKnownIndex + _limit, PlaybackControl.Instance.Items.Count - 1),
                PlaybackControl.Instance.Items))
            {
                Add(item);
            }
        }

        private int NowPlayingIndex => PlaybackControl.Instance.Items.IndexOf(PlaybackControl.Instance.Current);

        private IEnumerable<T> Subset<T>(int start, int stop, IList<T> collection)
        {
            for (int i = start + 1; i <= stop; i++)
                yield return collection[i];
            yield break;
        }

        private void RemoveExceededItems()
        {
            if (Count <= _limit)
            {
                return;
            }
            for (int i = Count - 1; i >= _limit; i--)
            {
                RemoveAt(i);
            }
        }

        private void AddInsufficientItems()
        {
            var np = NowPlayingIndex;
            var max = Math.Min(
                PlaybackControl.Instance.Items.Count - np - 1,
                _limit);

            if (Count >= max)
            {
                return;
            }
            for (int i = Count; i < max; i++)
            {
                Add(PlaybackControl.Instance.Items[i + np + 1]);
            }
        }

        private async void NowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                try
                {
                    var newIndex = PlaybackControl.Instance.Items.IndexOf(e.NewItem);
                    if (Math.Abs(newIndex - _lastKnownIndex) < 20)
                    {
                        if (newIndex > _lastKnownIndex)
                        {
                            var remove = Math.Min(Count, newIndex - _lastKnownIndex);
                            _lastKnownIndex = newIndex;
                            for (int i = 0; i < remove; i++)
                            {
                                RemoveItem(0);
                            }
                            AddInsufficientItems();
                        }
                        else if (newIndex < _lastKnownIndex)
                        {
                            var addedItems = Subset(newIndex, _lastKnownIndex, PlaybackControl.Instance.Items).ToList();
                            _lastKnownIndex = newIndex;
                            for (int i = addedItems.Count - 1; i >= 0; i--)
                            {
                                InsertItem(0, addedItems[i]);
                            }
                            RemoveExceededItems();
                        }
                    }
                    else
                    {
                        Clear();
                        _lastKnownIndex = newIndex;
                        foreach (var item in
                            Subset(_lastKnownIndex,
                                Math.Min(_lastKnownIndex + _limit, PlaybackControl.Instance.Items.Count - 1),
                                PlaybackControl.Instance.Items))
                        {
                            Add(item);
                        }
                    }
                }
                catch { }
            });
        }

        private void Refresh()
        {
            Clear();
            _lastKnownIndex = NowPlayingIndex;
            foreach (var item in Subset(_lastKnownIndex,
                Math.Min(_lastKnownIndex + _limit, PlaybackControl.Instance.Items.Count - 1),
                PlaybackControl.Instance.Items))
            {
                Add(item);
            }
        }

        private async void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var newIndex = NowPlayingIndex;
                if (newIndex == -1 && _lastKnownIndex == -1)
                    _lastKnownIndex = 0;
                else if (_lastKnownIndex == -1)
                    _lastKnownIndex = newIndex;
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems[0] == PlaybackControl.Instance.Current)
                        {
                            Refresh();
                        }
                        else if (e.NewStartingIndex > _lastKnownIndex)
                        {
                            if (e.NewStartingIndex <= _lastKnownIndex + _limit)
                            {
                                var item = (MusicPlaybackItem)e.NewItems[0];// Only 1 should be possible due to WinRT
                                if (!Contains(item))
                                {
                                    InsertItem(e.NewStartingIndex - _lastKnownIndex - 1, item);
                                }
                                RemoveExceededItems();
                            }
                        }
                        else if (e.NewStartingIndex < _lastKnownIndex)
                        {
                            _lastKnownIndex += e.NewItems.Count;
                            //Refresh();
                        }
                        else
                        {
                            _lastKnownIndex = NowPlayingIndex;
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldStartingIndex > _lastKnownIndex)
                        {
                            if (e.OldStartingIndex <= _lastKnownIndex + _limit)
                            {
                                RemoveItem(e.OldStartingIndex - _lastKnownIndex - 1); // Only 1 should be possible due to WinRT
                                AddInsufficientItems();
                            }
                        }
                        else if (e.OldStartingIndex < _lastKnownIndex)
                        {
                            _lastKnownIndex -= e.OldItems.Count;
                        }
                        else
                        {
                            _lastKnownIndex = NowPlayingIndex;
                            Clear();
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        Clear();
                        _lastKnownIndex = NowPlayingIndex;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            });
        }

        public void Close()
        {
            PlaybackControl.Instance.Items.CollectionChanged -= OnCollectionChanged;
            PlaybackControl.Instance.NowPlayingChanged -= NowPlayingChanged;
        }
    }
}
