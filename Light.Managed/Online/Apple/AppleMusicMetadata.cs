using Light.Managed.Database.Constant;
using Light.Managed.Database.Entities;
using Light.Managed.Online.Apple.Model;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Light.Managed.Online.Apple
{
    /// <summary>
    /// Provides artist metadata with iTunes' data source.
    /// </summary>
    public class AppleMusicMetadata
    {

        private const string AlbumEntityId = "album";
        private static readonly MetadataQueryCache<AppleMusicMetadata, DbAlbum> QueryCache = 
            new MetadataQueryCache<AppleMusicMetadata, DbAlbum>("AlbumImage");

        /// <summary>
        /// Get album metadata information from iTunes asynchronously.
        /// </summary>
        /// <param name="albumTitle">Album title.</param>
        /// <param name="country">iTunes market.</param>
        /// <returns>Task represents the operation.<</returns>
        public static async Task<IEntityInfo[]> GetAlbumsAsync(string albumTitle, string country = "jp")
        {
            if (string.IsNullOrEmpty(albumTitle) || Banlist.AlbumMetadataRetrieveBanlist.ContainsKey(albumTitle))
            {
                return new IEntityInfo[0];
            }

            var requestUrl = $"https://itunes.apple.com/search?term={WebUtility.UrlEncode(albumTitle)}" +
                $"&entity={AlbumEntityId}&country={country}";

            try
            {
                using (var httpClient = new HttpClient())
                using (var serverQueryResponse = await httpClient.GetAsync(requestUrl))
                {
                    if (serverQueryResponse.IsSuccessStatusCode)
                    {
                        var parsedContent = JsonConvert.DeserializeObject<AppleMusicSearchResult>(
                            await serverQueryResponse.Content.ReadAsStringAsync());

                        return parsedContent.Results;
                    }
                }
            }
            catch
            {
                // Assume content error, ignore
            }

            return new IEntityInfo[0];
        }

        /// <summary>
        /// Get album cover information from iTunes asynchronously.
        /// </summary>
        /// <param name="albumTitle">Album title.</param>
        /// <param name="country">iTunes market.</param>
        /// <returns>Task represents the operation.<</returns>
        public static async Task<string> GetAlbumImageImageUrlAsync(string albumTitle, string country = "jp")
        {
            if (string.IsNullOrEmpty(albumTitle) || Banlist.AlbumMetadataRetrieveBanlist.ContainsKey(albumTitle))
                return string.Empty;

            if (QueryCache.ContainsKey(albumTitle))
                return QueryCache.Get(albumTitle);

            var requestUrl = $"https://itunes.apple.com/search?term={WebUtility.UrlEncode(albumTitle)}" +
                $"&entity={AlbumEntityId}&country={country}";

            try
            {
                using (var httpClient = new HttpClient())
                using (var serverQueryResponse = await httpClient.GetAsync(requestUrl))
                {
                    if (serverQueryResponse.IsSuccessStatusCode)
                    {
                        var parsedContent = JsonConvert.DeserializeObject<AppleMusicSearchResult>(
                            await serverQueryResponse.Content.ReadAsStringAsync());

                        if (parsedContent.ResultCount != 0)
                        {
                            var selectedEntity = parsedContent.Results[0];
                            if (!string.IsNullOrEmpty(selectedEntity?.ArtworkUrl100))
                            {
                                // Return resized result.
                                var trimedSrcUrl =
                                    selectedEntity.ArtworkUrl100.Substring(0,
                                        selectedEntity.ArtworkUrl100.LastIndexOf('/'));

                                var contentUrl =
                                    $"{trimedSrcUrl}/{DatabaseConstants.ResizedSize}x{DatabaseConstants.ResizedSize}bb.jpg";

                                QueryCache.Add(albumTitle, contentUrl);

                                return contentUrl;
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Assume content error, ignore
            }
            catch (HttpRequestException)
            {
                // Assume network error, ignore
            }

            return string.Empty;
        }
    }
}
