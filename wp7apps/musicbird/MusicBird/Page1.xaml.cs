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
using RestSharp;
using Codeplex.OAuth;
using Microsoft.Phone.Reactive;

namespace MusicBird
{
    public enum Results { UserInfo, AlbumInfo };

    public partial class Page1 : PhoneApplicationPage
    {

        private RequestToken RequestToken;
        private static ManualResetEvent allDone = new ManualResetEvent(false);

        private static readonly string RedirectUri = "https://oauth.live.com/desktop";
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

        public static void delete( string name )
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains(name))
            {
                settings.Remove(name);
            }
        }

        public static void save( string name, string value )
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains(name))
            {
                settings.Remove(name);
            }
            settings.Add(name, value);
        }

        public static object read( string name )
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            object item;
            if(settings.Contains(name))
            {
                if(settings.TryGetValue<object>(name, out item))
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
                settings.Add(name, null);
                //settings.Save();
                return null;
            }
        }

        #endregion

        private void PhoneApplicationPage_Loaded( object sender, RoutedEventArgs e )
        {
            albumart.IsChecked = (bool)read("albumart");
            allowCellular.IsChecked = (bool)read("allowCellular");
            allowBattery.IsChecked = (bool)read("allowBattery");
        }

        private void button1_Click( object sender, RoutedEventArgs e )
        {
            using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(myIsolatedStorage.FileExists("Playlist.xml"))
                {
                    myIsolatedStorage.DeleteFile("Playlist.xml");
                }
                delete("skydrive-access-token");
                delete("dropbox-access-token");
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
            if((string)read("skydrive-access-token") != null){
                MessageBox.Show("already authenticated :-)");
                MessageBox.Show("Token is "+(string)read("skydrive-access-token"));
            }else{
                this.loadingGrid.Visibility = Visibility.Visible;
                this.authorizationBrowser.Navigating += this.OnAuthorizationBrowserNavigating;
                this.authorizationBrowser.Navigated += this.OnAuthorizationBrowserNavigated;
                this.authorizationBrowser.Navigate(SkydriveAuth.BuildOAuthUri());
            }
        }

        private void CompleteOAuthFlow( bool success )
        {
            this.authorizationBrowser.Navigated -= this.OnAuthorizationBrowserNavigated;
            this.authorizationBrowser.Navigating -= this.OnAuthorizationBrowserNavigating;

            this.authorizationBrowser.NavigateToString(String.Empty);
            this.authorizationBrowser.Visibility = Visibility.Collapsed;

            if(success)
            {
                this.UploadImage();
                this.GetUserData();
                this.signInButton1.Visibility = Visibility.Collapsed;
            }
        }

        private void GetUserData()
        {
            this.loadingGrid.Visibility = Visibility.Visible;
            WebClient client = new WebClient();
            client.OpenReadCompleted += this.OnClientOpenReadComplete;
            client.OpenReadAsync(SkydriveAuth.BuildApiUri(), Results.UserInfo);

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
        
            /*System.Diagnostics.Debug.WriteLine("\nSkyDrive Albums\n");
            foreach(AlbumInfo info in albumList.data)
            {
                string s = (info.Count == 1 ? String.Empty : "s");
                System.Diagnostics.Debug.WriteLine(String.Format("{0} ({1} photo{2})\n", info.Name, info.Count, s));
            }*/
        }

        private void UploadImage() {
            System.Diagnostics.Debug.WriteLine("accesstoken: "+SkydriveAuth.aToken());
            String url = "https://apis.live.net/v5.0/me/skydrive/files/HelloWorld.txt?access_token=" + SkydriveAuth.aToken();
            var web = new WebClient();
            web.UploadStringCompleted += ( s, e ) =>
            {
                System.Diagnostics.Debug.WriteLine(e.Error);
                string res = e.Result.ToString();
                System.Diagnostics.Debug.WriteLine("###" + res);
            };

            web.Headers["Content-type"] = "text/plain";
            web.Encoding = Encoding.UTF8;
            string xml = "asldjkfghlasidhgfhasdjfkl";
            web.UploadStringAsync(new Uri(url), "PUT", xml);    
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
                bool success = SkydriveAuth.getFragments(uri.Fragment);

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
                client.OpenReadAsync(SkydriveAuth.BuildApiUri(), Results.AlbumInfo);
            }
            else if(e.UserState.Equals(Results.AlbumInfo))
            {
                deserializer = new DataContractJsonSerializer(typeof(Albums));
                Albums albumInfo = (Albums)deserializer.ReadObject(e.Result);
                this.GetAlbumDataComplete(albumInfo);
            }
        } 
        #endregion

        const string ConsumerKey = "consumerkey";
        const string ConsumerSecret = "consumersecret";
        RequestToken requestToken;
        AccessToken accessToken;

        #region Dropbox
        private void getRequestToken()
        {
            /*if((string)Page1.read("dropbox-access-token") != "")
            {

            }
            else
            {*/
                System.Diagnostics.Debug.WriteLine("DB OAuth started");
                var authorizer = new OAuthAuthorizer(DropboxAuth.consumerKey, DropboxAuth.consumerSecret);
                authorizer.GetRequestToken("https://api.dropbox.com/1/oauth/request_token")
                    .ObserveOnDispatcher()
                    .Subscribe(token =>
                    {
                        this.RequestToken = token.Token;
                        var url = authorizer.BuildAuthorizeUrl("https://www.dropbox.com/1/oauth/authorize", token.Token);
       
                        url += "&oauth_callback=http://dummywebsite/dummy";

                        this.loadingGrid.Visibility = Visibility.Visible;
                        this.authorizationBrowser.Navigating += this.OnDBAuthorizationBrowserNavigating;
                        this.authorizationBrowser.Navigated += this.OnDBAuthorizationBrowserNavigated;
                        this.authorizationBrowser.Navigate(new Uri(url));
              });

            //}
        }

        private void OnDBAuthorizationBrowserNavigated( object sender, NavigationEventArgs e )
        {
            this.authorizationBrowser.Navigated -= this.OnDBAuthorizationBrowserNavigated;
            this.loadingGrid.Visibility = Visibility.Collapsed;
            this.authorizationBrowser.Visibility = Visibility.Visible;
        }

        private void OnDBAuthorizationBrowserNavigating( object sender, NavigatingEventArgs e )
        {
            if(e.Uri.AbsolutePath == "/dummy")
            {
                // Authorization done, cancel the navigation, hide the browser control, and proceed.
                e.Cancel = true;
                this.authorizationBrowser.Visibility = Visibility.Collapsed;
                this.getAccessToken();
            }
            else{
                this.authorizationBrowser.Visibility = Visibility.Visible;
            }
        }

        private void getAccessToken()
        {
            var authorizer = new OAuthAuthorizer(DropboxAuth.consumerKey, DropboxAuth.consumerSecret);
            authorizer.GetAccessToken("https://api.dropbox.com/1/oauth/access_token", this.RequestToken, this.RequestToken.Secret)
                .ObserveOnDispatcher()
                .Subscribe(token =>
                    {
                        //this.AccessToken = token.Token;
                        //this.SendFile();
                        this.accessToken = token.Token;
                        this.sendFile();
                    });
        }

        private void sendFile()
        {
            var client = new OAuthClient(DropboxAuth.consumerKey, DropboxAuth.consumerSecret, this.accessToken);
            client.Url = "https://api-content.dropbox.com/1/files_put/sandbox/test.txt";
            client.Parameters.Add("overwrite", "true");
            client.MethodType = MethodType.Put;
            var webRequest = client.CreateWebRequest();
            webRequest.BeginGetRequestStream(this.StartUpload, webRequest);
        }

        private void StartUpload( IAsyncResult asyncResult )
        {
            var request = (HttpWebRequest)asyncResult.AsyncState;
            var postStream = request.EndGetRequestStream(asyncResult);
            using(var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using(var stream = isolatedStorage.OpenFile("Playlist.xml", FileMode.Open))
                {
                    stream.CopyTo(postStream);
                    postStream.Close();
                }
            }
            request.BeginGetResponse(this.EndUpload, request);        
        }

        private void EndUpload( IAsyncResult asyncResult )
        {
            var request = (HttpWebRequest)asyncResult.AsyncState;
            try{
                var response = (HttpWebResponse)request.EndGetResponse(asyncResult);
                response.Dispose();
                this.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Your file has been sucessfully uploaded to Dropbox!");
                });
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvoke(() => MessageBox.Show("An error occured: " + ex.Message));
            }
        }

        #endregion

    }
}