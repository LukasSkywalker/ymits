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
using Windows.Media;
using Windows.Networking.BackgroundTransfer;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;

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
        private List<TrackListItem> resultsList;
        private DispatcherTimer _timer;
        private DispatcherTimer playTimeoutTimer;
        List<DownloadOperation> activeDownloads { get; set; }
        private CancellationTokenSource cts;
        private SearchPane searchPane;
        private List<TrackListItem> unFilteredList;
        private Windows.UI.Core.CoreDispatcher dispatcher;

        private const int BACKUP_SERVICE_LIMIT = 200;   // Default: 5

        public SearchResultsPage()
        {
            this.InitializeComponent();

            this.dispatcher = Window.Current.CoreWindow.Dispatcher;

            ResourceLoader loader = new ResourceLoader("Resources");
            string str = loader.GetString("resultText/Text");
            string modstring = str.Replace("&#00a0", "\x00a0");
            resultText.Text = modstring;

            searchPane = SearchPane.GetForCurrentView();

            cts = new CancellationTokenSource();
            resultsList = new List<TrackListItem>();
            
            searchClient = new HttpClient();
            searchClient.MaxResponseContentBufferSize = 256000;
            searchClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            suggestionClient = new HttpClient();
            suggestionClient.MaxResponseContentBufferSize = 256000;
            suggestionClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            MediaControl.PlayPressed += MediaControl_PlayPressed;
            MediaControl.PausePressed += MediaControl_PausePressed;
            MediaControl.PlayPauseTogglePressed += MediaControl_PlayPauseTogglePressed;
            MediaControl.StopPressed += MediaControl_StopPressed;
            MediaControl.SoundLevelChanged += MediaControl_SoundLevelChanged;

            volumeSlider.Value = playerElement.Volume * 10;

            progressSlider.ThumbToolTipValueConverter = new TimeSpanConverter();

            enableButtons(true, false);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _timer_Tick;

            playTimeoutTimer = new DispatcherTimer();
            playTimeoutTimer.Interval = TimeSpan.FromSeconds(4);
            playTimeoutTimer.Tick += playTimeoutTimer_Tick;
        }

        private void playTimeoutTimer_Tick(object sender, object e)
        {
            if (playerElement.CurrentState != MediaElementState.Playing) {
                playerElement.Stop();
                playerElement.Source = null;
                var msgd = new MessageDialog("Error playing this track. Please try another file.");
                try { msgd.ShowAsync(); }
                catch (Exception) { }
                HideWheel("playerTimeoutTimer_Tick");
            }
            playTimeoutTimer.Stop();
        }

        private async void MediaControl_SoundLevelChanged(object sender, object e)
        {
            await dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var soundLevel = Windows.Media.MediaControl.SoundLevel;
                    switch (soundLevel)
                    {
                        case Windows.Media.SoundLevel.Muted:
                            playerElement.Volume = 0;
                            volumeSlider.Value = 0;
                            break;
                        case Windows.Media.SoundLevel.Low:
                            playerElement.Volume = 0.2;
                            volumeSlider.Value = 2;
                            break;
                        case Windows.Media.SoundLevel.Full:
                            playerElement.Volume = 1;
                            volumeSlider.Value = 10;
                            break;
                    }
                });
            
        }

        void StartPage_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            App.AddSettingsCommands(args);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            searchPane.SuggestionsRequested += new TypedEventHandler<SearchPane, SearchPaneSuggestionsRequestedEventArgs>(OnSearchPaneSuggestionsRequested);

            SettingsPane.GetForCurrentView().CommandsRequested += StartPage_CommandsRequested;

            // SEARCH CONTRACT 2.5 Enable users to type into the search box directly from your app
            searchPane.ShowOnKeyboardInput = true;

            await DiscoverActiveDownloadsAsync();
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

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
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

                List<TrackListItem> resultList = await getResults(queryText);             
                
                Debug.WriteLine(sw.Stop("getResults"));

                if (resultList.Count < BACKUP_SERVICE_LIMIT) {
                    // NEVER EVER DO THIS EVER. EVER.
                    
                    /*System.Diagnostics.Debug.WriteLine("Less than "+BACKUP_SERVICE_LIMIT+" results, starting backup service...");
                    
                    sw.Restart();
                    
                    List<TrackListItem> secondaryResultsList = await getResultsBackup(queryText);
                    
                    Debug.WriteLine(sw.Stop("getResultsBackup"));

                    for (int i = 0; i < secondaryResultsList.Count; i++)
                    {
                        resultList.Insert(0, secondaryResultsList[i]);
                    }

                    Debug.WriteLine("Merged results: " + resultList.Count);
                     */
                }
                unFilteredList = resultList;
                resultsList = resultList;
                this.DefaultViewModel["Results"] = resultList;
                VisualStateManager.GoToState(this, "ResultsFound", true);

                /*Parallel.ForEach(
                    resultList,
                    new ParallelOptions { MaxDegreeOfParallelism = 5 },
                    result => {
                        GetSizeAsync(result.url);
                    }
                );*/

                // TODO: Application-specific searching logic.  The search process is responsible for
                //       creating a list of user-selectable result categories:
                //
                //       filterList.Add(new Filter("<filter name>", <result count>));
                //
                //       Only the first filter, typically "All", should pass true as a third argument in
                //       order to start in an active state.  Results for the active filter are provided
                //       in Filter_SelectionChanged below.

                var filterList = new List<Filter>();
                filterList.Add(new Filter("All", resultList.Count, true));

                var groupedList = resultList.GroupBy(c => c.artist);

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

        /// <summary>
        /// Invoked when a filter is selected using the ComboBox in snapped view state.
        /// </summary>
        /// <param name="sender">The ComboBox instance.</param>
        /// <param name="e">Event data describing how the selected filter was changed.</param>
        void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Determine what filter was selected
            var selectedFilter = e.AddedItems.FirstOrDefault() as Filter;
            if (selectedFilter != null)
            {
                // Mirror the results into the corresponding Filter object to allow the
                // RadioButton representation used when not snapped to reflect the change
                selectedFilter.Active = true;

                List<TrackListItem> fullList = unFilteredList;
                List<TrackListItem> filteredList = new List<TrackListItem>();

                Debug.WriteLine(selectedFilter.Name);
                Debug.WriteLine(selectedFilter.Description);

                //only test if filter has less items than full
                if (selectedFilter.Count < fullList.Count)
                {

                    for (int i = 0; i < fullList.Count; i++)
                    {
                        if (fullList[i].artist.Equals(selectedFilter.Name))
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

        /// <summary>
        /// Invoked when a filter is selected using a RadioButton when not snapped.
        /// </summary>
        /// <param name="sender">The selected RadioButton instance.</param>
        /// <param name="e">Event data describing how the RadioButton was selected.</param>
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

        private async Task<List<TrackListItem>> getResults(string searchterm)
        {
            cleanUp();

            searchProgress.IsIndeterminate = true;
            searchProgress.Visibility = Visibility.Visible;

            string responseText = "";
            List<TrackListItem> trackList = new List<TrackListItem>();

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
                            TrackListItem item = new TrackListItem(data[0], data[1], url, match);
                            int insertPos = 0;
                            for (int i = 0; i < trackList.Count; i++) {
                                if (trackList[i].match < match) {
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
            catch (HttpRequestException hre)
            {
                var messageDialog = new MessageDialog("Network error: "+hre.Message+" Please check your connection and try again.");
                messageDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                // For debugging
                Debug.WriteLine(ex.ToString());
            }
            finally {
                //toggleSearchIndicator();
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
            TrackListItem track = getTrackAt(index);
            playTrack(track);
        }

        private TrackListItem getTrackAt(int index)
        {
            TrackListItem track = resultsList[index];
            return track;
        }

        private void playTrack(TrackListItem track)
        {
            Debug.WriteLine(track.artist + " " + track.title);
            playerElement.Source = new Uri(track.url);

            MediaControl.ArtistName = track.artist;
            MediaControl.TrackName = track.title;

            playerElement.Play();
            playTimeoutTimer.Start();
            ShowWheel("playTrack");
        }

        private async void ShowWheel(String msg) {
            await dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    progressWheel.IsActive = true;
                    progressWheel.Visibility = Visibility.Visible;
                    //progressPanel.Visibility = Visibility.Visible;
                });
            System.Diagnostics.Debug.WriteLine("Showing wheel: "+msg);
        }

        private void HideWheel(String msg)
        {
            System.Diagnostics.Debug.WriteLine("Hiding wheel: " + msg);
            progressWheel.IsActive = false;
            progressWheel.Visibility = Visibility.Collapsed;
            //progressPanel.Visibility = Visibility.Collapsed;
        }


        #region mediaControl handlers
        private void MediaControl_StopPressed(object sender, object e)
        {
            ShowWheel("MediaControl_StopPressed");
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                btnStop_Click(null, null);
            })).AsTask().Wait();
        }

        private void MediaControl_PlayPauseTogglePressed(object sender, object e)
        {
            ShowWheel("MediaControl_PlayPauseTogglePressed");
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                if (playerElement.CurrentState == MediaElementState.Playing)
                {
                    btnPlay_Click(null, null);
                }
                else
                {
                    btnPlay_Click(null, null);
                }
            })).AsTask().Wait();

        }

        private void MediaControl_PausePressed(object sender, object e)
        {
            ShowWheel("MediaControl_PausePressed");
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                btnPlay_Click(null, null);
            })).AsTask().Wait();
        }

        private void MediaControl_PlayPressed(object sender, object e)
        {
            ShowWheel("MediaControl_PlayPressed");
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                btnPlay_Click(null, null);
            })).AsTask().Wait();
        }
        #endregion
        
        #region stringhelpers
        #endregion

        #region UI helpers
        private void enableButtons(bool playButton, bool stopButton)
        {
            btnPlay.IsEnabled = playButton;
            btnStop.IsEnabled = stopButton;
        }

        private void cleanUp()
        {
            this.DefaultViewModel["Results"] = null;
            resultsListView.Items.Clear();
            resultsList.Clear();
        }

        private void toggleSearchIndicator()
        {
            /*if (!searchIndicator.IsActive)
            {

                searchIndicator.IsActive = true;
                searchIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                searchIndicator.IsActive = false;
                searchIndicator.Visibility = Visibility.Collapsed;
            }*/
        }
        #endregion

        #region playback buttons event handler
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (playerElement.Source != null)
                ShowWheel("btnPlay_Click");
            else
                return;
            if (playerElement.CurrentState == MediaElementState.Playing)
            {
                playerElement.Pause();
            }
            else
            {
                if (playerElement.DefaultPlaybackRate != 1)
                {
                    playerElement.DefaultPlaybackRate = 1.0;
                }
                playerElement.Play();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            ShowWheel("btnStop_Click");
            playerElement.Stop();
        }

        private void btnForward_Click(object sender, RoutedEventArgs e)
        {
            ShowWheel("btnForward_Click");
            playerElement.DefaultPlaybackRate = 1.8;
            playerElement.Play();
        }
        private void btnReverse_Click(object sender, RoutedEventArgs e)
        {
            ShowWheel("btnReverse_Click");
            playerElement.DefaultPlaybackRate = -1.8;
            playerElement.Play();
        }

        private void btnMute_Click(object sender, RoutedEventArgs e)
        {
            playerElement.IsMuted = !playerElement.IsMuted;
        }
        #endregion

        #region playback sliders event handler
        private void slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            playerElement.Position = TimeSpan.FromSeconds(e.NewValue);
            currentTimeTextBlock.Text = TimeSpan.FromSeconds(e.NewValue).ToString(@"mm\:ss");
        }

        private void _timer_Tick(object sender, object e)
        {
            progressSlider.Value = playerElement.Position.TotalSeconds;
            currentTimeTextBlock.Text = playerElement.Position.ToString(@"mm\:ss");
        }

        private void volume_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            playerElement.Volume = volumeSlider.Value / 10;
        }

        private void appBarSaveButton_Click(object sender, RoutedEventArgs e)
        {
            int index = resultsListView.SelectedIndex;
            TrackListItem track = getTrackAt(index);
            saveTrack(track);
        }

        private void appBarPlayButton_Click(object sender, RoutedEventArgs e)
        {
            int index = resultsListView.SelectedIndex;
            TrackListItem track = getTrackAt(index);
            playTrack(track);
        }

        #endregion

        private static Style GetStyle(string key)
        {
            return Application.Current.Resources[key] as Style;
        }


        #region playerElement event handlers
        private void mediaElement_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            switch (playerElement.CurrentState)
            {
                case MediaElementState.Playing:
                    HideWheel("mediaElement_CurrentStateChanged:playing");
                    _timer.Start();
                    playTimeoutTimer.Stop();
                    btnPlay.Style = GetStyle("PauseButtonStyle");
                    enableButtons(true, true);
                    break;

                case MediaElementState.Paused:
                    HideWheel("mediaElement_CurrentStateChanged:paused");
                    _timer.Stop();
                    playTimeoutTimer.Stop();
                    btnPlay.Style = GetStyle("PlayButtonStyle");
                    enableButtons(true, true);
                    break;

                case MediaElementState.Stopped:
                    HideWheel("mediaElement_CurrentStateChanged:stopped");
                    _timer.Stop();
                    playTimeoutTimer.Stop();
                    btnPlay.Style = GetStyle("PlayButtonStyle");
                    progressSlider.Value = 0;
                    enableButtons(true, false);
                    break;
            }
        }

        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            HideWheel("mediaElement_MediaOpened");
            double absvalue = (int)Math.Round(
            playerElement.NaturalDuration.TimeSpan.TotalSeconds,
            MidpointRounding.AwayFromZero);

            progressSlider.Maximum = absvalue;

            progressSlider.StepFrequency =
                SliderFrequency(playerElement.NaturalDuration.TimeSpan);

            totalTimeTextBlock.Text = playerElement.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            progressSlider.Value = 0.0;
            HideWheel("mediaElement_MediaEnded");
        }

        private async void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            HideWheel("mediaElement_MediaFailed");
            string hr = GetHresultFromErrorMessage(e);
            Debug.WriteLine("ERROR:" + hr);
            MessageDialog md = new MessageDialog("The playback of this file failed. Please try another.", "Error");
            await md.ShowAsync();
            playerElement.Stop();
            playerElement.Source = null;
            HideWheel("mediaElement_MediaFailed_After");
            //playerElement.Play();
        }

        private void mediaElement_DownloadProgressChanged(object sender, RoutedEventArgs e)
        {

        }
