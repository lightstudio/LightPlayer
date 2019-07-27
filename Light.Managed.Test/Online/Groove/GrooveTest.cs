using System;
using System.Threading.Tasks;
using Light.Managed.Online;
using Light.Managed.Online.Groove;
using Light.Managed.Online.Groove.Model;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace Light.Managed.Test.Online.Groove
{
    [TestClass]
    public class GrooveMetadataTest : GrooveMusicMetadata
    {
        public const string BlackBoxByAtcAlbumTitle = "Black Box";
        public const string UnknownAlbumTitle = "Unknown Album";
        public const string AirTrafficControllerName = "Air Traffic Controller";
        public const string TstExcludedGroupOfAyaseEliNishikinoMakiAndYazawaNicoName = "Bibi";

        [TestMethod]
        public void TestEmptyCredentials()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                var authRequest = new AuthRequest();
                var result = authRequest.ToDictionary();
            });
        }

        [TestMethod]
        public void TestNonEmptyCredentials()
        {
            var requestAuthData = new AuthRequest(AppId, AppKey, AppScope, AppGrantType);
            var result = requestAuthData.ToDictionary();
            Assert.AreEqual(AppScope, result["scope"]);
        }

        [TestMethod]
        public async Task TestAuthentication()
        {
            if (await AuthenticateAsync())
            {
                Assert.AreEqual(false, string.IsNullOrEmpty(AuthKey));
                Assert.AreEqual(false, RequireReAuth);
            }
            else
            {
                Assert.Inconclusive();
            }
        }

        [TestMethod]
        public async Task TestReAuth()
        {
            if (await AuthenticateAsync())
            {
                if (!string.IsNullOrEmpty(AuthKey))
                {
                    await EnsureAuthenticatedAsync();
                    return;
                }
            }

            Assert.Inconclusive();
        }

        [TestMethod]
        public async Task TestGetArtistImageUrl()
        {
            var artistImageUrl = await GetArtistImageUrl(AirTrafficControllerName);
            Assert.AreEqual(false, string.IsNullOrEmpty(artistImageUrl));

            Banlist.ArtistMetadataRetrieveBanlist.Add(
                TstExcludedGroupOfAyaseEliNishikinoMakiAndYazawaNicoName);
            Assert.AreEqual(true, string.IsNullOrEmpty(await GetArtistImageUrl(
                TstExcludedGroupOfAyaseEliNishikinoMakiAndYazawaNicoName)));
            Banlist.ArtistMetadataRetrieveBanlist.Remove(
                TstExcludedGroupOfAyaseEliNishikinoMakiAndYazawaNicoName);
        }

        [TestMethod]
        public async Task TestGetArtistBio()
        {
            var artistBio = await GetArtistBio(AirTrafficControllerName);
            Assert.AreEqual(false, string.IsNullOrEmpty(artistBio));

            Banlist.ArtistMetadataRetrieveBanlist.Add(
                TstExcludedGroupOfAyaseEliNishikinoMakiAndYazawaNicoName);
            Assert.AreEqual(true, string.IsNullOrEmpty(await GetArtistBio(
                TstExcludedGroupOfAyaseEliNishikinoMakiAndYazawaNicoName)));
            Banlist.ArtistMetadataRetrieveBanlist.Remove(
                TstExcludedGroupOfAyaseEliNishikinoMakiAndYazawaNicoName);
        }

        [TestMethod]
        public async Task TestGetAlbum()
        {
            var album = await GetAlbumMetadataAsync($"{BlackBoxByAtcAlbumTitle} {AirTrafficControllerName}");
            Assert.AreNotEqual(null, album);

            Banlist.AlbumMetadataRetrieveBanlist.Add(UnknownAlbumTitle);
            Assert.AreEqual(null, await GetAlbumMetadataAsync(UnknownAlbumTitle));
            Banlist.AlbumMetadataRetrieveBanlist.Remove(UnknownAlbumTitle);
        }

        [TestMethod]
        public async Task TestGetAlbumCoverUrl()
        {
            var albumCoverUrl = await GetAlbumImageLinkAsync("Black Box Air Traffic Controller");
            Assert.AreEqual(false, string.IsNullOrEmpty(albumCoverUrl));

            Banlist.AlbumMetadataRetrieveBanlist.Add(UnknownAlbumTitle);
            Assert.AreEqual(true, string.IsNullOrEmpty(await GetAlbumImageLinkAsync(UnknownAlbumTitle)));
            Banlist.AlbumMetadataRetrieveBanlist.Remove(UnknownAlbumTitle);
        }
    }
}
