using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using MusicBird.Common;
using System.Globalization;

namespace MusicBirdTest
{
    [TestClass]
    public class BytesMegabytesConverterTest
    {
        [TestMethod]
        public void ConvertWithNullValueReturnsNullMB()
        {
            var converter = new BytesMegabytesConverter();
            var value = converter.Convert(null, typeof(string), null, CultureInfo.CurrentCulture.DisplayName);
            Assert.AreEqual("0 MB", value);
        }

        [TestMethod]
        public void ConvertWith12Returns12MB()
        {
            var converter = new BytesMegabytesConverter();
            var value = converter.Convert(12*1024*1024, typeof(string), null, CultureInfo.CurrentCulture.DisplayName);
            Assert.AreEqual("12 MB", value);
        }

        [TestMethod]
        public void ConvertWith120Returns120MB()
        {
            var converter = new BytesMegabytesConverter();
            var value = converter.Convert(120 * 1024 * 1024, typeof(string), null, CultureInfo.CurrentCulture.DisplayName);
            Assert.AreEqual("120 MB", value);
        }

        [TestMethod]
        public void ConvertBackWithNullReturnsNull()
        {
            var converter = new BytesMegabytesConverter();
            var value = converter.ConvertBack(null, typeof(double), null, CultureInfo.CurrentCulture.DisplayName);
            Assert.AreEqual(0.0, value);
        }

        [TestMethod]
        public void ConvertBackWith12MBReturns12()
        {
            var converter = new BytesMegabytesConverter();
            var value = converter.ConvertBack("12 MB", typeof(double), null, CultureInfo.CurrentCulture.DisplayName);
            Assert.AreEqual(12*1024*1024+0.0, value);
        }
    }
}



