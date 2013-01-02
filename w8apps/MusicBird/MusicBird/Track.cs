using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace MusicBird
{
    public class Track : INotifyPropertyChanged
    {
        private string title;
        private string artist;
        private string url;
        private int match;
        private Uri image;
        private List<Track> candidates;

        public string Title { get { return title; } }
        public string Artist { get { return artist; } }
        public string Url { get { return url; } set { url = value; } }
        public int Match { get { return match; } }
        public Uri Image { get { return image; } }
        public List<Track> Candidates { get; set; }

        public Track(String artist, String title, String url, int match)
        {
            this.artist = artist;
            this.title = title;
            this.url = url;
            this.match = match;
            this.image = new Uri("ms-appx:///Assets/vinyl.png");
            this.candidates = new List<Track>();
        }

        public async Task FetchCover() {
            String coverUrl = await Helper.GetCoverUrl(Artist + " " + Title);
            System.Diagnostics.Debug.WriteLine(coverUrl);

            if (!String.IsNullOrEmpty(coverUrl))
            {
                BackgroundDownloader bd = new BackgroundDownloader();
                StorageFolder tempFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;
                String filename = DateTime.Now.Ticks.ToString();
                StorageFile sf = await tempFolder.CreateFileAsync(filename);

                DownloadOperation dlop = bd.CreateDownload(new Uri(coverUrl), sf);
                await dlop.StartAsync();
                this.image = new Uri("ms-appdata:///temp/" + filename);
            }
            else {
                this.image = new Uri("ms-appx:///Assets/vinyl.png");
            }
            RaisePropertyChanged("Image");
        }

        private void RaisePropertyChanged(string caller = "")
        {
            if (PropertyChanged != null)
            {
                System.Diagnostics.Debug.WriteLine("Track Property "+caller+" changed");
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}
