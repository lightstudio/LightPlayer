using System;
using Windows.UI.Core;
using GalaSoft.MvvmLight;
using Light.Utilities.EntityComparer;
using Light.Utilities.EntityIndexer;
using Light.Utilities.Grouping;

namespace Light.ViewModel.Library
{
    public class CoreViewBase : ViewModelBase
    {
        protected object Oplock;
        protected readonly CoreDispatcher Dispatcher;
        protected async void NotifyChange(string name)
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => NotifyChange(name));
                return;
            }
            // ReSharper disable once ExplicitCallerInfoArgument
            RaisePropertyChanged(name);
        }
        protected CoreViewBase(CoreDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            Oplock = new object();
            GroupedItems = new GroupedSource(new CharacterGroupingIndexer(), new AlphabetAscendComparer(), new AlphabetAscendComparer());
        }

        #region Grouped items
        private IGroupedViewSource _groupedItems;

        /// <summary>
        /// Self grouped and sorted entity collection.
        /// </summary>
        public IGroupedViewSource GroupedItems
        {
            get { return _groupedItems; }
            set
            {
                _groupedItems = value;
                RaisePropertyChanged();
            }
        }
        #endregion
    }
}
