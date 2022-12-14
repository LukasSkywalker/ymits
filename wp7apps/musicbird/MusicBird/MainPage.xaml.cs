using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using Codeplex.OAuth;
using com.mtiks.winmobile;
using Microsoft.Devices;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.BackgroundTransfer;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using System.Globalization;
using System.Reflection;


namespace MusicBird
{
    public partial class MainPage : PhoneApplicationPage
    {
        //public static bool IsTrial{get; private set;}

        // background transfer object container
        IEnumerable<BackgroundTransferRequest> transferRequests;

        // Booleans for tracking if any transfers are waiting for user action.
        bool WaitingForExternalPower;
        bool WaitingForExternalPowerDueToBatterySaverMode;
        bool WaitingForNonVoiceBlockingNetwork;
        bool WaitingForWiFi;

        // Timer for updating the UI
        DispatcherTimer playerProgressUpdateTimer;
        DispatcherTimer tileTimer;
        //DispatcherTimer _downloadTimer;
        DispatcherTimer playerErrorTimer;
        DispatcherTimer trialCheckTimer;

        // Indexes into the array of ApplicationBar.Buttons
        const int prevButton = 0;
        const int playButton = 1;
        const int downButton = 3;
        const int nextButton = 2;

        List<TrackListItem> trackList = new List<TrackListItem>();
        String query = "";

        List<HttpWebRequest> sizeRequests = new List<HttpWebRequest>();
        List<WebClient> searchRequests = new List<WebClient>();

        #region Initialization
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("Constructor called.");
            this.Loaded += new RoutedEventHandler(this.MainPage_Loaded);
            MainPage_Loaded(null, null);
            //initDropbox();
            //initSkydrive();
        }

        void MainPage_Loaded( object sender, RoutedEventArgs e )
        {
            System.Diagnostics.Debug.WriteLine("MainPage_Loaded");

            toggleAds();
            toggleLibAndDownloads();

            playerProgressUpdateTimer = new DispatcherTimer();
            playerProgressUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            playerProgressUpdateTimer.Tick += new EventHandler(this.updatePlayerProgress);

            tileTimer = new DispatcherTimer();
            tileTimer.Interval = TimeSpan.FromSeconds(20);
            tileTimer.Tick += new EventHandler(this.setAppTile);
            tileTimer.Start();

            playerErrorTimer = new DispatcherTimer();
            playerErrorTimer.Interval = TimeSpan.FromSeconds(8);
            playerErrorTimer.Tick += new EventHandler(this.getErrors);
            playerErrorTimer.Start();

            trialCheckTimer = new DispatcherTimer();
            trialCheckTimer.Interval = TimeSpan.FromSeconds(20);
            trialCheckTimer.Tick += new EventHandler(this.checkTrial);
            trialCheckTimer.Start();

            NetworkChange.NetworkAddressChanged += this.NetworkAddress_Changed;

            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(this.Instance_PlayStateChanged);

            updatePlaylist();
            updateLibrary();

            var assembly = Assembly.GetExecutingAssembly().FullName;
            var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];

