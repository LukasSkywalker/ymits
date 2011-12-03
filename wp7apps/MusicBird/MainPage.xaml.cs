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
using System.Text.RegularExpressions;

namespace MusicBird
{
    public partial class MainPage : PhoneApplicationPage
    {

        // Timer for updating the UI
        DispatcherTimer _timer;

        // Indexes into the array of ApplicationBar.Buttons
        const int prevButton = 0;
        const int playButton = 1;
        const int pauseButton = 2;
        const int nextButton = 3;

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

            BackgroundAudioPlayer.Instance.PlayStateChanged += new EventHandler(Instance_PlayStateChanged);

            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                // If audio was already playing when the app was launched, update the UI.
                positionIndicator.IsIndeterminate = false;
                positionIndicator.Maximum = BackgroundAudioPlayer.Instance.Track.Duration.TotalSeconds;
                UpdateButtons(true, false, true, true);
                UpdateState(null, null);
            }
        }


        /// <summary>
        /// PlayStateChanged event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_PlayStateChanged(object sender, EventArgs e)
        {
            switch (BackgroundAudioPlayer.Instance.PlayerState)
            {
                case PlayState.Playing:
                    // Update the UI.
                    positionIndicator.IsIndeterminate = false;
                    positionIndicator.Maximum = BackgroundAudioPlayer.Instance.Track.Duration.TotalSeconds;
                    UpdateButtons(true, false, true, true);
                    UpdateState(null, null);

                    // Start the timer for updating the UI.
                    _timer.Start();
                    positionIndicator.IsIndeterminate = false;
                    break;

                case PlayState.Paused:
                    // Update the UI.
                    UpdateButtons(true, true, false, true);
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
        void UpdateButtons(bool prevBtnEnabled, bool playBtnEnabled, bool pauseBtnEnabled, bool nextBtnEnabled)
        {
            // Set the IsEnabled state of the ApplicationBar.Buttons array
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[prevButton])).IsEnabled = prevBtnEnabled;
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[playButton])).IsEnabled = playBtnEnabled;
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[pauseButton])).IsEnabled = pauseBtnEnabled;
            ((ApplicationBarIconButton)(ApplicationBar.Buttons[nextButton])).IsEnabled = nextBtnEnabled;
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
                positionIndicator.IsIndeterminate = false;
                
            }
            else
            {
                BackgroundAudioPlayer.Instance.Play();
                positionIndicator.IsIndeterminate = true;
            }
            
        }


        /// <summary>
        /// Click handler for the Pause button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pauseButton_Click(object sender, EventArgs e)
        {
            // Tell the backgound audio agent to pause the current track.
            BackgroundAudioPlayer.Instance.Pause();
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
                    if (name.IndexOf("-") > -1)
                    {
                        artist = name.Split((char)45)[0];
                        artist.Trim();
                        title = name.Split((char)45)[1];
                        title.Trim();
                    }
                    else {
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
                    TrackListItem item = new TrackListItem(artist, title, g.ToString());
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

            Panorama.DefaultItem = playerItem;
        }

        private void trackItem_Hold(object sender, RoutedEventArgs e) {
            TrackListItem selectedTrack = (sender as FrameworkElement).DataContext as TrackListItem;
            AudioTrack current = new AudioTrack(new Uri(selectedTrack.url, UriKind.RelativeOrAbsolute), selectedTrack.artist, "", "", null);

            AudioPlaybackAgent1.AudioPlayer.addToList(current);            

            //Panorama.DefaultItem = playerItem;
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
}