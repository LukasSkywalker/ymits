using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using MusicBird;

namespace MusicBirdTest
{
    [TestClass]
    public class PlaylistTest
    {
        [TestMethod]
        public void TestPlaylistSize()
        {
            Playlist pls = new Playlist();
            Track track = new Track("TestArtist", "TestTitle", "TestUrl", 20);
            Track track2 = new Track("TestArtist2", "TestTitle2", "TestUrl2", 20);

            Assert.AreEqual(0, pls.Size);
            
            pls.Add(track);
            Assert.AreEqual(1, pls.Size);

            pls.Add(track2);
            Assert.AreEqual(2, pls.Size);
            
            pls.Remove(track);
            Assert.AreEqual(1, pls.Size);

            pls.Clear();
            Assert.AreEqual(0, pls.Size);
        }

        [TestMethod]
        public void TestNeighbors()
        {
            Playlist pls = new Playlist();
            Track track = new Track("TestArtist", "TestTitle", "TestUrl", 20);
            Track track2 = new Track("TestArtist2", "TestTitle2", "TestUrl2", 20);
            Track track3 = new Track("TestArtist3", "TestTitle3", "TestUrl3", 20);
            Track track4 = new Track("TestArtist4", "TestTitle4", "TestUrl4", 20);
            Track track5 = new Track("TestArtist5", "TestTitle5", "TestUrl5", 20);
            Track track6 = new Track("TestArtist6", "TestTitle6", "TestUrl6", 20);

            Assert.AreEqual(0, pls.Neighbors.Count);

            pls.Position = 0;
            pls.Add(track);
            Assert.AreEqual(1, pls.Neighbors.Count);
            Assert.AreEqual("TestArtist", pls.Neighbors[0].Artist);

            pls.Position = 0;
            pls.Add(track2);
            Assert.AreEqual(2, pls.Neighbors.Count);
            Assert.AreEqual("TestArtist", pls.Neighbors[0].Artist);
            Assert.AreEqual("TestArtist2", pls.Neighbors[1].Artist);

            pls.Position = 0;
            pls.Add(track3);
            Assert.AreEqual(3, pls.Neighbors.Count);
            Assert.AreEqual("TestArtist", pls.Neighbors[0].Artist);
            Assert.AreEqual("TestArtist2", pls.Neighbors[1].Artist);
            Assert.AreEqual("TestArtist3", pls.Neighbors[2].Artist);

            pls.Position = 0;
            pls.Add(track4);
            Assert.AreEqual(3, pls.Neighbors.Count);
            Assert.AreEqual("TestArtist", pls.Neighbors[0].Artist);
            Assert.AreEqual("TestArtist2", pls.Neighbors[1].Artist);
            Assert.AreEqual("TestArtist3", pls.Neighbors[2].Artist);

            pls.Position = 1;
            pls.Add(track5);
            Assert.AreEqual(3, pls.Neighbors.Count);
            Assert.AreEqual("TestArtist", pls.Neighbors[0].Artist);
            Assert.AreEqual("TestArtist2", pls.Neighbors[1].Artist);
            Assert.AreEqual("TestArtist3", pls.Neighbors[2].Artist);

            pls.Position = 2;
            pls.Add(track6);
            Assert.AreEqual(3, pls.Neighbors.Count);
            Assert.AreEqual("TestArtist2", pls.Neighbors[0].Artist);
            Assert.AreEqual("TestArtist3", pls.Neighbors[1].Artist);
            Assert.AreEqual("TestArtist4", pls.Neighbors[2].Artist);
            
            pls.Position = 3;
            Assert.AreEqual(3, pls.Neighbors.Count);
            Assert.AreEqual("TestArtist3", pls.Neighbors[0].Artist);
            Assert.AreEqual("TestArtist4", pls.Neighbors[1].Artist);
            Assert.AreEqual("TestArtist5", pls.Neighbors[2].Artist);
        }
        [TestMethod]
        public void TestCurrentTrack()
        {
            Playlist pls = new Playlist();
            Track track = new Track("TestArtist", "TestTitle", "TestUrl", 20);
            Track track2 = new Track("TestArtist2", "TestTitle2", "TestUrl2", 20);
            Track track3 = new Track("TestArtist3", "TestTitle3", "TestUrl3", 20);
            Track track4 = new Track("TestArtist4", "TestTitle4", "TestUrl4", 20);
            pls.Add(track);
            pls.Add(track2);
            pls.Add(track3);
            pls.Add(track4);

            pls.Position = -2;
            Assert.AreEqual(track3, pls.CurrentTrack);
            pls.Position = -1;
            Assert.AreEqual(track4, pls.CurrentTrack);
            pls.Position = 0;
            Assert.AreEqual(track, pls.CurrentTrack);
            pls.Position = 1;
            Assert.AreEqual(track2, pls.CurrentTrack);
            pls.Position = 2;
            Assert.AreEqual(track3, pls.CurrentTrack);
            pls.Position = 3;
            Assert.AreEqual(track4, pls.CurrentTrack);
            pls.Position = 4;
            Assert.AreEqual(track, pls.CurrentTrack);
            pls.Position = 5;
            Assert.AreEqual(track2, pls.CurrentTrack);
        }
    }
}
