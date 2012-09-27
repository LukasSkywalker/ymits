using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Search;

namespace WolframAlpha
{
    class SearchtermSuggester
    {

        public static async void GetSuggestions(SearchPane sender, SearchPaneSuggestionsRequestedEventArgs e)
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
                catch (FormatException)
                {
                    //MainPage.Current.NotifyUser("Suggestions could not be retrieved, please verify that the URL points to a valid service (for example http://contoso.com?q={searchTerms})", NotifyType.ErrorMessage);
                }
                catch (Exception exc)
                {
                    Helper.DumpException(exc);
                }
                finally
                {
                    deferral.Complete();
                }
            }
        }

        public static async Task GetSuggestionsAsync(string queryText, SearchSuggestionCollection searchSuggestionCollection)
        {
            String url = "http://www.wolframalpha.com/input/autocomplete.jsp?qr=0&i={0}";
            Uri uri = new Uri(String.Format(url, WebUtility.UrlEncode(queryText)));

            //TODO cancelling ends up with no suggestions at all (typing too fast), but leaving
            //     all impacts perfromance, maybe.
            //suggestionClient.CancelPendingRequests();

            HttpClient suggestionClient = new HttpClient();
            suggestionClient.MaxResponseContentBufferSize = 256000;
            suggestionClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            HttpResponseMessage response = await suggestionClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string responseText = await response.Content.ReadAsStringAsync();
            Regex pattern = new Regex("\"input\":\"(.*?)\",", RegexOptions.IgnoreCase);
            //"input":"mathematical rules"
            Match m = pattern.Match(responseText);
            while (m.Success)
            {
                Group g = m.Groups[1];
                String input = g.ToString();
                searchSuggestionCollection.AppendQuerySuggestion(input);
                m = m.NextMatch();
            }
        }
    }
}
