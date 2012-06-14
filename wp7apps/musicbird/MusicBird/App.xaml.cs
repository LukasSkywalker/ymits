using System;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using com.mtiks.winmobile;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Marketplace;
using Microsoft.Phone.Shell;


namespace MusicBird
{
    public partial class App : Application
    {

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        // this is 2.4
        private int appVersion = 24;

        private static LicenseInformation _licenseInfo = new LicenseInformation();

        private string CopyrightMessage = "Please note that listening to copyright protected music may not be allowed in some countries. MusicBird doesn't share/upload music, it simply streams them from publicly available web sites.";
        
        public string copyrightMessage{
            get{
                return CopyrightMessage;
            }
            private set{
                CopyrightMessage = value;
            }
        }

        private static bool _isTrial = true;

        public bool IsTrial
        {
            get
            {
                return _isTrial;
            }
        }

        public int dropboxUploads { get; set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += this.Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
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
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
            }

        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            CheckLicense();
            mtiks.Instance.Start("6fa3cfb581843c4b5d7fc7996", Assembly.GetExecutingAssembly());
            checkIfUpdated();
            //if (System.Diagnostics.Debugger.IsAttached) IsolatedStorageExplorer.Explorer.Start("192.168.0.4");
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            CheckLicense();
            mtiks.Instance.Start("6fa3cfb581843c4b5d7fc7996", Assembly.GetExecutingAssembly());
            //if(System.Diagnostics.Debugger.IsAttached) IsolatedStorageExplorer.Explorer.RestoreFromTombstone();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            mtiks.Instance.Stop();
            // Ensure that required application state is persisted here.
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            mtiks.Instance.Stop();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if(System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            else {
                mtiks.Instance.AddException(e.ExceptionObject);
            }
        }

        private void CheckLicense()
        {
            // When debugging, we want to simulate a trial mode experience. The following conditional allows us to set the _isTrial 
            // property to simulate trial mode being on or off. 
        #if DEBUG
            string message = "This sample demonstrates the implementation of a trial mode in an application. " +
                               "Press 'OK' to simulate trial mode. Press 'Cancel' to run the application in normal mode.";
            if ( MessageBox.Show( message, "Debug Trial",
                 MessageBoxButton.OKCancel ) == MessageBoxResult.OK )
            {
                _isTrial = true;
            }
            else
            {
                _isTrial = false;
            }
        #else
            _isTrial = _licenseInfo.IsTrial();
        #endif
            Helper.Preferences.write("trial", _isTrial);
        }

        public void checkIfUpdated() {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(!settings.Contains("firstrun"))
            {
                mtiks.Instance.postEventAttributes("FIRSTRUN", new Dictionary<string, string>() { { "TRIAL", _isTrial.ToString() } });
                settings.Add("firstrun", true);
            }
            else {
                
            }
            if(!settings.Contains("version"))
            {
                updateAction();
                settings.Add("version", this.appVersion);
            }
            else
            {
                int version = Convert.ToInt32(settings["version"], CultureInfo.InvariantCulture);
                if(version != this.appVersion) {
                    updateAction();
                    mtiks.Instance.postEventAttributes("UPDATE", new Dictionary<string, string>() { { "VERSION", this.appVersion.ToString() } });
                    settings["version"] = this.appVersion;
                }
            }
            settings.Save();
        }

        private void updateAction() {
            MessageBox.Show(this.copyrightMessage, "MusicBird", MessageBoxButton.OK);
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += this.CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += this.RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= this.CompleteInitializePhoneApplication;
        }

        #endregion
    }
}