using System;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Xml.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using System.IO.IsolatedStorage;

namespace FastFoodFinder
{
    public partial class MainPage : PhoneApplicationPage
    {
        GeoCoordinateWatcher watcher;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            int radius = (App.Current as App).rad;
            slider1.Value = radius / 1000;
            radiusTextBlock.Text = (radius / 1000) + " km";
            slider1.ValueChanged += new RoutedPropertyChangedEventHandler<double>(slider1_ValueChanged);
            IsolatedStorageSettings sett = IsolatedStorageSettings.ApplicationSettings;
            if(sett.Contains("location")) locationCheckBox.IsChecked = (bool)sett["location"];
            else locationCheckBox.IsChecked = false;
        }

        private void searchButton_Click( object sender, RoutedEventArgs e )
        {
            if(!(bool)locationCheckBox.IsChecked)
            {
                MessageBox.Show("Please enable location detection to use this feature. Tap the question mark at the bottom to get more information.");
            }
            else
            {
                searchButton.IsEnabled = false;
                searchButton.Content = "Please wait...";
                // The watcher variable was previously declared as type GeoCoordinateWatcher. 
                if(watcher == null)
                {
                    watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
                    watcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal
                    watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                    watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
                }
                watcher.Start();
            }
        }

        void watcher_StatusChanged( object sender, GeoPositionStatusChangedEventArgs e )
        {
            switch(e.Status)
            {
                case GeoPositionStatus.Disabled:
                    // The Location Service is disabled or unsupported.
                    // Check to see whether the user has disabled the Location Service.
                    if(watcher.Permission == GeoPositionPermission.Denied)
                    {
                        // The user has disabled the Location Service on their device.
                        statusTextBlock.Text = "you have this application access to location.";
                    }
                    else
                    {
                        statusTextBlock.Text = "location is not functioning on this device";
                    }
                    break;

                case GeoPositionStatus.Initializing:
                    // The Location Service is initializing.
                    // Disable the Start Location button.
                    //startLocationButton.IsEnabled = false;
                    statusTextBlock.Text = "please wait...";
                    break;

                case GeoPositionStatus.NoData:
                    // The Location Service is working, but it cannot get location data.
                    // Alert the user and enable the Stop Location button.
                    statusTextBlock.Text = "location data is not available.";
                    //stopLocationButton.IsEnabled = true;
                    break;

                case GeoPositionStatus.Ready:
                    // The Location Service is working and is receiving location data.
                    // Show the current position and enable the Stop Location button.
                    statusTextBlock.Text = "location data is available.";
                    //stopLocationButton.IsEnabled = true;
                    break;
            }
        }

        void watcher_PositionChanged( object sender, GeoPositionChangedEventArgs<GeoCoordinate> e )
        {
            double lat = e.Position.Location.Latitude;
            double lng = e.Position.Location.Longitude;
            watcher.Stop();
            statusTextBlock.Text = "";
            searchButton.IsEnabled = true;
            searchButton.Content = "Search";
            search(lat, lng);
            System.Diagnostics.Debug.WriteLine("Got position, searching restaurants...");
        }

        private void search( double lat, double lng) {
            (App.Current as App).lat = lat;
            (App.Current as App).lng = lng;
            if(NetworkInterface.GetIsNetworkAvailable())
            {
                WebClient wc = new WebClient();
                wc.DownloadStringCompleted += HttpCompleted;
                string radius = "";
                if((App.Current as App).rad == 50000){
                    radius = "rankby=distance";
                }else{
                    radius = "radius="+(App.Current as App).rad;
                }
                string name = (listBox1.SelectedItem as ListBoxItem).Tag.ToString();
                string url = String.Format("https://maps.googleapis.com/maps/api/place/search/xml?location={0},{1}"+
                    "&types=cafe|restaurant|fastfood|food|meal_delivery|meal_takeaway"+
                    "&name={2}"+
                    "&sensor=true"+
                    "&{3}" +
                    "&key=AIzaSyD59rBnMgHdYzHzBvN48B9H1nGVM9WbHsI",
                    lat.ToString(), lng.ToString(), name, radius);
                System.Diagnostics.Debug.WriteLine(url);
                wc.DownloadStringAsync(new Uri(url));
            }
            else{
                MessageBox.Show("Network connection lost. Please connect to a network if you want to search.");
            }
        }


