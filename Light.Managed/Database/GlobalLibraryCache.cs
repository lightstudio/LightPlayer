using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Light.Managed.Database
{
    public static class GlobalLibraryCache
    {
        private static readonly IServiceScopeFactory _scopeFactory;
        public static DbAlbum[] CachedDbAlbum;
        public static DbArtist[] CachedDbArtist;
        public static DbMediaFile[] CachedDbMediaFile;
        public static TrieTreeNode<char, DbAlbum> AlbumSearchTree;
        public static TrieTreeNode<char, DbArtist> ArtistSearchTree;
        public static TrieTreeNode<char, DbMediaFile> FileSearchTree;

        static GlobalLibraryCache()
        {
            _scopeFactory = ApplicationServiceBase.App
                .ApplicationServices.GetRequiredService<IServiceScopeFactory>();
        }

        public static void Invalidate()
        {
            CachedDbAlbum = null;
            AlbumSearchTree = null;
            CachedDbArtist = null;
            ArtistSearchTree = null;
            CachedDbMediaFile = null;
            FileSearchTree = null;
        }

        public static async Task LoadAlbumAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider
                .GetRequiredService<MedialibraryDbContext>())
            {
                await Task.Run(() =>
                {
                    var albums = context.Albums.ToArray();
                    AlbumSearchTree = albums.ToTrieTree(x => x.Title, new CaseInsensitiveCharComparer());
                    CachedDbAlbum = albums;
                });
            }
        }

        public static async Task LoadArtistAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider
                .GetRequiredService<MedialibraryDbContext>())
            {
                await Task.Run(() =>
                {
                    var artists = context.Artists.ToArray();
                    ArtistSearchTree = artists.ToTrieTree(x => x.Name, new CaseInsensitiveCharComparer());
                    CachedDbArtist = artists;
                });
            }
        }

        public static async Task LoadMediaAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider
                .GetRequiredService<MedialibraryDbContext>())
            {
                await Task.Run(() =>
                {
                    var files = context.MediaFiles.ToArray();
                    FileSearchTree = files.ToTrieTree(x => x.Title, new CaseInsensitiveCharComparer());
                    CachedDbMediaFile = files;
                });
            }
        }
    }
}
