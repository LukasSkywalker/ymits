using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.IO.IsolatedStorage;
using System.Net;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework.Audio;

namespace Sirius
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region Class variables
        BackgroundWorker worker = new BackgroundWorker();
        #endregion

        private string access_token = "";

        private Microphone _microphone = Microphone.Default;
        private TimeSpan _fromMilliseconds = TimeSpan.FromMilliseconds(1000);
        private byte[] _buffer;
        private DynamicSoundEffectInstance _dynamicSound;
        private MemoryStream _memoryStream = new MemoryStream();

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            /*Authentication auth = new Authentication();
             * auth.BrowserLoaded += new Authentication.BrowserLoadedEventHandler(auth_BrowserLoaded);
             * auth.TokenReceived += new Authentication.TokenReceivedEventHandler(auth_TokenReceived);
             */

            // Set the features
            worker.WorkerReportsProgress = false;
            worker.WorkerSupportsCancellation = true;

            

        }

        private void auth_TokenReceived( string auth_key )
        {
            System.Diagnostics.Debug.WriteLine("Key received.");
            //the token is stored in local auth_key
            MessageBox.Show("Auth received, auth=" + auth_key);
        }

        private void auth_BrowserLoaded( string bla )
        {
            NavigationService.Navigate(new Uri("/Authentication.xaml", UriKind.Relative));
        }

        private void button1_Click( object sender, RoutedEventArgs e )
        {
            if(_microphone.State == MicrophoneState.Stopped)
            {
                System.Diagnostics.Debug.WriteLine("Rec started...");
                _microphone.BufferReady += new EventHandler<System.EventArgs>(_microphone_BufferReady);
                _microphone.BufferDuration = _fromMilliseconds;
                _buffer = new byte[_microphone.GetSampleSizeInBytes(_microphone.BufferDuration)];
                _dynamicSound = new DynamicSoundEffectInstance(_microphone.SampleRate, AudioChannels.Mono);

                _memoryStream.SetLength(0);

                _microphone.Start();
            }
            else {
                System.Diagnostics.Debug.WriteLine("Rec stopped.");
                _microphone.BufferReady -= new EventHandler<System.EventArgs>(_microphone_BufferReady);
                _microphone.Stop();
                Dictionary.getActionAndTime("What's to do in my calendar today?");
                Dictionary.getActionAndTime("What are the tasks today?");
                recognizeVoice();   
            }

        }

        void _microphone_BufferReady( object sender, EventArgs e )
        {
            System.Diagnostics.Debug.WriteLine("Buffer ready.");
            System.Diagnostics.Debug.WriteLine("Buffer Ready at {0}", DateTime.Now);
            _microphone.GetData(_buffer);
            System.Diagnostics.Debug.WriteLine("Buffer Length {0}", _buffer.Length);

            int count = 0;
            int buff = 0;
            for(int i = 0 ; i < _buffer.Length ; i = i+10)
            {
                count++;
                buff += Convert.ToInt32(_buffer[i].ToString());
            }
            System.Diagnostics.Debug.WriteLine(buff/count);

            prg.Value = buff / count;

            _memoryStream.Write(_buffer, 0, _buffer.Length);
        }

        private void button2_Click( object sender, RoutedEventArgs e )
        {
            _dynamicSound.SubmitBuffer(_memoryStream.GetBuffer());
            _dynamicSound.Play();
        } 


        private void recognizeVoice() {
            SpeechRecognition sr = new SpeechRecognition();
            sr.start(_memoryStream);
        }
    }

    [DataContract]
    public class FreeBusyItem {
        [DataMember]
        public String startTime { get; set; }
        [DataMember]
        public String endTime { get; set; }
        public FreeBusyItem(String start, String end) {
            this.startTime = start;
            this.endTime = end;
        }
    }

    [DataContract]
    public class CalendarListRequest2
    {
        [DataMember]
        public int maxResults { get; set; }
        [DataMember]
        public string minAccessRole { get; set; }
        [DataMember]
        public bool showHidden { get; set; }
        [DataMember]
        public Id[] items { get; set; }
        public CalendarListRequest2( int max, String minAccessRole, bool showHidden, Id[] items ) {
            this.maxResults = max;
            this.minAccessRole = minAccessRole;
            this.showHidden = showHidden;
            this.items = items;
        }
        public string toJSON() {
            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(Type.GetType("CalendarListRequest"));
            ser.WriteObject(stream1, this);
            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);
            return sr.ReadToEnd();
        }
    }

    [DataContract]
    public class Id
    {
        [DataMember]
        public String id { get; set; }
        public Id(String id){
            this.id = id;
        }
    }
}