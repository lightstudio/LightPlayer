using Windows.Foundation;
using Windows.Storage;

namespace Light.NETCore.IO
{
    internal sealed class FutureAccessList
    {
        private Windows.Storage.AccessCache.StorageItemAccessList _list;

        public FutureAccessList()
        {
            _list = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
        }
        public string Add(string token, IStorageItem item)
        {
            if(_list.Entries.Count == _list.MaximumItemsAllowed) _list.Remove(_list.Entries[0].Token);
            _list.AddOrReplace(token, item);
            return token;
        }
        public void Remove(string token)
        {
            _list.Remove(token);
        }
        public void Clear()
        {
            _list.Clear();
        }

        public IAsyncOperation<IStorageItem> GetItemAsync(string token) => _list.GetItemAsync(token);
        public IAsyncOperation<StorageFile> GetFileAsync(string token) => _list.GetFileAsync(token);
        public IAsyncOperation<StorageFolder> GetFolderAsync(string token) => _list.GetFolderAsync(token);
        public bool CheckAccess(IStorageItem item) => _list.CheckAccess(item);
    }
}
