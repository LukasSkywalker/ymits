using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Search;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WolframAlpha.Common;

// The Split Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234234

namespace WolframAlpha
{
    /// <summary>
    /// A page that displays a group title, a list of items within the group, and details for
    /// the currently selected item.
    /// </summary>
    public sealed partial class ItemDetailPage : WolframAlpha.Common.LayoutAwarePage
    {
        private QueryResult QueryResult;
        private String QueryText;
        private String QueryAssumption = "";
        private Dictionary<String,List<String>> States;
        private SearchPane searchPane;

        // like podid-states, e.g. [Numeric definition:-more digits, -more digits, -less digits]

        public ItemDetailPage()
        {
            this.InitializeComponent();
            States = new Dictionary<string,List<string>>();

            searchPane = SearchPane.GetForCurrentView();
            searchPane.SuggestionsRequested += new TypedEventHandler<SearchPane, SearchPaneSuggestionsRequestedEventArgs>(SearchtermSuggester.GetSuggestions);

            this.DefaultViewModel["MyCommand"] = new DelegateCommand<int>(OnExecute);
        }

        private void OnExecute(int obj)
        {
            throw new NotImplementedException();
        }

        private async void SetResult(QueryResult result)
        {
            QueryResult = result;

            this.DefaultViewModel["Pods"] = result.Pods;
            this.DefaultViewModel["Assumptions"] = result.Assumptions;
            this.DefaultViewModel["Sources"] = result.Sources;
            this.DefaultViewModel["Result"] = result;

            VisualStateManager.GoToState(this, "ResultsFound", true);

            if (QueryResult.Errors != null)
            {
                foreach (Error Error in QueryResult.Errors)
                {
                    MessageDialog md = new MessageDialog(Error.Code + " " + Error.Message, "Error");
                    await md.ShowAsync();
                }
            }

            if (QueryResult.Warnings != null)
            {
                foreach (Warning Warning in QueryResult.Warnings)
                {
                    MessageDialog md = new MessageDialog("Something bad happened. But we don't know what, so just go on.", "Warning");
                    if (Warning.Spellcheck != null)
                    {
                        md = new MessageDialog(Warning.Spellcheck[0].Text, "Warning");
                    }
                    if (Warning.Delimiters != null)
                    {
                        md = new MessageDialog(Warning.Delimiters[0].Text, "Warning");
                    }
                    if (Warning.Reinterpret != null)
                    {
                        md = new MessageDialog(Warning.Reinterpret[0].Text + " " + Warning.Reinterpret[0].New, "Warning");
                    }
                    if (Warning.Translation != null)
                    {
                        md = new MessageDialog(Warning.Translation[0].Phrase + ": " + Warning.Translation[0].Text, "Warning");
                    }
                    await md.ShowAsync();
                }
            }
            if (AssumptionsListBox.Items.Count > 0)
                AssumptionsListBox.SelectedIndex = 0;
        }

        private void SetQueryText(String queryText)
        {
            this.DefaultViewModel["QueryText"] = queryText;
            this.DefaultViewModel["QueryTextQuoted"] = '\u201c' + queryText + '\u201d';

            QueryText = queryText;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            searchPane.ShowOnKeyboardInput = false;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // SEARCH CONTRACT 2.5 Enable users to type into the search box directly from your app
            searchPane.ShowOnKeyboardInput = true;


            String queryText = (String)e.Parameter;
            startNetworkAction(true, false);
            SetQueryText(queryText);

            string address = String.Format(App.ServiceURL, App.AppId, WebUtility.UrlEncode(queryText), QueryAssumption);
            Task<String> sourceTask = Helper.GetResultAsync(address);
            String source = await sourceTask;
            QueryResult result = Helper.ParseResult(source);

            SetResult(result);
            stopNetworkAction(true, false);
        }

