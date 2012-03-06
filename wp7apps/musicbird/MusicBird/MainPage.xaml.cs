
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xna.Framework.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Devices;
using System.Xml.Serialization;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.BackgroundTransfer;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;



namespace MusicBird
{
    public partial class MainPage : PhoneApplicationPage
    {
        public static bool IsTrial{get; private set;}

        // background transfer object container
        IEnumerable<BackgroundTransferRequest> transferRequests;

        // Booleans for tracking if any transfers are waiting for user action.
        bool WaitingForExternalPower;
        bool WaitingForExternalPowerDueToBatterySaverMode;
        bool WaitingForNonVoiceBlockingNetwork;
        bool WaitingForWiFi;

        // Timer for updating the UI
        DispatcherTimer _timer;
        DispatcherTimer _tileTimer;
        DispatcherTimer _downloadTimer;

        // Indexes into the array of ApplicationBar.Buttons
        const int prevButton = 0;
        const int playButton = 1;
        const int downButton = 3;
        const int nextButton = 2;

        List<TrackListItem> trackList = new List<TrackListItem>();
        String query = "";

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize a timer to update the UI every half-second.
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(0.5);
            _timer.Tick += new EventHandler(UpdateState);            

            _tileTimer = new DispatcherTimer();
            _tileTimer.Interval = TimeSpan.FromSeconds(5);
            _tileTimer.Tick += new EventHandler(setAppTile);
            _tileTimer.Start();

            _downloadTimer = new DispatcherTimer();
            _downloadTimer.Interval = TimeSpan.FromSeconds(2);
            _downloadTimer.Tick += new EventHandler(UpdateUI);

            NetworkChange.NetworkAddressChanged += NetworkAddress_Changed;

            

            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(Instance_PlayStateChanged);


            Instance_PlayStateChanged(null, null);

            showNagScreen();

            updatePlaylist();
            updateLibrary();
        }

        public void test(object s, EventArgs e) {
            System.Diagnostics.Debug.WriteLine("test");
            UpdateUI(null, null);
        }


