using System;
using System.Device.Location;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using HangoverRescue.GeoCodeService;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.UserData;
using Microsoft.Xna.Framework.Media;

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
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {

            getLocation(null, null);
            getDateTime();
            getCalendar();
            getPhotos();
        }

        public void getLocation( object sender, RoutedEventArgs e )
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(!settings.Contains("location") || !(bool)settings["location"])
            {
                locationTextBlock.Text = "Please enable location detection in\n"+
"the settings to use this feature";
            }else if( !NetworkInterface.GetIsNetworkAvailable() ){
                locationTextBlock.Text = "Please connect to the internet to\n"+
"get your location";
            }
            else
            {
                locationTextBlock.Text = "Please wait, I'm getting the data...";
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
                watcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
                watcher.Start();
            }
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
                        locationTextBlock.Text = "you have to enable location detection in\n"+
"the phone settings to use this feature.";
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
                    locationTextBlock.Text = "waiting for location data...";
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
            try
            {
                GeocodeResponse geocodeResponse = e.Result;

                if(geocodeResponse.Results.Count() > 0)
                    locationTextBlock.Text = geocodeResponse.Results[0].DisplayName;
                else
                    locationTextBlock.Text = "No Results found";
            }
            catch(Exception ex) {
                getLocation(null, null);
            }
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

        private String getAddress() {
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

        private void getPhotos() {
            MediaLibrary mediaLibrary = new MediaLibrary();
            var pictures = mediaLibrary.Pictures;
            myList.Items.Clear();
            int counter = 0;
            foreach(var picture in pictures)
            {
                if(picture.Date > DateTime.Now.AddHours(-24) && picture.Date <= DateTime.Now)
                {
                    BitmapImage image = new BitmapImage();
                    image.SetSource(picture.GetThumbnail());
                    MediaImage mediaImage = new MediaImage(counter);
                    mediaImage.ImageFile = image;
                    myList.Items.Add(mediaImage);
                }
                counter++;
            }
            if(myList.Items.Count() == 0) textBlock3.Visibility = Visibility.Visible;
            else textBlock3.Visibility = Visibility.Collapsed;
        }

        public class MediaImage
        {
            public MediaImage( int index ) {
                this.index = index;
            }

            public int index;
            public BitmapImage ImageFile { get; set; }
        }

        private void lstImages_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            MediaImage mI = (sender as ListBox).SelectedItem as MediaImage;
            MediaLibrary mediaLibrary = new MediaLibrary();
            BitmapImage image = new BitmapImage();
            if(mI != null) image.SetSource(mediaLibrary.Pictures[mI.index].GetImage());
            imgSelectedPhoto.Source = image;
            imgSelectedPhoto.Visibility = Visibility.Visible;

        }

        private void image_Tap( object sender, System.Windows.Input.GestureEventArgs e )
        {
            imgSelectedPhoto.Visibility = Visibility.Collapsed;
        }

        private void image_Tap2( object sender, RoutedEventArgs e )
        {
            imgSelectedPhoto.Visibility = Visibility.Collapsed;
        }

        private void lstImages_SelectionChanged2( object sender, System.Windows.Input.GestureEventArgs e )
        {
            lstImages_SelectionChanged(sender, null);
        }
    }
}