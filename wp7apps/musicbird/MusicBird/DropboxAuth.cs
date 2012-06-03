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
using System.Text;
using Microsoft.Phone.Reactive;

namespace MusicBird
{
    public class DropboxAuth
    {
        public static readonly string requestTokenUri = "https://api.dropbox.com/1/oauth/request_token";
        public static readonly string authorizeUri = "https://www.dropbox.com/1/oauth/authorize";
        public static readonly string accessTokenUri = "https://api.dropbox.com/1/oauth/access_token";
        public static readonly string consumerKey = "b8eahgi7u8ziyah";
        public static readonly string consumerSecret = "3r7s2wpnd1jesfz";
        private static string requestToken;
        private static string requestTokenSecret;
        

        public static string buildAuthorizeUri() {
            string url = authorizeUri;
            url += "?oauth_token=" + DropboxAuth.requestToken + "&oauth_callback=http%3A%2F%2Fwww.lukasdiener.tk&locale=en-US";
            return url;
        }

        public static string buildAccessTokenUri()
        {
            string url = accessTokenUri;
            return url;
        }


        private static string generateRandomString( int length )
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

        public static void storeRequestToken( string p )
        {
            DropboxAuth.requestTokenSecret = p.Substring(19, 15);
            DropboxAuth.requestToken = p.Substring(47, 15);

        }

        public static void storeAuthorization( string url )
        {
            int uidStart = url.IndexOf("uid=") + 4;
            int uidEnd = url.IndexOf("&oauth_token=");
            int tokenStart = uidEnd + 13;

            string uid = url.Substring(uidStart, uidEnd - uidStart);
            string token = url.Substring(tokenStart);

            //System.Diagnostics.Debug.WriteLine("UID: " + uid + " for token:" + token);
        }
    }
}
