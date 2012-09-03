using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using MusicBird.Common;


namespace MusicBirdTest
{
    [TestClass]
    public class StringHelperTest
    {
        [TestMethod]
        public void TestGetArtistAndTitle()
        {
            string[] data = StringHelper.getArtistAndTitle("lady gaga - telephone");
            Assert.AreEqual("Lady Gaga", data[0]);
            Assert.AreEqual("Telephone", data[1] );
        }

        [TestMethod]
        public void TestReplaceNumbersAndExtension()
        {
            string data = StringHelper.replaceNumbersAndExtension("04 lady gaga - telephone .mp3");
            Assert.AreEqual("lady gaga - telephone", data);

            data = StringHelper.replaceNumbersAndExtension("04 lady gaga - telephone.mp3");
            Assert.AreEqual("lady gaga - telephone", data);

            data = StringHelper.replaceNumbersAndExtension("04lady gaga - telephone .mp3");
            Assert.AreEqual("lady gaga - telephone", data);
        }

        [TestMethod]
        public void TestUppercaseWords()
        {
            string data = StringHelper.UppercaseWords("lady gaga - telephone");
            Assert.AreEqual("Lady Gaga - Telephone", data);

            data = StringHelper.UppercaseWords("the killers Human");
            Assert.AreEqual("The Killers Human", data);

            data = StringHelper.UppercaseWords("Whatsthestory");
            Assert.AreEqual("Whatsthestory", data);
        }
    }
}
