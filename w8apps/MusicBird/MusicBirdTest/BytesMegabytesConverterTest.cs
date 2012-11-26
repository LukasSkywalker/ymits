using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using MusicBird.Common;

namespace MusicBirdTest
{
    [TestClass]
    public class BytesMegabytesConverterTest
    {
        [TestMethod]
        public void TestZero()
        {
            BytesMegabytesConverter converter = new BytesMegabytesConverter();
            String val = (string)converter.Convert(0UL, null, null, null);
            Assert.AreEqual("0 B", val);
        }

        [TestMethod]
        public void HalfAndOneMB() {
            BytesMegabytesConverter converter = new BytesMegabytesConverter();
            String val = (string)converter.Convert(1024*512UL, null, null, null);
            Assert.AreEqual("512 KB", val);
            val = (string)converter.Convert(1024 * 1024UL, null, null, null);
            Assert.AreEqual("1 MB", val);
        }
    }
}
