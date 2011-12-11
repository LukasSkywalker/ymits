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
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;

namespace Sirius
{
    public partial class MainPage : PhoneApplicationPage
    {

        private string access_token = "";

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            Authentication auth = new Authentication();
            auth.BrowserLoaded += new Authentication.BrowserLoadedEventHandler(auth_BrowserLoaded);
            auth.TokenReceived += new Authentication.TokenReceivedEventHandler(auth_TokenReceived);

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
            updateToken();
            getFreeBusyTimes(new DateTime(2011,12,11,00,00,00), new DateTime(2011,12,12,0,0,0));
        }

        private void getFreeBusyTimes( DateTime start, DateTime end )
        {
            String startString = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
            String endString = end.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var client = new RestClient();
            client.BaseUrl = "https://www.googleapis.com";

            System.Diagnostics.Debug.WriteLine("Using auth key " + access_token);
            var request2 = new RestRequest("calendar/v3/freeBusy?access_token=" + access_token, Method.POST);
            String body = "{\"timeMin\":\""+startString+"\",\"timeMax\":\""+endString+"\",\"items\":[{\"id\":\"junglekiddy@gmail.com\"}]}";
            request2.AddParameter("application/json", body, ParameterType.RequestBody);

            client.ExecuteAsync(request2, ( response ) =>
            {
                string req = response.Content;
                MessageBox.Show(req);
                JObject o = JObject.Parse(req);
                String st = (string)o["calendars"]["junglekiddy@gmail.com"]["busy"][0]["start"];
                String en = (string)o["calendars"]["junglekiddy@gmail.com"]["busy"][0]["end"];
                MessageBox.Show(st+" "+en);
            });
        }

        private void updateToken() {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains("access_token"))
            {
                String token = "";
                if(settings.TryGetValue<String>("access_token", out token))
                {
                    access_token = token;
                }
            }
            else
            {
                MessageBox.Show("Error getting token");
            }
        }

        private void getCalendarList() {
            /*
             * GET to https://www.googleapis.com/calendar/v3/users/me/calendarList
             * maxResults=10
             * minAccessRole=freeBusyReader
             * showHidden=true
             */
        }

        private void getEvents(){
            /*
             * GET from https://www.googleapis.com/calendar/v3/calendars/junglekiddy@gmail.com/events
             * timeMax=2011-12-11T21:47:14.000Z
             * timeMin=2011-12-11T18:47:14.000Z
             */
        }
    }

    public class FreeBusyItem {
        public String startTime { get; set; }
        public String endTime { get; set; }
        public FreeBusyItem(String start, String end) {
            this.startTime = start;
            this.endTime = end;
        }
    }
}