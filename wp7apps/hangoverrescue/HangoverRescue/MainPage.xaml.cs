using System.IO.IsolatedStorage;
using Microsoft.Phone.Controls.Maps;
using System;
using Microsoft.Phone.UserData;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Phone.Tasks;
using System.Windows.Navigation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using HangoverRescue.GeoCodeService;
using System.Device.Location;

namespace HangoverRescue
{
    public partial class MainPage : PhoneApplicationPage
    {
        GeoCoordinateWatcher watcher;
        GeoCoordinate coord;
        String address;


        // Constructor
        public MainPage()
        {
            InitializeComponent();
            getLocation(null, null);
            getDateTime();
            getCalendar();
        }

        public void getLocation( object sender, RoutedEventArgs e )
        {
            locationTextBlock.Text = "Please wait, I'm getting the data...";
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
            watcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            watcher.Start();
        }

        public void getDateTime() {
            DateTime now = DateTime.Now;
            timeTextBlock.Text = "It's " + String.Format("{0:f}", now);
        }

        public void getCalendar() {
            Appointments appts = new Appointments();

            //Identify the method that runs after the asynchronous search completes.
            appts.SearchCompleted += new EventHandler<AppointmentsSearchEventArgs>(Appointments_SearchCompleted);

            DateTime start = DateTime.Now;
            DateTime end = start.AddDays(1);
            int max = 20;

            //Start the asynchronous search.
            appts.SearchAsync(start, end, max, "Appointments Test #1");
        }

        void Appointments_SearchCompleted( object sender, AppointmentsSearchEventArgs e )
        {
            //Do something with the results.
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            var aptCounter = 0;
            sb.AppendLine("Your next appointments today:");
            sb.AppendLine(" ");
            sb.AppendLine(" ");

            foreach(Appointment appt in e.Results)
            {
                if(appt.Subject!=null)
                {
                    sb.AppendLine(appt.Subject + " at " + appt.StartTime + " (" + appt.Location + ")");
                    sb.AppendLine(" ");
                    aptCounter++;
                }
            }
            if(aptCounter > 0)
            {
                calendarTextBlock.Text = sb.ToString();
            }
            else {
                calendarTextBlock.Text = "No appointments in the next time.";
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
                        locationTextBlock.Text = "you have to enable access to location for this application.";
                    }
                    else
                    {
                        locationTextBlock.Text = "location is not functioning on this device";
                    }
                    watcher.Stop();
                    break;

                case GeoPositionStatus.Initializing:
                    // The Location Service is initializing.
                    // Disable the Start Location button.
                    //startLocationButton.IsEnabled = false;
                    break;

                case GeoPositionStatus.NoData:
                    // The Location Service is working, but it cannot get location data.
                    // Alert the user and enable the Stop Location button.
                    locationTextBlock.Text = "location data is not available.";
                    //stopLocationButton.IsEnabled = true;
                    watcher.Stop();
                    break;

                case GeoPositionStatus.Ready:
                    // The Location Service is working and is receiving location data.
                    // Show the current position and enable the Stop Location button.
                    locationTextBlock.Text = "location data is available.";
                    //stopLocationButton.IsEnabled = true;
                    break;
            }
        }

        void watcher_PositionChanged( object sender, GeoPositionChangedEventArgs<GeoCoordinate> e )
        {
            //latitudeTextBlock.Text = e.Position.Location.Latitude.ToString("0.000");
            //longitudeTextBlock.Text = e.Position.Location.Longitude.ToString("0.000");

            double lat = e.Position.Location.Latitude;
            double lon = e.Position.Location.Longitude;

            string key = "AoNAuD9OH6XfOgvRyEJJLIYJQMKwJ41QnghP2ue1dD5zGVmUWsJ_uL4YF5IOsdhO";

            string Results = "";
            try
            {

                ReverseGeocodeRequest reverseGeocodeRequest = new ReverseGeocodeRequest();

                // Set the credentials using a valid Bing Maps key
                reverseGeocodeRequest.Credentials = new GeoCodeService.Credentials();
                reverseGeocodeRequest.Credentials.ApplicationId = key;

                // Set the point to use to find a matching address
                GeoCodeService.Location point = new GeoCodeService.Location();
                point.Latitude = lat;
                point.Longitude = lon;

                coord = new GeoCoordinate(lat, lon);

                reverseGeocodeRequest.Location = point;

                // Make the reverse geocode request
                GeocodeServiceClient geocodeService = new GeocodeServiceClient("BasicHttpBinding_IGeocodeService");
                geocodeService.ReverseGeocodeCompleted += new EventHandler<ReverseGeocodeCompletedEventArgs>(geocodeService_ReverseGeocodeCompleted);
                geocodeService.ReverseGeocodeAsync(reverseGeocodeRequest);

                updateMap(lat, lon);

            }
            catch (Exception ex)
            {
            Results = "An exception occurred: " + ex.Message;

            }
        }

        public void updateMap(double lat, double lon) {
            map1.Center = new GeoCoordinate(lat, lon);
            map1.ZoomLevel = 16;
            map1.ZoomBarVisibility = System.Windows.Visibility.Visible;

            Pushpin pin = new Pushpin();
            pin.Location = new GeoCoordinate(lat, lon);
            pin.Content = new TextBlock() { Text = "You." };
            map1.Children.Clear();
            map1.Children.Add(pin);
        }

        public void geocodeService_ReverseGeocodeCompleted( object sender, ReverseGeocodeCompletedEventArgs e )
        {
            // The result is a GeocodeResponse object
            GeocodeResponse geocodeResponse = e.Result;

            if(geocodeResponse.Results.Count() > 0)
                locationTextBlock.Text = geocodeResponse.Results[0].DisplayName;
            else
                locationTextBlock.Text = "No Results found";
        }

        private void stopLocationButton_Click( object sender, RoutedEventArgs e )
        {
            watcher.Stop();
        }

        private void button2_Click( object sender, RoutedEventArgs e )
        {
            address = getAddress();
            if(address == "")
            {
                NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
            }
            else
            {

                BingMapsDirectionsTask Direction = new BingMapsDirectionsTask();
                LabeledMapLocation start = new LabeledMapLocation();
                start.Location = coord;
                LabeledMapLocation End = new LabeledMapLocation(getAddress(), null);
                Direction.Start = start;
                Direction.End = End;
                Direction.Show();
            }
        }

        public String getAddress() {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(!settings.Contains("address")){
                return "";
            }
            String address;
            settings.TryGetValue<String>("address", out address);
            return address;
        }

        private void button3_Click( object sender, RoutedEventArgs e )
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }
    }
}