using System.Threading.Tasks;
using Light.Managed.Online.LastFm;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace Light.Managed.Test.Online.LastFm
{
    [TestClass]
    public class LastFmArtistTest : LastFmArtistMetadata
    {
        [TestMethod]
        public async Task TestGetArtistImage()
        {
            var url = await GetArtistImageUrlAsync("High School Musical");
            Assert.AreEqual(false, string.IsNullOrEmpty(url));
        }
    }
}
