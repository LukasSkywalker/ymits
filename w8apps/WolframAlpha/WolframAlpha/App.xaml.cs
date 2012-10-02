using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Search;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace WolframAlpha
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public string AppName = "WolframAlpha";
        public const string AppId = "R2R8RY-3X62YX7W3A";
        public const string ServiceURL = "http://api.wolframalpha.com/v2/query?appid={0}&input={1}&assumption={2}&latlong={3}&width=600";
        public const string ServiceURLState = "http://api.wolframalpha.com/v2/query?input={1}&appid={0}&podstate={2}&includepodid={3}&assumption={4}&latlong={5}&width=600";
        public const string ServiceURLAssumption = "http://api.wolframalpha.com/v2/query?input={1}&appid={0}&assumption={2}&latlong={3}&width=600";
        public static Geocoordinate Location;
        public static bool LocationEnabled = false;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            String a = new UrlBuilder().AddAppId(AppId).AddAssumption("Number").AddInput("mySearchTerm").Build();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), args.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// Invoked when the application is activated to display search results.
        /// </summary>
        /// <param name="args">Details about the activation request.</param>
        protected async override void OnSearchActivated(Windows.ApplicationModel.Activation.SearchActivatedEventArgs args)
        {
            // TODO: Register the Windows.ApplicationModel.Search.SearchPane.GetForCurrentView().QuerySubmitted
            // event in OnWindowCreated to speed up searches once the application is already running

            // If the Window isn't already using Frame navigation, insert our own Frame
            var previousContent = Window.Current.Content;
            var frame = previousContent as Frame;

            // If the app does not contain a top-level frame, it is possible that this 
            // is the initial launch of the app. Typically this method and OnLaunched 
            // in App.xaml.cs can call a common method.
            if (frame == null)
            {
                // Create a Frame to act as the navigation context and associate it with
                // a SuspensionManager key
                frame = new Frame();
                WolframAlpha.Common.SuspensionManager.RegisterFrame(frame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate
                    try
                    {
                        await WolframAlpha.Common.SuspensionManager.RestoreAsync();
                    }
                    catch (WolframAlpha.Common.SuspensionManagerException)
                    {
                        //Something went wrong restoring state.
                        //Assume there is no state and continue
                    }
                }
            }

            String searchterm = args.QueryText;
            if (!String.IsNullOrWhiteSpace(searchterm))
            {
                frame.Navigate(typeof(ItemDetailPage), searchterm);
            }
            else
            {
                frame.Navigate(typeof(ItemDetailPage));
            }
            Window.Current.Content = frame;

            // Ensure the current window is active
            Window.Current.Activate();
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            // At window creation time, access the SearchPane object and register SearchPane events
            // (like QuerySubmitted, SuggestionsRequested, and ResultSuggestionChosen) so that the app
            // can respond to the user's search queries at any time.

            // Get search pane
            SearchPane searchPane = SearchPane.GetForCurrentView();

            // Register event handlers for SearchPane events

            // Register QuerySubmitted event handler
            searchPane.QuerySubmitted += new TypedEventHandler<SearchPane, SearchPaneQuerySubmittedEventArgs>(OnQuerySubmitted);

            // Register a SuggestionsRequested if your app displays its own suggestions in the search pane (like from a web service)
            // Register a ResultSuggestionChosen if your app uses result suggestions in the search pane    
        }

        // SEARCH CONTRACT Respond to a search query while your app is the main app on screen.
        private void OnQuerySubmitted(SearchPane sender, SearchPaneQuerySubmittedEventArgs args)
        {
            string searchterm = args.QueryText;
            if (!string.IsNullOrWhiteSpace(searchterm))
                (Window.Current.Content as Frame).Navigate(typeof(ItemDetailPage), searchterm);
        }
    }
}
