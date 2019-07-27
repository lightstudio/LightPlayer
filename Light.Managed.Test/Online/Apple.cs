using System.Diagnostics;
using System.Threading.Tasks;
using Light.Managed.Online.Apple;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace Light.Managed.Test.Online
{
    [TestClass]
    public class Apple : AppleMusicMetadata
    {
        [TestMethod]
        public async Task TestGetAlbumCoverUrl()
        {
            var albumCoverUrl = await GetAlbumImageImageUrlAsync("Hello Alone");
            Assert.AreEqual(false, string.IsNullOrEmpty(albumCoverUrl));
            Debug.WriteLine(albumCoverUrl);
        }
    }
}
