using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using OAuth;
using System.Text;
using Microsoft.Phone.Reactive;

namespace MusicBird
{
    public abstract class CloudAuth
    {

        protected string requestTokenUri;
        protected string authorizeUri;
        protected string accessTokenUri;
        protected string consumerKey;
        protected string consumerSecret;
        protected string requestToken;
        protected string requestTokenSecret;
        protected string accessToken;
        protected string accessTokenSecret;

        public string buildRequestTokenUri()
        {
            //Step 1: get request token
            OAuthBase oAuth = new OAuthBase();
            string nonce = oAuth.GenerateNonce();
            string timeStamp = oAuth.GenerateTimeStamp();
            string signature = generateSig(consumerSecret, String.Empty);

            StringBuilder requestUri = new StringBuilder(requestTokenUri);
            requestUri.AppendFormat("?oauth_consumer_key={0}&", consumerKey);
            requestUri.AppendFormat("oauth_nonce={0}&", nonce);
            requestUri.AppendFormat("oauth_timestamp={0}&", timeStamp);
            requestUri.AppendFormat("oauth_signature_method={0}&", "PLAINTEXT");
            requestUri.AppendFormat("oauth_version={0}&", "1.0");
            requestUri.AppendFormat("oauth_signature={0}", signature);

            string auth_url = requestUri.ToString();
            return auth_url;
        }

        public string buildAuthorizeUri()
        {
            string url = this.authorizeUri;
            url += "?oauth_token=" + this.requestToken + "&oauth_callback=http%3A%2F%2Fwww.lukasdiener.tk&locale=en-US";
            return url;
        }

        public string buildAccessTokenUri()
        {
            string url = this.accessTokenUri;
            return url;
        }

        public string buildAccessTokenBody()
        {
            OAuthBase oAuth = new OAuthBase();
            string nonce = oAuth.GenerateNonce();
            string timeStamp = oAuth.GenerateTimeStamp();
            string signature = generateSig(consumerSecret, requestToken);

            StringBuilder requestUri = new StringBuilder("");
            requestUri.AppendFormat("oauth_consumer_key={0}&", consumerKey);
            requestUri.AppendFormat("oauth_nonce={0}&", nonce);
            requestUri.AppendFormat("oauth_token={0}&", requestToken);
            requestUri.AppendFormat("oauth_token_secret={0}&", requestTokenSecret);
            requestUri.AppendFormat("oauth_timestamp={0}&", timeStamp);
            requestUri.AppendFormat("oauth_signature_method={0}&", "PLAINTEXT");
            requestUri.AppendFormat("oauth_version={0}&", "1.0");
            requestUri.AppendFormat("oauth_signature={0}", signature);

            string auth_url = requestUri.ToString();
            return auth_url;
        }

        private string generateRandomString( int length )
        {
            Random rand = new Random();
            Char[] allowableChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLOMNOPQRSTUVWXYZ0123456789".ToCharArray();
            string final = "";
            for(int i = 0 ; i < length ; i++)
            {
                final += allowableChars[rand.Next(allowableChars.Length - 1)];
            }
            return final;
        }

        private static string generateSig( string DBSecretKey, string DBsecretToken )
        {
            return HttpUtility.UrlEncode(string.Format("{0}&{1}", DBSecretKey, DBsecretToken));
        }

        public void storeRequestToken( string p )
        {
            requestTokenSecret = p.Substring(19, 15);
            requestToken = p.Substring(47, 15);

        }

        public void storeAuthorization( string url )
        {
            int uidStart = url.IndexOf("uid=") + 4;
            int uidEnd = url.IndexOf("&oauth_token=");
            int tokenStart = uidEnd + 13;

            string uid = url.Substring(uidStart, uidEnd - uidStart);
            string token = url.Substring(tokenStart);
        }
    }
}
