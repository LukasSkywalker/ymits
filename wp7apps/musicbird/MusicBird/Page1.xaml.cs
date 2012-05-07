using System;
using System.IO.IsolatedStorage;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Windows.Navigation;
using System.Net;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using SkyDrive_Photos_Sample;
using System.ComponentModel;
using System.Windows.Controls;
using System.IO;
using OAuth;
using System.Text;
using System.Threading;

namespace MusicBird
{
    public enum Results { UserInfo, AlbumInfo };

    public partial class Page1 : PhoneApplicationPage
    {
        private static readonly string OAuthAuthorizeUri = "https://oauth.live.com/authorize";
        private static readonly string ApiServiceUri = "https://apis.live.net/v5.0/";
        private static readonly string ClientId = "000000004C0B2E84";
        private static readonly string RedirectUri = "https://oauth.live.com/desktop";
        private string accessToken;
        private string[] scopes = new string[] { "wl.basic", "wl.photos" };
        private string user = "me";
        private string albums = "me/albums";

        private static ManualResetEvent allDone = new ManualResetEvent(false);


        public Page1()
        {
            InitializeComponent();
        }

        #region settings
        private void albumart_Checked( object sender, RoutedEventArgs e )
        {
            save("albumart", true);
        }

        private void albumart_Unchecked( object sender, RoutedEventArgs e )
        {
            save("albumart", false);
        }

        private void allowCellular_Checked( object sender, RoutedEventArgs e )
        {
            save("allowCellular", true);
        }

        private void allowCellular_Unchecked( object sender, RoutedEventArgs e )
        {
            save("allowCellular", false);
        }

        private void allowBattery_Unchecked( object sender, RoutedEventArgs e )
        {
            save("allowBattery", false);
        }

        private void allowBattery_Checked( object sender, RoutedEventArgs e )
        {
            save("allowBattery", true);
        } 
        #endregion

