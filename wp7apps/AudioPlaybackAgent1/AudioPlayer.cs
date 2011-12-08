using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Windows;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Marketplace;

namespace AudioPlaybackAgent1
{
    public class AudioPlayer : AudioPlayerAgent
    {
        
        private static volatile bool _classInitialized;
        static int currentTrackNumber = 0;

        public static List<AudioTrack> _playList = new List<AudioTrack>
        {

            
        };

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

        

        private void PlayNextTrack(BackgroundAudioPlayer player)
        {
            if (++currentTrackNumber >= _playList.Count)
            {
                currentTrackNumber = 0;
            }
            
            PlayTrack(player);
        }

        private void PlayPreviousTrack(BackgroundAudioPlayer player)
        {
            if (--currentTrackNumber < 0)
            {
                currentTrackNumber = _playList.Count - 1;
            }

            PlayTrack(player);
        }

        private void PlayTrack(BackgroundAudioPlayer player)
        {
            updatePlaylist();
            // Sets the track to play. When the TrackReady state is received, 
            // playback begins from the OnPlayStateChanged handler.
            if(_playList.Count == 0) {
                if(player.PlayerState == PlayState.Playing)
                {
                    player.Stop();
                }
                else { 
                    System.Diagnostics.Debug.WriteLine("Not playing");
                }
                return;
            }
            if(_playList.Count>=currentTrackNumber+1)
            {
                System.Diagnostics.Debug.WriteLine("Index exists "+currentTrackNumber);
                if(_playList[currentTrackNumber] != null)
                {
                    System.Diagnostics.Debug.WriteLine("Index is not null " + currentTrackNumber);
                    player.Track = _playList[currentTrackNumber];
                }
                else {
                    System.Diagnostics.Debug.WriteLine("Index does is null" + currentTrackNumber);
                    GetNextTrack();
                    PlayTrack(player);
                }
            }
            else {
                System.Diagnostics.Debug.WriteLine("Index does not exist len<ctn+1; len=" + _playList.Count+", ctn+1=" + currentTrackNumber+1);
                GetNextTrack();
                PlayTrack(player);
            }
        }

        public static void addToList(AudioTrack track) {
            _playList.Add(track);
        }

        public static void removeFromList(AudioTrack track)
        {
            _playList.Remove(track);
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

        /// <summary>
        /// Called when the playstate changes, except for the Error state (see OnError)
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track playing at the time the playstate changed</param>
        /// <param name="playState">The new playstate of the player</param>
        /// <remarks>
        /// Play State changes cannot be cancelled. They are raised even if the application
        /// caused the state change itself, assuming the application has opted-in to the callback.
        /// 
        /// Notable playstate events: 
        /// (a) TrackEnded: invoked when the player has no current track. The agent can set the next track.
        /// (b) TrackReady: an audio track has been set and it is now ready for playack.
        /// 
        /// Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
        /// </remarks>
        protected override void OnPlayStateChanged(BackgroundAudioPlayer player, AudioTrack track, PlayState playState)
        {
            updatePlaylist();
            switch (playState)
            {
                case PlayState.TrackEnded:
                    GetNextTrack();
                    PlayTrack(player);
                    break;
                case PlayState.TrackReady:
                    if(isTrial()){
                        if(isPlaybackLimitExceeded())
                        {
                            player.Pause();
                            return;
                        }
                    }
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


        /// <summary>
        /// Called when the user requests an action using application/system provided UI
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track playing at the time of the user action</param>
        /// <param name="action">The action the user has requested</param>
        /// <param name="param">The data associated with the requested action.
        /// In the current version this parameter is only for use with the Seek action,
        /// to indicate the requested position of an audio track</param>
        /// <remarks>
        /// User actions do not automatically make any changes in system state; the agent is responsible
        /// for carrying out the user actions if they are supported.
        /// 
        /// Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
        /// </remarks>
        protected override void OnUserAction(BackgroundAudioPlayer player, AudioTrack track, UserAction action, object param)
        {
            updatePlaylist();
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
                    GetNextTrack();
                    System.Diagnostics.Debug.WriteLine("SkipNext; index is " + currentTrackNumber);
                    //if(nextTrack != null)
                    //{
                        PlayTrack(player);
                    //}
                    break;
                case UserAction.SkipPrevious:
                    GetPreviousTrack();
                    System.Diagnostics.Debug.WriteLine("SkipPrev; index is " + currentTrackNumber);
                    //if (previousTrack != null)
                    //{
                        PlayTrack(player);
                    //}
                    break;
            }

            NotifyComplete();
        }


        /// <summary>
        /// Implements the logic to get the next AudioTrack instance.
        /// In a playlist, the source can be from a file, a web request, etc.
        /// </summary>
        /// <remarks>
        /// The AudioTrack URI determines the source, which can be:
        /// (a) Isolated-storage file (Relative URI, represents path in the isolated storage)
        /// (b) HTTP URL (absolute URI)
        /// (c) MediaStreamSource (null)
        /// </remarks>
        /// <returns>an instance of AudioTrack, or null if the playback is completed</returns>
        private void GetNextTrack()
        {
            // TODO: add logic to get the next audio track

            updatePlaylist();

            if(++currentTrackNumber >= _playList.Count)
            {
                currentTrackNumber = 0;
            }

            //AudioTrack track = _playList[currentTrackNumber];

            // specify the track

            //return currentTrackNumber;
        }


        /// <summary>
        /// Implements the logic to get the previous AudioTrack instance.
        /// </summary>
        /// <remarks>
        /// The AudioTrack URI determines the source, which can be:
        /// (a) Isolated-storage file (Relative URI, represents path in the isolated storage)
        /// (b) HTTP URL (absolute URI)
        /// (c) MediaStreamSource (null)
        /// </remarks>
        /// <returns>an instance of AudioTrack, or null if previous track is not allowed</returns>
        private void GetPreviousTrack()
        {
            // TODO: add logic to get the previous audio track

            updatePlaylist();

            if(--currentTrackNumber < 0)
            {
                currentTrackNumber = _playList.Count-1;
            }

            //AudioTrack track = _playList[currentTrackNumber];
            

            // specify the track

            //return currentTrackNumber;
        }

        /// <summary>
        /// Called whenever there is an error with playback, such as an AudioTrack not downloading correctly
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track that had the error</param>
        /// <param name="error">The error that occured</param>
        /// <param name="isFatal">If true, playback cannot continue and playback of the track will stop</param>
        /// <remarks>
        /// This method is not guaranteed to be called in all cases. For example, if the background agent 
        /// itself has an unhandled exception, it won't get called back to handle its own errors.
        /// </remarks>
        protected override void OnError(BackgroundAudioPlayer player, AudioTrack track, Exception error, bool isFatal)
        {
            if (isFatal)
            {
                Abort();
            }
            else
            {
                NotifyComplete();
            }

        }

        /// <summary>
        /// Called when the agent request is getting cancelled
        /// </summary>
        /// <remarks>
        /// Once the request is Cancelled, the agent gets 5 seconds to finish its work,
        /// by calling NotifyComplete()/Abort().
        /// </remarks>
        protected override void OnCancel()
        {

        }

        private List<String[]> getFromPlaylist()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains("playlist"))
            {
                List<String[]> items;
                if(settings.TryGetValue<List<String[]>>("playlist", out items))
                {
                    return items;
                }
                else
                {
                    throw new IsolatedStorageException("Could not get Playlist from ApplicationSettings");
                }
            }
            else
            {
                return (new List<String[]>());
            }

        }

