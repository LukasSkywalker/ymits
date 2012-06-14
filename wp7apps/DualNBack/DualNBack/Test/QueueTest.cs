using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DualNBack.Test
{
    [TestClass]
    public class QueueTest : SilverlightTest
    {

        [TestMethod]
        [Description("Initialize stack with size 3")]
        public void init()
        {
            DualNBack.Queue<string> stack = new DualNBack.Queue<string>(3);
            Assert.IsFalse(stack.IsFull());
        }

        [TestMethod]
        [ExpectedException(typeof(QueueNotFullException))]
        [Description("Adding 2 items, popping should fail")]
        public void fillingTwoItemsShouldFailPop()
        {
            DualNBack.Queue<string> stack = new DualNBack.Queue<string>(3);
            Assert.IsFalse(stack.IsFull());
            stack.Add("123");
            Assert.IsFalse(stack.IsFull());
            stack.Add("234");
            Assert.IsFalse(stack.IsFull());
            stack.First();
        }

        [TestMethod]
        [Description("Adding 3 items")]
        public void fillingThreeItemsShouldMakeFull()
        {
            DualNBack.Queue<string> stack = new DualNBack.Queue<string>(3);
            Assert.IsFalse(stack.IsFull());
            stack.Add("123");
            Assert.IsFalse(stack.IsFull());
            stack.Add("234");
            Assert.IsFalse(stack.IsFull());
            stack.Add("345");
            Assert.IsTrue(stack.IsFull());
        }

        [TestMethod]
        [Description("Adding 4 items")]
        public void fillingFourItemsShouldMakeFull()
        {
            DualNBack.Queue<string> stack = new DualNBack.Queue<string>(3);
            Assert.IsFalse(stack.IsFull());
            stack.Add("123");
            Assert.IsFalse(stack.IsFull());
            stack.Add("234");
            Assert.IsFalse(stack.IsFull());
            stack.Add("345");
            Assert.IsTrue(stack.IsFull());
            stack.Add("456");
            Assert.IsTrue(stack.IsFull());
        }

        [TestMethod]
        [Description("Adding 4 items, popping 1")]
        public void fillingFourItemsShouldMakeFullPoppingSecond()
        {
            DualNBack.Queue<string> stack = new DualNBack.Queue<string>(3);
            Assert.IsFalse(stack.IsFull());
            stack.Add("123");
            Assert.IsFalse(stack.IsFull());
            stack.Add("234");
            Assert.IsFalse(stack.IsFull());
            stack.Add("345");
            Assert.IsTrue(stack.IsFull());
            stack.Add("456");
            Assert.IsTrue(stack.IsFull());
            Assert.AreEqual((string)stack.First(), "234");
        }
    }
}