        #region readwritePrefs
        public static void save( string name, Boolean value )
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains(name))
            {
                settings.Remove(name);
            }
            settings.Add(name, value);
            //settings.Save();
        }

        public static bool read( string name )
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            Boolean item;
            if(settings.Contains(name))
            {
                if(settings.TryGetValue<Boolean>(name, out item))
                {
                    return item;
                }
                else
                {
                    throw new IsolatedStorageException("could not read value of " + name + " from IsoStore. tryGetValue failed.");
                }
            }
            else
            {
                settings.Add(name, false);
                //settings.Save();
                return false;
            }
        } 
        #endregion

        private void PhoneApplicationPage_Loaded( object sender, RoutedEventArgs e )
        {
            albumart.IsChecked = read("albumart");
            allowCellular.IsChecked = read("allowCellular");
            allowBattery.IsChecked = read("allowBattery");
        }

        private void button1_Click( object sender, RoutedEventArgs e )
        {
            using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(myIsolatedStorage.FileExists("Playlist.xml"))
                {
                    myIsolatedStorage.DeleteFile("Playlist.xml");
                }
            }
        }

        protected override void OnBackKeyPress( CancelEventArgs e )
        {
            if(this.authorizationBrowser.Visibility == Visibility.Visible)
            {
                this.CompleteOAuthFlow(false);
                e.Cancel = true;
            }
            else
            {
                base.OnBackKeyPress(e);
            }
        }

        private void OnSignInButtonClicked( object sender, RoutedEventArgs e )
        {
            Button btn = sender as Button;
            if(btn.Tag.ToString().Equals("dropbox")) { this.getRequestToken(); }
            if(btn.Tag.ToString().Equals("skydrive")) { this.LaunchOAuthFlow(); }

        }

        #region Skydrive
        private void LaunchOAuthFlow()
        {
            this.loadingGrid.Visibility = Visibility.Visible;
            this.authorizationBrowser.Navigating += this.OnAuthorizationBrowserNavigating;
            this.authorizationBrowser.Navigated += this.OnAuthorizationBrowserNavigated;
            this.authorizationBrowser.Navigate(this.BuildOAuthUri(this.scopes));
        }

        private void CompleteOAuthFlow( bool success )
        {
            this.authorizationBrowser.Navigated -= this.OnAuthorizationBrowserNavigated;
            this.authorizationBrowser.Navigating -= this.OnAuthorizationBrowserNavigating;

            this.authorizationBrowser.NavigateToString(String.Empty);
            this.authorizationBrowser.Visibility = Visibility.Collapsed;

            if(success)
            {
                this.GetUserData();
                this.signInButton1.Visibility = Visibility.Collapsed;
            }
        }

        private void GetUserData()
        {
            this.loadingGrid.Visibility = Visibility.Visible;
            WebClient client = new WebClient();
            client.OpenReadCompleted += this.OnClientOpenReadComplete;
            client.OpenReadAsync(this.BuildApiUri(this.user), Results.UserInfo);

        }

        private void GetUserDataComplete( UserInfo info )
        {
            string output = String.Format(
                "First Name: {0}\nLast Name: {1}\nLink: {2}\n",
                info.FirstName,
                info.LastName,
                info.Link);
            System.Diagnostics.Debug.WriteLine(output);

            this.loadingGrid.Visibility = Visibility.Collapsed;
        }


        private void GetAlbumDataComplete( Albums albumList )
        {

            System.Diagnostics.Debug.WriteLine("\nSkyDrive Albums\n");
            foreach(AlbumInfo info in albumList.data)
            {
                string s = (info.Count == 1 ? String.Empty : "s");
                System.Diagnostics.Debug.WriteLine(String.Format("{0} ({1} photo{2})\n", info.Name, info.Count, s));
            }
        }

        private Uri BuildApiUri( string path )
        {
            UriBuilder builder = new UriBuilder(ApiServiceUri);
            builder.Path += path;
            builder.Query = "access_token=" + HttpUtility.UrlEncode(this.accessToken);
            return builder.Uri;
        }

        private Uri BuildOAuthUri( string[] scopes )
        {
            List<string> paramList = new List<string>();
            paramList.Add("client_id=" + HttpUtility.UrlEncode(ClientId));
            paramList.Add("scope=" + HttpUtility.UrlEncode(String.Join(" ", scopes)));
            paramList.Add("response_type=" + HttpUtility.UrlEncode("token"));
            paramList.Add("display=" + HttpUtility.UrlEncode("touch"));
            paramList.Add("redirect_uri=" + HttpUtility.UrlEncode(RedirectUri));

            UriBuilder authorizeUri = new UriBuilder(OAuthAuthorizeUri);
            authorizeUri.Query = String.Join("&", paramList.ToArray());
            return authorizeUri.Uri;
        }

        private Dictionary<string, string> ProcessFragments( string fragment )
        {
            Dictionary<string, string> processedFragments = new Dictionary<string, string>();

            if(fragment[0] == '#')
            {
                fragment = fragment.Substring(1);
            }

            string[] fragmentParams = fragment.Split('&');

            foreach(string fragmentParam in fragmentParams)
            {
                string[] keyValue = fragmentParam.Split('=');

                if(keyValue.Length == 2)
                {
                    processedFragments.Add(keyValue[0], HttpUtility.UrlDecode(keyValue[1]));
                }
            }

            return processedFragments;
        }

        private void client_UploadStringCompleted( object sender, UploadStringCompletedEventArgs e )
        {
            System.Diagnostics.Debug.WriteLine("DB OAuth result");
            System.Diagnostics.Debug.WriteLine(e.Result.ToString());
        }

        private void OnAuthorizationBrowserNavigated( object sender, NavigationEventArgs e )
        {
            this.authorizationBrowser.Navigated -= this.OnAuthorizationBrowserNavigated;
            this.loadingGrid.Visibility = Visibility.Collapsed;
            this.authorizationBrowser.Visibility = Visibility.Visible;
        }

        private void OnAuthorizationBrowserNavigating( object sender, NavigatingEventArgs e )
        {
            Uri uri = e.Uri;

            if(uri != null && uri.AbsoluteUri.StartsWith(RedirectUri))
            {
                Dictionary<string, string> fragments = this.ProcessFragments(uri.Fragment);

                bool success = fragments.TryGetValue("access_token", out this.accessToken);

                e.Cancel = true;
                this.CompleteOAuthFlow(success);
            }
        }

        private void OnClientOpenReadComplete( object sender, OpenReadCompletedEventArgs e )
        {
            DataContractJsonSerializer deserializer;

            if(e.UserState.Equals(Results.UserInfo))
            {
                deserializer = new DataContractJsonSerializer(typeof(UserInfo));
                UserInfo userInfo = (UserInfo)deserializer.ReadObject(e.Result);
                this.GetUserDataComplete(userInfo);

                WebClient client = new WebClient();
                client.OpenReadCompleted += this.OnClientOpenReadComplete;
                client.OpenReadAsync(this.BuildApiUri(this.albums), Results.AlbumInfo);
            }
            else if(e.UserState.Equals(Results.AlbumInfo))
            {
                deserializer = new DataContractJsonSerializer(typeof(Albums));
                Albums albumInfo = (Albums)deserializer.ReadObject(e.Result);
                this.GetAlbumDataComplete(albumInfo);
            }
        } 
        #endregion


        #region Dropbox
        private void getRequestToken()
        {
            System.Diagnostics.Debug.WriteLine("DB OAuth started");
            string requestTokenUrl = DropboxAuth.buildRequestTokenUri();

            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += getRequestTokenCompleted;
            wc.DownloadStringAsync(new Uri(requestTokenUrl));
        }

        private void getRequestTokenCompleted( Object sender, DownloadStringCompletedEventArgs e )
        {
            DropboxAuth.storeRequestToken(e.Result);
            string authorizeUrl = DropboxAuth.buildAuthorizeUri();

            this.loadingGrid.Visibility = Visibility.Visible;
            this.authorizationBrowser.Navigating += this.OnDBAuthorizationBrowserNavigating;
            this.authorizationBrowser.Navigated += this.OnDBAuthorizationBrowserNavigated;
            this.authorizationBrowser.Navigate(new Uri(authorizeUrl));
        }

        private void OnDBAuthorizationBrowserNavigated( object sender, NavigationEventArgs e )
        {
            this.authorizationBrowser.Navigated -= this.OnDBAuthorizationBrowserNavigated;
            this.loadingGrid.Visibility = Visibility.Collapsed;
            this.authorizationBrowser.Visibility = Visibility.Visible;
        }

        private void OnDBAuthorizationBrowserNavigating( object sender, NavigatingEventArgs e )
        {
            Uri uri = e.Uri;

            if(uri != null && uri.AbsoluteUri.StartsWith("http://www.lukasdiener.tk"))
            {
                string url = uri.ToString();
                DropboxAuth.storeAuthorization(url);

                this.authorizationBrowser.Navigated -= this.OnAuthorizationBrowserNavigated;
                this.authorizationBrowser.Navigating -= this.OnAuthorizationBrowserNavigating;

                this.authorizationBrowser.NavigateToString(String.Empty);
                this.authorizationBrowser.Visibility = Visibility.Collapsed;

                getAccessToken();
            }
        }

        private void getAccessToken()
        {
            string accessTokenUrl = DropboxAuth.buildAccessTokenUri();
            string accessTokenBody = DropboxAuth.buildAccessTokenBody();

            
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(getAccessTokenReceived);
            //wc.UploadStringCompleted += new UploadStringCompletedEventHandler();

            System.Diagnostics.Debug.WriteLine(accessTokenUrl);
            System.Diagnostics.Debug.WriteLine(accessTokenBody);

            wc.DownloadStringAsync(new Uri(accessTokenUrl+"?"+accessTokenBody));
            //wc.UploadStringAsync(new Uri(accessTokenUrl), "POST", accessTokenBody);
        }

        private void getAccessTokenReceived( object sender, DownloadStringCompletedEventArgs e ) {
            System.Diagnostics.Debug.WriteLine(e.Result);
        }

        #endregion

    }
}