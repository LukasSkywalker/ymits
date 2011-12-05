using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Threading;
using System.Windows.Navigation;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Shell;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Resources;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Media;
using Microsoft.Phone.Tasks;
using System.Net.NetworkInformation;
using Microsoft.Phone.BackgroundTransfer;

namespace MusicBird
{
    public partial class MainPage : PhoneApplicationPage
    {
        //background transfer object container
        IEnumerable<BackgroundTransferRequest> transferRequests;

        // Booleans for tracking if any transfers are waiting for user action.
        bool WaitingForExternalPower;
        bool WaitingForExternalPowerDueToBatterySaverMode;
        bool WaitingForNonVoiceBlockingNetwork;
        bool WaitingForWiFi;

        // Timer for updating the UI
        DispatcherTimer _timer;
        DispatcherTimer _tileTimer;

        // Indexes into the array of ApplicationBar.Buttons
        const int prevButton = 0;
        const int playButton = 1;
        const int downButton = 3;
        const int nextButton = 2;

        WebClient wc = new WebClient();
        

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            //DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);

            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(Instance_PlayStateChanged);

            /*MouseClickManager fMouseManager = new MouseClickManager(200);
            fMouseManager.Click += new MouseButtonEventHandler(spItem_Click);
            fMouseManager.DoubleClick += new MouseButtonEventHandler(YourControl_DoubleClick);*/
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
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

            NetworkChange.NetworkAddressChanged += NetworkAddress_Changed;

            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(Instance_PlayStateChanged);

            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                // If audio was already playing when the app was launched, update the UI.
                positionIndicator.IsIndeterminate = false;
                positionIndicator.Maximum = BackgroundAudioPlayer.Instance.Track.Duration.TotalSeconds;
                UpdateButtons(true, false, true);
                UpdateState(null, null);
            }

            //DEBUG IsolatedStorageExplorer.Explorer.Start("localhost");

