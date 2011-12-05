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

namespace MusicBird
{
    public partial class MainPage : PhoneApplicationPage
    {

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

            IsolatedStorageExplorer.Explorer.Start("localhost");

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
            if (name.IndexOf("-") > -1)                 //name contains a dash, split at its position
            {
                artist = name.Split((char)45)[0];
                title = name.Split((char)45)[1];            
            }
            else
            {
                int len = name.Split((char)32).Length;
                if (len >= 2)
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
            return new String[] { artist.Trim(), title.Trim() };
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
                var dl = new WebClient();
                dl.OpenReadAsync(new Uri(uri), filename);
                dl.OpenReadCompleted += new OpenReadCompletedEventHandler(dl_downloadCompleted);
                dl.DownloadProgressChanged += new DownloadProgressChangedEventHandler(dl_DownloadProgressChanged);

                //((ApplicationBarIconButton)(ApplicationBar.Buttons[downButton])).IsEnabled = false;
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

                MessageBox.Show(selectedListBoxItem.TabIndex.ToString());

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
                        AudioPlaybackAgent1.AudioPlayer.removeFromList(current);
                        AudioPlaybackAgent1.AudioPlayer._playList.RemoveAt(PlaylistElement.SelectedIndex);
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