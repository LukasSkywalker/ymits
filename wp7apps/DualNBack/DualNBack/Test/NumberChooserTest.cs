using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DualNBack.Test
{
    [TestClass]
    public class NumberChooserTest : SilverlightTest
    {

        [TestMethod]
        [Description("Initialize stack with size 3")]
        public void init()
        {
            int n = 3;
            NumberChooser soundChooser = new NumberChooser(n);
            NumberChooser positionChooser = new NumberChooser(n);
            Assert.IsNotNull(soundChooser);
            Assert.IsNotNull(positionChooser);
        }

        [TestMethod]
        [Description("")]
        public void soundShouldBeInBounds() {
            NumberChooser soundChooser = new NumberChooser(3);
            for(int i = 0 ; i < 20 ; i++)
            {
                int sound = soundChooser.getNew();
                Assert.IsTrue(sound > 0 && sound < 10);
            }
        }

        [TestMethod]
        [Description("")]
        public void positionShouldBeInBounds()
        {
            NumberChooser positionChooser = new NumberChooser(3);
            for(int i = 0 ; i < 20 ; i++)
            {
                int position = positionChooser.getNew();
                Assert.IsTrue(position > 0 && position < 10);
            }
        }
    }
}
