﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.Phone.BackgroundAudio;

namespace AudioPlaybackAgent1
{
    public class AudioPlayer : AudioPlayerAgent
    {
        
        private static volatile bool _classInitialized;
        public static int currentTrackNumber = 0;
        public static Mutex mut = new Mutex(false, "playlistMutex");

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

        public static void playAtPosition( int position, BackgroundAudioPlayer player ) {
            List<string[]> playlist = readPlaylist();
            if(playlist.Count <= position) position = 0;
            if(position < 0) position = playlist.Count-1;
            
            System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:playAtPosition _______ Starting playback...");
            player.Track =  getAudioTrackAt(position);
            //The PlayStateChangedEventHandler will be called as soon as the track is loaded
            currentTrackNumber = position;
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
                    /*Dictionary<String,String> prefs = readPrefs();
                    try
                    {
                        if(prefs["repeat"].Equals("true"))
                        {
                            playAtPosition(currentTrackNumber, player);
                            break;
                        }
                        if(prefs["shuffle"].Equals("true"))
                        {
                            Random rand = new Random(DateTime.Now.Millisecond);
                            int pos = rand.Next() % _playList.Count;
                            playAtPosition(pos, player);
                            break;
                        }
                    }
                    catch(Exception e) {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }*/
                    playAtPosition(currentTrackNumber+1, player);
                    break;
                case PlayState.TrackReady:
                    if(isTrial()){
                        System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:OnPlayStateChanged ___ Trial mode detected.");
                        if(isPlaybackLimitExceeded())
                        {
                            player.Pause();
                            System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:OnPlayStateChanged ___ Playback limit exceeded. Pausing.");
                            return;
                        }
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
                    /*try
                    {
                        if(prefs["repeat"].Equals("true"))
                        {
                            playAtPosition(currentTrackNumber, player);
                            break;
                        }
                        if(prefs["shuffle"].Equals("true"))
                        {
                            Random rand = new Random(DateTime.Now.Millisecond);
                            int pos = rand.Next() % _playList.Count;
                            playAtPosition(pos, player);
                            break;
                        }
                    }
                    catch(Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }*/
                    playAtPosition(currentTrackNumber + 1, player);
                    break;
                case UserAction.SkipPrevious:
                    /*try
                    {
                        if(prefs["repeat"].Equals("true"))
                        {
                            playAtPosition(currentTrackNumber, player);
                            break;
                        }
                        if(prefs["shuffle"].Equals("true"))
                        {
                            Random rand = new Random(DateTime.Now.Millisecond);
                            int pos = rand.Next() % _playList.Count;
                            playAtPosition(pos, player);
                            break;
                        }
                    }
                    catch(Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }*/
                    playAtPosition(currentTrackNumber -1, player);
                    break;
            }

            NotifyComplete();
        }

        /* Called whenever there is an error with playback, such as an AudioTrack not downloading correctly
         * 
         * This method is not guaranteed to be called in all cases. For example, if the background agent
         * itself has an unhandled exception, it won't get called back to handle its own errors.
         */

        protected override void OnError(BackgroundAudioPlayer player, AudioTrack track, Exception error, bool isFatal)
        {
            System.Diagnostics.Debug.WriteLine(error.Message);
            if (isFatal)
            {
                Abort();
            }
            else
            {
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
                    try
                    {
                        using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if(!myIsolatedStorage.FileExists(url))
                            {
                                System.Diagnostics.Debug.WriteLine("File not found...");
                                return new AudioTrack(null, null, null, null, null);
                            }
                        }
                    }
                    catch(Exception e) {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }
                return new AudioTrack(new Uri(item[2],UriKind.RelativeOrAbsolute), item[1], item[0], null, null);

            }
            else
            {
                throw new Exception("Damn wrong array index in getAudioTrackAt()...");
            }
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

        /*public static Dictionary<String, String> readPrefs()
        {
            try
            {
                using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(!myIsolatedStorage.FileExists("Preferences.xml"))
                    {
                        return new Dictionary<String, String>();
                    }
                    using(IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("Preferences.xml", FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(Dictionary<String, String>));
                        Dictionary<String, String> data = (Dictionary<String, String>)serializer.Deserialize(stream);
                        return data;
                    }
                }
            }
            catch
            {
                // add some code here
                throw new IsolatedStorageException("Could not get Playlist file from UserStore");
            }
        }*/

        private bool isTrial() {
            bool isTrial = true;
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains("isTrial"))
            {
                settings.TryGetValue<bool>("isTrial", out isTrial);
            }
            System.Diagnostics.Debug.WriteLine("AudioPlayer.cs:isTrial ______________ isTrial: "+isTrial.ToString());
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