        #region Page state management

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        /*protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: Assign a bindable group to this.DefaultViewModel["Group"]
            // TODO: Assign a collection of bindable items to this.DefaultViewModel["Items"]

            if (pageState == null)
            {
                // When this is a new page, select the first item automatically unless logical page
                // navigation is being used (see the logical page navigation #region below.)
                if (!this.UsingLogicalPageNavigation() && this.podsViewSource.View != null)
                {
                    this.podsViewSource.View.MoveCurrentToFirst();
                }
            }
            else
            {
                // Restore the previously saved state associated with this page
                if (pageState.ContainsKey("SelectedItem") && this.podsViewSource.View != null)
                {
                    // TODO: Invoke this.itemsViewSource.View.MoveCurrentTo() with the selected
                    //       item as specified by the value of pageState["SelectedItem"]
                }
            }
        }*/

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        /*protected override void SaveState(Dictionary<String, Object> pageState)
        {
            if (this.podsViewSource.View != null)
            {
                var selectedItem = this.podsViewSource.View.CurrentItem;
                // TODO: Derive a serializable navigation parameter and assign it to
                //       pageState["SelectedItem"]
            }
        }*/

        #endregion

        #region Logical page navigation

        // Visual state management typically reflects the four application view states directly
        // (full screen landscape and portrait plus snapped and filled views.)  The split page is
        // designed so that the snapped and portrait view states each have two distinct sub-states:
        // either the item list or the details are displayed, but not both at the same time.
        //
        // This is all implemented with a single physical page that can represent two logical
        // pages.  The code below achieves this goal without making the user aware of the
        // distinction.

        /// <summary>
        /// Invoked to determine whether the page should act as one logical page or two.
        /// </summary>
        /// <param name="viewState">The view state for which the question is being posed, or null
        /// for the current view state.  This parameter is optional with null as the default
        /// value.</param>
        /// <returns>True when the view state in question is portrait or snapped, false
        /// otherwise.</returns>
        private bool UsingLogicalPageNavigation(ApplicationViewState? viewState = null)
        {
            if (viewState == null) viewState = ApplicationView.Value;
            return viewState == ApplicationViewState.FullScreenPortrait ||
                viewState == ApplicationViewState.Snapped;
        }

        /// <summary>
        /// Invoked when an item within the list is selected.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is Snapped)
        /// displaying the selected item.</param>
        /// <param name="e">Event data that describes how the selection was changed.</param>
        void ItemListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Invalidate the view state when logical page navigation is in effect, as a change
            // in selection may cause a corresponding change in the current logical page.  When
            // an item is selected this has the effect of changing from displaying the item list
            // to showing the selected item's details.  When the selection is cleared this has the
            // opposite effect.
            if(itemListView.SelectedIndex>-1) ShowPopup();
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();
        }

