using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Xml.Serialization;
using Microsoft.Devices;
using Microsoft.Phone.BackgroundAudio;
using System.Globalization;

namespace AudioPlaybackAgent1
{
    public class AudioPlayer : AudioPlayerAgent
    {
        
        private static volatile bool _classInitialized;
        public static int currentTrackNumber
        {
            get
            {
                return Helper.Preferences.readInt("currentTrackNumber");
            }
            set
            {
                Helper.Preferences.write("currentTrackNumber", value);
            }
        }
        //private DispatcherTimer timer;

        public static List<AudioTrack> _playList = new List<AudioTrack>
        {

            
        };
        private bool IsTrial;
        private bool trialRead = false;

        /// <remarks>
        /// AudioPlayer instances can share the same process. 
        /// Static fields can be used to share state between AudioPlayer instances
        /// or to communicate with the Audio Streaming agent.
        /// </remarks>
        public AudioPlayer()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += AudioPlayer_UnhandledException;
                });
            }
        }

        public static void playAtPosition( int position, BackgroundAudioPlayer player ) {
            player.Volume = 1;
            List<string[]> playlist = readPlaylist();
            if(playlist.Count <= position)
            {
                position = 0;
            }
            if(position < 0)
            {
                position = playlist.Count - 1;
            }
            
            System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:playAtPosition _______ Starting playback...");
            AudioTrack currentTrack = getAudioTrackAt(position);
            if(currentTrack.Source == null) {
                player.Stop();
                setFlag("NotFound");
                return;
            }
            player.Track = currentTrack;
            //The PlayStateChangedEventHandler will be called as soon as the track is loaded
            currentTrackNumber = position;

            MediaHistoryItem mediaHistoryItem = new MediaHistoryItem();

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {                
                StreamResourceInfo sri = null;
                Uri imageUri = new Uri("Images/MusicBird_GREEN_358.png", UriKind.Relative);
                sri = Application.GetResourceStream(imageUri);

                WriteableBitmap wb = new WriteableBitmap(358, 358);
                wb.SetSource(sri.Stream);

                MemoryStream stream = new MemoryStream();
                wb.SaveJpeg(stream, 358, 358, 0, 90);
                stream.Seek(0, SeekOrigin.Begin);

                mediaHistoryItem.ImageStream = stream;
                mediaHistoryItem.Source = String.Empty;
                mediaHistoryItem.Title = currentTrack.Title;
                mediaHistoryItem.PlayerContext.Add("showPlayer", currentTrack.Artist+" - "+currentTrack.Title+".mp3");
                MediaHistory.Instance.NowPlaying = mediaHistoryItem;
                stream.Seek(0, SeekOrigin.Begin);

                //add to the history
                MediaHistory.Instance.WriteRecentPlay(mediaHistoryItem);
            });

            
        }

        /// Code to execute on Unhandled Exceptions
        private void AudioPlayer_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /* Called when the playstate changes, except for the Error state (see OnError)
         * 
         * Play State changes cannot be cancelled. They are raised even if the application
         * caused the state change itself, assuming the application has opted-in to the callback.
         * 
         * Notable playstate events: 
         * (a) TrackEnded: invoked when the player has no current track. The agent can set the next track.
         * (b) TrackReady: an audio track has been set and it is now ready for playack.
         * 
         * Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
         */

        protected override void OnPlayStateChanged(BackgroundAudioPlayer player, AudioTrack track, PlayState playState)
        {
            System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:OnPlayStateChanged ___ PlayState changed; is now "+playState.ToString());
            //updatePlaylist();
            switch (playState)
            {
                case PlayState.TrackEnded:
                    playAtPosition(getNextTrack(), player);
                    break;
                case PlayState.TrackReady:
                    if(isTrial()){
                        System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:OnPlayStateChanged ___ Trial mode detected.");
                        /*if(isPlaybackLimitExceeded())
                        {
                            player.Pause();
                            System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:OnPlayStateChanged ___ Playback limit exceeded. Pausing.");
                            setFlag("LimitExceeded");
                            return;
                        }*/
                    }
                    System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:OnPlayStateChanged ___ Track loaded, playing.");
                    player.Play();
                    break;
                case PlayState.Shutdown:
                    // TODO: Handle the shutdown state here (e.g. save state)
                    break;
                case PlayState.Unknown:
                    break;
                case PlayState.Stopped:
                    break;
                case PlayState.Paused:
                    break;
                case PlayState.Playing:
                    break;
                case PlayState.BufferingStarted:
                    break;
                case PlayState.BufferingStopped:
                    break;
                case PlayState.Rewinding:
                    break;
                case PlayState.FastForwarding:
                    break;
            }

            NotifyComplete();
        }

        /* Called when the user requests an action using application/system provided UI
         * User actions do not automatically make any changes in system state; the agent is responsible
         * for carrying out the user actions if they are supported.
         * Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
         */

        protected override void OnUserAction(BackgroundAudioPlayer player, AudioTrack track, UserAction action, object param)
        {
            //Dictionary<String, String> prefs = readPrefs();
            //updatePlaylist(currentTrackNumber+1);
            switch (action)
            {
                case UserAction.Play:
                    player.Play();
                    break;
                case UserAction.Stop:
                    if(player.PlayerState == PlayState.Playing)
                    {
                        player.Stop();
                        currentTrackNumber = 0;
                    }
                    break;
                case UserAction.Pause:
                    if (player.PlayerState == PlayState.Playing)
                    {
                        player.Pause();
                    }
                    break;
                case UserAction.FastForward:
                    player.FastForward();
                    break;
                case UserAction.Rewind:
                    player.Rewind();
                    break;
                case UserAction.Seek:
                    if(player.CanSeek)
                    {
                        player.Position = (TimeSpan)param;
                    }
                    break;
                case UserAction.SkipNext:
                    playAtPosition(getNextTrack(), player);
                    break;
                case UserAction.SkipPrevious:
                    if(player.Position.TotalSeconds < 5) playAtPosition(currentTrackNumber, player);
                    else playAtPosition(currentTrackNumber - 1, player);
                    break;
            }

            NotifyComplete();
        }

        private int getNextTrack() {
            if(Helper.Preferences.readBool("repeat"))
            {
                System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:getNextTrack__________ Playing in repeat mode at " + currentTrackNumber);
                return currentTrackNumber;
            }
            else if(Helper.Preferences.readBool("shuffle"))
            {
                Random random = new Random(DateTime.Now.Millisecond);
                int next = random.Next(readPlaylist().Count - 1);
                System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:getNextTrack__________ Playing in shuffle mode at "+next);
                return next;
            }
            else
            {
                return currentTrackNumber + 1;
            }
        }

        protected override void OnError(BackgroundAudioPlayer player, AudioTrack track, Exception error, bool isFatal)
        {
            System.Diagnostics.Debug.WriteLine("Error in BAP!");
            System.Diagnostics.Debug.WriteLine(error.Message);
            Helper.BackgroundErrorNotifier.addError(error);
            System.Diagnostics.Debug.WriteLine("Error was added");
            if (isFatal)
            {
                Abort();
            }
            else
            {
                player.Track = null;
                NotifyComplete();
            }

        }

        /* Called when the agent request is getting cancelled
         * 
         * Once the request is Cancelled, the agent gets 5 seconds to finish its work,
         * by calling NotifyComplete()/Abort().
         */
        protected override void OnCancel()
        {

        }

        private static AudioTrack getAudioTrackAt(int position){
            System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:getAudioTrackAt ______ Playing at position " + position);
            List<string[]> playlist = readPlaylist();
            if(playlist.Count > position && position >= 0)
            {
                String[] item = playlist[position];
                String url = item[2];
                System.Diagnostics.Debug.WriteLine(url);
                if(!url.StartsWith("http")){
                    /*try
                    {*/
                        using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if(!myIsolatedStorage.FileExists(url))
                            {
                                System.Diagnostics.Debug.WriteLine("File not found...");
                                setFlag("NotFound");
                                return new AudioTrack(null, null, null, null, null);
                            }
                        }
                    /*}
                    catch(Exception e) {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }*/
                }
                return new AudioTrack(new Uri(item[2],UriKind.RelativeOrAbsolute), item[1], item[0], null, null);

            }
            else
            {
                throw new Exception("Damn wrong array index in getAudioTrackAt()...");
            }
        }

        private static void setFlag(String type){
            //type is Limit or NotFound
            Helper.Preferences.write(type, true);
        }

        private  static List<String[]> readPlaylist()
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
                        return data;
                    }
                }
            }
            catch
            {
                //add some code here
                throw new IsolatedStorageException("Could not get Playlist file from UserStore");
            }
        }

        private bool isTrial() {
            if(this.trialRead){
                return this.IsTrial;
            }else{
                this.IsTrial = Helper.Preferences.readBool("trial");
                this.trialRead = true;
            }
            System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:isTrial ______________ isTrial: "+IsTrial.ToString());
            return this.IsTrial;
        }

        public static bool isPlaybackLimitExceeded()
        {
            /*DateTime now = new DateTime();
            now = DateTime.Now;
            DateTime date = now.Date;

            string playbackDate = Helper.Preferences.read("playback-date");
            string playbackNumber = Helper.Preferences.read("playback-number");

            if(playbackDate == null || DateTime.Parse(playbackDate) != date)
            {
                //last date was yesterday
                System.Diagnostics.Debug.WriteLine("Dates do not match. Counter set to 1.");
                playbackDate = date.ToString();
                Helper.Preferences.write("playback-number", 1);
                Helper.Preferences.write("playback-date", playbackDate);
                return false;
            }else{
                //already played today
                if(playbackNumber == null){
                    playbackNumber = 0.ToString();
                }
                if(Convert.ToInt32(playbackNumber, CultureInfo.InvariantCulture) >= 4)
                {
                    //more than or exactly 5 playbacks. Exceeded.
                    System.Diagnostics.Debug.WriteLine("Playback limit exceeded. " + Convert.ToInt32(playbackNumber, CultureInfo.InvariantCulture) + " plays.");
                    return true;
                }
                else{
                    //less than 5 replays. Count 1 up.
                    System.Diagnostics.Debug.WriteLine("Not exceeded. " + Convert.ToInt32(playbackNumber, CultureInfo.InvariantCulture) + " plays.");
                    playbackNumber = (Convert.ToInt32(playbackNumber, CultureInfo.InvariantCulture) + 1).ToString();
                    Helper.Preferences.write("playback-date", playbackDate);
                    Helper.Preferences.write("playback-number", playbackNumber);
                    return false;
                }
            }*/
            return false;
       }

    }
    public class TrackListItem
    {
        public string title { get; set; }
        public string artist { get; set; }
        public string size { get; set; }
        public string duration { get; set; }
        public string url { get; set; }

        public TrackListItem( String artist, String title, String url )
        {
            this.artist = artist;
            this.title = title;
            this.url = url;
        }
    }
}
