using System.Collections.Generic;
using System.Device.Location;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;

namespace FastFoodFinder
{
    public partial class App : Application
    {
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        public double lat = 0;
        public double lng = 0;
        public int rad = 10000;

        public List<SearchResult> searchResults = new List<SearchResult>();

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if(System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                //Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching( object sender, LaunchingEventArgs e )
        {
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated( object sender, ActivatedEventArgs e )
        {
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated( object sender, DeactivatedEventArgs e )
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing( object sender, ClosingEventArgs e )
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed( object sender, NavigationFailedEventArgs e )
        {
            if(System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException( object sender, ApplicationUnhandledExceptionEventArgs e )
        {
            if(System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        public void startNavigation( string name, double lat, double lng )
        {
            BingMapsDirectionsTask Direction = new BingMapsDirectionsTask();
            LabeledMapLocation start = new LabeledMapLocation();

            double latStart = (App.Current as App).lat;
            double lngStart = (App.Current as App).lng;
            start.Location = new GeoCoordinate(latStart, lngStart);

            double latEnd = lat;
            double lngEnd = lng;
            LabeledMapLocation End = new LabeledMapLocation(name, new GeoCoordinate(latEnd, lngEnd));
            Direction.Start = start;
            Direction.End = End;
            Direction.Show();
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if(phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication( object sender, NavigationEventArgs e )
        {
            // Set the root visual to allow the application to render
            if(RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        public class SearchResult {
            public string Name { get; set; }
            public string Address { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Distance { get; set; }
            public SearchResult( string name, string addr, double lat, double lng, double dist )
            {
                this.Name = name;
                this.Address = addr;
                this.Latitude = lat;
                this.Longitude = lng;
                this.Distance = Math.Floor(dist)+" km";
            }
        }
    }
}