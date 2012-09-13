using Calculator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Calculator.ViewModels
{
    class ResultsPageViewModel
    {
        private HttpClient searchClient;

        public async void StartRequest(String query){
            String url = App.ServiceURL;
            Uri uri = new Uri(String.Format(url, App.AppId,  WebUtility.UrlEncode(query)));

            HttpResponseMessage response = await searchClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string responseText = await response.Content.ReadAsStringAsync();


        }

        // Run on UI:
        //App.Dispatcher.RunAsync(DispatcherPriority.Normal, () => DoSomething());
    }
}
