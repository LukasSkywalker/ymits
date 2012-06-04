using System;
using System.IO.IsolatedStorage;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Windows.Navigation;
using System.Net;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.ComponentModel;
using System.Windows.Controls;
using System.IO;
using System.Text;
using System.Threading;
using Codeplex.OAuth;
using Microsoft.Phone.Reactive;

namespace MusicBird
{
    public partial class Page1 : PhoneApplicationPage
    {

        private RequestToken RequestToken;
        private AccessToken accessToken;
        public Page1()
        {
            InitializeComponent();

        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) 
        { 
            base.OnNavigatedTo(e);

#if DROPBOX
            try
            {
                String action = NavigationContext.QueryString["action"];
                if(action.Equals("dropboxauth"))
                {
                    MessageBox.Show("Please log in to your Dropbox and try again", "Dropbox", MessageBoxButton.OK);
                    dropboxAuthButton_Click(null, null);
                }
            }
            catch(KeyNotFoundException ex) {
                //Nothing to do here
            }
            
#else

            //dropboxUpload.Visibility = Visibility.Collapsed;
            dropboxAuthButton.Visibility = Visibility.Collapsed;
            textBlock1.Visibility = Visibility.Collapsed;

#endif
            
        } 

        #region settings
        private void albumart_Checked( object sender, RoutedEventArgs e )
        {
            Helper.Preferences.write("albumart", true);
        }

        private void albumart_Unchecked( object sender, RoutedEventArgs e )
        {
            Helper.Preferences.write("albumart", false);
        }

        private void allowCellular_Checked( object sender, RoutedEventArgs e )
        {
            Helper.Preferences.write("allowCellular", true);
        }

        private void allowCellular_Unchecked( object sender, RoutedEventArgs e )
        {
            Helper.Preferences.write("allowCellular", false);
        }

        private void allowBattery_Unchecked( object sender, RoutedEventArgs e )
        {
            Helper.Preferences.write("allowBattery", false);
        }

        private void allowBattery_Checked( object sender, RoutedEventArgs e )
        {
            Helper.Preferences.write("allowBattery", true);
        }

        private void dropboxUpload_Checked( object sender, RoutedEventArgs e )
        {
            Helper.Preferences.write("dropboxUpload", true);
            if(Helper.Preferences.read("dropbox-access-token-key") == null)
            {
                dropboxAuthButton_Click(null, null);
            }
        }

        private void dropboxUpload_Unchecked( object sender, RoutedEventArgs e ) {
            Helper.Preferences.write("dropboxUpload", false);
        }

        #endregion

        private void PhoneApplicationPage_Loaded( object sender, RoutedEventArgs e )
        {
            albumart.IsChecked = Helper.Preferences.readBool("albumart");
            /*allowCellular.IsChecked = Helper.Preferences.readBool("allowCellular");
            allowBattery.IsChecked = Helper.Preferences.readBool("allowBattery");*/
            //dropboxUpload.IsChecked = Helper.Preferences.readBool("dropboxUpload");
        }

        private void playlistErrorButton_Click( object sender, RoutedEventArgs e )
        {
            using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(myIsolatedStorage.FileExists("Playlist.xml"))
                {
                    myIsolatedStorage.DeleteFile("Playlist.xml");
                }
            }
        }

        private void dropboxAuthButton_Click( object sender, RoutedEventArgs e )
        {
            this.getRequestToken();
        }

        #region Dropbox

        public void getRequestToken()
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
                        Helper.Preferences.write("dropbox-access-token-key", this.accessToken.Key.ToString());
                        Helper.Preferences.write("dropbox-access-token-secret", this.accessToken.Secret.ToString());
                        System.Diagnostics.Debug.WriteLine("Saved access token");
                    });
        }


        #endregion

        private void infoButton_Click( object sender, RoutedEventArgs e )
        {
            string copyrightMessage = (Application.Current as App).copyrightMessage;
            MessageBox.Show("MusicBird is a product by MonkeyTech. Visit http://lukasdiener.tk for more information. "+copyrightMessage,"About MusicBird",MessageBoxButton.OK);
        }

    }
}