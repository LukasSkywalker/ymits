using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Serialization;
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
        public LastFmResult LastFmResult { get; set; }

        public SimilarPage()
        {
            this.InitializeComponent();
            LastFmResult = new LastFmResult();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            String requestUrl = (string)e.Parameter;
            const string key = "8a08d6d31c85b7780084b65a0608bb16";
            //const string secret = "a1ef21e96a3575dac1ea6094863617a6";
            
            Uri url = new Uri(String.Format(requestUrl, key));

            HttpClient lfmClient = new HttpClient();

            HttpResponseMessage response = await lfmClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseText = await response.Content.ReadAsStringAsync();

            LastFmResult lfr = new LastFmResult();

            try
            {
                XmlSerializer des = new XmlSerializer(typeof(LastFmResult));
                var reader = new StringReader(responseText);
                lfr = (LastFmResult)des.Deserialize(reader);

                LastFmResult = lfr;

                System.Diagnostics.Debug.WriteLine("Status:" + lfr.Status);
            }
            catch (Exception ex)
            {
                Helper.DumpException(ex);
            }
        } 
    }

    [XmlRoot("lfm")]
    public class LastFmResult
    {
        [XmlAttribute("status")]
        public string Status { get; set; }

        [XmlArray("similartracks")]
        [XmlArrayItem("track")]
        public ObservableCollection<Track> SimilarTracks { get; set; }

        [XmlArray("similarartists")]
        [XmlArrayItem("artist")]
        public ObservableCollection<Artist> SimilarArtists { get; set; }

        public class Track
        {
            [XmlElement("name")]
            public string Name { get; set; }

            [XmlElement("artist")]
            public Artist Artist { get; set; }
        }

        public class Artist
        {
            [XmlElement("name")]
            public string Name { get; set; }
        }
    }
}