            System.Diagnostics.Debug.WriteLine("MusicBird Version: "+new Version(fullVersionNumber));

        } 
        #endregion

        #region Trial Checks

        private void toggleAds()
        {
            if((Application.Current as App).IsTrial)
            {
                AdRotatorControl.IsEnabled = true;
                AdRotatorControl.Visibility = Visibility.Visible;
            }
            else
            {
                AdRotatorControl.IsEnabled = false;
                AdRotatorControl.Visibility = Visibility.Collapsed;
                this.Panorama.Margin = new System.Windows.Thickness(0, 0, 0, 0);
            }
        }

        private void toggleLibAndDownloads()
        {
            if((Application.Current as App).IsTrial)
            {
                System.Diagnostics.Debug.WriteLine("Hiding unused panorama items...");
                Panorama.Items.Remove(libraryItem);
                Panorama.Items.Remove(downloadItem);
            }
        }

        private void marketPlaceMessage()
        {
            if(MessageBox.Show("You are using MusicBird in trial mode. Please purchase " +
                            "the paid license to use this feature. Press OK to go to the market.", "Trial",
                                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
                _marketPlaceDetailTask.Show();
            }
        }

        private void checkTrial( object sender, EventArgs e )
        {
            /*if(checkFlag("LimitExceeded"))
            {
                BackgroundAudioPlayer.Instance.Stop();
                marketPlaceMessage();
            }*/
        }
        #endregion

        #region Player
        /// <summary>
        /// PlayStateChanged event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_PlayStateChanged(object sender, EventArgs e)
        {
            this.updateArtistAndTitle();
            this.updatePlayerProgress(null, null);
            PlayState playerState = BackgroundAudioPlayer.Instance.PlayerState;
            switch (playerState)
            {
                case PlayState.Playing:
                    positionIndicator.Maximum = BackgroundAudioPlayer.Instance.Track.Duration.TotalSeconds;
                    
                    updateButtonImage("pause");
                    enablePlayerUI(true);
                    playerProgressUpdateTimer.Start();
                    
                    checkAlbumArt();
                    break;

                case PlayState.Paused:
                    updateButtonImage("play");
                    enablePlayerUI(true);
                    playerProgressUpdateTimer.Stop();
                    break;
                case PlayState.Stopped:
                    updateButtonImage("play");
                    enablePlayerUI(true);
                    playerProgressUpdateTimer.Stop();
                    break;
                case PlayState.BufferingStarted:
                    updateButtonImage("pause");
                    enablePlayerUI(false);
                    playerProgressUpdateTimer.Stop();
                    break;
                case PlayState.BufferingStopped:
                    updateButtonImage("play");
                    enablePlayerUI(true);
                    playerProgressUpdateTimer.Start();
                    break;
            }
        }

        private void updateArtistAndTitle() {
            if(this.Panorama.SelectedIndex == 0)
            {
                string ps = BackgroundAudioPlayer.Instance.PlayerState.ToString();
                if(ps.Equals(PlayState.Unknown.ToString()))
                {
                    ps = "";
                }
                txtState.Text = ps;

                if(BackgroundAudioPlayer.Instance.Track != null)
                {
                    txtArtist.Text = BackgroundAudioPlayer.Instance.Track.Title;
                    txtTitle.Text = BackgroundAudioPlayer.Instance.Track.Artist;
                }
            }
        }

        private void updatePlayerProgress(object sender, EventArgs e)
        {
            if(this.Panorama.SelectedIndex == 0)
            {
                /*if(BackgroundAudioPlayer.Instance.PlayerState != PlayState.Playing)
                {
                    return;
                }*/
                
                System.Diagnostics.Debug.WriteLine("Updating player progress");
                AudioTrack track = BackgroundAudioPlayer.Instance.Track;
                if(track != null)
                {

                    if(track.Duration != null)
                    {
                        positionIndicator.Maximum = track.Duration.TotalSeconds;

                        // Set the current position on the ProgressBar.
                        TimeSpan position;
                        try
                        {
                            position = BackgroundAudioPlayer.Instance.Position;
                        }
                        catch(Exception ex) {
                            position = TimeSpan.FromSeconds(0);
                        }

                        if(position != null)
                        {
                            positionIndicator.Value = position.TotalSeconds;

                            // Update the current playback position.
                            textPosition.Text = String.Format("{0:d2}:{1:d2}:{2:d2}", position.Hours, position.Minutes, position.Seconds);

                            // Update the time remaining digits.
                            TimeSpan timeRemaining = BackgroundAudioPlayer.Instance.Track.Duration - position;
                            textRemaining.Text = String.Format("-{0:d2}:{1:d2}:{2:d2}", timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
                        }
                    }
                }
            }
        }

        private void enablePlayerUI(bool enable){
            if(enable)
            {
                enableButtons(true);
                positionIndicator.IsIndeterminate = false;
            }
            else {
                enableButtons(false);
                positionIndicator.IsIndeterminate = true;
            }
        }

        private void enableButtons( bool enable ) {
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[prevButton])).IsEnabled = enable;
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[playButton])).IsEnabled = enable;
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[nextButton])).IsEnabled = enable;
        }


        #region Replay Controls
        /// <summary>
        /// Click handler for the Skip Previous button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void prevButton_Click( object sender, EventArgs e )
        {
            updateButtonImage("pause");
            enablePlayerUI(false);
            BackgroundAudioPlayer.Instance.SkipPrevious();
        }


        /// <summary>
        /// Click handler for the Play button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playButton_Click( object sender, EventArgs e )
        {
            if(PlayState.Playing == BackgroundAudioPlayer.Instance.PlayerState)
            {
                BackgroundAudioPlayer.Instance.Pause();
                updateButtonImage("pause");
                enablePlayerUI(false);

            }
            else
            {
                /*if((Application.Current as App).IsTrial)
                {
                    marketPlaceMessage();
                }
                else
                {*/
                    BackgroundAudioPlayer.Instance.Play();
                    updateButtonImage("pause");
                    enablePlayerUI(false);
                //}
            }
        }


        /// <summary>
        /// Click handler for the Pause button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void downButton_Click( object sender, EventArgs e )
        {
            NavigationService.Navigate(new Uri("/Page1.xaml", UriKind.Relative));
        }


        /// <summary>
        /// Click handler for the Skip Next button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nextButton_Click( object sender, EventArgs e )
        {
            updateButtonImage("pause");
            enablePlayerUI(false);
            BackgroundAudioPlayer.Instance.SkipNext();
        }

        void updateButtonImage( string function )
        {
            if(function.Equals("play"))
            {
                ((ApplicationBarIconButton)(ApplicationBar.Buttons[playButton])).IconUri = new Uri("/Images/appbar.transport.play.rest.png", UriKind.Relative);
            }
            else if(function.Equals("pause"))
            {
                ((ApplicationBarIconButton)(ApplicationBar.Buttons[playButton])).IconUri = new Uri("/Images/appbar.transport.pause.rest.png", UriKind.Relative);
            }
        }

        private void checkAlbumArt() {
            if(Helper.Preferences.readBool("albumart") && BackgroundAudioPlayer.Instance.Track != null)
            {
                try
                {
                    getAlbumArt(BackgroundAudioPlayer.Instance.Track.Artist + " " + BackgroundAudioPlayer.Instance.Track.Title);
                }
                catch(WebException e)
                {
                    log(e);
                }
            }
        }
        #endregion

        #endregion

        #region Search
        private void search_KeyUp( object sender, KeyEventArgs e )
        {
            if(e.Key == Key.Enter)
            {
                queryProgress.IsIndeterminate = true;
                queryProgress.Visibility = Visibility.Visible;
                searchItem.Focus();
                query = queryTextbox.Text;
                TrackListElement.ItemsSource = null;
                getResults();
                this.NetworkAddress_Changed(null, null);
                mtiks.Instance.postEventAttributes("SEARCH",
                new Dictionary<string, string>() { { "SEARCHTERM", this.query } });
            }
        }

        private void search_textInputStart( object sender, TextCompositionEventArgs e )
        {
            string query = queryTextbox.Text;
            Uri uri = new Uri("http://google.com/complete/search?output=toolbar&q="+HttpUtility.UrlEncode(query), UriKind.Absolute);
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += getSuggestionCompleted;
            wc.DownloadStringAsync(uri);
        }

        private void getSuggestionCompleted( object sender, DownloadStringCompletedEventArgs e )
        {
            /*if(e.Error == null)
            {
                List<String> suggestions = new List<String>();
                XDocument xdoc = XDocument.Parse(e.Result, LoadOptions.None);
                foreach(XElement suggestion in xdoc.Descendants("suggestion"))
                {
                    System.Diagnostics.Debug.WriteLine(suggestion.ToString());
                    suggestions.Add(suggestion.Attribute("data").Value);
                }
                System.Diagnostics.Debug.WriteLine(suggestions);
                queryTextbox.ItemsSource = suggestions;
            }*/
            var asdf = new List<String>();
            asdf.Add("Gaga");
            asdf.Add("del rey");
            queryTextbox.ItemsSource = asdf;
        }


        private void getResults()
        {
            trackList.Clear();
            while(sizeRequests.Count() > 0)
            {
                HttpWebRequest req = sizeRequests[0];
                req.Abort();
                sizeRequests.Remove(req);
            }

            while(searchRequests.Count() > 0)
            {
                WebClient wc = searchRequests[0];
                wc.CancelAsync();
                searchRequests.Remove(wc);
            }

            string url = "http://mp3skull.com/mp3/" + this.query.Trim().Replace(" ", "_") + ".html";
            System.Diagnostics.Debug.WriteLine("Opening URL " + url);
            WebClient wc_mp3skull = new WebClient();
            wc_mp3skull.OpenReadCompleted += new OpenReadCompletedEventHandler(this.wc_mp3skull_OpenReadCompleted);
            wc_mp3skull.OpenReadAsync(new Uri(url));
            searchRequests.Add(wc_mp3skull);
        }

        private void getResultsFromVpleer()
        {
            string url = "http://de.vpleer.ru/?q=" + this.query.Trim().Replace(" ", "+");
            System.Diagnostics.Debug.WriteLine("Opening URL " + url);
            WebClient wc_vpleer = new WebClient();
            wc_vpleer.OpenReadCompleted += new OpenReadCompletedEventHandler(this.wc_vpleer_OpenReadCompleted);
            wc_vpleer.OpenReadAsync(new Uri(url));
            searchRequests.Add(wc_vpleer);
        }

        private void wc_mp3skull_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e )
        {
            searchRequests.Remove(sender as WebClient);
            Regex pattern = new Regex("<a href=\"(.*?.mp3)\" rel=\"nofollow\"", RegexOptions.IgnoreCase);
            Regex pattern2 = new Regex("<div style=\"font-size:15px;\"><b>(.*?) mp3</b></div>", RegexOptions.IgnoreCase);
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

                while(m.Success)
                {
                    Group g = m.Groups[1];
                    Group h = n.Groups[1];
                    string name = h.ToString();
                    string artist = name;
                    string title = name;
                    string url = g.ToString();
                    string[] data = this.getArtistAndTitle(name);
                    if(url.IndexOf("4shared") == -1)
                    {
                        TrackListItem item = new TrackListItem(data[0], data[1], url);
                        if(matchCount < 20) this.getSize(new Uri(url, UriKind.RelativeOrAbsolute));
                        //getTags(url);
                        trackList.Add(item);
                        matchCount++;
                    }
                    m = m.NextMatch();
                    n = n.NextMatch();
                }

                System.Diagnostics.Debug.WriteLine("mp3skull: Results found: " + matchCount);
                if(matchCount < 5)
                {
                    getResultsFromVpleer();
                }
                else
                {
                    stopThrobber();
                }

            }
            catch(WebException ex)
            {
                String status = ex.Status.ToString();
                String msg = ex.Message;
                if(ex.Response != null)
                {
                    String statCode = ((HttpWebResponse)ex.Response).StatusCode.ToString();
                    String statDescr = ((HttpWebResponse)ex.Response).StatusDescription.ToString();
                }
                log(ex);
                stopThrobber();
            }
            finally
            {
                TrackListElement.ItemsSource = null;
                TrackListElement.ItemsSource = this.trackList;
            }
        }

        private void wc_vpleer_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e )
        {
            searchRequests.Remove(sender as WebClient);
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
                log(ex);
            }
            finally
            {
                stopThrobber();

                TrackListElement.ItemsSource = null;
                TrackListElement.ItemsSource = this.trackList;
            }
        }

        private void stopThrobber()
        {
            queryProgress.Visibility = Visibility.Collapsed;
            queryProgress.IsIndeterminate = false;
        }

        private string[] getArtistAndTitle( string name )
        {
            String artist = "";
            String title = "";

            replaceNumbersAndExtension(name);

            name = HttpUtility.HtmlDecode(name);

            name = name.Trim();

            int count = name.Length - name.Replace("-", "").Length + 1;     // count parts of string separated by dash

            if(count == 1)
            {                                           // John Wayne Heaven
                int len = name.Split((char)32).Length;                  // split at space
                if(len == 1)
                {                                         // Ex. JohnWayneHeaven
                    artist = name;
                    title = "";
                }
                else if(len > 1)                                      // Ex. JohnWayne Heaven or John Wayne Heaven
                {
                    string[] nameArray = name.Split((char)32);
                    for(int i = 0; i < len; i++)
                    {
                        if(i < len / 2)
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
            else if(count == 2)
            {                                      // John Wayne - Heaven
                artist = name.Split((char)45)[0];
                title = name.Split((char)45)[1];
            }
            else if(count > 2)
            {                                        // John Wayne - Heaven - Live at Brixton Academy
                string[] parts = name.Split((char)45);
                int len = parts.Length;
                for(int i = 0; i < len; i++)
                {
                    if(i < len / 2)
                    {
                        artist += parts[i];
                    }
                    else
                    {
                        title += parts[i];
                    }
                }
            }

            artist = artist.Replace("_", " ");
            title = title.Replace("_", " ");

            artist = UppercaseWords(artist);
            title = UppercaseWords(title);


            artist = artist.Trim();
            title = title.Trim();

            artist = replaceNumbersAndExtension(artist);
            title = replaceNumbersAndExtension(title);

            artist = artist.Trim();
            title = title.Trim();

            return new String[] { artist, title };
        }

        private string replaceNumbersAndExtension( string name )
        {
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

        private void getSize(Uri uri)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
            req.Method = "HEAD";
            req.BeginGetResponse(new AsyncCallback(this.gotSize), req);
            sizeRequests.Add(req);
        }

        private long getSizeLocal( String fileName ) {
            using(IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(!store.FileExists(fileName))
                    return 0;
                else {
                    long size = 0;
                    AudioTrack currentTrack = BackgroundAudioPlayer.Instance.Track;
                    if(currentTrack != null && fileName == HttpUtility.UrlDecode(currentTrack.Source.ToString()))
                        size = 0;
                    else
                    {
                        using(IsolatedStorageFileStream stream = store.OpenFile(fileName, FileMode.Open))
                        {
                            size = stream.Length;
                        }
                    }
                    return size;
                }
            }
        }

        private void getTags( string url ) {
            Uri uri = new Uri(url, UriKind.RelativeOrAbsolute);
            ID3v2 reader = new ID3v2(uri);
            reader.TagsRead += new ID3v2.TagsReadEventHandler(this.reader_TagsRead);
            reader.Read();
        }

        void reader_TagsRead( object sender, string artist, string title, string url )
        {
            foreach(TrackListItem trackListItem in this.trackList)
            {
                if(trackListItem.url.Equals(url))
                {
                    trackListItem.artist = artist;
                    trackListItem.title = title;
                }
            }
        }

        private void gotSize( IAsyncResult asynchronousResult ) {
            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
            sizeRequests.Remove(request);
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                int size = Convert.ToInt32(response.Headers["Content-Length"], CultureInfo.InvariantCulture);
                string url = request.RequestUri.ToString();
                foreach(TrackListItem trackListItem in this.trackList) {
                    if(trackListItem.url.Equals(url))
                    {
                        trackListItem.setSize(size);
                    }
                }
            }
            catch(WebException) {
                // do nothing ?
                System.Diagnostics.Debug.WriteLine("Error getting size for file " + request.RequestUri.ToString());
            }
        }

        #endregion

        #region Track View
        private void trackItem_Click( object sender, RoutedEventArgs e )
        {
            TrackListItem selectedTrack = (sender as FrameworkElement).DataContext as TrackListItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);

            addToPlaylist(selectedTrack.artist, selectedTrack.title, selectedTrack.url);
            updatePlaylist();

            List<String[]> oldItems = this.readPlaylist();
            int itemsCount = oldItems.Count;

            AudioPlaybackAgent1.AudioPlayer.playAtPosition(itemsCount - 1, BackgroundAudioPlayer.Instance);
            mtiks.Instance.postEventAttributes("TRACKITEM_PLAY",
                new Dictionary<string, string>() { { "ARTIST_TITLE", selectedTrack.artist + " - " + selectedTrack.title } });
            //Instance_PlayStateChanged(null, null);
            enablePlayerUI(false);
            this.Panorama.SelectedItem = this.playerItem;
        }

        private void trackItem_Hold( object sender, RoutedEventArgs e )
        {

            // get selected Item
            ListBoxItem selectedListBoxItem = this.getListBoxItem(sender);
            if(selectedListBoxItem == null)
            {
                return;
            }
            // get selected Track
            TrackListItem selectedTrack = selectedListBoxItem.DataContext as TrackListItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);

            // get selected Context menu item
            var menuItem = (MenuItem)sender;
            var tag = menuItem.Tag.ToString();
            switch(tag)
            {
                case "add":
                    addToPlaylist(current.Artist, current.Title, current.Source.ToString());
                    mtiks.Instance.postEventAttributes("TRACKITEM_ADDTOPLS",
                        new Dictionary<string, string>() { { "ARTIST_TITLE", current.Artist + " - " + current.Title } });
                    updatePlaylist();
                    break;
                case "play":
                    trackItem_Click(sender, e);
                    break;
                case "download":
                    if((Application.Current as App).IsTrial)
                    {
                        marketPlaceMessage();
                    }
                    else
                    {
                        saveTrack(current.Artist + " - " + current.Title, current.Source.ToString());
                    }
                    break;
                case "share":
                    shareTrack(current.Artist, current.Title, current.Source);
                    break;
                default:
                    break;

            }
        } 
        #endregion

        private void setAppTile(object sender, EventArgs e)
        {
            // Application Tile is always the first Tile, even if it is not pinned to Start.
            ShellTile TileToFind = ShellTile.ActiveTiles.First();

            // Application should always be found
            if (TileToFind != null)
            {
                string status = "Paused";
                string name = "";
                /*int count = 0;
                try
                {
                    count = BackgroundTransferService.Requests.Count();
                }
                catch(UriFormatException ex) {
                    count = 0;
                    System.Diagnostics.Debug.WriteLine("in setAppTile()");
                }*/

                if(BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
                {
                    status = "Playing...";
                }
                else
                {
                    status = "Paused";
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
                    Count = 0,
                    BackTitle = status,
                    //BackBackgroundImage = new Uri(textBoxBackBackgroundImage.Text, UriKind.Relative),
                    BackContent = name
                };

                // Update the Application Tile
                TileToFind.Update(NewTileData);
            }
        }

        #region Playlist View
        private void playlistItem_Click( object sender, RoutedEventArgs e )
        {
            TrackListItem selectedTrack = (sender as FrameworkElement).DataContext as TrackListItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);
            String[] current2 = new String[] { selectedTrack.artist, selectedTrack.title, selectedTrack.url };

            List<string[]> oldItems = this.readPlaylist();

            int counter = -1;

            foreach(String[] item in oldItems)
            {
                counter++;
                if(item[2].Equals(current2[2]))
                {
                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine("MainPage.xaml.cs:playlistItem_Click ___ Found playlist item at index " + counter);
            AudioPlaybackAgent1.AudioPlayer.playAtPosition(counter, BackgroundAudioPlayer.Instance);
            mtiks.Instance.postEventAttributes("PLSITEM_PLAY",
                new Dictionary<string, string>() { { "ARTIST_TITLE", selectedTrack.artist + " - " + selectedTrack.title } });
            //Instance_PlayStateChanged(null, null);
            enablePlayerUI(false);
            this.Panorama.SelectedItem = this.playerItem;
        }

        private void playlistItem_Hold( object sender, RoutedEventArgs e )
        {
            ListBoxItem selectedListBoxItem = this.PlaylistElement.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            if(selectedListBoxItem == null)
            {
                System.Diagnostics.Debug.WriteLine("Selected LB is null");
                return;
            }
            // get selected Track
            TrackListItem selectedTrack = selectedListBoxItem.Content as TrackListItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, selectedTrack.title, "", null);

            // loop through items and check if item matches selectedItem (selectedIndex is not updated with context menu...)
            int i = 0;
            TrackListItem currentTrack = (this.PlaylistElement.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem).Content as TrackListItem;
            while(currentTrack != selectedTrack)
            {
                i++;
                currentTrack = (this.PlaylistElement.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem).Content as TrackListItem;
            }
            int selectedIndex = i;

            // get selected Context menu item
            var menuItem = (MenuItem)sender;
            var tag = menuItem.Tag.ToString();
            switch(tag)
            {
                case "delete":
                    removeFromPlaylist(selectedIndex);
                    updatePlaylist();
                    break;
                case "download":
                    if((Application.Current as App).IsTrial)
                    {
                        marketPlaceMessage();
                    }
                    else if(selectedTrack.url.IndexOf("http") > -1)
                    {
                        saveTrack(selectedTrack.artist + " - " + selectedTrack.title, selectedTrack.url);
                    }
                    else {
                        MessageBox.Show("You already have this track in your library.");
                    }
                    break;
                case "play":
                    playlistItem_Click(sender, e);
                    break;
                default:
                    break;

            }
        }

        private void updatePlaylist()
        {
            List<TrackListItem> tList = new List<TrackListItem>();
            List<String[]> trackList = this.readPlaylist();
            foreach(var item in trackList)
            {
                tList.Add(new TrackListItem(item[0], item[1], item[2]));
            }
            PlaylistElement.ItemsSource = tList;
        }
        #endregion

        #region Library View
        private void libraryItem_Click( object sender, RoutedEventArgs e )
        {
            LibraryItem selectedTrack = (sender as FrameworkElement).DataContext as LibraryItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.fileName, UriKind.Relative), selectedTrack.fileName, "", "", null);

            String[] name = this.getArtistAndTitle(selectedTrack.fileName);

            addToPlaylist(name[0], name[1], selectedTrack.fileName);
            updatePlaylist();

            //BackgroundAudioPlayer.Instance.Play();
            //updatePlaylist();

            List<String[]> oldItems = this.readPlaylist();
            int itemsCount = oldItems.Count;

            AudioPlaybackAgent1.AudioPlayer.playAtPosition(itemsCount - 1, BackgroundAudioPlayer.Instance);
            mtiks.Instance.postEventAttributes("LIBITEM_PLAY",
                new Dictionary<string, string>() { { "FILENAME", selectedTrack.fileName } });
            //Instance_PlayStateChanged(null, null);
            enablePlayerUI(false);
            this.Panorama.SelectedItem = this.playerItem;

        }

        private void libraryItem_Hold( object sender, RoutedEventArgs e )
        {
            ListBoxItem selectedListBoxItem = this.LibraryElement.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            if(selectedListBoxItem == null)
            {
                return;
            }
            // get selected Track
            LibraryItem selectedTrack = selectedListBoxItem.DataContext as LibraryItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.fileName, UriKind.Relative), selectedTrack.fileName, "", "", null);

            // get selected Context menu item
            var menuItem = (MenuItem)sender;
            var tag = menuItem.Tag.ToString();
            switch(tag)
            {
                case "add":
                    addToPlaylist(current.Artist, current.Title, current.Source.ToString());
                    mtiks.Instance.postEventAttributes("LIBITEM_ADDTOPLS",
                        new Dictionary<string, string>() { { "ARTIST_TITLE", current.Artist + " - " + current.Title } });
                    updatePlaylist();
                    break;
                case "play":
                    libraryItem_Click(sender, e);
                    break;
                case "properties":
                    NavigationService.Navigate(new Uri("/Properties.xaml?fileName=" + Uri.EscapeDataString(selectedTrack.fileName), UriKind.Relative));
                    break;
                case "upload":
                    AccessToken token = this.authenticate();
                    if(token != null)
                    {
                        sendFile(token, selectedTrack.fileName);
                        this.Panorama.SelectedItem = this.downloadItem;
                    }
                    break;
                case "delete":
                    using(var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if(store.FileExists(selectedTrack.fileName))
                        {
                            try
                            {
                                if (HttpUtility.UrlDecode(BackgroundAudioPlayer.Instance.Track.Source.ToString()).Equals(selectedTrack.fileName))
                                {
                                    MessageBox.Show("The track is currently played. Please try again later.");
                                }
                                else
                                {
                                    store.DeleteFile(selectedTrack.fileName);
                                }
                            }
                            catch(NullReferenceException)
                            {
                                // No playback a.t.m.
                                store.DeleteFile(selectedTrack.fileName);
                            }
                        }
                        else
                        {
                            MessageBox.Show(selectedTrack.fileName);
                        }
                    }
                    updateLibrary();
                    break;
                default:
                    break;

            }
        }

        private void updateLibrary()
        {
            if((Application.Current as App).IsTrial) return;
            using(var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                List<LibraryItem> trackList = new List<LibraryItem>();

                string[] files = myStore.GetFileNames();
                if(files.Length > 0)
                {
                    for(int i = 0; i < files.Length; i++)
                    {
                        LibraryItem item = new LibraryItem(files[i]);
                        if(item.fileName.IndexOf(".mp3") == item.fileName.Length - 4)
                        {
                            item.setSize(this.getSizeLocal(item.fileName));
                            trackList.Add(item);
                        }
                    }
                    LibraryElement.ItemsSource = trackList;
                }
            }
        }
        #endregion

        private bool IsolatedStorageFileExists(string name)
        {
            using (var folder = IsolatedStorageFile.GetUserStoreForApplication())
            {
                return folder.FileExists(name);
            }
        }

        private void NetworkAddress_Changed(object sender, EventArgs e)
        {
            if(!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("Network connection lost. Please connect to a network if you want to access online music.");
            }
        }

        private ListBoxItem getListBoxItem(Object sender) {
            return TrackListElement.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
        }

        private void getErrors( object sender, EventArgs e ) {
            string msg = Helper.BackgroundErrorNotifier.getError();
            string message = "Player error: " + msg + ". The error has been reported to the developer. Sorry for the inconvenience.";
            if(msg != null && msg != "")
            {
                switch(msg) {
                    case "-1072873844": //C00D3E8C
                        // The Media format is recognized but is invalid.
                    case "-1072873852": //C00D3E84
                        // The media stream cannot process any more samples because there are no more samples in the stream.
                        message = "Invalid or corrupt file. Please try another.";
                        break;
                    case "-1072875819": //C00D36D5
                        // The specified object or value does not exist.
                        message = "Unknown error. Please try again.";
                        break;
                    case "-1072877850": //C00D2EE6
                        // The server is currently unable to handle the request due to a temporary overloading or maintenance of the server.
                        message = "Server unavailable. Please try again.";
                        break;
                    case "-1072877851": //C00D2EE5
                        // The proxy did not receive a timely response while attempting to contact the media server.
                        message = "Proxy timeout. Please try again.";
                        break;
                    case "-1072887838": //C00D07E2
                        // ASF_E_INVALIDHEADER
                        message = "Invalid header received. Please try again.";
                        break;
                    case "-1072889830": //C00D001A
                        // The system cannot find the file specified.
                        message = "The file could not be found. Please try another.";
                        break;
                    case "-2147012696": //20072FA8  --> UNKNOWN
                        // error message not available
                        message = "No available network connection.";
                        break;
                    case "-2147012865": //80072EFF
                        // error message not available
                        message = "Internet connection reset. Please try again.";
                        break;
                    case "-2147012867": //80072EFD
                        // ERROR_INTERNET_CANNOT_CONNECT
                        message = "A connection with the server could not be established.";
                        break;
                    case "-2147012889": //80072EE7
                        // error message not available
                        message = "Can’t resolve hostname (is the phone in flight mode?)";
                        break;
                    case "-2147024638": //80070102
                        // error message not available
                        message = "Connection timeout. Please check if you have a working internet connection.";
                        break;
                    case "-2147024891": //80070005
                        // General access denied error.
                        message = "Access denied error. Please check if you are allowed to access this file.";
                        break;
                    case "-2147024894": //80070002
                        // The system cannot find the file specified.
                        message = "File not found. Please try again.";
                        break;
                    case "-2147467259": //80004005
                        // error message not available
                        message = "Action failed. The error has been reported to the developer. Sorry for the inconvenience.";
                        break;
                }
                MessageBox.Show(message, "Player Error", MessageBoxButton.OK);
                Helper.BackgroundErrorNotifier.addError(null);
                BackgroundAudioPlayer.Instance.SkipNext();
                mtiks.Instance.AddException(new Exception("BAP Error: " + message));
            }

            if(checkFlag("NotFound"))
            {
                MessageBox.Show("The file was not found. Please try another file.");
                BackgroundAudioPlayer.Instance.Stop();
                positionIndicator.IsIndeterminate = false;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo( e );

            (Application.Current as App).checkIfUpdated();

            setAppTile(null, null);

            // Reset all of the user action Booleans on page load.
            WaitingForExternalPower = false;
            WaitingForExternalPowerDueToBatterySaverMode = false;
            WaitingForNonVoiceBlockingNetwork = false;
            WaitingForWiFi = false;

            // When the page loads, refresh the list of file transfers.
            InitialTransferStatusCheck();
            //UpdateUI(null, null);
            updateLibrary();
            Instance_PlayStateChanged(null, null);
            updatePlayerProgress("force", null);

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

                System.Diagnostics.Debug.WriteLine("Trying to play song: " + songToPlay);

                String[] name = this.getArtistAndTitle(songToPlay);

                addToPlaylist(name[0], name[1], songToPlay);

                List<String[]> oldItems = this.readPlaylist();
                int itemsCount = oldItems.Count;

                AudioPlaybackAgent1.AudioPlayer.playAtPosition(itemsCount - 1, BackgroundAudioPlayer.Instance);
                //Instance_PlayStateChanged(null, null);
                enablePlayerUI(false);
                this.Panorama.SelectedItem = this.playerItem;
            }
            else if(NavigationContext.QueryString.ContainsKey(_showPlayerKey))
            {
                this.Panorama.SelectedItem = this.libraryItem;
            }

        }

        #region Transfers
        private void InitialTransferStatusCheck()
        {
            UpdateRequestsList();

            /*foreach(var transfer in transferRequests)
            {
                transfer.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferStatusChanged);
                transfer.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(transfer_TransferProgressChanged);
                ProcessTransfer(transfer);
            }*/


            if(WaitingForExternalPower)
            {
                MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power or change the settings in MusicBird to continue transferring.");
            }
            if(WaitingForExternalPowerDueToBatterySaverMode)
            {
                MessageBox.Show("You have one or more file transfers waiting for external power. Connect your device to external power or disable Battery Saver Mode to continue transferring.");
            }
            if(WaitingForNonVoiceBlockingNetwork)
            {
                MessageBox.Show("You have one or more file transfers waiting for a network that supports simultaneous voice and data.");
            }
            if(WaitingForWiFi)
            {
                MessageBox.Show("You have one or more file transfers waiting for a WiFi connection. Connect your device to a WiFi network to continue transferring or change the settings in MusicBird.");
            }
        }

        private void ProcessTransfer( BackgroundTransferRequest transfer )
        {
            switch(transfer.TransferStatus)
            {
                case TransferStatus.Transferring:
                    //_downloadTimer.Start();
                    break;
                case TransferStatus.Completed:

                    // If the status code of a completed transfer is 200 or 206, the
                    // transfer was successful
                    if(transfer.StatusCode == 200 || transfer.StatusCode == 206)
                    {
                        // Remove the transfer request in order to make room in the 
                        // queue for more transfers. Transfers are not automatically
                        // removed by the system.
                        RemoveTransferRequest(transfer.RequestId);
                        UpdateDownloads(null, null, true);

                        if(transferRequests.Count<BackgroundTransferRequest>() > 0)
                        {
                            //_downloadTimer.Stop();
                        }

                        // In this example, the downloaded file is moved into the root
                        // Isolated Storage directory
                        string fileName = "";
                        using(IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            fileName = transfer.Tag;
                            if(isoStore.FileExists(fileName))
                            {
                                isoStore.DeleteFile(fileName);
                            }
                            isoStore.MoveFile(transfer.DownloadLocation.OriginalString, fileName);
                        }

                        updateLibrary();
                        var upload = Helper.Preferences.read("dropboxUpload");
                        if(upload != null && upload.Equals(true.ToString()))
                        {
                            AccessToken token = this.authenticate();
                            sendFile(token, fileName);
                        }

                        try
                        {
                            if(this.Panorama.SelectedItem.Equals(this.downloadItem)) this.Panorama.SelectedItem = this.libraryItem;
                        }
                        catch(NullReferenceException e)
                        {
                            log(e);
                        }

                        StreamResourceInfo sri = null;
                        Uri imageUri = new Uri("Images/MusicBird_GREEN_173.png", UriKind.Relative);
                        sri = Application.GetResourceStream(imageUri);

                        WriteableBitmap wb = new WriteableBitmap(173, 173);
                        wb.SetSource(sri.Stream);

                        MemoryStream stream = new MemoryStream();
                        wb.SaveJpeg(stream, 173, 173, 0, 90);
                        stream.Seek(0, SeekOrigin.Begin);

                        MediaHistoryItem mediaHistoryItem = new MediaHistoryItem();

                        //<hubTileImageStream> must be a valid ImageStream.
                        mediaHistoryItem.ImageStream = stream;
                        mediaHistoryItem.Source = String.Empty;
                        mediaHistoryItem.Title = this.getArtistAndTitle(fileName)[0];
                        System.Diagnostics.Debug.WriteLine("Adding " + this.getArtistAndTitle(fileName)[0] + " as new");
                        mediaHistoryItem.PlayerContext.Add("playSong", fileName);
                        MediaHistory.Instance.WriteAcquiredItem(mediaHistoryItem);

                        stream.Close();
                    }
                    else
                    {
                        // This is where you can handle whatever error is indicated by the
                        // StatusCode and then remove the transfer from the queue. 
                        RemoveTransferRequest(transfer.RequestId);

                        if(transfer.TransferError != null)
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

        void transfer_TransferStatusChanged( object sender, BackgroundTransferEventArgs e )
        {
            ProcessTransfer(e.Request);
            UpdateDownloads(null, null, true);
        }

        void transfer_TransferProgressChanged( object sender, BackgroundTransferEventArgs e )
        {
            System.Diagnostics.Debug.WriteLine("Downloaded " + (e.Request.BytesReceived * 100) / e.Request.TotalBytesToReceive + " %");
            UpdateDownloads(null, null, false);
        }

        private void CancelButton_Click( object sender, EventArgs e )
        {
            // The ID for each transfer request is bound to the
            // Tag property of each Remove button.
            string transferID = ((Button)sender).Tag as string;

            // Delete the transfer request
            RemoveTransferRequest(transferID);

            // Refresh the list of file transfers
            UpdateDownloads(null, null, false);
        }

        private void RemoveTransferRequest( string transferID )
        {
            // Use Find to retrieve the transfer request with the specified ID.
            try
            {
                BackgroundTransferRequest transferToRemove = BackgroundTransferService.Find(transferID);
                BackgroundTransferService.Remove(transferToRemove);
            }
            catch(Exception e) {
                // Do nothing.
            }
        }

        private void UpdateDownloads( object sender, EventArgs e, bool force )
        {
            if(this.Panorama.SelectedItem.Equals(this.downloadItem) || force)
            {
                System.Diagnostics.Debug.WriteLine("Updating Downloads");

                UpdateRequestsList();

                // If there are 1 or more transfers, hide the "no transfers"
                // TextBlock. IF there are zero transfers, show the TextBlock.
                /*try
                {*/
                    if(transferRequests.Count<BackgroundTransferRequest>() > 0)
                    {
                        EmptyTextBlock.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        EmptyTextBlock.Visibility = Visibility.Visible;
                    }
                    /*}
                    catch(UriFormatException ex) {
                        EmptyTextBlock.Visibility = Visibility.Collapsed;
                    }*/

                // Update the TransferListBox with the list of transfer requests.
                TransferListBox.ItemsSource = this.transferRequests;
            }
        }

        private void UpdateRequestsList()
        {
            // The Requests property returns new references, so make sure that
            // you dispose of the old references to avoid memory leaks.
            if (transferRequests != null)
            {
                foreach (var request in this.transferRequests)
                {
                    request.Dispose();
                }
            }
            // this is apparently not necessary. Works without disposing,
            // the memory goes down again after the req. is finished.

            transferRequests = BackgroundTransferService.Requests;
            //System.Diagnostics.Debug.WriteLine(Microsoft.Phone.Info.DeviceStatus.ApplicationCurrentMemoryUsage.ToString());
        }

        private void saveTrack( string fileName, string uri )
        {
            uri = Uri.EscapeUriString(uri);
            System.Diagnostics.Debug.WriteLine(uri);
            mtiks.Instance.postEventAttributes("DOWNLOAD",
            new Dictionary<string, string>() { { "FILENAME_URI", fileName + " @ " + uri } });
            // Check to see if the maximum number of requests per app has been exceeded.
            /*try
            {*/
                if(BackgroundTransferService.Requests.Count() >= 5)
                {
                    // Note: Instead of showing a message to the user, you could store the
                    // requested file URI in isolated storage and add it to the queue later.
                    MessageBox.Show("The maximum number of background file transfer requests for this application has been exceeded. ");
                    return;
                }
            /*}
            catch(UriFormatException) {
                // nothing
            }*/

            // Get the URI of the file to be transferred from the Tag property
            // of the button that was clicked.
            Uri transferUri = new Uri(uri, UriKind.RelativeOrAbsolute);


            // Create the new transfer request, passing in the URI of the file to 
            // be transferred.
            BackgroundTransferRequest transferRequest = new BackgroundTransferRequest(transferUri);

            // Set the transfer method. GET and POST are supported.
            transferRequest.Method = "GET";

            // Get the file name from the end of the transfer URI and create a local URI 
            // in the "transfers" directory in isolated storage.

            using(IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(!isoStore.DirectoryExists("/shared/transfers"))
                {
                    isoStore.CreateDirectory("/shared/transfers");
                }
            }

            //Prevent PathTooLongException
            if(fileName.Length > 150) fileName = fileName.Substring(0, 150);

            System.Diagnostics.Debug.WriteLine("fileName is " + fileName + ".mp3");
            transferRequest.DownloadLocation = new Uri("/shared/transfers/" + fileName + ".mp3", UriKind.Relative);

            // Pass custom data with the Tag property. In this example, the friendly name
            // is passed.
            transferRequest.Tag = fileName + ".mp3";
            transferRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:7.0.1) Gecko/20100101 Firefox/7.0.12011-10-16 20:23:00");
            System.Diagnostics.Debug.WriteLine(transferRequest.Headers["User-Agent"]);

            /*bool allowCellular = Helper.Preferences.readBool("allowCellular");
            bool allowBattery = Helper.Preferences.readBool("allowBattery");
            if(allowCellular && allowBattery)
            {*/
            transferRequest.TransferPreferences = TransferPreferences.AllowCellularAndBattery;
            /*}
            else if(allowCellular)
            {
                transferRequest.TransferPreferences = TransferPreferences.AllowCellular;
            }
            else if(allowBattery)
            {
                transferRequest.TransferPreferences = TransferPreferences.AllowBattery;
            }*/


            // Add the transfer request using the BackgroundTransferService. Do this in 
            // a try block in case an exception is thrown.
            try
            {
                BackgroundTransferService.Add(transferRequest);
                transferRequest.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(this.transfer_TransferStatusChanged);
                transferRequest.TransferProgressChanged += new EventHandler<BackgroundTransferEventArgs>(this.transfer_TransferProgressChanged);
                UpdateDownloads(null, null, true);
                this.Panorama.SelectedItem = this.downloadItem;
                //_downloadTimer.Start();
            }
            catch(InvalidOperationException ex)
            {
                MessageBox.Show("Unable to add background transfer request. " + ex.Message);
                log(ex);
            }
            setAppTile(null, null);
        }

        #endregion

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
            for ( int i = 1; i < array.Length; i++ )
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

        #region Playlist IO
        private void addToPlaylist( String artist, String title, String url )
        {
            List<String[]> pl = readPlaylist();
            pl.Add(new String[] { artist, title, url });
            writePlaylist(pl);
        }

        private List<String[]> getFromPlaylist()
        {
            return readPlaylist();
        }

        private void removeFromPlaylist( object item )
        {
            List<String[]> pl = this.readPlaylist();
            if(item is int)
            {
                int index = (int)item;
                pl.RemoveAt(index);
            }
            else if(item is string[])
            {
                string[] part = (string[])item;
                pl.Remove(part);
            }
            writePlaylist(pl);
        }

        private void writePlaylist( List<String[]> playlist )
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;

            using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(myIsolatedStorage.FileExists("Playlist.xml"))
                {
                    myIsolatedStorage.DeleteFile("Playlist.xml");
                }
                using(IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("Playlist.xml", FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<String[]>));
                    using(XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSettings))
                    {
                        serializer.Serialize(xmlWriter, playlist);
                    }
                    stream.Close();
                }
            }
        }

        private List<String[]> readPlaylist()
        {
            try
            {
                using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(!myIsolatedStorage.FileExists("Playlist.xml"))
                    {
                        return new List<string[]>();
                    }
                    using(IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("Playlist.xml", FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<String[]>));
                        List<String[]> data = (List<String[]>)serializer.Deserialize(stream);
                        stream.Close();
                        return data;
                    }
                }
            }
            catch(IsolatedStorageException e)
            {
                log(e);
                return new List<string[]>();
            }
        } 
        #endregion

        #region albumart
        private void getAlbumArt( string searchterm )
        {
            String websiteURL = "http://api.bing.net/json.aspx?AppId=A34B1552C3B3DF826089895CCA0D868F5B4AE201&Query=" + searchterm.Trim() + "%20Cover&Sources=Image&Filters=Size:Small";
            WebClient c = new WebClient();
            c.DownloadStringAsync(new Uri(websiteURL));
            c.DownloadStringCompleted += new DownloadStringCompletedEventHandler(this.c_DownloadStringCompleted);
        }

        private void c_DownloadStringCompleted( object sender, DownloadStringCompletedEventArgs e)
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
                        albumartImage.Source = new BitmapImage(new Uri(url, UriKind.RelativeOrAbsolute));
                    }
                }
                catch(WebException ex) {
                    log(ex);
                }
            }
        }
        #endregion

        public static T FindFirstElementInVisualTree<T>( DependencyObject parentElement ) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parentElement);
            if(count == 0)
                return null;
            for(int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parentElement, i);
                if(child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    var result = FindFirstElementInVisualTree<T>(child);
                    if(result != null)
                        return result;
                }
            }
            return null;
        }

        /*private void toggleRepeat( object sender, RoutedEventArgs e )
        {
            if(Helper.Preferences.readBool("repeat"))   //disable it!
            {
                repeatButton.Opacity = 0.4;
                Helper.Preferences.write("repeat", false);
            }
            else {  //enable it
                repeatButton.Opacity = 1;
                Helper.Preferences.write("repeat", true);
            }
        }

        private void toggleShuffle( object sender, RoutedEventArgs e )
        {
            if(Helper.Preferences.readBool("shuffle"))   //disable it!
            {
                shuffleButton.Opacity = 0.4;
                Helper.Preferences.write("shuffle", false);
            }
            else
            {  //enable it
                shuffleButton.Opacity = 1;
                Helper.Preferences.write("shuffle", true);
            }
        }*/

        private static bool checkFlag(String type)
        {
            bool value = Helper.Preferences.readBool(type);
            if(value) Helper.Preferences.write(type, false);
            return value;
        }

        #region Dropbox Upload
        private AccessToken authenticate()
        {
            if(Helper.Preferences.read("dropbox-access-token-key") != null)
            {
                System.Diagnostics.Debug.WriteLine("Access token exists");
                string key = Helper.Preferences.read("dropbox-access-token-key");
                string secret = Helper.Preferences.read("dropbox-access-token-secret");
                return new AccessToken(key, secret);
            }
            else
            {
                NavigationService.Navigate(new Uri("/Page1.xaml?action=dropboxauth", UriKind.Relative));
                return null;
            }
        }

        private void sendFile( AccessToken accessToken, String fileName )
        {
            System.Diagnostics.Debug.WriteLine(accessToken.Key + " " + accessToken.Secret + " " + fileName);
            var client = new OAuthClient(DropboxAuth.consumerKey, DropboxAuth.consumerSecret, accessToken);
            string remoteName = Regex.Replace(fileName, "[^A-Za-z0-9.-]", "");
            System.Diagnostics.Debug.WriteLine("Remote name is " + remoteName);
            client.Url = "https://api-content.dropbox.com/1/files_put/sandbox/" + remoteName;
            client.Parameters.Add("overwrite", "true");
            client.MethodType = MethodType.Put;
            var webRequest = client.CreateWebRequest();
            webRequest.BeginGetRequestStream(this.StartUpload, new object[] { webRequest, fileName, remoteName });
            addUploadCounter(1);
        }

        private void receiveFile( AccessToken accessToken, string fileName ) {
            System.Diagnostics.Debug.WriteLine(accessToken.Key + " " + accessToken.Secret + " " + fileName);
            var client = new OAuthClient(DropboxAuth.consumerKey, DropboxAuth.consumerSecret, accessToken);
            client.Url = "https://api-content.dropbox.com/1/metadata/sandbox/";
            client.Parameters.Add("overwrite", "true");
            client.MethodType = MethodType.Get;
            var webRequest = client.CreateWebRequest();
            webRequest.Method = "GET";
            webRequest.BeginGetResponse(r =>
            {
                var httpRequest = (HttpWebRequest)r.AsyncState;
                var httpResponse = (HttpWebResponse)httpRequest.EndGetResponse(r);

                using(var reader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var response = reader.ReadToEnd();

                    Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        System.Diagnostics.Debug.WriteLine(response);
                    }));
                }
            }, webRequest);
        }

        private void StartUpload( IAsyncResult asyncResult )
        {
            object[] args = (object[])asyncResult.AsyncState;
            HttpWebRequest request = (HttpWebRequest)args[0];
            string fileName = (string)args[1];

            var postStream = request.EndGetRequestStream(asyncResult);
            using(var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    using(var stream = isolatedStorage.OpenFile(fileName, FileMode.Open))
                    {
                        stream.CopyTo(postStream);
                        postStream.Close();
                        stream.Close();
                    }
                }
                catch(IsolatedStorageException)
                {
                    this.Dispatcher.BeginInvoke(() =>
                    {
                        MessageBox.Show("You can't upload the file your playing. Please wait until it's finished and try again.");
                    });
                }
            }
            request.BeginGetResponse(this.EndUpload, new object[] { request, fileName });
        }

        private void EndUpload( IAsyncResult asyncResult )
        {
            object[] args = (object[])asyncResult.AsyncState;
            HttpWebRequest request = (HttpWebRequest)args[0];
            string fileName = (string)args[1];

            int statusCode = 0;

            try
            {
                var response = (HttpWebResponse)request.EndGetResponse(asyncResult);
                statusCode = (int)response.StatusCode;
                response.Dispose();
            }
            catch(WebException ex)
            {
                log(ex);
                var response = ((HttpWebResponse)ex.Response);
                statusCode = (int)response.StatusCode;
                response.Dispose();
            }
            catch(NotSupportedException ex)
            {
                //Upload was cancelled (e.g. file in-use)
                log(ex);
            }
            finally
            {
                string msg = "Unknown Error";
                #region Switch for msg
                switch(statusCode)
                {
                    case 200:
                        msg = "File upload completed.";
                        break;
                    case 400:
                        msg = "Bad input parameter.";
                        msg += " This is an error on our side. Sorry for the inconvenience. Please try again.";
                        break;
                    case 401:
                        msg = "Bad or expired authentication data. Please log in again.";
                        break;
                    case 403:
                        msg = "Bad OAuth request (wrong parameter). Re-authentication won't help.";
                        msg += " This is an error on our side. Sorry for the inconvenience. Please try again.";
                        break;
                    case 404:
                        msg = "File/folder not found. Please check your internet connection.";
                        break;
                    case 405:
                        msg = "Request method not expected.";
                        msg += " This is an error on our side. Sorry for the inconvenience. Please try again.";
                        break;
                    case 503:
                        msg = "Too many request. App is being rate-limited. Please try again later";
                        break;
                    case 507:
                        msg = "User is over Dropbox storage quota. Try to clean up or buy additional space.";
                        break;
                    default:
                        msg = "Error " + statusCode + ". Unknown description.";
                        msg += " This is an error on our side. Sorry for the inconvenience. Please try again.";
                        break;
                }
                #endregion

                this.Dispatcher.BeginInvoke(() =>
                {
                    /*if(statusCode != 0) MessageBox.Show(msg, "Dropbox Upload", MessageBoxButton.OK);
                    if(statusCode == 401) NavigationService.Navigate(new Uri("/Page1.xaml?action=dropboxauth", UriKind.Relative));
                    mtiks.Instance.postEventAttributes("UPLOAD",
                            new Dictionary<string, string>() { { statusCode.ToString(), fileName + "-->" + remoteName } });

                    //Count the 'running uploads' counter down

                    addUploadCounter(-1);*/
                });
            }
        }

        private void addUploadCounter( int howMany )
        {
            int counter = (Application.Current as App).dropboxUploads += howMany;
            if(counter == 0)
            {
                /*uploadProgress.Visibility = Visibility.Collapsed;
                uploadProgress.IsIndeterminate = false;*/
            }
            if(counter > 0)
            {
               /* uploadProgress.Visibility = Visibility.Visible;
                uploadProgress.IsIndeterminate = true;*/
            }
            //uploadCounter.Text = counter.ToString() + " running uploads";
        }

        private void shareTrack( string artist, string title, Uri source ) {
            var shareLinkTask = new ShareLinkTask();
            shareLinkTask.Title = artist+" - "+title;
            shareLinkTask.LinkUri = source;
            shareLinkTask.Message = "I'm listening to '" + artist + " - " + title + "' on MusicBird, " +
                "the free music app for Windows Phone 7.";
            shareLinkTask.Show();
        }

        #endregion

        private void log( Exception ex ) {
            System.Diagnostics.Debug.WriteLine("Message         : " + ex.Message);
            System.Diagnostics.Debug.WriteLine("Inner Exception : " + ex.InnerException);
            System.Diagnostics.Debug.WriteLine("Stacktrace      : " + ex.InnerException);
            mtiks.Instance.AddException(ex);
        }

        private void image1_Tap( object sender, System.Windows.Input.GestureEventArgs e )
        {
            if(BackgroundAudioPlayer.Instance.Track == null) return;
            AudioTrack currentTrack = BackgroundAudioPlayer.Instance.Track;
            if(currentTrack.Source.ToString().IndexOf("http") == 0)
            {
                shareTrack(currentTrack.Artist, currentTrack.Title, currentTrack.Source);
            }
            else {
                MessageBox.Show("You can only share online tracks, not those which are in your library.");
            }
        }

        private void shuffle_tap( object sender, System.Windows.Input.GestureEventArgs e )
        {
            if(Helper.Preferences.readBool("shuffle"))
            {
                Helper.Preferences.write("shuffle", false);
                shuffleButton.Opacity = 0.5;
            }
            else
            {
                Helper.Preferences.write("shuffle", true);
                shuffleButton.Opacity = 1;
            }
        }

        private void repeat_tap( object sender, System.Windows.Input.GestureEventArgs e )
        {
            if(Helper.Preferences.readBool("repeat"))
            {
                Helper.Preferences.write("repeat", false);
                repeatButton.Opacity = 0.5;
            }
            else
            {
                Helper.Preferences.write("repeat", true);
                repeatButton.Opacity = 1;
            }
        }
    }

    #region Class Definitions
    public class TrackListItem
    {
        public string title { get; set; }
        public string artist { get; set; }
        public double size { get; private set; }
        public string duration { get; set; }
        public string url { get; set; }
        public string sizeText { get; private set; }

        public TrackListItem( String artist, String title, String url )
        {
            this.artist = artist;
            this.title = title;
            this.url = url;
            this.size = 0;
        }

        public void setSize( double size ) {
            this.size = size;
            this.sizeText = (size / (double)1048576).ToString("F2") + " MB";
        }
    }

    public class LibraryItem
    {
        public string fileName { get; set; }
        public long size { get; set; }
        public string sizeText { get; set; }

        public LibraryItem( String fileName )
        {
            this.fileName = fileName;
        }

        public void setSize( long size )
        {
            this.size = size;
            double mb = size/(double)1048576;
            this.sizeText = mb.ToString("F2") + " MB";
        }
    } 
    #endregion
}