        /// <summary>
        /// PlayStateChanged event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_PlayStateChanged(object sender, EventArgs e)
        {
            if(readPlaylist().Count == 0) {
                UpdateButtons(false, null, false);
            }

            //_tileTimer.Start();
            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    
                    // Update the UI.
                    positionIndicator.IsIndeterminate = false;
                    positionIndicator.Maximum = BackgroundAudioPlayer.Instance.Track.Duration.TotalSeconds;
                    UpdateButtons(true, false, true);
                    UpdateState(null, null);

                    // Start the timer for updating the UI.
                    _timer.Start();
                    positionIndicator.IsIndeterminate = false;
                    break;

                case PlayState.Paused:
                    // Update the UI.
                    UpdateButtons(true, true, true);
                    UpdateState(null, null);

                    // Stop the timer for updating the UI.
                    _timer.Stop();
                    positionIndicator.IsIndeterminate = false;
                    break;
                case PlayState.Stopped:
                    UpdateButtons(true, true, true);
                    positionIndicator.IsIndeterminate = false;
                    break;
            }
        }


        /// <summary>
        /// Helper method to update the state of the ApplicationBar.Buttons
        /// </summary>
        /// <param name="prevBtnEnabled"></param>
        /// <param name="playBtnEnabled"></param>
        /// <param name="pauseBtnEnabled"></param>
        /// <param name="nextBtnEnabled"></param>
        void UpdateButtons(bool prevBtnEnabled, bool? playBtnEnabled, bool nextBtnEnabled)
        {
            // Set the IsEnabled state of the ApplicationBar.Buttons array
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[prevButton])).IsEnabled = prevBtnEnabled;
            if(playBtnEnabled.HasValue)
            {
                if(playBtnEnabled.Value)
                {
                    ((ApplicationBarIconButton)(ApplicationBar.Buttons[playButton])).IconUri = new Uri("/Images/appbar.transport.play.rest.png", UriKind.Relative);
                }
                else
                {
                    ((ApplicationBarIconButton)(ApplicationBar.Buttons[playButton])).IconUri = new Uri("/Images/appbar.transport.pause.rest.png", UriKind.Relative);
                }
            }
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[nextButton])).IsEnabled = nextBtnEnabled;

            if(Page1.read("albumart"))
            {
                try
                {
                    getAlbumArt(BackgroundAudioPlayer.Instance.Track.Artist + " " + BackgroundAudioPlayer.Instance.Track.Title);
                }
                catch(Exception e) {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        }


        /// <summary>
        /// Updates the status indicators including the State, Track title, 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateState(object sender, EventArgs e)
        {
            txtState.Text = string.Format("State: {0}", BackgroundAudioPlayer.Instance.PlayerState);

            if(BackgroundAudioPlayer.Instance.PlayerState == PlayState.Stopped) {
                return;
            }

            if (BackgroundAudioPlayer.Instance.Track != null)
            {
                String artist="";
                String title = "";
                try
                {
                    title = BackgroundAudioPlayer.Instance.Track.Title;
                }
                catch(Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                try{
                    artist = BackgroundAudioPlayer.Instance.Track.Artist;
                }
                catch(Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }

                string[] arguments = new string[] { title, artist };
                txtTrack.Text = string.Format("Track: {1} - {0}", arguments);

                // Set the current position on the ProgressBar.
                try
                {
                    positionIndicator.Value = BackgroundAudioPlayer.Instance.Position.TotalSeconds;
                }
                catch(Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }

                // Update the current playback position.
                TimeSpan position = new TimeSpan();
                position = BackgroundAudioPlayer.Instance.Position;
                textPosition.Text = String.Format("{0:d2}:{1:d2}:{2:d2}", position.Hours, position.Minutes, position.Seconds);

                // Update the time remaining digits.
                TimeSpan timeRemaining = new TimeSpan();
                TimeSpan duration = new TimeSpan(0, 0, 0, 0, 0);
                try
                {
                    duration = BackgroundAudioPlayer.Instance.Track.Duration;
                }
                catch(Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                timeRemaining = duration - position;
                textRemaining.Text = String.Format("-{0:d2}:{1:d2}:{2:d2}", timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
            }
        }


        /// <summary>
        /// Click handler for the Skip Previous button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void prevButton_Click(object sender, EventArgs e)
        {
            // Show the indeterminate progress bar.
            positionIndicator.IsIndeterminate = true;

            // Disable the button so the user can't click it multiple times before 
            // the background audio agent is able to handle their request.
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[prevButton])).IsEnabled = false;

            // Tell the backgound audio agent to skip to the previous track.
            BackgroundAudioPlayer.Instance.SkipPrevious();
        }


        /// <summary>
        /// Click handler for the Play button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playButton_Click(object sender, EventArgs e)
        {
            if (PlayState.Playing == BackgroundAudioPlayer.Instance.PlayerState)
            {
                BackgroundAudioPlayer.Instance.Pause();
                ApplicationBarIconButton btn = sender as ApplicationBarIconButton;
                btn.IconUri = new Uri("/Images/appbar.transport.play.rest.png",UriKind.Relative);
                positionIndicator.IsIndeterminate = false;
                
            }
            else
            {
                if((Application.Current as App).IsTrial)
                {
                    if(MessageBox.Show("You are using MusicBird in trial mode. Please purchase " +
                        "the paid license to listen to more than 5 tracks per day. Press OK to go to the market.", "Trial",
                            MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
                        _marketPlaceDetailTask.Show();
                    }
                }else{
                    BackgroundAudioPlayer.Instance.Play();
                    ApplicationBarIconButton btn = sender as ApplicationBarIconButton;
                    btn.IconUri = new Uri("/Images/appbar.transport.pause.rest.png", UriKind.Relative);
                    positionIndicator.IsIndeterminate = true;
                }
            }
        }


        /// <summary>
        /// Click handler for the Pause button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void downButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Page1.xaml", UriKind.Relative));
        }


        /// <summary>
        /// Click handler for the Skip Next button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nextButton_Click(object sender, EventArgs e)
        {
            // Show the indeterminate progress bar.
            positionIndicator.IsIndeterminate = true;

            // Disable the button so the user can't click it multiple times before 
            // the background audio agent is able to handle their request.
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[nextButton])).IsEnabled = false;

            // Tell the backgound audio agent to skip to the next track.
            BackgroundAudioPlayer.Instance.SkipNext();
        }

        private void seek( object sender, EventArgs e )
        {
            // Show the indeterminate progress bar.
            positionIndicator.IsIndeterminate = true;

            // Tell the backgound audio agent to skip to the next track.
            BackgroundAudioPlayer.Instance.Position = new TimeSpan(0, 1, 1);
        }

        private void search_KeyUp( object sender, KeyEventArgs e ) {
            if(e.Key == Key.Enter) {
                queryProgress.IsIndeterminate = true;
                queryProgress.Visibility = Visibility.Visible;
                searchItem.Focus();
                query = queryTextbox.Text;
                getResults();
            }
        }

        private void getResults()
        {
            trackList.Clear();

            string url = "http://mp3skull.com/mp3/" + query.Trim().Replace(" ", "_") + ".html";
            System.Diagnostics.Debug.WriteLine("Opening URL "+url);
            WebClient wc = new WebClient();
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);
            NetworkAddress_Changed(null, null);
            wc.OpenReadAsync(new Uri(url));
        }

        private void getResultsFromVpleer() {
            string url = "http://de.vpleer.ru/?q=" + query.Trim().Replace(" ", "+");
            System.Diagnostics.Debug.WriteLine("Opening URL " + url);
            WebClient wc2 = new WebClient();
            wc2.OpenReadCompleted += new OpenReadCompletedEventHandler(wc2_OpenReadCompleted);
            NetworkAddress_Changed(null, null);
            wc2.OpenReadAsync(new Uri(url));
        }

        private void wc_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            
            Regex pattern = new Regex("<a href=\"(.*?.mp3)\" rel=\"nofollow\"", RegexOptions.IgnoreCase);
            Regex pattern2 = new Regex("<div style=\"font-size:15px;\"><b>(.*?) mp3</b></div>",RegexOptions.IgnoreCase);
            String s;
            try
            {
                Stream response = e.Result;
                StreamReader sr = new StreamReader(response, System.Text.Encoding.UTF8);
                try
                {
                    s = sr.ReadToEnd();
                    s = s.Replace("\\/", "/");
                }
                finally
                {
                    sr.Close();
                    response.Close();
                }


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
                    string[] data = getArtistAndTitle(name);
                    TrackListItem item = new TrackListItem(data[0], data[1], g.ToString());
                    trackList.Add(item);
                    matchCount += 1;
                    m = m.NextMatch();
                    n = n.NextMatch();
                }

                System.Diagnostics.Debug.WriteLine("mp3skull: Results found: "+matchCount);
                if(matchCount < 5)
                {
                    getResultsFromVpleer();
                }
                else {
                    stopThrobber();
                }

            }
            catch(WebException ex)
            {
                String status = ex.Status.ToString();
                String msg = ex.Message;
                String statCode = ((HttpWebResponse)ex.Response).StatusCode.ToString();
                String statDescr = ((HttpWebResponse)ex.Response).StatusDescription.ToString();
                stopThrobber();
            }
            finally
            {
                TrackListElement.ItemsSource = null;
                TrackListElement.ItemsSource = trackList;
            }
        }

        private void stopThrobber() {
            queryProgress.Visibility = Visibility.Collapsed;
            queryProgress.IsIndeterminate = false;
        }

        private void wc2_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e )
        {

            //Regex pattern = new Regex("getSize([0-9]+, '(.*?)', '(.*?)', '0', '(.*?)', '(.*?)', 'vpeer.ru');", RegexOptions.IgnoreCase);
            Regex pattern = new Regex("getSize((.*?), '(.*?)', '(.*?)', '0', '(.*?)', '(.*?)'(.*?));", RegexOptions.IgnoreCase);
            String s;
            try
            {
                Stream response = e.Result;
                StreamReader sr = new StreamReader(response, System.Text.Encoding.UTF8);
                try
                {
                    s = sr.ReadToEnd();
                    s = s.Replace("\\/", "/");
                }
                finally
                {
                    sr.Close();
                    response.Close();
                }


                // Match the regular expression pattern against a text string.
                Match m = pattern.Match(s);
                int matchCount = 0;

                while(m.Success)
                {
                    string artist = HttpUtility.HtmlDecode(Uri.UnescapeDataString(m.Groups[5].ToString())).Replace("_", " ");
                    string title = HttpUtility.HtmlDecode(Uri.UnescapeDataString(m.Groups[6].ToString())).Replace("_", " ");
                    string hash = m.Groups[4].ToString();
                    string path = m.Groups[3].ToString();

                    String[] paths = path.Split('/');

                    string url = "http://vpleer.ru/download/0/" + paths[2] + "/" + paths[3] + "/" + paths[4] + "/" + artist + " - " + title + ".mp3";
                    //string asdf = m.Groups[0].ToString();
                    //System.Diagnostics.Debug.WriteLine(url);
                    //System.Diagnostics.Debug.WriteLine(asdf);

                    TrackListItem item = new TrackListItem(artist, title, url);
                    trackList.Add(item);
                    //System.Diagnostics.Debug.WriteLine(trackList.Count);
                    matchCount += 1;
                    m = m.NextMatch();
                }


                System.Diagnostics.Debug.WriteLine("vpleer: Results found: " + matchCount);

            }
            catch(WebException ex)
            {
                String status = ex.Status.ToString();
                String msg = ex.Message;
                String statCode = ((HttpWebResponse)ex.Response).StatusCode.ToString();
                String statDescr = ((HttpWebResponse)ex.Response).StatusDescription.ToString();
            }
            finally
            {
                stopThrobber();

                TrackListElement.ItemsSource = null;
                TrackListElement.ItemsSource = trackList;
            }
        }

        private string[] getArtistAndTitle(string name)
        {
            String artist = "";
            String title = "";

            replaceNumbersAndExtension(name);

            name = HttpUtility.HtmlDecode(name);

            name = name.Trim();

            int count = name.Length - name.Replace("-", "").Length+1;     // count parts of string separated by dash

            if (count == 1) {                                           // John Wayne Heaven
                int len = name.Split((char)32).Length;                  // split at space
                if (len == 1) {                                         // Ex. JohnWayneHeaven
                    artist = name;
                    title = "";
                }else if (len > 1)                                      // Ex. JohnWayne Heaven or John Wayne Heaven
                {
                    string[] nameArray = name.Split((char)32);
                    for (int i = 0; i < len; i++)
                    {
                        if (i < len / 2)
                        {
                            artist += nameArray[i];
                        }
                        else
                        {
                            title += nameArray[i];
                        }
                    }
                }
            }
            else if (count == 2) {                                      // John Wayne - Heaven
                artist = name.Split((char)45)[0];
                title = name.Split((char)45)[1];
            }else if(count > 2){                                        // John Wayne - Heaven - Live at Brixton Academy
                string[] parts = name.Split((char)45);
                int len = parts.Length;
                for (int i = 0; i < len; i++)
                {
                    if (i < len / 2)
                    {
                        artist += parts[i];
                    }
                    else
                    {
                        title += parts[i];
                    }
                }
            }

            artist = artist.Replace( "_", " " );
            title = title.Replace( "_", " " );

            artist = UppercaseWords( artist );
            title = UppercaseWords( title );


            artist = artist.Trim();
            title = title.Trim();

            artist = replaceNumbersAndExtension(artist);
            title = replaceNumbersAndExtension(title);

            artist = artist.Trim();
            title = title.Trim();

            return new String[] { artist, title };
        }

        private string replaceNumbersAndExtension( string name ) {
            string pattern = "^[0-9]{1,3}?\\.(\\s+)?";                     // replace leading track number (01. or 1. or 12. )
            Regex rgx = new Regex(pattern);
            name = rgx.Replace(name, "");

            string pattern2 = "[0-9]{1,3}?[\\s]*-(\\s+)?";                 // replace leading track number (01- or 1 - or 12- )
            Regex rgx2 = new Regex(pattern2);
            name = rgx2.Replace(name, "");

            string pattern3 = "(\\.wma|\\.mp3|\\.mid)";                        // replace file ext. (.mp3)
            Regex rgx3 = new Regex(pattern3, RegexOptions.IgnoreCase);
            name = rgx3.Replace(name, "");

            string pattern4 = "\\([0-9]+[|\\.|-]?\\)";                  // replace number in brackets ((13.) (09) (11))
            Regex rgx4 = new Regex(pattern4);
            name = rgx4.Replace(name, "");

            string pattern1 = "^[0-9]{1,3}(\\s+)?";                     // replace leading track number (01 or 1 or 12 )
            Regex rgx1 = new Regex(pattern1);
            name = rgx1.Replace(name, "");


            return name;
        }

        private void trackItem_Click(object sender, RoutedEventArgs e)
        {
            TrackListItem selectedTrack = (sender as FrameworkElement).DataContext as TrackListItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);

            addToPlaylist(selectedTrack.artist, selectedTrack.title, selectedTrack.url);
            updatePlaylist();
            
            List<String[]> oldItems = readPlaylist();
            int itemsCount = oldItems.Count;

            AudioPlaybackAgent1.AudioPlayer.playAtPosition(itemsCount-1,BackgroundAudioPlayer.Instance);
            Instance_PlayStateChanged(null, null);
            positionIndicator.IsIndeterminate = true;
            Panorama.SelectedItem = playerItem;
        }

        private void trackItem_Hold(object sender, RoutedEventArgs e) {
            
            // get selected Item
            ListBoxItem selectedListBoxItem = getListBoxItem(sender);
            if (selectedListBoxItem == null)
            {
                return;
            }
            // get selected Track
            TrackListItem selectedTrack = selectedListBoxItem.DataContext as TrackListItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);

            // get selected Context menu item
            var menuItem = (MenuItem) sender;
            var tag = menuItem.Tag.ToString();
            switch (tag) { 
                case "add":
                    addToPlaylist(current.Artist, current.Title, current.Source.ToString());
                    updatePlaylist();
                    break;
                case "play":
                    trackItem_Click(sender, e);
                    break;
                case "download":
                    if((Application.Current as App).IsTrial)
                    {
                        if(MessageBox.Show("You are using MusicBird in trial mode. Please purchase " +
                            "the paid license to use this feature. Press OK to go to the market.", "Trial",
                                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
                            _marketPlaceDetailTask.Show();
                        }
                    }
                    else {
                        saveTrack( selectedTrack.artist + " - " + selectedTrack.title, selectedTrack.url );
                    }
                    break;
                default:
                    break;
            
            }
        }

        private void setAppTile(object sender, EventArgs e)
        {
            // Application Tile is always the first Tile, even if it is not pinned to Start.
            ShellTile TileToFind = ShellTile.ActiveTiles.First();

            // Application should always be found
            if (TileToFind != null)
            {

                string status = "Paused";
                string name = "";
                int count = 0;
                try
                {
                    count = BackgroundTransferService.Requests.Count();
                }
                catch(Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                try
                {
                    if(BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
                    {
                        status = "Playing...";
                    }
                    else
                    {
                        status = "Paused";
                    }
                }
                catch(Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }


                if(BackgroundAudioPlayer.Instance != null)
                {
                    if(BackgroundAudioPlayer.Instance.Track != null)
                    {
                        name = BackgroundAudioPlayer.Instance.Track.Title + " - " + BackgroundAudioPlayer.Instance.Track.Artist;
                    }
                }


                // Set the properties to update for the Application Tile.
                // Empty strings for the text values and URIs will result in the property being cleared.
                StandardTileData NewTileData = new StandardTileData
                {
                    Title = "MusicBird",
                    //BackgroundImage = new Uri(textBoxBackgroundImage.Text, UriKind.Relative),
                    Count = count,
                    BackTitle = status,
                    //BackBackgroundImage = new Uri(textBoxBackBackgroundImage.Text, UriKind.Relative),
                    BackContent = name
                };

                // Update the Application Tile
                TileToFind.Update(NewTileData);
            }

        }

        

            private void saveTrack(string filename, string uri)
            {
                // Check to see if the maximum number of requests per app has been exceeded.
                if (BackgroundTransferService.Requests.Count() >= 5)
                {
                    // Note: Instead of showing a message to the user, you could store the
                    // requested file URI in isolated storage and add it to the queue later.
                    MessageBox.Show("The maximum number of background file transfer requests for this application has been exceeded. ");
                    return;
                }

                // Get the URI of the file to be transferred from the Tag property
                // of the button that was clicked.
                string transferFileName = filename;
                Uri transferUri = new Uri(uri, UriKind.RelativeOrAbsolute);


                // Create the new transfer request, passing in the URI of the file to 
                // be transferred.
                BackgroundTransferRequest transferRequest = new BackgroundTransferRequest(transferUri);

                // Set the transfer method. GET and POST are supported.
                transferRequest.Method = "GET";

                // Get the file name from the end of the transfer URI and create a local URI 
                // in the "transfers" directory in isolated storage.

                using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!isoStore.DirectoryExists("/shared/transfers"))
                    {
                        isoStore.CreateDirectory("/shared/transfers");
                    }
                }

                Uri downloadUri = new Uri(filename, UriKind.RelativeOrAbsolute);
                System.Diagnostics.Debug.WriteLine("filename is " + filename + ".mp3");
                transferRequest.DownloadLocation = new Uri("/shared/transfers/" + filename + ".mp3", UriKind.Relative);

                // Pass custom data with the Tag property. In this example, the friendly name
                // is passed.
                transferRequest.Tag = filename + ".mp3";

                bool allowCellular = Page1.read("allowCellular");
                bool allowBattery = Page1.read("allowBattery");
                if(allowCellular && allowBattery)
                {
                    transferRequest.TransferPreferences = TransferPreferences.AllowCellularAndBattery;
                }
                else if(allowCellular)
                {
                    transferRequest.TransferPreferences = TransferPreferences.AllowCellular;
                }
                else if(allowBattery)
                {
                    transferRequest.TransferPreferences = TransferPreferences.AllowBattery;
                }
                

                // Add the transfer request using the BackgroundTransferService. Do this in 
                // a try block in case an exception is thrown.
                try
                {
                    BackgroundTransferService.Add(transferRequest);
                    transferRequest.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);
                    transferRequest.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);
                    UpdateUI(null, null);
                    Panorama.SelectedItem = downloadItem;
                    _downloadTimer.Start();
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show("Unable to add background transfer request. " + ex.Message);
                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to add background transfer request.");
                }
                setAppTile(null, null);
            }

            private void updatePlaylist() {
                List<TrackListItem> tList = new List<TrackListItem>();
                List<String[]> trackList = readPlaylist();
                foreach(var item in trackList){
                    tList.Add(new TrackListItem(item[0], item[1], item[2]));
                }
                PlaylistElement.ItemsSource = tList;
            }

            private void updateLibrary()
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    List<LibraryItem> trackList = new List<LibraryItem>();
                    
                    string[] files = myStore.GetFileNames();
                    if (files.Length > 0)
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            LibraryItem item = new LibraryItem(files[i]);
                            if(item.filename.IndexOf(".mp3") == item.filename.Length-4)
                            {
                                trackList.Add(item);
                            }
                        }
                        LibraryElement.ItemsSource = trackList;
                    }
                } 
            }

            private void playlistItem_Click(object sender, RoutedEventArgs e) {
                TrackListItem selectedTrack = (sender as FrameworkElement).DataContext as TrackListItem;
                AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);
                String[] current2 = new String[] { selectedTrack.artist, selectedTrack.title, selectedTrack.url };

                List<string[]> oldItems = readPlaylist();
                
                int counter = -1;

                foreach(String[] item in oldItems) {
                    counter++;
                    if(item[2].Equals(current2[2])){
                        break;
                    }
                }

                System.Diagnostics.Debug.WriteLine("MainPage.xaml.cs:playlistItem_Click ___ Found playlist item at index " + counter);
                AudioPlaybackAgent1.AudioPlayer.playAtPosition(counter, BackgroundAudioPlayer.Instance);

                Instance_PlayStateChanged(null, null);
                positionIndicator.IsIndeterminate = true;
                Panorama.SelectedItem = playerItem;
            }

            private void playlistItem_Hold(object sender, RoutedEventArgs e)
            {
                ListBoxItem selectedListBoxItem = PlaylistElement.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
                if (selectedListBoxItem == null)
                {
                    System.Diagnostics.Debug.WriteLine("Selected LB is null");
                    return;
                }
                // get selected Track
                TrackListItem selectedTrack = selectedListBoxItem.Content as TrackListItem;
                AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);
                
                // loop through items and check if item matches selectedItem (selectedIndex is not updated with context menu...)
                int i = 0;
                TrackListItem currentTrack = (PlaylistElement.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem).Content as TrackListItem;
                while (currentTrack != selectedTrack) {
                    i++;
                    currentTrack = (PlaylistElement.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem).Content as TrackListItem;
                }
                int selectedIndex = i;

                // get selected Context menu item
                var menuItem = (MenuItem)sender;
                var tag = menuItem.Tag.ToString();
                switch (tag)
                {
                    case "delete":
                        removeFromPlaylist(selectedIndex);
                        updatePlaylist();
                        break;
                    case "play":
                        playlistItem_Click(sender, e);
                        break;
                    default:
                        break;

                }
            }

            private void libraryItem_Click(object sender, RoutedEventArgs e)
            {
                LibraryItem selectedTrack = (sender as FrameworkElement).DataContext as LibraryItem;
                AudioTrack current = new AudioTrack(new Uri(selectedTrack.filename, UriKind.Relative), selectedTrack.filename, "", "", null);

                String[] name = getArtistAndTitle(selectedTrack.filename);

                addToPlaylist(name[0] ,name[1], selectedTrack.filename);
                updatePlaylist();

                //BackgroundAudioPlayer.Instance.Play();
                //updatePlaylist();
               
                List<String[]> oldItems = readPlaylist();
                int itemsCount = oldItems.Count;

                AudioPlaybackAgent1.AudioPlayer.playAtPosition(itemsCount - 1, BackgroundAudioPlayer.Instance);
                Instance_PlayStateChanged(null, null);
                positionIndicator.IsIndeterminate = true;
                Panorama.SelectedItem = playerItem;

            }

            private void libraryItem_Hold(object sender, RoutedEventArgs e)
            {
                ListBoxItem selectedListBoxItem = LibraryElement.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
                if (selectedListBoxItem == null)
                {
                    return;
                }
                // get selected Track
                LibraryItem selectedTrack = selectedListBoxItem.DataContext as LibraryItem;
                AudioTrack current = new AudioTrack(new Uri(selectedTrack.filename, UriKind.RelativeOrAbsolute), selectedTrack.filename, "", "", null);

                // get selected Context menu item
                var menuItem = (MenuItem)sender;
                var tag = menuItem.Tag.ToString();
                switch (tag)
                {
                    case "add":
                        addToPlaylist(current.Artist, current.Title, current.Source.ToString());
                        updatePlaylist();
                        break;
                    case "play":
                        libraryItem_Click(sender, e);
                        break;
                    case "properties":
                        NavigationService.Navigate(new Uri("/Properties.xaml?filename="+selectedTrack.filename, UriKind.Relative));
                        break;
                    case "delete":
                        using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (store.FileExists(selectedTrack.filename))
                            {
                                try
                                {
                                    if(HttpUtility.UrlDecode(BackgroundAudioPlayer.Instance.Track.Source.ToString()).Equals(selectedTrack.filename))
                                    {
                                        MessageBox.Show("The track is currently played. Please try again later.");
                                    }
                                    else
                                    {
                                        store.DeleteFile(selectedTrack.filename);
                                    }
                                }
                                catch(Exception ex)
                                {
                                    store.DeleteFile(selectedTrack.filename);
                                }
                            }
                            else {
                                MessageBox.Show(selectedTrack.filename);
                            }
                        }
                        updateLibrary();
                        break;
                    default:
                        break;

                }
            }

            private bool IsolatedStorageFileExists(string name)
            {
                using (var folder = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    return folder.FileExists(name);
                }
            }

            private bool IsNetworkAvailable()
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }

            private void NetworkAddress_Changed(object sender, EventArgs e)
            {
                if (!IsNetworkAvailable())
                {
                    MessageBox.Show("Network connection lost. Please connect to a network if you want to access online music.");
                }
            }

            private ListBoxItem getListBoxItem(Object sender) {
                return TrackListElement.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            }

            private void UpdateRequestsList(Object sender, EventArgs e)
            {
                // The Requests property returns new references, so make sure that
                // you dispose of the old references to avoid memory leaks.
                /*if (transferRequests != null)
                {
                    foreach (var request in transferRequests)
                    {
                        request.Dispose();
                    }
                }*/
                // this is apparently not necessary. Works without disposing,
                // the memory goes down again after the req. is finished.

                transferRequests = BackgroundTransferService.Requests;
                System.Diagnostics.Debug.WriteLine(Microsoft.Phone.Info.DeviceStatus.ApplicationCurrentMemoryUsage.ToString());
            }

            private void UpdateUI( object sender, EventArgs e )
            {
                System.Diagnostics.Debug.WriteLine("Updating UI");
                // Update the list of transfer requests
                UpdateRequestsList(null, null);

                // If there are 1 or more transfers, hide the "no transfers"
                // TextBlock. IF there are zero transfers, show the TextBlock.
                if (transferRequests.Count<BackgroundTransferRequest>() > 0)
                {
                    EmptyTextBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyTextBlock.Visibility = Visibility.Visible;
                }

                // Update the TransferListBox with the list of transfer requests.
                TransferListBox.ItemsSource = transferRequests;

                /*Dictionary<String,String> prefs = MySystem.readPrefs();

                if(!prefs.ContainsKey("shuffle") || prefs["shuffle"].Equals("false")) button1.Opacity = 0.5;
                else button1.Opacity = 1;

                if(!prefs.ContainsKey("repeat") || prefs["repeat"].Equals("false")) button2.Opacity = 0.5;
                else button2.Opacity = 1;*/

            }

            protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
            {
                base.OnNavigatedTo( e );

                // Reset all of the user action Booleans on page load.
                WaitingForExternalPower = false;
                WaitingForExternalPowerDueToBatterySaverMode = false;
                WaitingForNonVoiceBlockingNetwork = false;
                WaitingForWiFi = false;

                // When the page loads, refresh the list of file transfers.
                InitialTransferStatusCheck();
                UpdateUI(null, null);
                updateLibrary();

                const String _playSongKey = "playSong";
                const String _showPlayerKey = "showPlayer";

                MediaLibrary library = new MediaLibrary();

                if(NavigationContext.QueryString.ContainsKey(_playSongKey))
                {
                    // We were launched from a history item.
                    // Change _playingSong even if something was already playing 
                    // because the user directly chose a song history item.

                    // Use the navigation context to find the song by name.
                    String songToPlay = NavigationContext.QueryString[_playSongKey];

                    System.Diagnostics.Debug.WriteLine("Trying to play song: "+songToPlay);

                    String[] name = getArtistAndTitle(songToPlay);

                    addToPlaylist(name[0], name[1], songToPlay);

                    List<String[]> oldItems = readPlaylist();
                    int itemsCount = oldItems.Count;

                    AudioPlaybackAgent1.AudioPlayer.playAtPosition(itemsCount - 1, BackgroundAudioPlayer.Instance);
                    Instance_PlayStateChanged(null, null);
                    positionIndicator.IsIndeterminate = true;
                    Panorama.SelectedItem = playerItem;
                }
                else if(NavigationContext.QueryString.ContainsKey(_showPlayerKey))
                {
                    Panorama.SelectedItem = playerItem;
                }

            }

            private void showNagScreen() {
                if((Application.Current as App).IsTrial)
                {
                    if(MessageBox.Show("You are using MusicBird in Trial mode. You can only listen " +
                        "to 5 tracks per day and you can't download the music. Press OK to go to the market.", "Trial",
                                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
                        _marketPlaceDetailTask.Show();
                    }
                }
            }

            private void InitialTransferStatusCheck()
            {
                UpdateRequestsList(null, null);

                foreach (var transfer in transferRequests)
                {
                    transfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);
                    transfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);
                    ProcessTransfer(transfer);
                }

                if (WaitingForExternalPower)
                {
                    MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power or change the settings in MusicBird to continue transferring.");
                }
                if (WaitingForExternalPowerDueToBatterySaverMode)
                {
                    MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power or disable Battery Saver Mode to continue transferring.");
                }
                if (WaitingForNonVoiceBlockingNetwork)
                {
                    MessageBox.Show("You have one or more file transfers waiting for a network that supports simultaneous voice and data.");
                }
                if (WaitingForWiFi)
                {
                    MessageBox.Show("You have one or more file transfers waiting for a WiFi connection. Connect your device to a WiFi network to continue transferring or change the settings in MusicBird.");
                }
            }

            private void ProcessTransfer(BackgroundTransferRequest transfer)
            {
                switch (transfer.TransferStatus)
                {
                    case TransferStatus.Transferring:
                        System.Diagnostics.Debug.WriteLine("Transferring");
                        _downloadTimer.Start();
                        break;
                    case TransferStatus.Completed:

                        // If the status code of a completed transfer is 200 or 206, the
                        // transfer was successful
                        if (transfer.StatusCode == 200 || transfer.StatusCode == 206)
                        {
                            // Remove the transfer request in order to make room in the 
                            // queue for more transfers. Transfers are not automatically
                            // removed by the system.
                            RemoveTransferRequest(transfer.RequestId);
                            UpdateUI(null, null);

                            if(transferRequests.Count<BackgroundTransferRequest>() > 0) {
                                _downloadTimer.Stop();
                            }

                            // In this example, the downloaded file is moved into the root
                            // Isolated Storage directory
                            string filename = "";
                            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                filename = transfer.Tag;
                                if (isoStore.FileExists(filename))
                                {
                                    isoStore.DeleteFile(filename);
                                }
                                isoStore.MoveFile(transfer.DownloadLocation.OriginalString, filename);
                            }

                            updateLibrary();
                            try
                            {
                                if(Panorama.SelectedItem.Equals(downloadItem)) Panorama.SelectedItem = libraryItem;
                            }
                            catch(NullReferenceException e) {
                                System.Diagnostics.Debug.WriteLine(e.Message);
                            }

                            StreamResourceInfo sri = null;
                            Uri imageUri = new Uri("MB_173.png", UriKind.Relative);
                            sri = Application.GetResourceStream(imageUri);

                            WriteableBitmap wb = new WriteableBitmap(173, 173);
                            wb.SetSource(sri.Stream);

                            MemoryStream stream = new MemoryStream();
                            wb.SaveJpeg(stream, 173, 173, 0, 90);
                            stream.Seek(0, SeekOrigin.Begin);

                            MediaHistoryItem mediaHistoryItem = new MediaHistoryItem();

                            //<hubTileImageStream> must be a valid ImageStream.
                            mediaHistoryItem.ImageStream = stream; 
                            //mediaHistoryItem.Source = "asdfasdf";
                            mediaHistoryItem.Title = getArtistAndTitle(filename)[0];
                            mediaHistoryItem.PlayerContext.Add("playSong", filename);
                            MediaHistory.Instance.WriteAcquiredItem(mediaHistoryItem);

                        }
                        else
                        {
                            // This is where you can handle whatever error is indicated by the
                            // StatusCode and then remove the transfer from the queue. 
                            RemoveTransferRequest(transfer.RequestId);

                            if (transfer.TransferError != null)
                            {
                                // Handle TransferError if one exists.
                            }
                        }
                        break;


                    case TransferStatus.WaitingForExternalPower:
                        WaitingForExternalPower = true;
                        break;

                    case TransferStatus.WaitingForExternalPowerDueToBatterySaverMode:
                        WaitingForExternalPowerDueToBatterySaverMode = true;
                        break;

                    case TransferStatus.WaitingForNonVoiceBlockingNetwork:
                        WaitingForNonVoiceBlockingNetwork = true;
                        break;

                    case TransferStatus.WaitingForWiFi:
                        WaitingForWiFi = true;
                        break;
                }
            }

            void transfer_TransferStatusChanged(object sender, BackgroundTransferEventArgs e)
            {
                ProcessTransfer(e.Request);
                UpdateUI(null, null);
            }

            void transfer_TransferProgressChanged(object sender, BackgroundTransferEventArgs e)
            {
                System.Diagnostics.Debug.WriteLine("Downloaded " + e.Request.BytesReceived/e.Request.TotalBytesToReceive*100+" %");
                //UpdateUI();
            }

            private void CancelButton_Click(object sender, EventArgs e)
            {
                // The ID for each transfer request is bound to the
                // Tag property of each Remove button.
                string transferID = ((Button)sender).Tag as string;

                // Delete the transfer request
                RemoveTransferRequest(transferID);

                // Refresh the list of file transfers
                UpdateUI(null, null);
            }

            private void RemoveTransferRequest(string transferID)
            {
                // Use Find to retrieve the transfer request with the specified ID.
                BackgroundTransferRequest transferToRemove = BackgroundTransferService.Find(transferID);

                // Try to remove the transfer from the background transfer service.
                try
                {
                    BackgroundTransferService.Remove(transferToRemove);
                }
                catch (Exception e)
                {
                    // Handle the exception.
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            static string UppercaseWords( string value )
            {
                char[] array = value.ToCharArray();
                // Handle the first letter in the string.
                if ( array.Length >= 1 )
                {
                    if ( char.IsLower( array[0] ) )
                    {
                        array[0] = char.ToUpper( array[0] );
                    }
                }
                // Scan through the letters, checking for spaces.
                // ... Uppercase the lowercase letters following spaces.
                for ( int i = 1 ; i < array.Length ; i++ )
                {
                    if ( array[i - 1] == ' ' )
                    {
                        if ( char.IsLower( array[i] ) )
                        {
                            array[i] = char.ToUpper( array[i] );
                        }
                    }
                }
                return new string( array );
            }

            private void addToPlaylist( String artist, String title, String url )
            {
                List<String[]> pl = readPlaylist();
                pl.Add(new String[]{artist, title, url});
                writePlaylist(pl);
            }

            private List<String[]> getFromPlaylist()
            {
                return readPlaylist();
            }

            private void removeFromPlaylist( object item ) {
                List<String[]> pl = readPlaylist();
                if(item is int){
                    int index = (int) item;
                    pl.RemoveAt(index);
                }
                else if(item is string[])
                {
                    string[] part = (string[])item;
                    pl.Remove(part);
                }
                writePlaylist(pl);
            }

            private void writePlaylist( List<String[]> playlist ) {
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                xmlWriterSettings.Indent = true;

                using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(myIsolatedStorage.FileExists("Playlist.xml")) {
                        myIsolatedStorage.DeleteFile("Playlist.xml");
                    }
                    using(IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("Playlist.xml", FileMode.Create))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<String[]>));
                        using(XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSettings))
                        {
                            serializer.Serialize(xmlWriter, playlist);
                        }
                    }
                }
            }

            private List<String[]> readPlaylist() {
                try
                {
                    using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if(!myIsolatedStorage.FileExists("Playlist.xml")) {
                            return new List<string[]>();
                        }
                        using(IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("Playlist.xml", FileMode.Open))
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(List<String[]>));
                            List<String[]> data = (List<String[]>)serializer.Deserialize(stream);
                            return data;
                        }
                    }
                }
                catch
                {
                    // add some code here
                    throw new IsolatedStorageException("Could not get Playlist file from UserStore"); 
                }
            }

            private void getAlbumArt( string searchterm )
            {
                String websiteURL = "http://api.bing.net/json.aspx?AppId=A34B1552C3B3DF826089895CCA0D868F5B4AE201&Query=" + searchterm.Trim() + "%20Cover&Sources=Image&Filters=Size:Small";
                WebClient c = new WebClient();
                c.DownloadStringAsync(new Uri(websiteURL));
                c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(c_DownloadStringCompleted);
            }

            private void c_DownloadStringCompleted( object sender, DownloadStringCompletedEventArgs e )
            {
                lock(this)
                {
                    try
                    {
                        string s = e.Result;
                        s = s.Replace("\\/", "/");
                        //"MediaUrl":"http:\/\/plasticosydecibelios.com\/coldplay-paradise-cover.jpg"
                        Regex pattern = new Regex("\"MediaUrl\":\"(.*?)\"", RegexOptions.IgnoreCase);
                        Match m = pattern.Match(s);
                        if(m.Success)
                        {
                            Group g = m.Groups[1];
                            string url = g.ToString();
                            albumartImage.Source = new BitmapImage(new Uri(url, UriKind.Absolute));
                        }
                    }
                    catch(WebException ex) {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }

            private void toggleRepeat( object sender, RoutedEventArgs e )
            {
                
            }

            private void toggleShuffle( object sender, RoutedEventArgs e )
            {
                
            }

    }

    public class TrackListItem {
        public string title { get; set; }
        public string artist { get; set; }
        public string size { get; set; }
        public string duration { get; set; }
        public string url { get; set; }

        public TrackListItem(String artist, String title, String url) {
            this.artist = artist;
            this.title = title;
            this.url = url;
        }
    }

    public class LibraryItem
    {
        public string filename { get; set; }
        

        public LibraryItem(String filename)
        {
            this.filename = filename;
        }
    }
}