        private void HttpCompleted( object sender, DownloadStringCompletedEventArgs e )
        {
            if(e.Error == null)
            {
                try
                {
                    XDocument xdoc = XDocument.Parse(e.Result, LoadOptions.None);
                    switch(xdoc.Element("PlaceSearchResponse").Element("status").FirstNode.ToString()){
                        case "OK":
                            System.Diagnostics.Debug.WriteLine("Got restaurants, parsing XML...");
                            var results = from r in xdoc.Descendants("result")
                                          select new
                                          {
                                              name = r.Descendants("name").First().Value,
                                              addr = r.Descendants("vicinity").First().Value,
                                              lat = r.Descendants("lat").First().Value,
                                              lng = r.Descendants("lng").First().Value
                                          };
                            foreach(var result in results)
                            {
                                double dist = this.calculateDistance((App.Current as App).lat, (App.Current as App).lng, Double.Parse(result.lat), Double.Parse(result.lng));
                                App.SearchResult res = new App.SearchResult(result.name, result.addr, Double.Parse(result.lat), Double.Parse(result.lng), dist);
                                (App.Current as App).searchResults.Add(res);
                            }
                            Uri targetUri = new Uri("/Map.xaml", System.UriKind.Relative);
                            NavigationService.Navigate(targetUri);
                            break;
                        case "ZERO_RESULTS":
                            MessageBox.Show("No results found. Try increasing the radius or changing type.");
                            break;
                        case "OVER_QUERY_LIMIT":
                            MessageBox.Show("Too many searches were made. Please try again later");
                            break;
                        case "REQUEST_DENIED":
                            MessageBox.Show("Missing sensor value in the search request. Please try again later.");
                            break;
                        case "INVALID_REQUEST":
                            MessageBox.Show("Invalid search. This is my error. Sorry for the inconvenience.");
                            break;
                        default:
                            MessageBox.Show("Error getting results. Please try again later.");
                            break;
                    }
                }
                catch(WebException ex)
                {
                    MessageBox.Show("Could not contact server. Please check that your internet connection is enabled.");
                }
            }
        }

        private double calculateDistance( double lat1, double lon1, double lat2, double lon2 ) {
            var R = 6371; // km (change this constant to get miles)
            var dLat = (lat2-lat1) * Math.PI / 180;
            var dLon = (lon2-lon1) * Math.PI / 180;
            var a = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
            Math.Cos(lat1 * Math.PI / 180 ) * Math.Cos(lat2 * Math.PI / 180 ) *
            Math.Sin(dLon/2) * Math.Sin(dLon/2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
            var d = R * c;
            return d;
        }

        private double toRad( double deg ) {
            return deg * Math.PI / 180;
        }

        private void slider1_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            if(Math.Floor(slider1.Value) == 50)
            {
                radiusTextBlock.Text = "All";
            }
            else {
                radiusTextBlock.Text = Math.Floor(slider1.Value) + " km";
            }
            (App.Current as App).rad = 1000 * (int)slider1.Value;
        }

        private void helButton_Click( object sender, RoutedEventArgs e )
        {
            MessageBox.Show("Your location is used to find the nearest fastfood restaurants. Only your latitude and longitude are sent to Google\u2122 in order to find the restaurants. No personal data such as your name or your devices serial number is sent. If you choose a restaurant, your location is sent to Bing\u2122 in order to find the shortest way there. Again, no personal information is sent.");
        }

        private void locationCheckBox_Checked( object sender, RoutedEventArgs e )
        {
            IsolatedStorageSettings sett = IsolatedStorageSettings.ApplicationSettings;
            if(sett.Contains("location")) sett["location"] = true;
            else sett.Add("location", true);
        }

        private void locationCheckBox_UnChecked( object sender, RoutedEventArgs e )
        {
            IsolatedStorageSettings sett = IsolatedStorageSettings.ApplicationSettings;
            if(sett.Contains("location")) sett["location"] = false;
            else sett.Add("location", false);
        }
    }
}