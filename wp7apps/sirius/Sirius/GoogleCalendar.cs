using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Sirius
{
    public static class GoogleCalendar
    {
        public static string access_token = "";
        public static bool authenticated = false;

        static GoogleCalendar(){
            var settings = IsolatedStorageSettings.ApplicationSettings;
            String token = "";
            if(settings.TryGetValue("access_token",out token))
            {
                access_token = token;
            }
        }

        private static void getFreeBusyTimes( DateTime start, DateTime end )
        {
            if(!isAuthenticated())
            {
                return;
            }
            String startString = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
            String endString = end.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var client = new RestClient();
            client.BaseUrl = "https://www.googleapis.com";

            System.Diagnostics.Debug.WriteLine("Using auth key " + access_token);
            var request2 = new RestRequest("calendar/v3/freeBusy?access_token=" + access_token, Method.POST);
            String body = "{\"timeMin\":\"" + startString + "\",\"timeMax\":\"" + endString + "\",\"items\":[{\"id\":\"junglekiddy@gmail.com\"}]}";
            request2.AddParameter("application/json", body, ParameterType.RequestBody);

            client.ExecuteAsync(request2, ( response ) =>
            {
                string req = response.Content;
                MessageBox.Show(req);
                JObject o = JObject.Parse(req);
                String st = (string)o["calendars"]["junglekiddy@gmail.com"]["busy"][0]["start"];
                String en = (string)o["calendars"]["junglekiddy@gmail.com"]["busy"][0]["end"];
                MessageBox.Show(st + " " + en);
            });
        }

        private static void updateToken()
        {
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

        private static void getCalendarList()
        {
            /*
             * GET to https://www.googleapis.com/calendar/v3/users/me/calendarList
             * maxResults=10
             * minAccessRole=freeBusyReader
             * showHidden=true
             */
            if(!isAuthenticated()) {
                return;
            }

            var client = new RestClient();
            client.BaseUrl = "https://www.googleapis.com";

            var request = new RestRequest("calendar/v3/users/me/calendarList?access_token=" + access_token, Method.POST);
            toJSON<RestRequest>(request);
            CalendarListRequest2 bla = new CalendarListRequest2(10, "freeBusyReader", true, new Id[] { new Id("junglekiddy@gmail.com") });
            MessageBox.Show(bla.toJSON());
            String body = "{\"maxResults\"=\"10\",\"minAccessRole\":\"freeBusyReader\",\"showHidden\":\"true\",\"items\":[{\"id\":\"junglekiddy@gmail.com\"}]}";
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            client.ExecuteAsync(request, ( response ) =>
            {
                string req = response.Content;
                MessageBox.Show(req);
                JObject o = JObject.Parse(req);
                String st = (string)o["calendars"]["junglekiddy@gmail.com"]["busy"][0]["start"];
                String en = (string)o["calendars"]["junglekiddy@gmail.com"]["busy"][0]["end"];
                MessageBox.Show(st + " " + en);
            });
        }

        private static void getEvents()
        {
            /*
             * GET from https://www.googleapis.com/calendar/v3/calendars/junglekiddy@gmail.com/events
             * timeMax=2011-12-11T21:47:14.000Z
             * timeMin=2011-12-11T18:47:14.000Z
             */
            if(!isAuthenticated())
            {
                return;
            }
        }

        public static string toJSON<T>( T obj )
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            using(MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                return Encoding.Unicode.GetString(ms.ToArray(), 0, 100);
                //return Encoding.Unicode.GetString(ms.ToArray());
            }
        }

        private static bool isAuthenticated()
        {
            if(!authenticated) {
                Authentication auth = new Authentication();
                auth.BrowserLoaded += new Authentication.BrowserLoadedEventHandler(onBrowserLoaded);
                auth.TokenReceived += new Authentication.TokenReceivedEventHandler(onTokenReceived);
                return false;
            }
            return true;
        }

        private static void onBrowserLoaded( string bla )
        {
            //NavigationService.Navigate(new Uri("/Authentication.xaml", UriKind.Relative));
        }

        private static void onTokenReceived( string bla )
        { 
            
        }
    }
}
