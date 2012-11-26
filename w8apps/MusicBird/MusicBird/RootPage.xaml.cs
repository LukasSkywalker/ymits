using MusicBird.Common;
using System;
using Windows.Media;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MusicBird
{
    public sealed partial class RootPage : Page
    {
        private DispatcherTimer PlaybackTimer;
        private DispatcherTimer PlayTimeoutTimer;
        private Windows.UI.Core.CoreDispatcher EventDispatcher;

        public Playlist Playlist { get; set; }
        public DownloadManager DownloadManager { get; set; }

        public RootPage()
        {
            this.InitializeComponent();

            Playlist = new Playlist();
            DownloadManager = new DownloadManager();

            PlaybackTimer = new DispatcherTimer();
            PlaybackTimer.Interval = TimeSpan.FromSeconds(1);
            PlaybackTimer.Tick += PlaybackTimer_Tick;

            PlayTimeoutTimer = new DispatcherTimer();
            PlayTimeoutTimer.Interval = TimeSpan.FromSeconds(4);
            PlayTimeoutTimer.Tick += PlayTimeoutTimer_Tick;

            EventDispatcher = Window.Current.CoreWindow.Dispatcher;

            MediaControl.PlayPressed += MediaControl_PlayPressed;
            MediaControl.PausePressed += MediaControl_PausePressed;
            MediaControl.PlayPauseTogglePressed += MediaControl_PlayPauseTogglePressed;
            MediaControl.StopPressed += MediaControl_StopPressed;
            MediaControl.SoundLevelChanged += MediaControl_SoundLevelChanged;

            EnableButtons(true, false);

            volumeSlider.Value = playerElement.Volume * 10;
            progressSlider.ThumbToolTipValueConverter = new TimeSpanConverter();

            TransparentGrid.Visibility = Visibility.Collapsed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DownloadManager.ResumeDownloads();

            progressWheel.Visibility = Visibility.Collapsed;
            _frame.Navigate(typeof(StartPage));
        }

        /* Public Methods To Be Used By Subpages */

        public void PlayTrack(Track track) {
            Playlist.Add(track);
            Playlist.Position = Playlist.Size - 1;

            Play(track);
        }
        

        public void PlaylistAddTrack(Track track) {
            Playlist.Add(track);
        }

        public void PlayPosition(int position) {
            Playlist.Position = position;
            Track track = Playlist.CurrentTrack;
            
            Play(track);
        }

        private void slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            playerElement.Position = TimeSpan.FromSeconds(e.NewValue);
            currentTimeTextBlock.Text = TimeSpan.FromSeconds(e.NewValue).ToString(@"mm\:ss");
        }

        private void Play(Track track)
        {
            playerElement.Source = new Uri(Playlist.CurrentTrack.Url);

            MediaControl.ArtistName = Playlist.CurrentTrack.Artist;
            MediaControl.TrackName = Playlist.CurrentTrack.Title;

            playerElement.Play();
            PlayTimeoutTimer.Start();
        }

        private void volume_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            playerElement.Volume = volumeSlider.Value / 10;
        }

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

        private void mediaElement_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            switch (playerElement.CurrentState)
            {
                case MediaElementState.Playing:
                    HideWheel("mediaElement_CurrentStateChanged:playing");
                    PlaybackTimer.Start();
                    PlayTimeoutTimer.Stop();
                    btnPlay.Style = Helper.GetStyle("PauseButtonStyle");
                    EnableButtons(true, true);
                    break;

                case MediaElementState.Paused:
                    HideWheel("mediaElement_CurrentStateChanged:paused");
                    PlaybackTimer.Stop();
                    PlayTimeoutTimer.Stop();
                    btnPlay.Style = Helper.GetStyle("PlayButtonStyle");
                    EnableButtons(true, true);
                    break;

                case MediaElementState.Stopped:
                    HideWheel("mediaElement_CurrentStateChanged:stopped");
                    PlaybackTimer.Stop();
                    PlayTimeoutTimer.Stop();
                    btnPlay.Style = Helper.GetStyle("PlayButtonStyle");
                    progressSlider.Value = 0;
                    EnableButtons(true, false);
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
                Helper.GetSliderFrequency(playerElement.NaturalDuration.TimeSpan);

            totalTimeTextBlock.Text = playerElement.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            progressSlider.Value = 0.0;
            HideWheel("mediaElement_MediaEnded");
        }

        private async void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            HideWheel("mediaElement_MediaFailed");
            string hr = Helper.GetHresultFromErrorMessage(e);
            System.Diagnostics.Debug.WriteLine("ERROR:" + hr);
            MessageDialog md = new MessageDialog("The playback of this file failed. Please try another.", "Error");
            await md.ShowAsync();
            playerElement.Stop();
            playerElement.Source = null;
            HideWheel("mediaElement_MediaFailed_After");
        }

        private void mediaElement_DownloadProgressChanged(object sender, RoutedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine((sender as MediaElement).DownloadProgress);
        }

        private async void ShowWheel(String msg)
        {
            await EventDispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    progressWheel.IsActive = true;
                    progressWheel.Visibility = Visibility.Visible;
                });
            System.Diagnostics.Debug.WriteLine("Showing wheel: " + msg);
        }

        private void HideWheel(String msg)
        {
            System.Diagnostics.Debug.WriteLine("Hiding wheel: " + msg);
            progressWheel.IsActive = false;
            progressWheel.Visibility = Visibility.Collapsed;
        }

        public  void HidePopup(object sender, TappedRoutedEventArgs e)
        {
            TransparentGrid.Visibility = Visibility.Collapsed;
        }

        public void ShowPopup(Type ContentFrame)
        {
            ShowPopup(ContentFrame, null);
        }

        public void ShowPopup(Type ContentFrame, object parameter) {
            PopupContent.Navigate(ContentFrame, parameter);
            TransparentGrid.Visibility = Visibility.Visible;
        }

        private void PlaybackTimer_Tick(object sender, object e)
        {
            progressSlider.Value = playerElement.Position.TotalSeconds;
            currentTimeTextBlock.Text = playerElement.Position.ToString(@"mm\:ss");
        }

        private async void PlayTimeoutTimer_Tick(object sender, object e)
        {
            if (playerElement.CurrentState != MediaElementState.Playing)
            {
                playerElement.Stop();
                playerElement.Source = null;
                var msgd = new MessageDialog("Error playing this track. Please try another file.");
                try { await msgd.ShowAsync(); }
                catch (Exception) { }
                HideWheel("playerTimeoutTimer_Tick");
            }
            PlayTimeoutTimer.Stop();
        }

        private void EnableButtons(bool playButton, bool stopButton)
        {
            btnPlay.IsEnabled = playButton;
            btnStop.IsEnabled = stopButton;
        }

        private void MediaControl_StopPressed(object sender, object e)
        {
            ShowWheel("MediaControl_StopPressed");
            EventDispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                btnStop_Click(null, null);
            })).AsTask().Wait();
        }

        private void MediaControl_PlayPauseTogglePressed(object sender, object e)
        {
            ShowWheel("MediaControl_PlayPauseTogglePressed");
            EventDispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
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
            EventDispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                btnPlay_Click(null, null);
            })).AsTask().Wait();
        }

        private void MediaControl_PlayPressed(object sender, object e)
        {
            ShowWheel("MediaControl_PlayPressed");
            EventDispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                btnPlay_Click(null, null);
            })).AsTask().Wait();
        }

        private async void MediaControl_SoundLevelChanged(object sender, object e)
        {
            await EventDispatcher.RunAsync(
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
                }
            );
        }

        private void ShowPopup_Downloads(object sender, TappedRoutedEventArgs e)
        {
            ShowPopup(typeof(DownloadPage));
        }

        private void PlaylistPreview_Click(object sender, RoutedEventArgs e)
        {
            ShowPopup(typeof(PlaylistPage));
        }
    }
}
