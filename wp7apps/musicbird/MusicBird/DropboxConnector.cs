using Codeplex.OAuth;
using Microsoft.Phone.Reactive;

namespace MusicBird
{
    public class DropboxConnector
    {
        public event RequestTokenReceivedEventHandler RequestTokenReceived;
        public event AccessTokenReceivedEventHandler AccessTokenReceived;
        
        private string consumerSecret;
        private string consumerKey;

        private string requestToken = "asdfg";
        private string requestTokenSecret = "asdfgh";

        private string accessToken;
        private string accessTokenSecret;
        
        public delegate void RequestTokenReceivedEventHandler( object sender, RequestTokenReceivedEventArgs e );
        public delegate void AccessTokenReceivedEventHandler( object sender, AccessTokenReceivedEventArgs e );

        public class AccessTokenReceivedEventArgs
        {
            public AccessTokenReceivedEventArgs( string accessToken, string accessTokenSecret ) {
                AccessToken = accessToken;
                AccessTokenSecret = accessTokenSecret;
            }
            public string AccessToken { get; private set; }
            public string AccessTokenSecret { get; private set; }
        }

        public class RequestTokenReceivedEventArgs
        {
            public RequestTokenReceivedEventArgs( string requestToken, string requestTokenSecret, string url)
            {
                this.RequestToken = requestToken;
                this.RequestTokenSecret = requestTokenSecret;
                this.Url = url;
            }
            public string RequestToken { get; private set; }
            public string RequestTokenSecret { get; private set; }
            public string Url { get; private set; }
        }

        public DropboxConnector(string consumerKey, string consumerSecret) {
            this.consumerKey = consumerKey;
            this.consumerSecret = consumerSecret;
        }

        protected virtual void OnAccessTokenReceived( AccessTokenReceivedEventArgs e )
        {
            if(AccessTokenReceived != null) AccessTokenReceived(this, e);
        }

        protected virtual void OnRequestTokenReceived( RequestTokenReceivedEventArgs e )
        {
            if(RequestTokenReceived != null) RequestTokenReceived(this, e);
        }

        public void getRequestToken()
        {
            System.Diagnostics.Debug.WriteLine("DB OAuth started");
            var authorizer = new OAuthAuthorizer(this.consumerKey, this.consumerSecret);
            System.Diagnostics.Debug.WriteLine("OAuth created");
            authorizer.GetRequestToken("https://api.dropbox.com/1/oauth/request_token")
                .ObserveOnDispatcher()
                .Subscribe(token =>
                {
                    System.Diagnostics.Debug.WriteLine("Got token.");
                    this.requestToken = token.Token.Key;
                    this.requestTokenSecret = token.Token.Secret;
                    var url = authorizer.BuildAuthorizeUrl("https://www.dropbox.com/1/oauth/authorize", token.Token);

                    url += "&oauth_callback=http://dummywebsite/dummy";

                    var e = new RequestTokenReceivedEventArgs(
                        this.requestToken,
                        this.requestTokenSecret,
                        url);
                    OnRequestTokenReceived(e);
                });
            
        }

        public void getAccessToken()
        {
            System.Diagnostics.Debug.WriteLine("Get access token started");
            var authorizer = new OAuthAuthorizer(consumerKey, consumerSecret);
            authorizer.GetAccessToken("https://api.dropbox.com/1/oauth/access_token", this.requestToken, this.requestTokenSecret)
                .ObserveOnDispatcher()
                .Subscribe(token =>
                {
                    this.accessToken = token.Token.Key;
                    this.accessTokenSecret = token.Token.Secret;
                    Preferences.write("dropbox-access-token-key", this.accessToken.ToString());
                    Preferences.write("dropbox-access-token-secret", this.accessTokenSecret.ToString());
                    
                    System.Diagnostics.Debug.WriteLine("Saved access token");
                    var e = new AccessTokenReceivedEventArgs(
                        this.accessToken,
                        this.accessTokenSecret);
                    OnAccessTokenReceived(e);
                });
        }

    }
}
