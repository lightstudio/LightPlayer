using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.UI.Core;

namespace Light.Utilities
{
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        // Override the event so this class can access it
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly CoreDispatcher _dispatcher;

        public ObservableCollectionEx(CoreDispatcher dispatcher) : base(new List<T>())
        {
            _dispatcher = dispatcher;
        }

        public ObservableCollectionEx(IEnumerable<T> collection, CoreDispatcher dispatcher) : base(collection)
        {
            _dispatcher = dispatcher;
        }

        public ObservableCollectionEx(List<T> collection, CoreDispatcher dispatcher) : base(collection)
        {
            _dispatcher = dispatcher;
        }

        protected override async void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // Be nice - use BlockReentrancy like MSDN said
            using (BlockReentrancy())
            {
                var eventHandler = CollectionChanged;
                if (eventHandler != null)
                {
                    Delegate[] delegates = eventHandler.GetInvocationList();
                    // Walk thru invocation list
                    foreach (var @delegate in delegates)
                    {
                        var handler = (NotifyCollectionChangedEventHandler) @delegate;
                        // If the subscriber is a DispatcherObject and different thread
                        if (!_dispatcher.HasThreadAccess)
                            // Invoke handler in the target dispatcher's thread
                            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => handler(this, e));
                        else // Execute handler as is
                            handler(this, e);
                    }
                }
            }
        }
    }

}
