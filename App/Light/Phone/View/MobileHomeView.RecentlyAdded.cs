using GalaSoft.MvvmLight.Messaging;
using Light.Core;
using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Light.Phone.View
{
    partial class MobileHomeView
    {
        ObservableCollection<RecentlyAddedAlbumModel> RecentAlbums = new ObservableCollection<RecentlyAddedAlbumModel>();
        
        private void OnLibraryListViewLoaded(object sender, RoutedEventArgs e)
        {
            LoadRecentlyAddedItems();
        }

        private void OnLibraryListViewUnloaded(object sender, RoutedEventArgs e)
        {
            UnregisterRecentlyAddedEvents();
        }

        private void LoadRecentlyAddedItems()
        {
            RecentAlbums.Clear();
            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                var query = (from album
                            in context.Albums
                             orderby album.DatabaseItemAddedDate descending
                             select album)
                            .Take(10);
                foreach (var m in query)
                {
                    RecentAlbums.Add(new RecentlyAddedAlbumModel(m));
                }
            }
            Messenger.Default.Register<GenericMessage<DbAlbum>>(this, "NewAlbumAdded", OnNewAlbumAdded);
        }

        private async void OnNewAlbumAdded(GenericMessage<DbAlbum> obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RecentAlbums.Insert(0, new RecentlyAddedAlbumModel(obj.Content));
                for (int i = RecentAlbums.Count - 1; i >= 10; i--)
                {
                    RecentAlbums.RemoveAt(i);
                }
            });
        }

        private void UnregisterRecentlyAddedEvents()
        {
            Messenger.Default.Unregister<GenericMessage<DbAlbum>>(this, "NewAlbumAdded", OnNewAlbumAdded);
        }

        private void OnRecentlyAddedItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var model = (sender as FrameworkElement).DataContext as RecentlyAddedAlbumModel;
            var album = model.Album;
            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                // Ensure the album exists in database.
                var result = context.Albums.Where(a => a.Id == album.Id).FirstOrDefault();
                if (result != null)
                {
                    Frame.Navigate(typeof(MobileAlbumDetailView), result.Id);
                }
            }
        }
    }
}