        /// <summary>
        /// Invoked when the page's back button is pressed.
        /// </summary>
        /// <param name="sender">The back button instance.</param>
        /// <param name="e">Event data that describes how the back button was clicked.</param>
        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            if (this.UsingLogicalPageNavigation() && itemListView.SelectedItem != null)
            {
                // When logical page navigation is in effect and there's a selected item that
                // item's details are currently displayed.  Clearing the selection will return to
                // the item list.  From the user's point of view this is a logical backward
                // navigation.
                this.itemListView.SelectedItem = null;
            }
            else
            {
                // When logical page navigation is not in effect, or when there is no selected
                // item, use the default back button behavior.
                base.GoBack(sender, e);
            }
        }

        /// <summary>
        /// Invoked to determine the name of the visual state that corresponds to an application
        /// view state.
        /// </summary>
        /// <param name="viewState">The view state for which the question is being posed.</param>
        /// <returns>The name of the desired visual state.  This is the same as the name of the
        /// view state except when there is a selected item in portrait and snapped views where
        /// this additional logical page is represented by adding a suffix of _Detail.</returns>
        protected override string DetermineVisualState(ApplicationViewState viewState)
        {
            // Update the back button's enabled state when the view state changes
            var logicalPageBack = this.UsingLogicalPageNavigation(viewState) && this.itemListView.SelectedItem != null;
            var physicalPageBack = this.Frame != null && this.Frame.CanGoBack;
            this.DefaultViewModel["CanGoBack"] = logicalPageBack || physicalPageBack;

            // Determine visual states for landscape layouts based not on the view state, but
            // on the width of the window.  This page has one layout that is appropriate for
            // 1366 virtual pixels or wider, and another for narrower displays or when a snapped
            // application reduces the horizontal space available to less than 1366.
            if (viewState == ApplicationViewState.Filled ||
                viewState == ApplicationViewState.FullScreenLandscape)
            {
                var windowWidth = Window.Current.Bounds.Width;
                if (windowWidth >= 1366) return "FullScreenLandscapeOrWide";
                return "FilledOrNarrow";
            }

            // When in portrait or snapped start with the default visual state name, then add a
            // suffix when viewing details instead of the list
            var defaultStateName = base.DetermineVisualState(viewState);
            return logicalPageBack ? defaultStateName + "_Detail" : defaultStateName;
        }

        #endregion


        private async void getState(String StateName, String PodId) {

            if (States.ContainsKey(PodId))
                States[PodId].Add(StateName);
            else
            {
                States.Add(PodId, new List<String>());
                States[PodId].Add(StateName);
            }

            startNetworkAction(false, true);

            string stateParameter = "";

            if (States.ContainsKey(PodId))
            {
                foreach (String s in States[PodId]) {
                    stateParameter += "&podstate=" + s;
                }
                stateParameter = stateParameter.Substring(10);
            }

            string address = String.Format(App.ServiceURLState, App.AppId, WebUtility.UrlEncode(QueryText), stateParameter, PodId, QueryAssumption);

            System.Diagnostics.Debug.WriteLine(address);

            System.Diagnostics.Debug.WriteLine("Getting state for pod ID "+PodId);

            Task<String> sourceTask = Helper.GetResultAsync(address);
            String source = await sourceTask;
            QueryResult result = Helper.ParseResult(source);

            if (!result.Success)
                throw new Exception("Errör");

            // TODO NRE HERE!!!
            Pod Pod = result.Pods[0];
            int oldIndex = QueryResult.getIndexById(Pod.Id);
            if (oldIndex != -1)
            {
                System.Diagnostics.Debug.WriteLine("Updating Pod '" + Pod.Title + " " + Pod.Id + "' at position " + oldIndex);
                QueryResult.Pods[oldIndex] = Pod;
                ((ObservableCollection<Pod>)this.DefaultViewModel["Pods"])[oldIndex] = Pod;
            }

            stopNetworkAction(false, true);
        }

        private void startNetworkAction(bool main, bool popup) {
            if (main)
            {
                progressMeter.Visibility = Visibility.Visible;
                progressMeter.IsIndeterminate = true;
            }

            if (popup)
            {
                progressMeter2.Visibility = Visibility.Visible;
                progressMeter2.IsIndeterminate = true;
            }
        }

        private void stopNetworkAction(bool main, bool popup)
        {
            if (main)
            {
                progressMeter.Visibility = Visibility.Collapsed;
                progressMeter.IsIndeterminate = false;
            }

            if (popup)
            {
                progressMeter2.Visibility = Visibility.Collapsed;
                progressMeter2.IsIndeterminate = false;
            }
        }

        private async void ItemSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = ((ListBox)sender);
            if (lb.SelectedItem == null)
                return;
            String Url = (string)lb.SelectedValue;
            await Launcher.LaunchUriAsync(new Uri(Url));
        }

        // gets invoked when user clicks on an item in the states listbox, e.g. [More Digits], [Fractual representation]
        private void ItemStates_SelectionChanged(object sender, TappedRoutedEventArgs e)
        {
            TextBlock lb = ((TextBlock)sender);
            
            Pod Pod = (Pod)itemListView.SelectedItem;
            String PodId = Pod.Id;
            String StatesValue = ((State)lb.Tag).Input;

            getState(StatesValue, PodId);
        }

        private async void ItemInfos_SelectionChanged(object sender, TappedRoutedEventArgs e)
        {
            TextBlock lb = ((TextBlock)sender);

            Pod Pod = (Pod)itemListView.SelectedItem;
            String PodId = Pod.Id;
            Info Info = (Info)lb.Tag;

            var menu = new PopupMenu();

            if (Info.Image != null) {
                /*foreach(Image Image in Info.Images){
                    menu.Commands.Add(new UICommand("bla", (command) =>
                    {
                        OutputTextBlock.Text = "'" + command.Label + "' selected";
                    }));
                }*/
                //do nothing. img is only vis. rep. of text
            }
            if (Info.Links != null) {
                foreach (Link Link in Info.Links)
                {
                    menu.Commands.Add(new UICommand(Link.Text, async (command) =>
                    {
                        await Launcher.LaunchUriAsync(new Uri(Link.Url));
                    }));
                }
            }
            if (Info.Units != null) {
                foreach (Unit Unit in Info.Units)
                {
                    menu.Commands.Add(new UICommand(Unit.Long, (command) =>
                    {
                        System.Diagnostics.Debug.WriteLine(Unit.Long + " " + Unit.Short);
                    }));
                }
            }
            var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender, Placement.Above));
        }

        private Rect GetElementRect(FrameworkElement frameworkElement, Placement placement)
        {
            GeneralTransform buttonTransform = frameworkElement.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(frameworkElement.ActualWidth, frameworkElement.ActualHeight)); 
        }

        private async void SourcesButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new PopupMenu();
            foreach (Source Source in QueryResult.Sources) {
                menu.Commands.Add(new UICommand(Source.Text, async (command) =>
                {
                    await Launcher.LaunchUriAsync(new Uri(Source.URL));
                }));
            }
            var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender, Placement.Above));
        }

        private async void SaveImage(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            String ImageSource = (String)btn.Tag;
            String SelectedPodName = ((Pod)itemListView.SelectedItem).Title;
            String Input = ((String)this.DefaultViewModel["QueryText"]);

            if (EnsureUnsnapped())
            {
                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("GIF", new List<string>() { ".gif" });
                savePicker.SuggestedFileName = Input+"_"+SelectedPodName;

                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file == null) return;

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                HttpRequestMessage request = new
                    HttpRequestMessage(HttpMethod.Get, new Uri(ImageSource));
                var response = await client.
                    SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                Byte[] b = await response.Content.ReadAsByteArrayAsync();

                using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (IOutputStream outputStream = fileStream.GetOutputStreamAt(0))
                    {
                        using (DataWriter dataWriter = new DataWriter(outputStream))
                        {
                            //TODO: Replace "Bytes" with the type you want to write.
                            dataWriter.WriteBytes(b);
                            await dataWriter.StoreAsync();
                            dataWriter.DetachStream();
                        }

                        await outputStream.FlushAsync();
                    }
                }
                MessageDialog md = new MessageDialog("The image was saved successfully to "+file.Path, "Image");
                await md.ShowAsync();
            }
        }

        private async void CopyPlaintext(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            String Plaintext = (String)btn.Tag;

            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(Plaintext);

            Clipboard.SetContent(dataPackage);

            MessageDialog md = new MessageDialog("The text was copied to your clipboard", "Text");
            await md.ShowAsync();
        }

        internal bool EnsureUnsnapped()
        {
            // FilePicker APIs will not work if the application is in a snapped state.
            // If an app wants to show a FilePicker while snapped, it must attempt to unsnap first
            bool unsnapped = ((ApplicationView.Value != ApplicationViewState.Snapped) || ApplicationView.TryUnsnap());
            if (!unsnapped)
            {
                System.Diagnostics.Debug.WriteLine("Cannot unsnap the sample.");
            }

            return unsnapped;
        }

        private void Assumption_SelectionChanged(object sender, TappedRoutedEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            String input = (String)lb.SelectedValue;
            getAssumption(input);
            
        }

        private async void getAssumption(String AssumptionName)
        {
            States = new Dictionary<string, List<string>>();
            //reset states
            SetQueryText(QueryText);

            QueryAssumption = AssumptionName;

            string address = String.Format(App.ServiceURLAssumption, App.AppId, WebUtility.UrlEncode(QueryText), AssumptionName);

            System.Diagnostics.Debug.WriteLine(address);
            System.Diagnostics.Debug.WriteLine("Getting assumption " + AssumptionName);

            startNetworkAction(true, false);

            Task<String> sourceTask = Helper.GetResultAsync(address);
            String source = await sourceTask;
            QueryResult result = Helper.ParseResult(source);

            if (!result.Success)
                throw new Exception("Errör");

            SetResult(result);

            stopNetworkAction(true, false);
        }

        private void HidePopup(object sender, TappedRoutedEventArgs e)
        {
            TransparentGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowPopup()
        {
            TransparentGrid.Visibility = Visibility.Visible;
        }
    }
}
