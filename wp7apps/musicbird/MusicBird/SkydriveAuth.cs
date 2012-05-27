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
using System.Collections.Generic;

namespace MusicBird
{
    public static class SkydriveAuth
    {
        private static readonly string OAuthAuthorizeUri = "https://oauth.live.com/authorize";
        private static readonly string ApiServiceUri = "https://apis.live.net/v5.0/";
        private static readonly string ClientId = "000000004C0B2E84";
        private static string accessToken;
        private static string[] scopes = new string[] { "wl.basic", "wl.photos", "wl.skydrive_update" };
        private static string user = "me";
        private static string albums = "me/albums";
        private static string upload = "me/skydrive/files";
        private static readonly string RedirectUri = "https://oauth.live.com/desktop";

        public static Uri BuildOAuthUri()
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

        public static string aToken() {
            return HttpUtility.UrlEncode(accessToken);
        }

        public static Uri BuildApiUri()
        {
            string path = user;
            UriBuilder builder = new UriBuilder(ApiServiceUri);
            builder.Path += path;
            builder.Query = "access_token=" + HttpUtility.UrlEncode(accessToken);
            return builder.Uri;
        }

        public static Dictionary<string, string> ProcessFragments( string fragment )
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

        public static bool getFragments(string fragment){
            Dictionary<string, string> fragments = SkydriveAuth.ProcessFragments(fragment);
            bool success = fragments.TryGetValue("access_token", out accessToken);
            Page1.save("skydrive-access-token", accessToken);
            return success;
        }
    }
}
