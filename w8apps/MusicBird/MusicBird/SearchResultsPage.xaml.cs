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
using Windows.ApplicationModel.Resources.Core;
using Windows.ApplicationModel.Search;
using Windows.Foundation;
using Windows.Media;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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
        private List<TrackListItem> trackList;
        private DispatcherTimer _timer;
        private List<DownloadOperation> activeDownloads;
        private CancellationTokenSource cts;
        private ulong totalSize;
        private SearchPane searchPane;
        private List<TrackListItem> unFilteredList;

        public SearchResultsPage()
        {
            this.InitializeComponent();

            ResourceLoader loader = new ResourceLoader("Resources");
            string str = loader.GetString("resultText/Text");
            string modstring = str.Replace("&#00a0", "\x00a0");
            resultText.Text = modstring;

            searchPane = SearchPane.GetForCurrentView();

            totalSize = 0;

            cts = new CancellationTokenSource();
            trackList = new List<TrackListItem>();
            
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

            volumeSlider.Value = playerElement.Volume * 10;

            progressSlider.ThumbToolTipValueConverter = new TimeSpanConverter();

            enableButtons(true, false);

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _timer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            searchPane.SuggestionsRequested += new TypedEventHandler<SearchPane, SearchPaneSuggestionsRequestedEventArgs>(OnSearchPaneSuggestionsRequested);

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
                catch (FormatException)
                {
                    //MainPage.Current.NotifyUser("Suggestions could not be retrieved, please verify that the URL points to a valid service (for example http://contoso.com?q={searchTerms})", NotifyType.ErrorMessage);
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
                Task<List<TrackListItem>> trackList = getResults(queryText);
                this.DefaultViewModel["QueryText"] = '\u201c' + queryText + '\u201d';
                List<TrackListItem> resultList = await trackList;
                unFilteredList = resultList;
                this.DefaultViewModel["Results"] = resultList;
                VisualStateManager.GoToState(this, "ResultsFound", true);

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

            string responseText = "";

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
                    int matchCount = 0;

                    while (m.Success)
                    {
                        Group g = m.Groups[1];
                        Group h = n.Groups[1];
                        string name = h.ToString();
                        string artist = name;
                        string title = name;
                        string url = g.ToString();
                        string[] data = StringHelper.getArtistAndTitle(name);
                        if (url.IndexOf("4shared") == -1)
                        {
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
                            matchCount++;
                        }
                        m = m.NextMatch();
                        n = n.NextMatch();
                    }

                    System.Diagnostics.Debug.WriteLine("mp3skull: Results found: " + matchCount);

                }
                finally
                {
                    // SEARCH CONTRACT 2.2 Populate your page with results from your app's data
                }
            }
            catch (HttpRequestException hre)
            {
                Debug.WriteLine(hre.ToString());
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
            TrackListItem track = trackList[index];
            return track;
        }

        private void playTrack(TrackListItem track)
        {
            Debug.WriteLine(track.artist + " " + track.title);
            playerElement.Source = new Uri(track.url);

            MediaControl.ArtistName = track.artist;
            MediaControl.TrackName = track.title;

            playerElement.Play();
        }


        #region mediaControl handlers
        private void MediaControl_StopPressed(object sender, object e)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                btnStop_Click(null, null);
            })).AsTask().Wait();
        }

        private void MediaControl_PlayPauseTogglePressed(object sender, object e)
        {
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
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                btnPlay_Click(null, null);
            })).AsTask().Wait();
        }

        private void MediaControl_PlayPressed(object sender, object e)
        {
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
            trackList.Clear();
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
            playerElement.Stop();
        }

        private void btnForward_Click(object sender, RoutedEventArgs e)
        {
            playerElement.DefaultPlaybackRate = 1.8;
            playerElement.Play();
        }
        private void btnReverse_Click(object sender, RoutedEventArgs e)
        {
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
                    _timer.Start();
                    btnPlay.Style = GetStyle("PauseButtonStyle");
                    enableButtons(true, true);
                    break;

                case MediaElementState.Paused:
                    _timer.Stop();
                    btnPlay.Style = GetStyle("PlayButtonStyle");
                    enableButtons(true, true);
                    break;

                case MediaElementState.Stopped:
                    _timer.Stop();
                    btnPlay.Style = GetStyle("PlayButtonStyle");
                    progressSlider.Value = 0;
                    enableButtons(true, false);
                    break;
            }
        }

        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
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
        }

        private void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            string hr = GetHresultFromErrorMessage(e);
            Debug.WriteLine("ERROR:" + hr);
            playerElement.Play();
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
            try
            {
                Uri source = new Uri(url);
                string destination = track.artist+" - "+track.title+".mp3";

                if (destination == "")
                {
                    return;
                }

                StorageFile destinationFile = await KnownFolders.MusicLibrary.CreateFileAsync(
                    destination, CreationCollisionOption.GenerateUniqueName);

                BackgroundDownloader downloader = new BackgroundDownloader();
                DownloadOperation download = downloader.CreateDownload(source, destinationFile);

                // Attach progress and completion handlers.
                await HandleDownloadAsync(download, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Download Error"+ ex);
            }
        }

        private void DownloadProgress(DownloadOperation download)
        {
            //MarshalLog(String.Format("Progress: {0}, Status: {1}", download.Guid, download.Progress.Status));

            double percent = 100;
            if (download.Progress.TotalBytesToReceive > 0)
            {
                percent = download.Progress.BytesReceived * 100 / download.Progress.TotalBytesToReceive;
                downloadStatusTextBlock.Text = "Downloading: "+percent+"%";
                totalSize += download.Progress.TotalBytesToReceive;
            }

            //MarshalLog(String.Format(" - Transfered bytes: {0} of {1}, {2}%",
               // download.Progress.BytesReceived, download.Progress.TotalBytesToReceive, percent));

            if (download.Progress.HasRestarted)
            {
               // MarshalLog(" - Download restarted");
            }

            if (download.Progress.HasResponseChanged)
            {
                // We've received new response headers from the server. 
               // MarshalLog(" - Response updated; Header count: " + download.GetResponseInformation().Headers.Count);

                // If you want to stream the response data this is a good time to start. 
                // download.GetResultStreamAt(0); 
            }
        } 

        private async Task HandleDownloadAsync(DownloadOperation download, bool start)
        {
            try
            {
                // Store the download so we can pause/resume.
                //activeDownloads.Add(download);

                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
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
                //Log(String.Format("Completed: {0}, Status Code: {1}", download.Guid, response.StatusCode));
            }
            catch (TaskCanceledException)
            {
                //Log("Download cancelled.");
            }
            catch (Exception)
            {
                //LogException("Error", ex);
            }
            finally
            {
                //activeDownloads.Remove(download);
            }
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

            }

            //Log("Loading background downloads: " + downloads.Count);

            if (downloads.Count > 0)
            {
                totalSize = 0;

                List<Task> tasks = new List<Task>();
                foreach (DownloadOperation download in downloads)
                {
                    //Log(String.Format("Discovered background download: {0}, Status: {1}", download.Guid,
                    //download.Progress.Status));

                    // Attach progress and completion handlers. 
                    tasks.Add(HandleDownloadAsync(download, false));
                }

                // Don't await HandleDownloadAsync() in the foreach loop since we would attach to the second 
                // download only when the first one completed; attach to the third download when the second one 
                // completes etc. We want to attach to all downloads immediately. 
                // If there are actions that need to be taken once downloads complete, await tasks here, outside 
                // the loop. 
                await Task.WhenAll(tasks);

                downloadStatusTextBlock.Text = totalSize.ToString();
            }
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
            playTrack(track);
        }

        private void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            TrackListItem track = (TrackListItem)(sender as FrameworkElement).DataContext;
            saveTrack(track);
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
}
