using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
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
        private TimeSpan _fromMilliseconds = TimeSpan.FromMilliseconds(100);
        private byte[] _buffer;
        private DynamicSoundEffectInstance _dynamicSound;
        private MemoryStream _memoryStream = new MemoryStream();
        private int startTime;
        private int canvasCounter = 0;

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

                startTime = Environment.TickCount;
                canvasCounter = 0;

                canvas1.Children.Clear();

                _microphone.Start();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Rec stopped.");
                _microphone.BufferReady -= new EventHandler<System.EventArgs>(_microphone_BufferReady);
                canvasCounter = 0;
                startTime = 0;
                _microphone.Stop();
                //Dictionary.getActionAndTime("What's to do in my calendar today?");
                //Dictionary.getActionAndTime("What are the tasks today?");
                //parseResult res1 = Dictionary.getActionAndTime("I want to go outside today?");
                //parseResult res2 = Dictionary.getActionAndTime("What's the weather today?");

                //doAction(res1);
                //doAction(res2);

                recognizeVoice();
            }

        }

        void _microphone_BufferReady( object sender, EventArgs e )
        {
            //System.Diagnostics.Debug.WriteLine("Buffer ready.");
            //System.Diagnostics.Debug.WriteLine("Buffer Ready at {0}", DateTime.Now);
            _microphone.GetData(_buffer);
            //System.Diagnostics.Debug.WriteLine("Buffer Length {0}", _buffer.Length);

            short level = BitConverter.ToInt16(_buffer, 0);


            Line line = new Line();
            line.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Purple);
            line.StrokeThickness = 4;

            Point point1 = new Point();
            point1.X = canvasCounter;
            point1.Y = canvas1.ActualHeight / 2 - (level - 50);

            Point point2 = new Point();
            point2.X = point1.X;
            point2.Y = canvas1.ActualHeight / 2 + (level - 50);

            line.X1 = point1.X;
            line.Y1 = point1.Y;
            line.X2 = point2.X;
            line.Y2 = point2.Y;

            canvas1.Children.Add(line);

            canvasCounter += 5;


            _memoryStream.Write(_buffer, 0, _buffer.Length);

            if(Environment.TickCount - startTime > 10000)
            {
                button1_Click(null, null);
            }
        }

        private void button2_Click( object sender, RoutedEventArgs e )
        {
            Dictionary dict = new Dictionary();
            dict.ResultParsed += new Dictionary.ResultParsedHandler(dict_ResultParsed);
            dict.getActionAndTime("Where am I?");
            /* DICTIONARY TEST
             * Translation.Translated += new Translation.TranslatedHandler(Translation_Translated);
             * Translation.start("de", "en", "Was wollen wir heute tun?");
             */
            
            
            /*_dynamicSound.SubmitBuffer(_memoryStream.GetBuffer());
            _dynamicSound.Play();*/
        }


        private void recognizeVoice()
        {
            SpeechRecognition sr = new SpeechRecognition();
            sr.SpeechRecognized += new SpeechRecognition.SpeechRecognizedHandler(sr_SpeechRecognized);
            sr.start(_memoryStream);
        }

        private void sr_SpeechRecognized( string result )
        {
            Dictionary dict = new Dictionary();
            dict.ResultParsed += new Dictionary.ResultParsedHandler(dict_ResultParsed);
            dict.getActionAndTime(result);
        }

        private void dict_ResultParsed( parseResult result ) {
            doAction(result);
        }

        private void doAction( parseResult result )
        {
            switch(result.action)
            {
                case "calendar":
                    break;
                case "weather":
                    Weather.WeatherReceived += new Weather.WeatherReceivedHandler(Weather_WeatherReceived);
                    Weather.getWeather("Bern");
                    break;
                case "translate":
                    Translation.Translated += new Translation.TranslatedHandler(Translation_Translated);
                    Translation.start("en", result.time, result.data);
                    break;
                case "map":
                    Location.LocationFound += new Location.LocationFoundHandler(Location_LocationFound);
                    Location.start("empty");
                    break;
                default:
                    break;
            }
        }

        void Location_LocationFound( string lat, string lon )
        {
            System.Diagnostics.Debug.WriteLine("Location received, lat="+lat+", lon="+lon);
        }

        private void Weather_WeatherReceived( List<WeatherData> forecast ) {
            System.Diagnostics.Debug.WriteLine("Weather received.");
        }

        private void Translation_Translated( String result )
        {
            MessageBox.Show(result);
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