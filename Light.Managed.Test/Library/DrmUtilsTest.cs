using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Light.Managed.Library;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace Light.Managed.Test.Library
{
    [TestClass]
    public class DrmUtilsTest
    {
        private StorageFolder _folder;

        [TestInitialize]
        public async Task PrepGetFolderTask()
        {
            var testDataFolder = await Package.Current.InstalledLocation.GetFolderAsync("TestData");
            _folder = testDataFolder;
        }

        [TestMethod]
        public async Task TestDrmProtectedFile()
        {
            if (_folder == null)
            {
                Assert.Inconclusive();
            }
            else
            {
                var file = await _folder.GetFileAsync("01 People Watching.wma");
                Assert.AreEqual(true, await DrmUtils.RetrieveDrmStatus(file));
            }
        }

        [TestMethod]
        public async Task TestNonMusicFile()
        {
            if (_folder == null)
            {
                Assert.Inconclusive();
            }
            else
            {
                var file = await _folder.GetFileAsync("Every_Teardrop_Is_a_Waterfall.mscz");
                Assert.AreEqual(false, await DrmUtils.RetrieveDrmStatus(file));
            }
        }

        [TestMethod]
        public async Task TestNonDrmProtectedFile()
        {
            if (_folder == null)
            {
                Assert.Inconclusive();
            }
            else
            {
                var file = await _folder.GetFileAsync("06 22.mp3");
                Assert.AreEqual(false, await DrmUtils.RetrieveDrmStatus(file));
            }
        }
    }
}
