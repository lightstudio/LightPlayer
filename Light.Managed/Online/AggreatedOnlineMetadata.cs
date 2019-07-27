using System;
using System.Threading.Tasks;
using Windows.Storage;
using Light.Managed.Online.Apple;
using Light.Managed.Online.Groove;
using Light.Managed.Online.LastFm;
using Windows.Networking.Connectivity;
using System.Linq;

namespace Light.Managed.Online
{
    public static class AggreatedOnlineMetadata
    {
        private static readonly ApplicationDataContainer Container
            = ApplicationData.Current.LocalSettings.CreateContainer(nameof(AggreatedOnlineMetadata),
                ApplicationDataCreateDisposition.Always);

        public static bool IsEnabled
        {
            get { return (bool)Container.Values[nameof(IsEnabled)]; }
            set { Container.Values[nameof(IsEnabled)] = value; }
        }
        public static bool EnableUnderMeteredNetwork
        {
            get { return (bool)Container.Values[nameof(EnableUnderMeteredNetwork)]; }
            set { Container.Values[nameof(EnableUnderMeteredNetwork)] = value; }
        }
        public static bool Availability
        {
            get
            {
                if (!IsEnabled)
                {
                    return false;
                }

                if (EnableUnderMeteredNetwork)
                {
                    return true;
                }

                var cost = NetworkInformation.GetInternetConnectionProfile()?.GetConnectionCost();
                if (cost == null ||
                    cost.NetworkCostType == NetworkCostType.Fixed ||
                    cost.NetworkCostType == NetworkCostType.Variable)
                {
                    return false;
                }
                return true;
            }
        }
        public static string AppleMusicProviderMkrt
        {
            get { return (string)Container.Values[nameof(AppleMusicProviderMkrt)]; }
            set { Container.Values[nameof(AppleMusicProviderMkrt)] = value; }
        }

        public static void InitializeSettings()
        {
            Container.Values.Clear();
            Container.Values.Add(nameof(IsEnabled), true);
            Container.Values.Add(nameof(EnableUnderMeteredNetwork), false);
            Container.Values.Add(nameof(AppleMusicProviderMkrt), "ja-JP");
        }

        public static async Task<string> GetArtistImageUrlAsync(string artist)
        {
            var urlToReturn = string.Empty;

            if (Availability)
            {
                if (!string.IsNullOrEmpty(artist) && !Banlist.ArtistMetadataRetrieveBanlist.ContainsKey(artist))
                {
                    try
                    {
                        urlToReturn = await GrooveMusicMetadata.GetArtistImageUrl(artist);
                        if (string.IsNullOrEmpty(urlToReturn))
                            urlToReturn = await LastFmArtistMetadata.GetArtistImageUrlAsync(artist);
                    }
                    catch (Exception)
                    {
                        // Typically that should not happen.
                        // Investigate if that happens.
                    }
                }
            }

            return urlToReturn;
        }

        public static async Task<string> GetAlbumImageImageUrlAsync(string album)
        {
            var urlToReturn = string.Empty;

            if (Availability)
            {
                if (!string.IsNullOrEmpty(album) && !Banlist.AlbumMetadataRetrieveBanlist.ContainsKey(album))
                {
                    try
                    {
                        urlToReturn = await GrooveMusicMetadata.GetAlbumImageLinkAsync(album);
                        if (string.IsNullOrEmpty(urlToReturn))
                            urlToReturn = await AppleMusicMetadata.GetAlbumImageImageUrlAsync(album);
                    }
                    catch (Exception)
                    {
                        // Typically that should not happen.
                        // Investigate if that happens.
                    }
                }
            }

            return urlToReturn;
        }

        public static async Task<IEntityInfo[]> GetAlbumsAsync(string album, string artist)
        {
            var albums = (await Task.WhenAll(
                AppleMusicMetadata.GetAlbumsAsync($"{album} {artist}"),
                GrooveMusicMetadata.GetAlbumsAsync($"{album} {artist}")))
                .SelectMany(x => x)
                .ToList();
            albums.Sort(new AlbumSimilarityComparer(album, artist));
            return albums.ToArray();
        }

        public static async Task<IEntityInfo[]> GetArtistsAsync(string artist)
        {
            var artists = (await Task.WhenAll(
                GrooveMusicMetadata.GetArtistsAsync(artist),
                LastFmArtistMetadata.GetArtistsAsync(artist)))
                .SelectMany(x => x)
                .ToList();
            artists.Sort(new ArtistSimilarityComparer(artist));
            return artists.ToArray();
        }
    }
}
