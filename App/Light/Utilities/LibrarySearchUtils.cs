using Light.Managed.Database;
using Light.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Utilities
{
    public static class LibrarySearchUtils
    {
        public static async Task LoadLibraryCacheAsync()
        {
            if (GlobalLibraryCache.CachedDbAlbum == null)
            {
                await GlobalLibraryCache.LoadAlbumAsync();
            }
            if (GlobalLibraryCache.CachedDbArtist == null)
            {
                await GlobalLibraryCache.LoadArtistAsync();
            }
            if (GlobalLibraryCache.CachedDbMediaFile == null)
            {
                await GlobalLibraryCache.LoadMediaAsync();
            }
        }
        public static void UpdateSuggestions(string keyword, ObservableCollection<SearchResultModel> suggestions)
        {
            var count = 0;
            IEnumerable<SearchResultModel> q = Enumerable.Empty<SearchResultModel>();

            var albums = GlobalLibraryCache.AlbumSearchTree?.Lookup(((IEnumerable<char>)keyword).GetEnumerator());
            if (albums != null)
            {
                q = q.Concat(albums.GetChildValues().Select(x => new SearchResultModel(x)).Take(10 - count));
                count += Math.Min(10 - count, albums.ChildrenValueCount);
            }

            if (count < 10)
            {
                var artists = GlobalLibraryCache.ArtistSearchTree?.Lookup(((IEnumerable<char>)keyword).GetEnumerator());
                if (artists != null)
                {
                    q = q.Concat(artists.GetChildValues().Select(x => new SearchResultModel(x)).Take(10 - count));
                    count += Math.Min(10 - count, artists.ChildrenValueCount);
                }
            }

            if (count < 10)
            {
                var files = GlobalLibraryCache.FileSearchTree?.Lookup(((IEnumerable<char>)keyword).GetEnumerator());
                if (files != null)
                {
                    q = q.Concat(files.GetChildValues().Select(x => new SearchResultModel(x)).Take(10 - count));
                    count += Math.Min(10 - count, files.ChildrenValueCount);
                }
            }

            var results = q.ToList();
            foreach (var item in results)
            {
                if (!suggestions.Contains(item))
                {
                    suggestions.Add(item);
                }
            }
            foreach (var item in suggestions.ToArray())
            {
                if (!results.Contains(item))
                {
                    suggestions.Remove(item);
                }
            }
        }
    }
}
