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


            var client = new RestClient();
            client.BaseUrl = "https://www.googleapis.com";

            System.Diagnostics.Debug.WriteLine("Using auth key " + access_token);
            var request2 = new RestRequest("calendar/v3/freeBusy?access_token=" + access_token, Method.POST);
            String body = "{\"timeMin\":\"2011-12-11T00:00:00Z\",\"timeMax\":\"2011-12-12T00:00:00Z\",\"items\":[{\"id\":\"junglekiddy@gmail.com\"}]}";
            request2.AddParameter("application/json", body, ParameterType.RequestBody);

            client.ExecuteAsync(request2, ( response ) =>
            {
                string req = response.Content;
                MessageBox.Show(req);
                JObject o = JObject.Parse(req);
                String kind = (string)o["kind"];
                MessageBox.Show(kind);
            });

        }
    }
    
    public class FreeBusy {
        public string timeMin { get; set; }
        public string timeMax { get; set; }
        public Id[] items { get; set; }
        public FreeBusy( string min, string max ) {
            timeMax = max;
            timeMin = min;
            items = new Id[] { new Id("primary") };
        }
    }

    public class Id{
        string id { get; set; }
        public Id(string bla){
            this.id = bla;
        }
    }
}