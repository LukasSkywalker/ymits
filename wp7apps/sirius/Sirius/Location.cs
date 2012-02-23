using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;

namespace Sirius
{
    public static class Location
    {
        private static GeoCoordinateWatcher watcher;

        public delegate void LocationFoundHandler( String lat, String lon );//ResultParsed
        public static event LocationFoundHandler LocationFound;

        static Location() {
        }

        public static void start(String where){
            if(watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default); // using high accuracy
                watcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal
                watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            }
            watcher.Start();
        }

        private static void watcher_StatusChanged( object sender, GeoPositionStatusChangedEventArgs e )
        {
            String status = "";
            switch(e.Status)
            {
                case GeoPositionStatus.Disabled:
                    // The Location Service is disabled or unsupported.
                    // Check to see whether the user has disabled the Location Service.
                    if(watcher.Permission == GeoPositionPermission.Denied)
                    {
                        // The user has disabled the Location Service on their device.
                        status = "you have disabled access to location data for this app.";
                    }
                    else
                    {
                        status = "location is not functioning on this device";
                    }
                    break;

                case GeoPositionStatus.Initializing:
                    // The Location Service is initializing.
                    // Disable the Start Location button.
                    //startLocationButton.IsEnabled = false;
                    status = "initializing";
                    break;

                case GeoPositionStatus.NoData:
                    // The Location Service is working, but it cannot get location data.
                    // Alert the user and enable the Stop Location button.
                    status = "location data is not available.";
                    //stopLocationButton.IsEnabled = true;
                    break;

                case GeoPositionStatus.Ready:
                    // The Location Service is working and is receiving location data.
                    // Show the current position and enable the Stop Location button.
                    status = "location data is available.";
                    //stopLocationButton.IsEnabled = true;
                    break;
            }
        }

        private static void watcher_PositionChanged( object sender, GeoPositionChangedEventArgs<GeoCoordinate> e )
        {
            String lat = e.Position.Location.Latitude.ToString("0.000");
            String lon = e.Position.Location.Longitude.ToString("0.000");
            if(LocationFound != null) {
                LocationFound(lat, lon);
            }
            watcher.Stop();
        }
    }
}
