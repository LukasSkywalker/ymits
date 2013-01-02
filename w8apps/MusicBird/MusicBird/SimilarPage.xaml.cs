using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicBird
{
    public sealed partial class SimilarPage : Page
    {
        private static RootPage RootPage { get { return (RootPage)((App)Application.Current).RootFrame.Content; } }

        public SimilarPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Track reference = (Track)e.Parameter;
            const string key = "8a08d6d31c85b7780084b65a0608bb16";
            
            Uri url = new Uri(String.Format(App.URL_SIMILAR_TRACKS, reference.Artist, reference.Title, key));

            HttpClient lfmClient = new HttpClient();

            HttpResponseMessage response = await lfmClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseText = await response.Content.ReadAsStringAsync();

            List<Track> similarTracks = new List<Track>();

            XDocument doc = XDocument.Parse(responseText);
            
            foreach (XElement element in doc.Descendants("track")){
                String title = (string)element.Element("name").Value;
                String artist = (string)element.Element("artist").Element("name").Value;
                int match = (int)(Convert.ToDouble(element.Element("match").Value) * 100);
                similarTracks.Add(new Track(artist, title, null, match));
            }

            resultsListView.ItemsSource = similarTracks;
            if (similarTracks.Count == 0) {
                RootPage.NotifyUser("No similar tracks found");
            }
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Track track = (Track)(sender as FrameworkElement).DataContext;
            await FindAlternatives(track);
            RootPage.PlayTrack(track);
        }

        private async Task FindAlternatives(Track track) {
            List<Track> trackList = await Helper.GetResult(track.Artist + " " + track.Title);
            track.Candidates = trackList;
        }
    }
}
