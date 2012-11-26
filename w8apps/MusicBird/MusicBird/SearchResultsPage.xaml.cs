using MusicBird.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Search;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ApplicationSettings;
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
        private HttpClient searchClient;
        private HttpClient suggestionClient;
        private List<Track> resultsList;
        private SearchPane searchPane;
        private List<Track> unFilteredList;

        private RootPage RootPage { get { return (RootPage)((App)Application.Current).RootFrame.Content; } }
        private Slider volumeSlider { get { return (RootPage.FindName("volumeSlider") as Slider); } }
        private Slider progressSlider { get { return (RootPage.FindName("progressSlider") as Slider); } }
        private ProgressBar DownloadProgressBar { get { return (RootPage.FindName("DownloadProgressBar") as ProgressBar); } }
        private MediaElement playerElement { get { return (RootPage.FindName("playerElement") as MediaElement);  } }
        private Button btnPlay { get { return (RootPage.FindName("btnPlay") as Button); } }
        private Button btnStop { get { return (RootPage.FindName("btnStop") as Button); } }
        private TextBlock currentTimeTextBlock { get { return (RootPage.FindName("currentTimeTextBlock") as TextBlock); } }
        private TextBlock totalTimeTextBlock { get { return (RootPage.FindName("totalTimeTextBlock") as TextBlock); } }
        private TextBlock DownloadProgressText { get { return (RootPage.FindName("DownloadProgressText") as TextBlock); } }

        private const int BACKUP_SERVICE_LIMIT = 200;   // Default: 5

        public SearchResultsPage()
        {
            this.InitializeComponent();

            ResourceLoader loader = new ResourceLoader("Resources");
            string str = loader.GetString("resultText/Text");
            string modstring = str.Replace("&#00a0", "\x00a0");
            resultText.Text = modstring;

            searchPane = SearchPane.GetForCurrentView();

            resultsList = new List<Track>();
            
            searchClient = new HttpClient();
            searchClient.MaxResponseContentBufferSize = 256000;
            searchClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            suggestionClient = new HttpClient();
            suggestionClient.MaxResponseContentBufferSize = 256000;
            suggestionClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
        }

        void StartPage_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            App.AddSettingsCommands(args);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            searchPane.SuggestionsRequested += new TypedEventHandler<SearchPane, SearchPaneSuggestionsRequestedEventArgs>(OnSearchPaneSuggestionsRequested);

            SettingsPane.GetForCurrentView().CommandsRequested += StartPage_CommandsRequested;

            // SEARCH CONTRACT 2.5 Enable users to type into the search box directly from your app
            searchPane.ShowOnKeyboardInput = true;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            searchPane.ShowOnKeyboardInput = false;
        }

        private async void OnSearchPaneSuggestionsRequested(SearchPane sender, SearchPaneSuggestionsRequestedEventArgs e)
        {
            var queryText = e.QueryText;
            if (string.IsNullOrEmpty(queryText))
            {
                //Wait
            }
            else
            {
                var request = e.Request;
                var deferral = request.GetDeferral();

                try
                {
                    Task task = GetSuggestionsAsync(queryText, request.SearchSuggestionCollection);
                    await task;
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        if (request.SearchSuggestionCollection.Size > 0)
                        {
                            //MainPage.Current.NotifyUser("Suggestions provided for query: " + queryText, NotifyType.StatusMessage);
                        }
                        else
                        {
                            //MainPage.Current.NotifyUser("No suggestions provided for query: " + queryText, NotifyType.StatusMessage);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Previous suggestion request was canceled.
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Debug.WriteLine("Err:"+exc.Message);
                }
                finally
                {
                    System.Diagnostics.Debug.WriteLine("Sugg. completed.");
                    deferral.Complete();
                }
            }
        }

        private async Task GetSuggestionsAsync(string queryText, SearchSuggestionCollection searchSuggestionCollection)
        {
            String url = "http://www.lastfm.de/search/autocomplete?q={0}&force=1";
            Uri uri = new Uri(String.Format(url, WebUtility.UrlEncode(queryText)));

            //TODO cancelling ends up with no suggestions at all (typing too fast), but leaving
            //     all impacts perfromance, maybe.
            //suggestionClient.CancelPendingRequests();
            HttpResponseMessage response = await suggestionClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string responseText = await response.Content.ReadAsStringAsync();
            Regex pattern = new Regex("\"track\":\"(.*?)\",", RegexOptions.IgnoreCase);
            Regex pattern2 = new Regex("\"artist\":\"(.*?)\",", RegexOptions.IgnoreCase);
            Match m = pattern.Match(responseText);
            Match n = pattern2.Match(responseText);
            while (m.Success)
            {
                Group g = m.Groups[1];
                Group h = n.Groups[1];
                String title = g.ToString();
                String artist = h.ToString();
                searchSuggestionCollection.AppendQuerySuggestion(artist + " " + title);
                m = m.NextMatch();
                n = n.NextMatch();
            }
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

                ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (InternetConnectionProfile == null)
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

            string responseText = "";
            List<Track> trackList = new List<Track>();

            bool showMessage = false;

            try
            {
                string address = "http://mp3skull.com/mp3/" + searchterm.Replace(" ", "_") + ".html";
                searchClient.CancelPendingRequests();
                HttpResponseMessage response = await searchClient.GetAsync(address);
                response.EnsureSuccessStatusCode();
                responseText = await response.Content.ReadAsStringAsync();

                Regex pattern = new Regex("<a href=\"(.*?.mp3)\" rel=\"nofollow\"", RegexOptions.IgnoreCase);
                Regex pattern2 = new Regex("<div style=\"font-size:15px;\"><b>(.*?) mp3</b></div>", RegexOptions.IgnoreCase);
                try
                {
                    String s = responseText;

                    // Match the regular expression pattern against a text string.
                    Match m = pattern.Match(s);
                    Match n = pattern2.Match(s);

                    while (m.Success)
                    {
                        Group g = m.Groups[1];
                        Group h = n.Groups[1];
                        string name = h.ToString();
                        string artist = name;
                        string title = name;
                        string url = g.ToString();
                        string[] data = StringHelper.getArtistAndTitle(name);
                        // 4shared lets users download again (2012-09-20)
                        //if (url.IndexOf("4shared") == -1)
                        //{
                            int match1 = getDistance(searchterm, data[0] + " " + data[1]);
                            int match2 = getDistance(searchterm, data[1]+" "+data[0]);
                            int match = Math.Min(match1, match2);
                            Track item = new Track(data[0], data[1], url, match);
                            int insertPos = 0;
                            for (int i = 0; i < trackList.Count; i++) {
                                if (trackList[i].Match < match) {
                                    insertPos = i;
                                    break;
                                }
                            }
                            trackList.Insert(insertPos, item);
                        //}
                        m = m.NextMatch();
                        n = n.NextMatch();
                    }
                    System.Diagnostics.Debug.WriteLine("mp3skull: Results found: " + trackList.Count);
                }
                finally
                {
                    // SEARCH CONTRACT 2.2 Populate your page with results from your app's data
                }
            }
            catch (HttpRequestException)
            {
                showMessage = true;
            }
            catch (Exception)
            {
                showMessage = true;
            }
            finally {
                //toggleSearchIndicator();
            }
            if (showMessage)
            {
                var messageDialog = new MessageDialog("Network error. Please check your connection and try again.");
                await messageDialog.ShowAsync();
            }
            return trackList;
        }

        private int getDistance(string searchterm, string p)
        {
            double distance = Distance.compareStrings(searchterm, p);
            int dist = (int)Math.Round(distance * 100);
            return dist;
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
                RootPage.ShowPopup(typeof(SimilarPage), "http://ws.audioscrobbler.com/2.0/?method=track.getsimilar&artist=cher&track=believe&api_key={0}");
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

    public class ActionQueue {
        private const int QUEUE_SIZE = 3;
        List<Task> actions = new List<Task>();
        List<Task> runningActions = new List<Task>();
        List<Task> completedActions = new List<Task>();
        int actionCount = 0;

        public ActionQueue() {
            actions.Clear();
            runningActions.Clear();
            completedActions.Clear();
        }

        public void Add(Task item)
        {
            actions.Add(item);
        }

        public void RunNext() {
            if (actions.Count > 0)
            {
                Debug.WriteLine("Starting next action");
                Task action = actions[0];
                if (actions.Count > QUEUE_SIZE)
                {
                    action.ContinueWith((t) => actions[QUEUE_SIZE]);
                }
                action.Start();
                actions.RemoveAt(0);
                runningActions.Add(action);
                Debug.Assert(actionCount == actions.Count + runningActions.Count + completedActions.Count);
            }
        }

        public void Start() {
            actionCount = actions.Count;
            int max = Math.Min(QUEUE_SIZE, actionCount);
            Debug.WriteLine("Starting " + max + " actions");
            for (int i = 0; i < max; i++) {
                RunNext();
            }
        }

        public void OnCompleted(Task action)
        {
            Debug.WriteLine("Action Completed");
            completedActions.Add(action);
            runningActions.Remove(action);
            Debug.Assert(actionCount == actions.Count + runningActions.Count + completedActions.Count);
            RunNext();
        }
    }
}
