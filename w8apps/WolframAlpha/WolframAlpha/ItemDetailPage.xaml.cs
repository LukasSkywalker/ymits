using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Split Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234234

namespace WolframAlpha
{
    /// <summary>
    /// A page that displays a group title, a list of items within the group, and details for
    /// the currently selected item.
    /// </summary>
    public sealed partial class ItemDetailPage : WolframAlpha.Common.LayoutAwarePage
    {
        private Dictionary<String, Dictionary<String, int>> StatesMap;

        public ItemDetailPage()
        {
            this.InitializeComponent();
            StatesMap = new Dictionary<String, Dictionary<String, int>>();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.DefaultViewModel["Results"] = App.QueryResult;

            /*int index = ((int)e.Parameter);
            itemListView.SelectedIndex = index;
            itemListView.ScrollIntoView(itemListView.Items[index]);*/

            if (App.QueryResult.Errors != null)
            {
                foreach(Error Error in App.QueryResult.Errors){
                    MessageDialog md = new MessageDialog(Error.Code+" "+Error.Message, "Error");
                    await md.ShowAsync();
                }
            }

            if (App.QueryResult.Warnings != null)
            {
                foreach (Warning Warning in App.QueryResult.Warnings)
                {
                    MessageDialog md = new MessageDialog("Something bad happened. But we don't know what, so just go on.", "Warning");
                    if (Warning.Spellcheck != null) {
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
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: Assign a bindable group to this.DefaultViewModel["Group"]
            // TODO: Assign a collection of bindable items to this.DefaultViewModel["Items"]

            if (pageState == null)
            {
                // When this is a new page, select the first item automatically unless logical page
                // navigation is being used (see the logical page navigation #region below.)
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null)
                {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                }
            }
            else
            {
                // Restore the previously saved state associated with this page
                if (pageState.ContainsKey("SelectedItem") && this.itemsViewSource.View != null)
                {
                    // TODO: Invoke this.itemsViewSource.View.MoveCurrentTo() with the selected
                    //       item as specified by the value of pageState["SelectedItem"]
                }
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            if (this.itemsViewSource.View != null)
            {
                var selectedItem = this.itemsViewSource.View.CurrentItem;
                // TODO: Derive a serializable navigation parameter and assign it to
                //       pageState["SelectedItem"]
            }
        }

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
        void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Invalidate the view state when logical page navigation is in effect, as a change
            // in selection may cause a corresponding change in the current logical page.  When
            // an item is selected this has the effect of changing from displaying the item list
            // to showing the selected item's details.  When the selection is cleared this has the
            // opposite effect.
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
            int multiplier = 1;

            try
            {
                multiplier = StatesMap[PodId][StateName];
                StatesMap[PodId][StateName]++;
            }
            catch (KeyNotFoundException ex) {
                System.Diagnostics.Debug.WriteLine("Creating Dict Key for pod "+PodId+", state"+StateName);
                if (StatesMap.ContainsKey(PodId))
                    StatesMap[PodId][StateName] = 2;
                else {
                    StatesMap.Add(PodId, new Dictionary<string,int>(100,null));
                    StatesMap[PodId][StateName] = 2;
                }
            }

            string address = String.Format(App.ServiceURLState, App.AppId, WebUtility.UrlEncode(App.QueryText), StateName, multiplier, PodId);

            System.Diagnostics.Debug.WriteLine(address);

            Task<String> sourceTask = Helper.GetResultAsync(address);
            String source = await sourceTask;
            QueryResult result = Helper.ParseResult(source);

            if (!result.Success)
                throw new Exception("Errör");

            
            Pod Pod = result.Pods[0];
            int oldIndex = App.QueryResult.getIndexByPodTitle(Pod.Title);
            System.Diagnostics.Debug.WriteLine("Updating Pod '" + Pod.Title+"' at position "+oldIndex);
            App.QueryResult.Pods[oldIndex] = Pod;
            ((QueryResult)this.DefaultViewModel["Results"]).Pods[oldIndex] = Pod;
        }

        private void ItemSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = ((ListBox)sender);
            if (lb.SelectedItem == null)
                return;
            String Url = (string)lb.SelectedValue;
            // TODO await Launcher.LaunchUriAsync(new Uri(Url));
        }

        // gets invoked when user clicks on an item in the states listbox, e.g. [More Digits], [Fractual representation]
        private void ItemStates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = ((ListBox)sender);
            if (lb.SelectedItem == null)
                return;

            Pod Pod = (Pod)itemListView.SelectedItem;
            String PodId = Pod.Id;
            String StatesValue = (string)lb.SelectedValue;

            getState(StatesValue, PodId);
        }

        private async void ItemInfos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = ((ListBox)sender);
            if (lb.SelectedItem == null)
                return;

            Pod Pod = (Pod)itemListView.SelectedItem;
            String PodId = Pod.Id;
            Info Info = (Info)lb.SelectedValue;

            var menu = new PopupMenu();

            if (Info.Images != null) {
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
            foreach (Source Source in App.QueryResult.Sources) {
                menu.Commands.Add(new UICommand(Source.Text, async (command) =>
                {
                    await Launcher.LaunchUriAsync(new Uri(Source.URL));
                }));
            }
            var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender, Placement.Above));
        }
    }
}
