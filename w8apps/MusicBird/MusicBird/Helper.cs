using MusicBird.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Xaml;

namespace MusicBird
{
    class Helper
    {
        public static string GetHresultFromErrorMessage(ExceptionRoutedEventArgs e)
        {
            String hr = String.Empty;
            String token = "HRESULT - ";
            const int hrLength = 10;     // eg "0xFFFFFFFF"

            int tokenPos = e.ErrorMessage.IndexOf(token, StringComparison.Ordinal);
            if (tokenPos != -1)
            {
                hr = e.ErrorMessage.Substring(tokenPos + token.Length, hrLength);
            }
            return hr;
        }

        public static double GetSliderFrequency(TimeSpan timevalue)
        {
            double stepfrequency = -1;

            double absvalue = (int)Math.Round(
                timevalue.TotalSeconds, MidpointRounding.AwayFromZero);

            stepfrequency = (int)(Math.Round(absvalue / 100));

            if (timevalue.TotalMinutes >= 10 && timevalue.TotalMinutes < 30)
            {
                stepfrequency = 10;
            }
            else if (timevalue.TotalMinutes >= 30 && timevalue.TotalMinutes < 60)
            {
                stepfrequency = 30;
            }
            else if (timevalue.TotalHours >= 1)
            {
                stepfrequency = 60;
            }

            if (stepfrequency == 0) stepfrequency += 1;

            if (stepfrequency == 1)
            {
                stepfrequency = absvalue / 100;
            }

            return stepfrequency;
        }

        public static Style GetStyle(string key)
        {
            return Application.Current.Resources[key] as Style;
        }

        private static int LevelCounter = 0;

        public static void DumpException(Exception ex)
        {
            WriteExceptionInfo(ex);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                WriteExceptionInfo(ex);
            }
            LevelCounter = 0;
        }

        private static void WriteExceptionInfo(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(LevelCounter + ": " + ex.Message + " " + ex.HResult);

            //at WolframAlpha.Helper.ParseResult(String src) in e:[...]\Helper.cs:line 57
            Regex pattern = new Regex("in (.*?)\\.cs\\:line (.*?)$", RegexOptions.IgnoreCase);
            Match m = pattern.Match(ex.StackTrace);
            bool found = false;
            while (m.Success)
            {
                found = true;
                Group g = m.Groups[1];
                Group h = m.Groups[2];
                String file = g.ToString();
                String filename = file.Substring(file.LastIndexOf("\\") + 1);
                String line = h.ToString();
                System.Diagnostics.Debug.WriteLine(filename + ".cs : line " + line);
                m = m.NextMatch();
            }

            if (!found) System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            LevelCounter++;
        }

        public static async Task<List<Track>> GetResult(String searchterm) {
            HttpClient searchClient = new HttpClient();
            searchClient.MaxResponseContentBufferSize = 256000;
            searchClient.DefaultRequestHeaders.Add("User-Agent", App.USER_AGENT_FIREFOX);

            List<Track> trackList = new List<Track>();

            try
            {
                string address = "http://mp3skull.com/mp3/" + searchterm.Replace(" ", "_") + ".html";
                searchClient.CancelPendingRequests();
                HttpResponseMessage response = await searchClient.GetAsync(address);
                response.EnsureSuccessStatusCode();
                String responseText = await response.Content.ReadAsStringAsync();

                trackList = Helper.ParseResult(searchterm, responseText);
                trackList.Sort((a, b) => b.Match.CompareTo(a.Match));
            }
            catch (Exception) { }
            System.Diagnostics.Debug.WriteLine("Results: "+trackList.Count);
            return trackList;
        }

        public static List<Track> ParseResult(String searchterm, String text){
            List<Track> trackList = new List<Track>();
            
            Regex pattern = new Regex("<a href=\"(.*?.mp3)\" rel=\"nofollow\"", RegexOptions.IgnoreCase);
            Regex pattern2 = new Regex("<div style=\"font-size:15px;\"><b>(.*?) mp3</b></div>", RegexOptions.IgnoreCase);
            try
            {
                String s = text;

                // Match the regular expression pattern against a text string.
                Match m = pattern.Match(s);
                Match n = pattern2.Match(s);

                while (m.Success)
                {
                    Group g = m.Groups[1];
                    Group h = n.Groups[1];
                    string name = h.ToString();
                    string artist = name;
                    string title = name;
                    string url = g.ToString();
                    string[] data = StringHelper.getArtistAndTitle(name);
                    // 4shared lets users download again (2012-09-20)
                    //if (url.IndexOf("4shared") == -1)
                    //{
                    int match1 = getDistance(searchterm, data[0] + " " + data[1]);
                    int match2 = getDistance(searchterm, data[1] + " " + data[0]);
                    int match = Math.Min(match1, match2);
                    Track item = new Track(data[0], data[1], url, match);
                    int insertPos = 0;
                    for (int i = 0; i < trackList.Count; i++)
                    {
                        if (trackList[i].Match < match)
                        {
                            insertPos = i;
                            break;
                        }
                    }
                    trackList.Insert(insertPos, item);
                    //}
                    m = m.NextMatch();
                    n = n.NextMatch();
                }
                System.Diagnostics.Debug.WriteLine("mp3skull: Results found: " + trackList.Count);
            }
            finally
            {
                // SEARCH CONTRACT 2.2 Populate your page with results from your app's data
            }
            return trackList;
        }

        private static int getDistance(string searchterm, string p)
        {
            double distance = Distance.compareStrings(searchterm, p);
            int dist = (int)Math.Round(distance * 100);
            return dist;
        }

        public async static Task<HttpResponseMessage> GetHead(String url)
        {
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, new Uri(url));

            return await httpClient.SendAsync(request);
        }

        public static async Task<String> GetCoverUrl(String searchterm)
        {
            HttpClient coverClient = new HttpClient();
            coverClient.MaxResponseContentBufferSize = 256000;
            coverClient.DefaultRequestHeaders.Add("User-Agent", App.USER_AGENT_FIREFOX);

            String imageUrl = "";

            try
            {
                string address = String.Format(App.URL_COVER, WebUtility.UrlEncode(searchterm));
                coverClient.CancelPendingRequests();
                HttpResponseMessage response = await coverClient.GetAsync(address);
                response.EnsureSuccessStatusCode();
                String responseText = await response.Content.ReadAsStringAsync();

                Regex pattern = new Regex("<img  src=\"(.*?)\" class=\"productImage\"", RegexOptions.IgnoreCase);
                Regex pattern2 = new Regex("<img class=\"productImage\" alt=\"Product Details\" src=\"(.*?)\"", RegexOptions.IgnoreCase);
                Match m = pattern.Match(responseText);
                Match n = pattern2.Match(responseText);

                if (m.Success) {
                    System.Diagnostics.Debug.WriteLine("Cover match for first pattern.");
                    Group g = m.Groups[1];
                    imageUrl = g.ToString();
                }
                else if (n.Success) {
                    System.Diagnostics.Debug.WriteLine("Cover match for second pattern.");
                    Group h = n.Groups[1];
                    imageUrl = h.ToString();
                }
            }
            catch (Exception) { }
            return imageUrl;
        }
    }
}
