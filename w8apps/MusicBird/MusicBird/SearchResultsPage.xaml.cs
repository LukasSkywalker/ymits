using MusicBird.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Search Contract item template is documented at http://go.microsoft.com/fwlink/?LinkId=234240

namespace MusicBird
{
    /// <summary>
    /// This page displays search results when a global search is directed to this application.
    /// </summary>
    public sealed partial class SearchResultsPage : MusicBird.Common.LayoutAwarePage
    {
        private List<Track> resultsList;
        private List<Track> unFilteredList;

        private static RootPage RootPage { get { return (RootPage)((App)Application.Current).RootFrame.Content; } }

        private const int BACKUP_SERVICE_LIMIT = 200;   // Default: 5

        public SearchResultsPage()
        {
            this.InitializeComponent();

            ResourceLoader loader = new ResourceLoader("Resources");
            string str = loader.GetString("resultText/Text");
            string modstring = str.Replace("&#00a0", "\x00a0");
            resultText.Text = modstring;

            resultsList = new List<Track>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            string queryText = (string)navigationParameter as String;
            // SEARCH CONTRACT 2.3 Handle activation for empty search queries
            if (String.IsNullOrWhiteSpace(queryText))
            {
                this.Frame.Navigate(typeof(StartPage));
                return;
            }
            else
            {
                this.DefaultViewModel["QueryText"] = '\u201c' + queryText + '\u201d';

                if (!NetworkWatcher.Connected)
                {
                    var msgd = new MessageDialog("No internet connection. Please connect to the internet and try again");
                    await msgd.ShowAsync();
                    this.Frame.Navigate(typeof(StartPage));
                    return;
                }
                StopWatch sw = new StopWatch(true);

                List<Track> resultList = await getResults(queryText);             
                
                Debug.WriteLine(sw.Stop("getResults"));

                if (resultList.Count < BACKUP_SERVICE_LIMIT) {
                }
                unFilteredList = resultList;
                resultsList = resultList;
                this.DefaultViewModel["Results"] = resultList;
                VisualStateManager.GoToState(this, "ResultsFound", true);

                var filterList = new List<Filter>();
                filterList.Add(new Filter("All", resultList.Count, true));

                var groupedList = resultList.GroupBy(c => c.Artist);

                for(int i=0; i<Math.Min(5, groupedList.Count()-1); i++)
                {
                    var group = groupedList.ElementAt(i);
                    filterList.Add(new Filter(group.Key, group.Count()));
                }

                // Communicate results through the view model
                this.DefaultViewModel["Filters"] = filterList;
                this.DefaultViewModel["ShowFilters"] = filterList.Count > 1;
            }
        }

        void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Determine what filter was selected
            var selectedFilter = e.AddedItems.FirstOrDefault() as Filter;
            if (selectedFilter != null)
            {
                // Mirror the results into the corresponding Filter object to allow the
                // RadioButton representation used when not snapped to reflect the change
                selectedFilter.Active = true;

                List<Track> fullList = unFilteredList;
                List<Track> filteredList = new List<Track>();

                Debug.WriteLine(selectedFilter.Name);
                Debug.WriteLine(selectedFilter.Description);

                //only test if filter has less items than full
                if (selectedFilter.Count < fullList.Count)
                {

                    for (int i = 0; i < fullList.Count; i++)
                    {
                        if (fullList[i].Artist.Equals(selectedFilter.Name))
                            filteredList.Add(fullList[i]);
                    }
                }
                else {
                    filteredList = fullList;
                }

                this.DefaultViewModel["Results"] = filteredList;

                // TODO: Respond to the change in active filter by setting this.DefaultViewModel["Results"]
                //       to a collection of items with bindable Image, Title, Subtitle, and Description properties

                // Ensure results are found
                object results;
                ICollection resultsCollection;
                if (this.DefaultViewModel.TryGetValue("Results", out results) &&
                    (resultsCollection = results as ICollection) != null &&
                    resultsCollection.Count != 0)
                {
                    VisualStateManager.GoToState(this, "ResultsFound", true);
                    return;
                }
            }

            // Display informational text when there are no search results.
            VisualStateManager.GoToState(this, "NoResultsFound", true);
        }

        void Filter_Checked(object sender, RoutedEventArgs e)
        {
            // Mirror the change into the CollectionViewSource used by the corresponding ComboBox
            // to ensure that the change is reflected when snapped
            if (filtersViewSource.View != null)
            {
                var filter = (sender as FrameworkElement).DataContext;
                filtersViewSource.View.MoveCurrentTo(filter);
            }
        }

        private async Task<List<Track>> getResults(string searchterm)
        {
            cleanUp();

            searchProgress.IsIndeterminate = true;
            searchProgress.Visibility = Visibility.Visible;

            List<Track> trackList = await Helper.GetResult(searchterm);

            return trackList;
        }

        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = resultsListView.SelectedIndex;
            if (index == -1) return;
            Track track = getTrackAt(index);
            RootPage.PlayTrack(track);
        }

        private Track getTrackAt(int index)
        {
            Track track = resultsList[index];
            return track;
        }

        #region UI helpers

        private void cleanUp()
        {
            this.DefaultViewModel["Results"] = null;
            resultsListView.Items.Clear();
            resultsList.Clear();
        }
        #endregion

        private void OnItemTapped(object sender, TappedRoutedEventArgs e)
        {
            Track track = (Track)(sender as FrameworkElement).DataContext;
            RootPage.PlayTrack(track);
        }

        private async void OnItemRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Track track = (Track)(sender as FrameworkElement).DataContext;
            //int index = trackList.IndexOf(track);

            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand("Play", (command) =>
            {
                RootPage.PlayTrack(track);
            }));
            menu.Commands.Add(new UICommand("Save", (command) =>
            {
                RootPage.DownloadManager.Add(track);
            }));
            menu.Commands.Add(new UICommand("Add to Playlist", (command) =>
            {
                RootPage.Playlist.Add(track);
            }));
            menu.Commands.Add(new UICommand("Similar tracks", (command) =>
            {
                RootPage.ShowPopup(typeof(SimilarPage), track);
            }));
            await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));
        }

        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            Track track = (Track)(sender as FrameworkElement).DataContext;
            if(track != null)
                RootPage.PlayTrack(track);
        }

        private void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            Track track = (Track)(sender as FrameworkElement).DataContext;
            System.Diagnostics.Debug.WriteLine(track.Url);
            RootPage.DownloadManager.Add(track);
        }
    }

    /// <summary>
    /// View model describing one of the filters available for viewing search results.
    /// </summary>
    sealed class Filter : MusicBird.Common.BindableBase
    {
        private String _name;
        private int _count;
        private bool _active;

        public Filter(String name, int count, bool active = false)
        {
            this.Name = name;
            this.Count = count;
            this.Active = active;
        }

        public override String ToString()
        {
            return Description;
        }

        public String Name
        {
            get { return _name; }
            set { if (this.SetProperty(ref _name, value)) this.OnPropertyChanged("Description"); }
        }

        public int Count
        {
            get { return _count; }
            set { if (this.SetProperty(ref _count, value)) this.OnPropertyChanged("Description"); }
        }

        public bool Active
        {
            get { return _active; }
            set { this.SetProperty(ref _active, value); }
        }

        public String Description
        {
            get { return String.Format("{0} ({1})", _name, _count); }
        }
    }
}
