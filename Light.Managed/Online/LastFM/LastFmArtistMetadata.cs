using Light.Managed.Database.Entities;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Light.Managed.Online.LastFm
{
    /// <summary>
    /// Provides artist metadata with Last.fm's data source.
    /// </summary>
    public class LastFmArtistMetadata
    {

        private static readonly MetadataQueryCache<LastFmArtistMetadata, DbArtist> QueryCache = 
            new MetadataQueryCache<LastFmArtistMetadata, DbArtist>("ArtistImage");

        // Fix by case: 3 Idiots (Bug #463)
        private const string LastFmStubBio = "This is not an artist";

        /// <summary>
        /// Fix by case: 3 Idiots (Bug #463) Check whether the given entity is a stub entity.
        /// </summary>
        /// <param name="entity">Instance of <see cref="LastFmArtistEntity"/>.</param>
        /// <returns>Whether this is a stub entity or not.</returns>
        /// <remarks>
        /// It seems that the only way to identify non-artist entities from Last.fm is checking bio.
        /// Because checking image URL is not always reliable.
        /// Bio starts with "This is not an artist".
        /// </remarks>
        private static bool DetermineStubFromBio(LastFmArtistEntity entity)
        {
            // Will not perform null check on main entity because caller path is determined
            var bioRef = entity.Artist?.Bio?.Content;
            if (bioRef != null && bioRef.StartsWith(LastFmStubBio)) return true;

            return false;
        }

        /// <summary>
        /// Get artist metadata information from Last.fm asynchronously.
        /// </summary>
        /// <param name="artist">Name of the artist.</param>
        /// <returns>Task represents the operation.</returns>
        public static async Task<IEntityInfo[]> GetArtistsAsync(string artist)
        {
            if (string.IsNullOrEmpty(artist) || Banlist.ArtistMetadataRetrieveBanlist.ContainsKey(artist))
                return new IEntityInfo[0];

            var requestUrl = $"https://ws.audioscrobbler.com/2.0/?method={ArtistGetInfoVerb}" +
                $"&api_key={AppId}&artist={WebUtility.UrlEncode(artist)}&autocorrect=1&format=json";

            using (var httpClient = new HttpClient())
            using (var serverQueryResponse = await httpClient.GetAsync(new Uri(requestUrl)))
            {
                try
                {
                    if (!serverQueryResponse.IsSuccessStatusCode) return new IEntityInfo[0];

                    var artistInfoString = await serverQueryResponse.Content.ReadAsStringAsync();
                    var responseEntity = JsonConvert.DeserializeObject<LastFmArtistEntity>(artistInfoString);
                    var imageCollection = responseEntity?.Artist?.Image;

                    if (imageCollection == null || responseEntity.Artist.Thumbnail == null || DetermineStubFromBio(responseEntity))
                    {
                        return new IEntityInfo[0];
                    }

                    return new IEntityInfo[] { responseEntity.Artist };
                }
                catch
                {
                    // Ignore, assume network error
                }
            }

            return new IEntityInfo[0];
        }

        /// <summary>
        /// Get artist cover image link from Last.fm asynchronously.
        /// </summary>
        /// <param name="artist">Artist name.</param>
        /// <returns>Task represents the operation.</returns>
        public static async Task<string> GetArtistImageUrlAsync(string artist)
        {
            if (string.IsNullOrEmpty(artist) || Banlist.ArtistMetadataRetrieveBanlist.ContainsKey(artist))
                return string.Empty;

            if (QueryCache.ContainsKey(artist))
                return QueryCache.Get(artist);

            var requestUrl = $"https://ws.audioscrobbler.com/2.0/?method={ArtistGetInfoVerb}" +
                $"&api_key={AppId}&artist={WebUtility.UrlEncode(artist)}&autocorrect=1&format=json";

            using (var httpClient = new HttpClient())
            using (var serverQueryResponse = await httpClient.GetAsync(new Uri(requestUrl)))
            {
                try
                {
                    if (!serverQueryResponse.IsSuccessStatusCode) return string.Empty;

                    var artistInfoString = await serverQueryResponse.Content.ReadAsStringAsync();
                    var responseEntity = JsonConvert.DeserializeObject<LastFmArtistEntity>(artistInfoString);
                    var imageCollection = responseEntity?.Artist?.Image;

                    if (imageCollection == null || DetermineStubFromBio(responseEntity)) return string.Empty;
                    imageCollection.Sort();

                    var imageUrl = imageCollection.Last()?.Text;
                    if (!string.IsNullOrEmpty(imageUrl)) QueryCache.Add(artist, imageUrl);

                    return imageUrl;
                }
                catch (HttpRequestException)
                {
                    // Ignore, assume network error
                }
                catch (JsonException)
                {
                    // Ignore, assume content error
                }
            }

            return string.Empty;
        }

        #region Auth Defs

        protected const string AppId = "b4dfdb92b55c24e0dab9f94031d9343e";
        protected const string AppSecret = "d356c8c7ee13e796166b8964fc11ae52";
        protected const string AppTitle = "Light 2";
        protected const string ArtistGetInfoVerb = "artist.getInfo";

        #endregion
    }
}