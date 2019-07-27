using System;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using GalaSoft.MvvmLight;
using Light.Model;
using Light.Utilities;

namespace Light.ViewModel.Library.Detailed
{
    public class DetailedViewModelBase : ViewModelBase
    {
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

        protected DetailedViewModelBase(CoreDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            ViewItems = new ObservableCollectionEx<CommonViewItemModel>(dispatcher);
        }

        private string _viewTitle;
        public string ViewTitle
        {
            get { return _viewTitle; }
            set
            {
                _viewTitle = value;
                NotifyChange(nameof(ViewTitle));
            }
        }

        private ObservableCollection<CommonViewItemModel> _viewItems;
        public ObservableCollection<CommonViewItemModel> ViewItems
        {
            get { return _viewItems; }
            set
            {
                _viewItems = value;
                NotifyChange(nameof(ViewItems));
            }
        }

        protected async void CleanViewItems()
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.High, CleanViewItems);
                return;
            }
            ViewItems.Clear();
        }

        private ObservableCollection<string> _groupOptions;
        public ObservableCollection<string> GroupOptions
        {
            get { return _groupOptions; }
            set
            {
                _groupOptions = value;
                NotifyChange(nameof(GroupOptions));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                NotifyChange(nameof(IsLoading));
            }
        }

        private CommonItemType _viewType;
        public CommonItemType ViewType
        {
            get { return _viewType; }
            set
            {
                _viewType = value;
                NotifyChange(nameof(ViewType));
            }
        }

        public async void RequestCollectionChange(CommonViewItemModel newItem, CommonViewItemModel oldItem)
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => RequestCollectionChange(newItem, oldItem));
                return;
            }

            var index = ViewItems.IndexOf(oldItem);
            if (index < 0) return;
            ViewItems[index] = newItem;
        }
    }
}
