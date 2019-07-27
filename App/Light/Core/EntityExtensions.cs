using System;
using System.Linq;
using System.Threading.Tasks;
using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.Managed.Online.Groove;
using Light.Managed.Tools;
using Light.Utilities.EntityComparer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Core
{
    /// <summary>
    /// General entity extensions.
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Parse integer with default fallback. Used for track ID and disk ID parsing.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static int ParseWithDefaultFallback(string s)
        {
            int r = default(int);
            int.TryParse(s, out r);
            return r;
        }

        /// <summary>
        /// Play an album.
        /// </summary>
        /// <param name="album"></param>
        /// <param name="requireClear"></param>
        /// <param name="isInsert"></param>
        public static async void Play(this DbAlbum album, bool requireClear = false, bool isInsert = false)
        {
            if (album?.MediaFiles != null)
            {
                var sortedFiles = album.MediaFiles
                    .OrderBy(file => ParseWithDefaultFallback(file.DiscNumber))
                    .ThenBy(file => ParseWithDefaultFallback(file.TrackNumber))
                    .Select(file => MusicPlaybackItem.CreateFromMediaFile(file));
                if (requireClear)
                    PlaybackControl.Instance.Clear();
                await PlaybackControl.Instance.AddFile(sortedFiles.ToList(), isInsert ? -2 : -1);
            }
        }

        /// <summary>
        /// Play all songs by a specific artist.
        /// </summary>
        /// <param name="artist"></param>
        /// <param name="requireClear"></param>
        /// <param name="isInsert"></param>
        public static async void Play(this DbArtist artist, bool requireClear = false, bool isInsert = false)
        {
            if (artist?.MediaFiles != null)
            {
                var sortedFiles = artist.MediaFiles
                    .OrderBy(file => file.Title, new AlphabetAscendComparer())
                    .Select(file => MusicPlaybackItem.CreateFromMediaFile(file));
                if (requireClear)
                    PlaybackControl.Instance.Clear();
                await PlaybackControl.Instance.AddFile(sortedFiles.ToList(), isInsert ? -2 : -1);
            }
        }

        public static async void Shuffle(this DbArtist artist)
        {
            if (artist?.MediaFiles != null)
            {
                var random = new Random();
                var shuffledFiles = artist.MediaFiles
                    .OrderBy(file => random.Next())
                    .Select(file => MusicPlaybackItem.CreateFromMediaFile(file));
                PlaybackControl.Instance.Clear();
                await PlaybackControl.Instance.AddFile(shuffledFiles.ToList(), -1);
            }
        }

        /// <summary>
        /// Load online information for a specific artist.
        /// </summary>
        /// <param name="artist"></param>
        /// <returns></returns>
        public static async Task<DbArtist> LoadOnlineArtistInfoAsync(this DbArtist artist)
        {
            if (artist == null)
            {
                throw new ArgumentNullException(nameof(artist));
            }

            if (InternetConnectivityDetector.HasInternetConnection)
            {
                try
                {
                    if (string.IsNullOrEmpty(artist.Intro))
                    {
                        artist = await artist.LoadArtistIntroAsync();
                    }

                    return artist;
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                    return artist;
                }
            }

            return artist;
        }

        /// <summary>
        /// Load online content for a specific artist.
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public static async Task<string> LoadArtistOnlineContentAsync(this DbArtist elem)
        {
            // Check internet connection
            if (InternetConnectivityDetector.HasInternetConnection)
            {
                // Return Online content
                try
                {
                    var modifyFlag = false;

                    if (string.IsNullOrEmpty(elem.Intro))
                    {
                        var bio = await GrooveMusicMetadata.GetArtistBio(elem.Name);

                        if (!string.IsNullOrEmpty(bio)) modifyFlag = true;
                    }

                    if (modifyFlag)
                    {
                        using (var scope = ApplicationServiceBase.App.GetScope())
                        using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                        {
                            context.Entry(elem).Entity.ImagePath = elem.ImagePath;
                            context.Entry(elem).Entity.Intro = elem.Intro;
                            context.Entry(elem).State = EntityState.Modified;
                            await context.SaveChangesAsync();
                        }

                        return elem.ImagePath;
                    }
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Load online content for a specific artist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<string> LoadArtistOnlineContentAsync(string name)
        {
            // Check internet connection and return
            return await LoadArtistOnlineContentAsync(
                await name.GetArtistByNameAsync());
        }

        /// <summary>
        /// Load artist intro for a specific artist.
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public static async Task<DbArtist> LoadArtistIntroAsync(this DbArtist elem)
        {
            if (InternetConnectivityDetector.HasInternetConnection)
            {
                try
                {
                    var bio = await GrooveMusicMetadata.GetArtistBio(elem.Name);
                    elem.Intro = bio;
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                }
            }

            return elem;
        }
    }
}
