using System.Threading.Tasks;
using Light.Managed.Online;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace Light.Managed.Test.Online
{
    [TestClass]
    public class AggreatedOnlineMetadataTest
    {
        [TestMethod]
        public async Task TestGetAggreatedMetadata()
        {
            var artistImage = await AggreatedOnlineMetadata.GetArtistImageUrlAsync("μ's");
            // Last.FM
            Assert.AreEqual(false, string.IsNullOrEmpty(artistImage));

            artistImage = await AggreatedOnlineMetadata.GetArtistImageUrlAsync("American Authors");
            // Groove
            Assert.AreEqual(false, string.IsNullOrEmpty(artistImage));

            // Nobody
            artistImage = await AggreatedOnlineMetadata.GetArtistImageUrlAsync("This artist is absolutely not exist");
            Assert.AreEqual(true, string.IsNullOrEmpty(artistImage));
        }

        [TestMethod]
        public async Task TestGetAggreatedAlbumUrl()
        {
            var albumImage = await AggreatedOnlineMetadata.GetAlbumImageImageUrlAsync("Mylo Xyloto");
            // Groove
            Assert.AreEqual(false, string.IsNullOrEmpty(albumImage));

            // Apple Music
            albumImage = await AggreatedOnlineMetadata.GetAlbumImageImageUrlAsync("Start Dash");
            Assert.AreEqual(false, string.IsNullOrEmpty(albumImage));

            // Nobody
            albumImage = await AggreatedOnlineMetadata.GetAlbumImageImageUrlAsync("This album is absolutely not exist");
            Assert.AreEqual(true, string.IsNullOrEmpty(albumImage));
        }
    }
}
