using Light.Managed.Database.Entities;
using Light.Managed.Online;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace Light.Managed.Test.Online
{
    [TestClass]
    public class MetadataRetrieveBanlistTest
    {
        public const string TestAlbumTitle = "Test Album 1";

        [TestMethod]
        public void TestCreation()
        {
            var banlist = new MetadataRetrieveBanlist<DbMediaFile>();
        }

        [TestMethod]
        public void TestAdd()
        {
            var banlist = InternalAdd();
            Assert.AreEqual(true, banlist.ContainsKey(TestAlbumTitle));
        }

        private MetadataRetrieveBanlist<DbAlbum> InternalAdd()
        {
            var banlist = new MetadataRetrieveBanlist<DbAlbum>();
            banlist.Add(TestAlbumTitle);
            return banlist;
        }

        [TestMethod]
        public void TestRemove()
        {
            var banlist = InternalAdd();
            if (!banlist.ContainsKey(TestAlbumTitle))
            {
                Assert.Inconclusive();
            }
            else
            {
                banlist.Remove(TestAlbumTitle);
                Assert.AreEqual(false, banlist.ContainsKey(TestAlbumTitle));
            }
        }
    }
}