            updatePlaylist();
            updateLibrary();
        }


        /// <summary>
        /// PlayStateChanged event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_PlayStateChanged(object sender, EventArgs e)
        {
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
        void UpdateButtons(bool prevBtnEnabled, bool playBtnEnabled, bool nextBtnEnabled)
        {
            // Set the IsEnabled state of the ApplicationBar.Buttons array
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[prevButton])).IsEnabled = prevBtnEnabled;
            if (playBtnEnabled) {
                ((ApplicationBarIconButton)(ApplicationBar.Buttons[playButton])).IconUri = new Uri("/Images/appbar.transport.play.rest.png", UriKind.Relative);
            }else{
                ((ApplicationBarIconButton)(ApplicationBar.Buttons[playButton])).IconUri = new Uri("/Images/appbar.transport.pause.rest.png", UriKind.Relative);
            }
            //((ApplicationBarIconButton)(ApplicationBar.Buttons[playButton])).IsEnabled = playBtnEnabled;
            //((ApplicationBarIconButton)(ApplicationBar.Buttons[pauseButton])).IsEnabled = pauseBtnEnabled;
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[nextButton])).IsEnabled = nextBtnEnabled;
            //((ApplicationBarIconButton)(ApplicationBar.Buttons[downButton])).IsEnabled = downBtnEnabled;
        }


        /// <summary>
        /// Updates the status indicators including the State, Track title, 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateState(object sender, EventArgs e)
        {
            txtState.Text = string.Format("State: {0}", BackgroundAudioPlayer.Instance.PlayerState);

            if (BackgroundAudioPlayer.Instance.Track != null)
            {
                string[] arguments = new string[] { BackgroundAudioPlayer.Instance.Track.Title, BackgroundAudioPlayer.Instance.Track.Artist };
                txtTrack.Text = string.Format("Track: {1} - {0}", arguments);

                // Set the current position on the ProgressBar.
                positionIndicator.Value = BackgroundAudioPlayer.Instance.Position.TotalSeconds;

                // Update the current playback position.
                TimeSpan position = new TimeSpan();
                position = BackgroundAudioPlayer.Instance.Position;
                textPosition.Text = String.Format("{0:d2}:{1:d2}:{2:d2}", position.Hours, position.Minutes, position.Seconds);

                // Update the time remaining digits.
                TimeSpan timeRemaining = new TimeSpan();
                timeRemaining = BackgroundAudioPlayer.Instance.Track.Duration - position;
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
                BackgroundAudioPlayer.Instance.Play();
                ApplicationBarIconButton btn = sender as ApplicationBarIconButton;
                btn.IconUri = new Uri("/Images/appbar.transport.pause.rest.png", UriKind.Relative);
                positionIndicator.IsIndeterminate = true;
            }
            
        }


        /// <summary>
        /// Click handler for the Pause button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void downButton_Click(object sender, EventArgs e)
        {
            string url = BackgroundAudioPlayer.Instance.Track.Source.ToString();
            string name = BackgroundAudioPlayer.Instance.Track.Artist + " - " + BackgroundAudioPlayer.Instance.Track.Title;
            saveTrack(name, url);
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

        private void getResults(string query)
        {
            string url = "http://mp3skull.com/mp3/" + query.Replace(" ", "_") + ".html";
            System.Diagnostics.Debug.WriteLine("Opening URL "+url);
            wc.CancelAsync();
            wc.OpenReadAsync(new Uri(url));
        }

        private void wc_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            
            Regex pattern = new Regex("<a href=\"(.*?.mp3)\" rel=\"nofollow\"", RegexOptions.IgnoreCase);
            Regex pattern2 = new Regex("<div style=\"font-size:15px;\"><b>(.*?) mp3</b></div>",RegexOptions.IgnoreCase);
            String s;
            Stream response = e.Result;
            try
            {
                StreamReader sr = new StreamReader(response, System.Text.Encoding.UTF8);
                try
                {
                    s = sr.ReadToEnd();
                    s = s.Replace("\\/", "/");
                }
                finally
                {
                    sr.Close();
                }


                // Match the regular expression pattern against a text string.
                Match m = pattern.Match(s);
                Match n = pattern2.Match(s);
                //Dim urls(m.Length) As String
                int matchCount = 0;
                
                List<TrackListItem> trackList = new List<TrackListItem>();
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
                    //urls.Push(g.ToString());
                    matchCount += 1;
                    m = m.NextMatch();
                    n = n.NextMatch();
                }

                TrackListElement.ItemsSource = trackList;

                System.Diagnostics.Debug.WriteLine("Results found.");

                //startDownload();

            }
            finally
            {
                response.Close();
                queryProgress.Visibility = Visibility.Collapsed;
                queryProgress.IsIndeterminate = false;
            }
        }

        private string[] getArtistAndTitle(string name)
        {
            String artist = "";
            String title = "";

            string pattern = "^[0-9]{1,2}?\\.\\s+";                     //replace leading track number (01. or 1. or 12. )
            Regex rgx = new Regex(pattern);
            name = rgx.Replace(name, "");

            string pattern2 = "[0-9]{1,2}?[\\s]*-\\s+";                 //replace leading track number (01- or 1 - or 12- )
            Regex rgx2 = new Regex(pattern2);
            name = rgx2.Replace(name, "");

            string pattern3 = "(\\.wma|\\.mp3)";                        //replace file ext. (.mp3)
            Regex rgx3 = new Regex(pattern3);
            name = rgx3.Replace(name, "");

            name = name.Trim();

            int count = name.Length - name.Replace("-", "").Length+1;     //count parts of string separated by dash

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

            return new String[] { artist, title };
        }

        private void queryButton_Click(object sender, RoutedEventArgs e)
        {
            queryProgress.Visibility = Visibility.Visible;
            queryProgress.IsIndeterminate = true;
            getResults(queryTextbox.Text);
        }


        private void trackItem_Click(object sender, RoutedEventArgs e)
        {
            TrackListItem selectedTrack = (sender as FrameworkElement).DataContext as TrackListItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);

            AudioPlaybackAgent1.AudioPlayer.addToList(current);
            BackgroundAudioPlayer.Instance.Track = current;
            BackgroundAudioPlayer.Instance.Play();
            updatePlaylist();
            Panorama.DefaultItem = playerItem;
        }

        private void trackItem_Hold(object sender, RoutedEventArgs e) {
            //OLD TrackListItem selectedTrack = (sender as FrameworkElement).DataContext as TrackListItem;
            
            //get selected Item
            ListBoxItem selectedListBoxItem = getListBoxItem(sender);
            if (selectedListBoxItem == null)
            {
                return;
            }
            //get selected Track
            TrackListItem selectedTrack = selectedListBoxItem.DataContext as TrackListItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);

            //get selected Context menu item
            var menuItem = (MenuItem) sender;
            var tag = menuItem.Tag.ToString();
            switch (tag) { 
                case "add":
                    AudioPlaybackAgent1.AudioPlayer.addToList(current);
                    updatePlaylist();
                    break;
                case "play":
                    AudioPlaybackAgent1.AudioPlayer.addToList(current);
                    BackgroundAudioPlayer.Instance.Track = current;
                    BackgroundAudioPlayer.Instance.Play();
                    updatePlaylist();
                    Panorama.DefaultItem = playerItem;
                    break;
                case "download":
                    saveTrack(selectedTrack.artist+" - "+selectedTrack.title, selectedTrack.url);
                    break;
                default:
                    break;
            
            }
            
            //Panorama.DefaultItem = playerItem;
        }

        private void setAppTile(object sender, EventArgs e)
        {
            // Application Tile is always the first Tile, even if it is not pinned to Start.
            ShellTile TileToFind = ShellTile.ActiveTiles.First();

            // Application should always be found
            if (TileToFind != null)
            {

                string status;
                int count;
                if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
                {
                    status = "Playing...";
                    count = 1;
                }
                else {
                    status = "Paused";
                    count = 0;
                }


                // Set the properties to update for the Application Tile.
                // Empty strings for the text values and URIs will result in the property being cleared.
                StandardTileData NewTileData = new StandardTileData
                {
                    Title = "MusicBird",
                    //BackgroundImage = new Uri(textBoxBackgroundImage.Text, UriKind.Relative),
                    Count = 0,
                    //BackTitle = status,
                    //BackBackgroundImage = new Uri(textBoxBackBackgroundImage.Text, UriKind.Relative),
                    //BackContent = BackgroundAudioPlayer.Instance.Track.Title + " - " + BackgroundAudioPlayer.Instance.Track.Artist
                };

                // Update the Application Tile
                TileToFind.Update(NewTileData);
            }

        }

        

            private void saveTrack(string filename, string uri)
            {  
                /*var dl = new WebClient();
                dl.OpenReadAsync(new Uri(uri), filename);
                dl.OpenReadCompleted += new OpenReadCompletedEventHandler(dl_downloadCompleted);
                dl.DownloadProgressChanged += new DownloadProgressChangedEventHandler(dl_DownloadProgressChanged);*/

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

                transferRequest.TransferPreferences = TransferPreferences.AllowCellular;
                transferRequest.TransferPreferences = TransferPreferences.AllowBattery;
                transferRequest.TransferPreferences = TransferPreferences.AllowCellularAndBattery;

                // Add the transfer request using the BackgroundTransferService. Do this in 
                // a try block in case an exception is thrown.
                try
                {
                    BackgroundTransferService.Add(transferRequest);
                    transferRequest.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);
                    transferRequest.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);
                    MessageBox.Show("Download was added.");
                    UpdateUI();
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show("Unable to add background transfer request. " + ex.Message);
                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to add background transfer request.");
                }
            }

            void dl_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
                downloadProgressTextBlock.Text = "Downloading... ("+e.ProgressPercentage+"%)";
            }

            void dl_downloadCompleted(object sender, OpenReadCompletedEventArgs e)
            {
                if (e.Error != null) return;

                var filename = e.UserState.ToString()+".mp3";
                var str = e.Result;

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                  if (myStore.FileExists(filename)) myStore.DeleteFile(filename);

                  var buffer = new byte[1024];
                  using (var isoStorStr = myStore.OpenFile(filename, FileMode.CreateNew))
                  {
                    int bytesRead = 0;
                    while ((bytesRead = str.Read(buffer, 0, 1024)) > 0)
                        isoStorStr.Write(buffer, 0, bytesRead);
                  }
               }
                updateLibrary();
                downloadProgressTextBlock.Text = "Download completed!";
            }

            private void updatePlaylist() {
                List<TrackListItem> trackList = new List<TrackListItem>();
                for(int i=0; i<AudioPlaybackAgent1.AudioPlayer._playList.Count; i++){
                    AudioTrack track = AudioPlaybackAgent1.AudioPlayer._playList[i];
                    Uri uri = track.Source;
                    String artist = track.Artist;
                    String title = track.Title;
                    trackList.Add(new TrackListItem(artist, title, uri.ToString()));
                }
                PlaylistElement.ItemsSource = trackList;
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
                            trackList.Add(item);
                        }
                        LibraryElement.ItemsSource = trackList;
                    }
                } 
            }

            private void playlistItem_Click(object sender, RoutedEventArgs e) {
                TrackListItem selectedTrack = (sender as FrameworkElement).DataContext as TrackListItem;
                AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.Relative), selectedTrack.artist, selectedTrack.title, "", null);

                AudioPlaybackAgent1.AudioPlayer.addToList(current);
                BackgroundAudioPlayer.Instance.Track = current;
                BackgroundAudioPlayer.Instance.Play();
                updatePlaylist();
                Panorama.DefaultItem = playerItem;
            }

            private void playlistItem_Hold(object sender, RoutedEventArgs e)
            {
                ListBoxItem selectedListBoxItem = PlaylistElement.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
                if (selectedListBoxItem == null)
                {
                    System.Diagnostics.Debug.WriteLine("Selected LB is null");
                    return;
                }
                //get selected Track
                TrackListItem selectedTrack = selectedListBoxItem.Content as TrackListItem;
                AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);
                
                //loop through items and check if item matches selectedItem (selectedIndex is not updated with context menu...)
                int i = 0;
                TrackListItem currentTrack = (PlaylistElement.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem).Content as TrackListItem;
                while (currentTrack != selectedTrack) {
                    i++;
                    currentTrack = (PlaylistElement.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem).Content as TrackListItem;
                }
                int selectedIndex = i;

                //get selected Context menu item
                var menuItem = (MenuItem)sender;
                var tag = menuItem.Tag.ToString();
                switch (tag)
                {
                    case "play":
                        AudioPlaybackAgent1.AudioPlayer.addToList(current);
                        BackgroundAudioPlayer.Instance.Track = current;
                        BackgroundAudioPlayer.Instance.Play();
                        updatePlaylist();
                        Panorama.DefaultItem = playerItem;
                        break;
                    case "delete":
                        AudioPlaybackAgent1.AudioPlayer._playList.RemoveAt(selectedIndex);
                        updatePlaylist();
                        break;
                    default:
                        break;

                }
            }

            private void libraryItem_Click(object sender, RoutedEventArgs e)
            {
                LibraryItem selectedTrack = (sender as FrameworkElement).DataContext as LibraryItem;
                AudioTrack current = new AudioTrack(new Uri(selectedTrack.filename, UriKind.Relative), selectedTrack.filename, "", "", null);
                
                AudioPlaybackAgent1.AudioPlayer.addToList(current);
                BackgroundAudioPlayer.Instance.Track = current;
                BackgroundAudioPlayer.Instance.Play();
                updatePlaylist();
                Panorama.DefaultItem = playerItem;
            }

            private void libraryItem_Hold(object sender, RoutedEventArgs e)
            {
                ListBoxItem selectedListBoxItem = LibraryElement.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
                if (selectedListBoxItem == null)
                {
                    return;
                }
                //get selected Track
                LibraryItem selectedTrack = selectedListBoxItem.DataContext as LibraryItem;
                AudioTrack current = new AudioTrack(new Uri(selectedTrack.filename, UriKind.RelativeOrAbsolute), selectedTrack.filename, "", "", null);

                //get selected Context menu item
                var menuItem = (MenuItem)sender;
                var tag = menuItem.Tag.ToString();
                switch (tag)
                {
                    case "add":
                        AudioPlaybackAgent1.AudioPlayer.addToList(current);
                        updatePlaylist();
                        break;
                    case "play":
                        AudioPlaybackAgent1.AudioPlayer.addToList(current);
                        BackgroundAudioPlayer.Instance.Track = current;
                        BackgroundAudioPlayer.Instance.Play();
                        updatePlaylist();
                        Panorama.DefaultItem = playerItem;
                        break;
                    case "delete":
                        using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (store.FileExists(selectedTrack.filename))
                            {
                                store.DeleteFile(selectedTrack.filename);
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

            private void UpdateRequestsList()
            {
                // The Requests property returns new references, so make sure that
                // you dispose of the old references to avoid memory leaks.
                if (transferRequests != null)
                {
                    foreach (var request in transferRequests)
                    {
                        request.Dispose();
                    }
                }
                transferRequests = BackgroundTransferService.Requests;
            }

            private void UpdateUI()
            {
                System.Diagnostics.Debug.WriteLine("Updating UI");
                // Update the list of transfer requests
                UpdateRequestsList();

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

            }

            protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
            {
                // Reset all of the user action Booleans on page load.
                WaitingForExternalPower = false;
                WaitingForExternalPowerDueToBatterySaverMode = false;
                WaitingForNonVoiceBlockingNetwork = false;
                WaitingForWiFi = false;

                // When the page loads, refresh the list of file transfers.
                InitialTransferStatusCheck();
                UpdateUI();
            }

            private void InitialTransferStatusCheck()
            {
                UpdateRequestsList();

                foreach (var transfer in transferRequests)
                {
                    transfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);
                    transfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);
                    ProcessTransfer(transfer);
                }

                //DEBUG MessageBox.Show("added event handlers");

                if (WaitingForExternalPower)
                {
                    MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power to continue transferring.");
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
                    MessageBox.Show("You have one or more file transfers waiting for a WiFi connection. Connect your device to a WiFi network to continue transferring.");
                }
            }

            private void ProcessTransfer(BackgroundTransferRequest transfer)
            {
                switch (transfer.TransferStatus)
                {
                    case TransferStatus.Completed:

                        // If the status code of a completed transfer is 200 or 206, the
                        // transfer was successful
                        if (transfer.StatusCode == 200 || transfer.StatusCode == 206)
                        {
                            // Remove the transfer request in order to make room in the 
                            // queue for more transfers. Transfers are not automatically
                            // removed by the system.
                            RemoveTransferRequest(transfer.RequestId);

                            // In this example, the downloaded file is moved into the root
                            // Isolated Storage directory
                            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                string filename = transfer.Tag;
                                if (isoStore.FileExists(filename))
                                {
                                    isoStore.DeleteFile(filename);
                                }
                                isoStore.MoveFile(transfer.DownloadLocation.OriginalString, filename);
                            }

                            updateLibrary();
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
                UpdateUI();
            }

            void transfer_TransferProgressChanged(object sender, BackgroundTransferEventArgs e)
            {
                System.Diagnostics.Debug.WriteLine("Downloaded " + e.Request.BytesReceived/e.Request.TotalBytesToReceive);
                UpdateUI();
            }

            private void CancelButton_Click(object sender, EventArgs e)
            {
                // The ID for each transfer request is bound to the
                // Tag property of each Remove button.
                string transferID = ((Button)sender).Tag as string;

                // Delete the transfer request
                RemoveTransferRequest(transferID);

                // Refresh the list of file transfers
                UpdateUI();
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