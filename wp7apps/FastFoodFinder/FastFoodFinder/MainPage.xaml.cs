using System;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Navigation;
using System.Xml.Linq;
using Microsoft.Phone.Controls;

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
            
        }

        private void searchButton_Click( object sender, RoutedEventArgs e )
        {
            // The watcher variable was previously declared as type GeoCoordinateWatcher. 
            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
                watcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            }
            watcher.Start();
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
            search(lat, lng);
            System.Diagnostics.Debug.WriteLine("Got position, searching restaurants...");
        }

        private void search( double lat, double lng ) {
            (App.Current as App).lat = lat;
            (App.Current as App).lng = lng;
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += HttpCompleted;
            wc.DownloadStringAsync(new Uri("https://maps.googleapis.com/maps/api/place/search/xml?location="+lat.ToString()+","+lng.ToString()+"&radius=20000&types=cafe|restaurant|fastfood|food|meal_delivery|meal_takeaway&name=mcdonald%27s&sensor=true&key=AIzaSyD59rBnMgHdYzHzBvN48B9H1nGVM9WbHsI"));
        }


        private void HttpCompleted( object sender, DownloadStringCompletedEventArgs e )
        {
            if(e.Error == null)
            {
                XDocument xdoc = XDocument.Parse(e.Result, LoadOptions.None);
                if(xdoc.Element("PlaceSearchResponse").Element("status").FirstNode.ToString().Equals("OK"))
                {
                    System.Diagnostics.Debug.WriteLine("Got restaurants, parsing XML...");
                    var results = from r in xdoc.Descendants("result")
                            select new
                            {
                                name = r.Descendants("name").First().Value,
                                addr = r.Descendants("vicinity").First().Value,
                                lat = r.Descendants("lat").First().Value,
                                lng = r.Descendants("lng").First().Value
                            };
                    foreach(var result in results){
                        App.SearchResult res = new App.SearchResult(result.name, result.addr, Double.Parse(result.lat), Double.Parse(result.lng));
                        (App.Current as App).searchResults.Add(res);
                    }
                    Uri targetUri = new Uri("/Map.xaml", System.UriKind.Relative);
                    NavigationService.Navigate(targetUri);

                }
                else {
                    MessageBox.Show("");
                }
                    
                    var widgets = from x in xdoc.Descendants("widget")
                select new
                {
                    URL = x.Descendants("url").First().Value,
                    Category = x.Descendants("PortalCategoryId").First().Value
                };
            }
        }

    }
}