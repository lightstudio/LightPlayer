
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Managed.Database.Entities
{
    public static class EntityRetrievalExtensions
    {

        /// <summary>
        /// Get a single album by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DbAlbum GetAlbumById(this int id)
        {
            DbAlbum album = null;

            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                var query = context.Albums
                    .Include(c => c.RelatedArtist)
                    .Include(c => c.MediaFiles)
                    .Where(i => i.Id == id);

                album = query.SingleOrDefault();
            }

            return album;
        }

        /// <summary>
        /// Get a single album by its ID asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<DbAlbum> GetAlbumByIdAsync(this int id)
        {
            DbAlbum album = null;

            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                var query = context.Albums
                    .Include(c => c.RelatedArtist)
                    .Include(c => c.MediaFiles)
                    .Where(i => i.Id == id);

                album = await query.SingleOrDefaultAsync();
            }

            return album;
        }

        public static DbMediaFile GetFileById(this int id)
        {
            DbMediaFile file = null;

            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider
                .GetRequiredService<MedialibraryDbContext>())
            {
                file = context.MediaFiles.Find(id);
            }

            return file;
        }

        public static async Task<DbMediaFile> GetFileByIdAsync(this int id)
        {
            DbMediaFile file = null;

            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider
                .GetRequiredService<MedialibraryDbContext>())
            {
                file = await context.MediaFiles.FindAsync(id);
            }

            return file;
        }

        public static DbArtist GetArtistById(this int id)
        {
            DbArtist artist = null;

            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider
                .GetRequiredService<MedialibraryDbContext>())
            {
                var query = context.Artists
                    .Include(c => c.MediaFiles)
                    .Include(c => c.Albums)
                    .Where(i => i.Id == id);

                artist = query.SingleOrDefault();
            }

            return artist;
        }

        public static async Task<DbArtist> GetArtistByIdAsync(this int id)
        {
            DbArtist artist = null;

            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider
                .GetRequiredService<MedialibraryDbContext>())
            {
                var query = context.Artists
                    .Include(c => c.MediaFiles)
                    .Include(c => c.Albums)
                    .Where(i => i.Id == id);

                artist = await query.SingleOrDefaultAsync();
            }

            return artist;
        }

        public static async Task<DbArtist> GetArtistByNameAsync(this string name)
        {
            DbArtist artist = null;

            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider
                .GetRequiredService<MedialibraryDbContext>())
            {
                var query = context.Artists
                    .Where(i => i.Name == name);

                artist = await query.FirstOrDefaultAsync();
            }

            return artist;
        }
    }
}
