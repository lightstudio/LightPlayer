using Light.Managed.Database.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Light.Utilities
{
    public class FutureAccessListHelper
    {
        static public FutureAccessListHelper Instance = new FutureAccessListHelper();

        private uint _maxItems =
            StorageApplicationPermissions.FutureAccessList.MaximumItemsAllowed;

        List<string> _authorizedItems;
        private FutureAccessListHelper()
        {
            LoadAuthroizedItems();
        }

        private void CheckAndAllocateForNewItem(int count)
        {
            if (_authorizedItems.Count + count > _maxItems) //Not likely to happen.
                throw new Exception("You've authorized too many files. please try to remove some of them.");
            else
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count + count >
                    StorageApplicationPermissions.FutureAccessList.MaximumItemsAllowed)
            {
                var removeCount = StorageApplicationPermissions.FutureAccessList.Entries.Count
                    + count - StorageApplicationPermissions.FutureAccessList.MaximumItemsAllowed;
                var all =
                    (from i
                     in StorageApplicationPermissions.FutureAccessList.Entries
                     where !_authorizedItems.Contains(i.Token)
                     select i.Token).ToList();
                for (int i = 0; i < removeCount; i++)
                {
                    StorageApplicationPermissions.FutureAccessList.Remove(all[i]);
                }
            }
        }

        private void SaveAuthorizedItems()
        {
            File.WriteAllLines(
                Path.Combine(
                    ApplicationData.Current.LocalFolder.Path, "Authorized"),
                _authorizedItems.ToArray());
        }

        private async Task SaveAuthorizedItemsAsync()
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                "Authorized",
                CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenStreamForWriteAsync())
            using (var sw = new StreamWriter(stream))
            {
                foreach (var item in _authorizedItems)
                {
                    await sw.WriteLineAsync(item);
                }
            }
        }

        private void LoadAuthroizedItems()
        {
            try
            {
                var path = Path.Combine(
                    ApplicationData.Current.LocalFolder.Path, "Authorized");
                if (File.Exists(path))
                {
                    _authorizedItems = File.ReadAllLines(path).ToList();
                    return;
                }
            }
            catch { }
            _authorizedItems = new List<string>();
        }

        public async Task<string> AuthorizeStorageItem(IStorageItem item)
        {
            CheckAndAllocateForNewItem(1);
            var token = StorageApplicationPermissions.FutureAccessList.Add(item);
            _authorizedItems.Add(token);
            await SaveAuthorizedItemsAsync();
            return token;
        }

        public async Task<List<Tuple<string, IStorageItem>>> GetAuthroizedStorageItemsAsync()
        {
            var ret = new List<Tuple<string, IStorageItem>>();
            foreach (var item in _authorizedItems)
            {
                try
                {
                    ret.Add(
                        new Tuple<string, IStorageItem>(
                            item,
                            await StorageApplicationPermissions.FutureAccessList.GetItemAsync(item)));
                }
                catch
                {

                }
            }
            return ret;
        }

        public async Task RemoveAuthorizedItemAsync(string item)
        {
            if (_authorizedItems.Contains(item))
            {
                _authorizedItems.Remove(item);
                if (StorageApplicationPermissions.FutureAccessList.ContainsItem(item))
                    StorageApplicationPermissions.FutureAccessList.Remove(item);
                await SaveAuthorizedItemsAsync();
            }
        }

        public void AddTempItem(IEnumerable<IStorageItem> items)
        {
            var l = items.ToList();
            CheckAndAllocateForNewItem(l.Count);
            foreach (var item in l)
            {
                if (!CanAccess(item))
                {
                    StorageApplicationPermissions.FutureAccessList.Add(item);
                }
            }
        }

        public bool CanAccess(IStorageItem item) =>
            StorageApplicationPermissions.FutureAccessList.CheckAccess(item);
    }
}