#endregion

        #region other
        private string GetHresultFromErrorMessage(ExceptionRoutedEventArgs e)
        {
            String hr = String.Empty;
            String token = "HRESULT - ";
            const int hrLength = 10;     // eg "0xFFFFFFFF"

            int tokenPos = e.ErrorMessage.IndexOf(token, StringComparison.Ordinal);
            if (tokenPos != -1)
            {
                hr = e.ErrorMessage.Substring(tokenPos + token.Length, hrLength);
            }

            return hr;
        }

        private double SliderFrequency(TimeSpan timevalue)
        {
            double stepfrequency = -1;

            double absvalue = (int)Math.Round(
                timevalue.TotalSeconds, MidpointRounding.AwayFromZero);

            stepfrequency = (int)(Math.Round(absvalue / 100));

            if (timevalue.TotalMinutes >= 10 && timevalue.TotalMinutes < 30)
            {
                stepfrequency = 10;
            }
            else if (timevalue.TotalMinutes >= 30 && timevalue.TotalMinutes < 60)
            {
                stepfrequency = 30;
            }
            else if (timevalue.TotalHours >= 1)
            {
                stepfrequency = 60;
            }

            if (stepfrequency == 0) stepfrequency += 1;

            if (stepfrequency == 1)
            {
                stepfrequency = absvalue / 100;
            }

            return stepfrequency;
        }

        #endregion

        #region downloads
        private async void saveTrack(TrackListItem track)
        {
            string url = track.url;

            Uri source = new Uri(url);
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            savePicker.FileTypeChoices.Add("MPEG Layer 3 Audio", new List<string>() { ".mp3" });
            savePicker.SuggestedFileName = track.artist + " - " + track.title;

            StorageFile destinationFile = await savePicker.PickSaveFileAsync();

            if (destinationFile == null) return;

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, destinationFile);

            await HandleDownloadAsync(download, true);
        }

        private async Task DiscoverActiveDownloadsAsync()
        {
            activeDownloads = new List<DownloadOperation>();
            IReadOnlyList<DownloadOperation> downloads = null;
            try
            {
                downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            }
            catch (Exception)
            {
                if (false)
                {
                    throw;
                }
                return;
            }
            System.Diagnostics.Debug.WriteLine("Loading background downloads: " + downloads.Count);
            if (downloads.Count > 0)
            {
                List<Task> tasks = new List<Task>();
                foreach (DownloadOperation download in downloads)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Discovered background download: {0}, Status: {1}", download.Guid,
                        download.Progress.Status));
                    tasks.Add(HandleDownloadAsync(download, false));
                }
                await Task.WhenAll(tasks);
            }
        }

        private async Task HandleDownloadAsync(DownloadOperation download, bool start)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Running: " + download.Guid);

                // Store the download so we can pause/resume. 
                activeDownloads.Add(download);

                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgressHandler);
                if (start)
                {
                    // Start the download and attach a progress handler. 
                    await download.StartAsync().AsTask(cts.Token, progressCallback);
                }
                else
                {
                    // The download was already running when the application started, re-attach the progress handler. 
                    await download.AttachAsync().AsTask(cts.Token, progressCallback);
                }

                ResponseInformation response = download.GetResponseInformation();

                System.Diagnostics.Debug.WriteLine(String.Format("Completed: {0}, Status Code: {1}", download.Guid, response.StatusCode));
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Canceled: " + download.Guid);
            }
            catch (Exception)
            {

            }
            finally
            {
                activeDownloads.Remove(download);
                UpdateDownloads();
            }
        }

        private void DownloadProgressHandler(DownloadOperation obj)
        {
            UpdateDownloads();
        }

        private void UpdateDownloads() {
            ulong totalSize = 0;
            ulong totalReceived = 0;

            foreach (DownloadOperation dlop in activeDownloads)
            {
                totalSize += dlop.Progress.TotalBytesToReceive;
                totalReceived += dlop.Progress.BytesReceived;
            }

            try
            {
                double percentage = (double)totalReceived / (double)totalSize;
                DownloadProgressBar.Value = percentage;
                DownloadProgressText.Text = activeDownloads.Count + " Downloads. " + totalReceived / 1024 / 1024 + " of " + totalSize / 1024 / 1024 + "MB (" + Math.Round(percentage * 100) + "%)";
            }
            catch (Exception) { }
        }

        private void HidePopup(object sender, TappedRoutedEventArgs e)
        {
            TransparentGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowPopup()
        {
            TransparentGrid.Visibility = Visibility.Visible;
        }

        #endregion

        private void OnItemTapped(object sender, TappedRoutedEventArgs e)
        {
            TrackListItem track = (TrackListItem)(sender as FrameworkElement).DataContext;
            playTrack(track);
        }

        private async void OnItemRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            TrackListItem track = (TrackListItem)(sender as FrameworkElement).DataContext;
            //int index = trackList.IndexOf(track);

            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand("Play", (command) =>
            {
                playTrack(track);
            }));
            menu.Commands.Add(new UICommand("Save", (command) =>
            {
                saveTrack(track);
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
            TrackListItem track = (TrackListItem)(sender as FrameworkElement).DataContext;
            if(track != null)
                playTrack(track);
        }

        private void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            TrackListItem track = (TrackListItem)(sender as FrameworkElement).DataContext;
            System.Diagnostics.Debug.WriteLine(track.url);
            saveTrack(track);
        }

        private void ShowPopup(object sender, TappedRoutedEventArgs e)
        {
            ShowPopup();
        }

        /*private void GoBack(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack) {
                this.Frame.GoBack();
            }
        }*/
    }

    public class TrackListItem
    {
        public string title { get; set; }
        public string artist { get; set; }
        public double size
        {
            get{return size;}
            set
            {
                sizeText = (value / (double)1048576).ToString("F2") + " MB";
            }
        }
        public string duration { get; set; }
        public string url { get; set; }
        public string sizeText { get; private set; }
        public int match { get; set; }

        public TrackListItem(String artist, String title, String url, int match)
        {
            this.artist = artist;
            this.title = title;
            this.url = url;
            this.size = 0;
            this.match = match;
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
