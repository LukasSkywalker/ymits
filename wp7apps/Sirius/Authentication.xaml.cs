using System;
using System.Text.RegularExpressions;
using System.IO.IsolatedStorage;
using System.Windows;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Windows.Threading;

namespace Sirius
{
    public partial class Authentication : PhoneApplicationPage
    {
        public delegate void TokenReceivedEventHandler( string auth_key );
        public event TokenReceivedEventHandler TokenReceived;

        public delegate void BrowserLoadedEventHandler( string bla );
        public event BrowserLoadedEventHandler BrowserLoaded;

        private DispatcherTimer authTimer;
        private DispatcherTimer refreshTimer;

        private static string clientID = "1023800741200.apps.googleusercontent.com";
        private static string scope = "https://www.googleapis.com/auth/calendar";
        private static string redirect = "urn:ietf:wg:oauth:2.0:oob";
        private static string clientSecret = "IOtEdLN4_6YZul02jsVOmNCb";

        private string access_token = "";
        private int expires_in = 0;
        private string token_type = "";
        private string refresh_token = "";

        public Authentication()
        {
            InitializeComponent();
            authTimer = new DispatcherTimer();
            authTimer.Interval = new TimeSpan(0, 0, 1);
            authTimer.Tick += new EventHandler(loadConfirmation);
            authTimer.Start();
        }

        public void loadConfirmation( object sender, EventArgs e )
        {
            authTimer.Stop();
            webBrowser1.Source = new Uri("https://accounts.google.com/o/oauth2/auth?client_id=" + clientID + "&redirect_uri=" + redirect + "&scope=" + scope + "&response_type=code", UriKind.Absolute);
            if(this.BrowserLoaded != null)
            {
                this.BrowserLoaded("asdf");
            }
            
        }

        private void webbrowser_Navigated( object sender, System.Windows.Navigation.NavigationEventArgs e )
        {
            if(e.Uri.ToString().IndexOf("https://accounts.google.com/o/oauth2/approval") > -1)
            {
                var html = e.Content;
                WebBrowser wb = sender as WebBrowser;
                string body = wb.SaveToString();
                Regex pattern = new Regex("<title>Success code=(.*?)</title>", RegexOptions.IgnoreCase);
                Match m = pattern.Match(body);
                string code = "";
                while(m.Success)
                {
                    Group g = m.Groups[1];
                    code = g.ToString();
                    m = m.NextMatch();
                }


                var client = new RestClient();
                client.BaseUrl = "https://accounts.google.com";

                var request = new RestRequest("o/oauth2/token", Method.POST);
                request.AddParameter("code", code, ParameterType.GetOrPost);
                request.AddParameter("client_id", clientID, ParameterType.GetOrPost);
                request.AddParameter("client_secret", clientSecret, ParameterType.GetOrPost);
                request.AddParameter("redirect_uri", redirect, ParameterType.GetOrPost);
                request.AddParameter("grant_type", "authorization_code", ParameterType.GetOrPost);
                /*
                 * POST to https://accounts.google.com/o/oauth2/token
                 * 'code': auth_code,
                 * 'client_id': client_id,
                 * 'client_secret': client_secret,
                 * 'redirect_uri': redirect_uri,
                 * 'grant_type': 'authorization_code'
                */

                System.Diagnostics.Debug.WriteLine("Loading key...");

                client.ExecuteAsync(request, ( response ) =>
                {
                    string auth = response.Content;

                    JObject o = JObject.Parse(auth);
                    access_token = (string)o["access_token"];
                    expires_in = (int)o["expires_in"];
                    token_type = (string)o["token_type"];
                    refresh_token = (string)o["refresh_token"];

                    refreshTimer = new DispatcherTimer();
                    refreshTimer.Interval = new TimeSpan(0, 0, expires_in - 30);
                    System.Diagnostics.Debug.WriteLine("Refreshing token in " + (expires_in - 30) + " seconds.");
                    refreshTimer.Tick +=new EventHandler(refreshToken);
                    refreshTimer.Start();

                    System.Diagnostics.Debug.WriteLine("Key loaded, key is "+access_token+".");

                    var settings = IsolatedStorageSettings.ApplicationSettings;
                    if(settings.Contains("access_token"))
                    {
                        settings.Remove("access_token");
                    }
                    settings.Add("access_token", access_token);

                    NavigationService.GoBack();// .Navigate(new Uri("/MainPage.xaml",UriKind.Relative));

                    if(this.TokenReceived != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Event sent");
                        this.TokenReceived(access_token);
                    }
                });
            }
        }

        private void refreshToken(object sender, EventArgs e) {
            /*
             * POST to https://accounts.google.com/o/oauth2/token with
             * client_id=8819981768.apps.googleusercontent.com&
             * client_secret={client_secret}&
             * refresh_token=1/6BMfW9j53gdGImsiyUH5kU5RsR4zwI9lUVX-tqf8JXQ&
             * grant_type=refresh_token
             */

            var client = new RestClient();
            client.BaseUrl = "https://accounts.google.com";

            var request = new RestRequest("o/oauth2/token", Method.POST);
            request.AddParameter("client_id", clientID, ParameterType.GetOrPost);
            request.AddParameter("client_secret", clientSecret, ParameterType.GetOrPost);
            request.AddParameter("refresh_token", refresh_token, ParameterType.GetOrPost);
            request.AddParameter("grant_type", "refresh_token", ParameterType.GetOrPost);

            client.ExecuteAsync(request, ( response ) =>
            {
                string auth = response.Content;
                JObject o = JObject.Parse(auth);
                access_token = (string)o["access_token"];
                expires_in = (int)o["expires_in"];
                token_type = (string)o["token_type"];
            });
        }
    }
}