using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.IO;
using System.Windows.Shapes;
using System.Net.NetworkInformation;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;

namespace ChuckNorrisJokes
{
    public partial class MainPage : PhoneApplicationPage
    {
        private WebClient wc;
        private IsolatedStorageSettings settings;
        Random rand;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            wc = new WebClient();
            wc.Encoding = System.Text.Encoding.UTF8;

            settings = IsolatedStorageSettings.ApplicationSettings;
            rand = new Random();

            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);

            button1_Click(null, null);
        }

        private void button1_Click( object sender, RoutedEventArgs e )
        {
            if(!NetworkInterface.GetIsNetworkAvailable())
            {
                getLocalJoke();
            }
            else
            {
                String url = "http://api.icndb.com/jokes/random";
                Uri uri = new Uri(url);
                try
                {
                    wc.OpenReadAsync(uri);
                }
                catch(NotSupportedException ex) {
                    MessageBox.Show("Unknown error occured. Please try again.");
                }
            }
        }

        private void wc_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e )
        {
            //{ "type": "success", "value": { "id": 322, "joke": "When Chuck Norris plays Monopoly, it affects the actual world economy.", "categories": ["chuck norris"] } }
            Regex pattern = new Regex("\"joke\": \"(.*?)\", \"", RegexOptions.IgnoreCase);
            Regex pattern_id = new Regex("\"id\": (.*?),", RegexOptions.IgnoreCase);
            String s;
            try
            {
                Stream response = e.Result;
                try
                {
                    StreamReader sr = new StreamReader(response, System.Text.Encoding.UTF8);
                    try
                    {
                        s = sr.ReadToEnd();
                        s = s.Replace("\\/", "/");
                        s = HttpUtility.HtmlDecode(s);
                    }
                    finally
                    {
                        sr.Close();
                    }

                    System.Diagnostics.Debug.WriteLine(s);

                    // Match the regular expression pattern against a text string.
                    Match m = pattern.Match(s);
                    Match n = pattern_id.Match(s);
                    if(m.Success)
                    {
                        Group g = m.Groups[1];
                        Group h = n.Groups[1];
                        textBlock1.Text = g.ToString();
                        if(!settings.Contains(h.ToString())) {
                            settings.Add(h.ToString(), g.ToString());
                            settings.Save();
                        }
                    }
                    textBlock2.Visibility = Visibility.Collapsed;
                }
                finally
                {
                    response.Close();
                }
            }
            catch(WebException ex) {
                getLocalJoke();
            }
        }

        private void getLocalJoke() {
            textBlock2.Visibility = Visibility.Visible;
            KeyValuePair<string,object>[] jokes = settings.ToArray();
            int number = rand.Next(jokes.Length);
            textBlock1.Text = (String) jokes[number].Value;
        }
    }
}