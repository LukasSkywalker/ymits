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

namespace ChuckNorrisJokes
{
    public partial class MainPage : PhoneApplicationPage
    {
        private WebClient wc;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            wc = new WebClient();
            wc.Encoding = System.Text.Encoding.UTF8;

            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);

            button1_Click(null, null);
        }

        private void button1_Click( object sender, RoutedEventArgs e )
        {
            if(!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("Please connect to the internet");
                textBlock1.Text = "No internet connection available.";
            }
            else
            {
                String url = "http://api.icndb.com/jokes/random";
                Uri uri = new Uri(url);
                wc.OpenReadAsync(uri);
            }
        }

        private void wc_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e )
        {
            //{ "type": "success", "value": { "id": 322, "joke": "When Chuck Norris plays Monopoly, it affects the actual world economy.", "categories": ["chuck norris"] } }
            Regex pattern = new Regex("\"joke\": \"(.*?)\",", RegexOptions.IgnoreCase);
            String s;
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
                if(m.Success)
                {
                    Group g = m.Groups[1];
                    textBlock1.Text = g.ToString();
                }

                System.Diagnostics.Debug.WriteLine("URLs matched");

            }
            finally
            {
                response.Close();
            }
        }
    }
}