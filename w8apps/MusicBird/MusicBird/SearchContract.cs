using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Search;
using Windows.Foundation;

namespace MusicBird
{
    class SearchContract
    {
        private static SearchPane SearchPane;

        public static void AttachSearchHandler(SearchPane sp) {
            SearchPane = sp;
            SearchPane.SuggestionsRequested += new TypedEventHandler<SearchPane, SearchPaneSuggestionsRequestedEventArgs>(OnSearchPaneSuggestionsRequested);
            SearchPane.ShowOnKeyboardInput = true;
        }

        public static void WatchKeyboard(bool watch) {
            SearchPane.ShowOnKeyboardInput = watch;
        }

        private static async void OnSearchPaneSuggestionsRequested(SearchPane sender, SearchPaneSuggestionsRequestedEventArgs e)
        {
            var queryText = e.QueryText;
            if (string.IsNullOrEmpty(queryText))
            {
                //Wait
            }
            else
            {
                var request = e.Request;
                var deferral = request.GetDeferral();

                try
                {
                    Task task = GetSuggestionsAsync(queryText, request.SearchSuggestionCollection);
                    await task;
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        if (request.SearchSuggestionCollection.Size > 0)
                        {
                            //MainPage.Current.NotifyUser("Suggestions provided for query: " + queryText, NotifyType.StatusMessage);
                        }
                        else
                        {
                            //MainPage.Current.NotifyUser("No suggestions provided for query: " + queryText, NotifyType.StatusMessage);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Previous suggestion request was canceled.
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Debug.WriteLine("Err:" + exc.Message);
                }
                finally
                {
                    System.Diagnostics.Debug.WriteLine("Sugg. completed.");
                    deferral.Complete();
                }
            }
        }

        private static async Task GetSuggestionsAsync(string queryText, SearchSuggestionCollection searchSuggestionCollection)
        {
            String url = "http://www.lastfm.de/search/autocomplete?q={0}&force=1";
            Uri uri = new Uri(String.Format(url, WebUtility.UrlEncode(queryText)));

            HttpClient suggestionClient = new HttpClient();
            suggestionClient.MaxResponseContentBufferSize = 256000;
            suggestionClient.DefaultRequestHeaders.Add("User-Agent", App.USER_AGENT_FIREFOX);

            HttpResponseMessage response = await suggestionClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string responseText = await response.Content.ReadAsStringAsync();
            Regex pattern = new Regex("\"track\":\"(.*?)\",", RegexOptions.IgnoreCase);
            Regex pattern2 = new Regex("\"artist\":\"(.*?)\",", RegexOptions.IgnoreCase);
            Match m = pattern.Match(responseText);
            Match n = pattern2.Match(responseText);
            while (m.Success)
            {
                Group g = m.Groups[1];
                Group h = n.Groups[1];
                String title = g.ToString();
                String artist = h.ToString();
                searchSuggestionCollection.AppendQuerySuggestion(artist + " " + title);
                m = m.NextMatch();
                n = n.NextMatch();
            }
        }
    }
}
