using Light.Managed.Database.Constant;
using Light.Managed.Database.Entities;
using Light.Managed.Online.Groove.Model;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Light.Managed.Online.Groove
{
    /// <summary>
    /// Provides artist metadata with Microsoft Groove's data source.
    /// </summary>
    public class GrooveMusicMetadata
    {

        private const string ApiPrefix = "https://music.xboxlive.com/1/content/";

        protected const string AppId = "00000000441D6B08";
        protected const string AppKey = "5paMwwKwWeiTGgOnfyF19RV";
        protected const string AppServiceAuthEndpoint = "https://login.live.com/accesstoken.srf";
        protected const string AppScope = "app.music.xboxlive.com";
        protected const string AppGrantType = "client_credentials";
        protected static string AuthKey = string.Empty;

        protected static DateTimeOffset LastTimeGetAuthKey;
        protected static TimeSpan ValidIn = TimeSpan.Zero;

        private static readonly MetadataQueryCache<GrooveMusicMetadata, DbArtist> QueryArtistImageCache =
            new MetadataQueryCache<GrooveMusicMetadata, DbArtist>("ArtistImage");
        private static readonly MetadataQueryCache<GrooveMusicMetadata, DbArtist> QueryArtistBioCache =
            new MetadataQueryCache<GrooveMusicMetadata, DbArtist>("ArtistBio");
        private static readonly MetadataQueryCache<GrooveMusicMetadata, DbAlbum> QueryAlbumImageCache =
            new MetadataQueryCache<GrooveMusicMetadata, DbAlbum>("AlbumImage");

        /// <summary>
        /// Indicates whether re-authentication is required.
        /// </summary>
        internal static bool RequireReAuth
            => ValidIn == TimeSpan.Zero
            || (DateTimeOffset.Now - LastTimeGetAuthKey).TotalSeconds >= ValidIn.TotalSeconds;

        /// <summary>
        /// Authenticate client to Xbox Music(Groove Music) service asynchronously.
        /// </summary>
        /// <returns>Task represents the asynchronous operation.</returns>
        internal static async Task<bool> AuthenticateAsync()
        {
            var requestAuthData = new AuthRequest(AppId, AppKey, AppScope, AppGrantType);

            using (var httpClient = new HttpClient())
            using (var hcFormContent = new FormUrlEncodedContent(requestAuthData.ToDictionary()))
            using (var hcResponse = await httpClient.PostAsync(new Uri(AppServiceAuthEndpoint), hcFormContent))
            {
                try
                {
                    if (hcResponse.IsSuccessStatusCode)
                    {
                        var strResponseContent = await hcResponse.Content.ReadAsStringAsync();
                        var responseEntity = JsonConvert.DeserializeObject<AzureAuthKey>(strResponseContent);

                        ValidIn = TimeSpan.FromSeconds(responseEntity.ExpiresInSecs);
                        LastTimeGetAuthKey = DateTimeOffset.Now;
                        AuthKey = responseEntity.AccessToken;

                        return true;
                    }
                }
                catch (HttpRequestException)
                {
                    // Ignore network errors
                }
            }

            return false;
        }

        /// <summary>
        /// Ensure client is authenticated, or re-authenticate if necessary.
        /// </summary>
        /// <returns>Task represents the asynchronous operation.</returns>
        internal static async Task EnsureAuthenticatedAsync()
        {
            if (RequireReAuth) await AuthenticateAsync();
        }

        /// <summary>
        /// Get a list of artists by given word asynchronously.
        /// </summary>
        /// <param name="artist">Partial of full artist name.</param>
        /// <returns>Array of <see cref="IEntityInfo"/>.</returns>
        public static async Task<IEntityInfo[]> GetArtistsAsync(string artist)
        {
            if (string.IsNullOrEmpty(artist) || Banlist.ArtistMetadataRetrieveBanlist.ContainsKey(artist)) return new IEntityInfo[0];
            await EnsureAuthenticatedAsync();
            var strRequestUrl = $"{ApiPrefix}music/search?q={WebUtility.UrlEncode(artist)}&accessToken=Bearer+{WebUtility.UrlEncode(AuthKey)}";

            using (var httpClient = new HttpClient())
            using (var hcResponse = await httpClient.GetAsync(strRequestUrl))
            {
                try
                {
                    if (!hcResponse.IsSuccessStatusCode) return new IEntityInfo[0];

                    var gcgResponse = JsonConvert.DeserializeObject<GrooveContentResponse>
                        (await hcResponse.Content.ReadAsStringAsync());

                    if (gcgResponse.Artists?.Items != null) return gcgResponse.Artists.Items.ToArray();
                }
                catch
                {
                    // Ignore network errors
                }
            }

            return new IEntityInfo[0];
        }

        /// <summary>
        /// Get a list of albums by given title asynchronously.
        /// </summary>
        /// <param name="albumTitle">Partial of full album name.</param>
        /// <returns>Array of <see cref="IEntityInfo"/>.</returns>
        public static async Task<IEntityInfo[]> GetAlbumsAsync(string albumTitle)
        {
            if (string.IsNullOrEmpty(albumTitle) || Banlist.AlbumMetadataRetrieveBanlist.ContainsKey(albumTitle)) return new IEntityInfo[0];
            await EnsureAuthenticatedAsync();

            var strRequestUrl = $"{ApiPrefix}music/search?q={WebUtility.UrlEncode(albumTitle)}&accessToken=Bearer+{WebUtility.UrlEncode(AuthKey)}";

            using (var httpClient = new HttpClient())
            using (var hcResponse = await httpClient.GetAsync(strRequestUrl))
            {
                try
                {
                    if (!hcResponse.IsSuccessStatusCode) return new IEntityInfo[0];

                    var gcgResponse = JsonConvert.DeserializeObject<GrooveContentResponse>(await hcResponse.Content.ReadAsStringAsync());
                    if (gcgResponse?.Albums?.Items != null) return gcgResponse.Albums.Items.ToArray();
                }
                catch
                {
                    // Ignore
                }
            }

            return new IEntityInfo[0];
        }

        /// <summary>
        /// Get artist cover image's link asynchronously.
        /// </summary>
        /// <param name="artist">Name of the artist.</param>
        /// <returns>Task represents the asynchronous operation.</returns>
        public static async Task<string> GetArtistImageUrl(string artist)
        {
            if (string.IsNullOrEmpty(artist) || Banlist.ArtistMetadataRetrieveBanlist.ContainsKey(artist)) return string.Empty;
            if (QueryArtistImageCache.ContainsKey(artist)) return QueryArtistImageCache.Get(artist);

            await EnsureAuthenticatedAsync();

            var strRequestUrl = $"{ApiPrefix}music/search?q={WebUtility.UrlEncode(artist)}&accessToken=Bearer+{WebUtility.UrlEncode(AuthKey)}";

            using (var httpClient = new HttpClient())
            using (var hcResponse = await httpClient.GetAsync(strRequestUrl))
            {
                try
                {
                    if (!hcResponse.IsSuccessStatusCode) return string.Empty;

                    var gcgResponse = JsonConvert.DeserializeObject<GrooveContentResponse>(await hcResponse.Content.ReadAsStringAsync());
                    var olArtist = gcgResponse?.Artists?.Items?.First();

                    if (olArtist != null)
                    {
                        QueryArtistImageCache.Add(artist, olArtist.ImageUrl);
                        return olArtist.ImageUrl;
                    }
                }
                catch (HttpRequestException)
                {
                    // Ignore, assume network error
                }
                catch (JsonException)
                {
                    // Ignore, assume response error
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Get artist biography by Groove ID asynchronously.
        /// </summary>
        /// <param name="grooveCatalogId">Groove catalog ID.</param>
        /// <returns>Task represents the asynchronous operation.</returns>
        public static async Task<string> GetArtistBioByGrooveId(string grooveCatalogId)
        {
            if (string.IsNullOrEmpty(grooveCatalogId)) return string.Empty;
            if (QueryArtistBioCache.ContainsKey(grooveCatalogId)) return QueryArtistBioCache.Get(grooveCatalogId);

            await EnsureAuthenticatedAsync();

            var strRequestUrl = $"{ApiPrefix}{grooveCatalogId}/lookup?accessToken=Bearer+{WebUtility.UrlEncode(AuthKey)}";

            using (var httpClient = new HttpClient())
            using (var hcResponse = await httpClient.GetAsync(strRequestUrl))
            {
                try
                {
                    if (!hcResponse.IsSuccessStatusCode) return string.Empty;

                    var gcgResponse = JsonConvert.DeserializeObject<GrooveContentResponse>(await hcResponse.Content.ReadAsStringAsync());
                    var gaReturnEntity = gcgResponse.Artists?.Items?.First();

                    if (!string.IsNullOrEmpty(gaReturnEntity?.Biography))
                    {
                        QueryArtistBioCache.Add(grooveCatalogId, gaReturnEntity.Biography);
                        QueryArtistBioCache.Add(gaReturnEntity.Name, gaReturnEntity.Biography);
                        return gaReturnEntity.Biography;
                    }
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

        /// <summary>
        /// Get artist biography by name asynchronously.
        /// </summary>
        /// <param name="artist">Artist name.</param>
        /// <returns>Task represents the asynchronous operation.</returns>
        public static async Task<string> GetArtistBio(string artist)
        {
            if (string.IsNullOrEmpty(artist) || Banlist.ArtistMetadataRetrieveBanlist.ContainsKey(artist)) return string.Empty;
            if (QueryArtistBioCache.ContainsKey(artist)) return QueryArtistBioCache.Get(artist);

            await EnsureAuthenticatedAsync();

            var strRequestUrl = $"{ApiPrefix}music/search?q={WebUtility.UrlEncode(artist)}&accessToken=Bearer+{WebUtility.UrlEncode(AuthKey)}";

            using (var httpClient = new HttpClient())
            using (var hcResponse = await httpClient.GetAsync(strRequestUrl))
            {
                try
                {
                    if (!hcResponse.IsSuccessStatusCode) return string.Empty;
                    var gcgResponse = JsonConvert.DeserializeObject<GrooveContentResponse>(await hcResponse.Content.ReadAsStringAsync());

                    GrooveArtist gaEntity = gcgResponse?.Artists?.Items?.First();
                    if (gaEntity != null) return await GetArtistBioByGrooveId(gaEntity.Id);
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

        /// <summary>
        /// Get album metadata by title asynchronously.
        /// </summary>
        /// <param name="albumTitle">Album title.</param>
        /// <returns>Task represents the asynchronous operation.</returns>
        public static async Task<GrooveAlbum> GetAlbumMetadataAsync(string albumTitle)
        {
            if (string.IsNullOrEmpty(albumTitle) || Banlist.AlbumMetadataRetrieveBanlist.ContainsKey(albumTitle)) return null;
            await EnsureAuthenticatedAsync();

            var strRequestUrl = $"{ApiPrefix}music/search?q={WebUtility.UrlEncode(albumTitle)}&accessToken=Bearer+{WebUtility.UrlEncode(AuthKey)}";

            using (var httpClient = new HttpClient())
            using (var hcResponse = await httpClient.GetAsync(strRequestUrl))
            {
                try
                {
                    if (!hcResponse.IsSuccessStatusCode) return null;
                    var gcgEntity = JsonConvert.DeserializeObject<GrooveContentResponse>(await hcResponse.Content.ReadAsStringAsync());

                    return gcgEntity.Albums?.Items?.First();
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

            return null;
        }

        /// <summary>
        /// Get album cover image link asynchronously.
        /// </summary>
        /// <param name="albumTitle"></param>
        /// <returns>Task represents the asynchronous operation.</returns>
        public static async Task<string> GetAlbumImageLinkAsync(string albumTitle)
        {
            if (string.IsNullOrEmpty(albumTitle)) return string.Empty;
            if (QueryAlbumImageCache.ContainsKey(albumTitle)) return QueryAlbumImageCache.Get(albumTitle);

            var album = await GetAlbumMetadataAsync(albumTitle);

            if (!string.IsNullOrEmpty(album?.ImageUrl))
            {
                var imageUrl = $"{album.ImageUrl}&w={DatabaseConstants.ResizedSize}&h={DatabaseConstants.ResizedSize}";
                QueryAlbumImageCache.Add(albumTitle, imageUrl);

                return imageUrl;
            }

            return string.Empty;
        }
    }
}