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
            Preferences.write("albumart", 1);
        }

        private void albumart_Unchecked( object sender, RoutedEventArgs e )
        {
            Preferences.write("albumart", 0);
        }

        private void allowCellular_Checked( object sender, RoutedEventArgs e )
        {
            Preferences.write("allowCellular", 1);
        }

        private void allowCellular_Unchecked( object sender, RoutedEventArgs e )
        {
            Preferences.write("allowCellular", 0);
        }

        private void allowBattery_Unchecked( object sender, RoutedEventArgs e )
        {
            Preferences.write("allowBattery", 0);
        }

        private void allowBattery_Checked( object sender, RoutedEventArgs e )
        {
            Preferences.write("allowBattery", 1);
        } 
        #endregion

        private void PhoneApplicationPage_Loaded( object sender, RoutedEventArgs e )
        {
            albumart.IsChecked = Preferences.readBool("albumart");
            allowCellular.IsChecked = Preferences.readBool("allowCellular");
            allowBattery.IsChecked = Preferences.readBool("allowBattery");
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

        

        private void OnSignInButtonClicked( object sender, RoutedEventArgs e )
        {
            Button btn = sender as Button;
            if(btn.Tag.ToString().Equals("dropbox")) { this.getRequestToken(); }

        }

        const string ConsumerKey = "consumerkey";
        const string ConsumerSecret = "consumersecret";
        RequestToken requestToken;
        AccessToken accessToken;

        #region Dropbox
        private void getRequestToken()
        {
            if(Preferences.read("dropbox-access-token-key") != null)
            {
                System.Diagnostics.Debug.WriteLine("Access token exists");
                string key = Preferences.read("dropbox-access-token-key");
                string secret = Preferences.read("dropbox-access-token-secret");
                this.accessToken = new AccessToken(key, secret);
                sendFile();
            }
            else
            {
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
            }
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
                        this.accessToken = token.Token;
                        Preferences.write("dropbox-access-token-key", this.accessToken.Key.ToString());
                        Preferences.write("dropbox-access-token-secret", this.accessToken.Secret.ToString());
                        System.Diagnostics.Debug.WriteLine("Saved access token");
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