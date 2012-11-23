using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MusicBird
{
    public class Track
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Url { get; set; }
        public int Match { get; set; }

        public Track(String artist, String title, String url, int match)
        {
            Artist = artist;
            Title = title;
            Url = url;
            Match = match;
        }
    }
}