        private void updatePlaylist() {
            List<string[]> strings = getFromPlaylist();
            List<AudioTrack> trackList = new List<AudioTrack>();
            foreach(var item in strings)
            {
                trackList.Add(new AudioTrack(new Uri(item[2]), item[1], item[0], null, null));
            }
            _playList = trackList;
        }

        private bool isTrial() {
            bool isTrial = true;
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains("isTrial"))
            {
                settings.TryGetValue<bool>("isTrial", out isTrial);
            }
            System.Diagnostics.Debug.WriteLine("isTrial in AP is "+isTrial.ToString());
            return isTrial;
        }

        public static bool isPlaybackLimitExceeded()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            DateTime now = new DateTime();
            now = DateTime.Now;
            DateTime date = now.Date;

            if(settings.Contains("playback"))
            {
                String[] items;
                if(settings.TryGetValue<String[]>("playback", out items))
                {
                    if(DateTime.Parse(items[0]) != date)
                    {
                        //last date was yesterday
                        System.Diagnostics.Debug.WriteLine("Dates do not match. Counter set to 1.");
                        items[0] = date.ToString();
                        items[1] = "1";
                        return false;
                    }
                    else
                    {
                        //already played today
                        if(Convert.ToInt32(items[1]) >= 5)
                        {
                            //more than or exactly 5 playbacks. Exceeded.
                            System.Diagnostics.Debug.WriteLine("Playback limit exceeded. "+Convert.ToInt32(items[1])+" plays.");
                            return true;
                        }
                        else
                        {
                            //less than 5 replays. Count 1 up.
                            System.Diagnostics.Debug.WriteLine("Not exceeded. " + Convert.ToInt32(items[1]) + " plays.");
                            items[1] = (Convert.ToInt32(items[1]) + 1).ToString();
                            settings.Remove("playback");
                            settings.Add("playback", items);
                            settings.Save();
                            return false;
                        }
                    }
                }
                else
                {
                    settings.Remove("playback");
                    System.Diagnostics.Debug.WriteLine("Tryget failed. Setting to 1.");
                    String[] nullItem = new String[] {date.ToString(), "1" };
                    settings.Add("playback", nullItem);
                    settings.Save();
                    //failed to get value
                    return false;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(".contains() returns false. Created with count=1.");
                String[] nullItem = new String[] { date.ToString(), "1" };
                settings.Add("playback", nullItem);
                settings.Save();
                return false;
            }
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
