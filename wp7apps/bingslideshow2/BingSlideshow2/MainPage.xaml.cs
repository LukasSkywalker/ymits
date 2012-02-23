using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Media.Imaging;

namespace BingSlideshow2
{
    public partial class MainPage : PhoneApplicationPage
    {
        private Stack<String> urls = new Stack<String>();
        private int imageDownloadCounter = 0;
        private WebClient wc;


        // Constructor
        public MainPage()
        {
            InitializeComponent();

            this.wc = new WebClient();
            this.wc.Encoding = System.Text.Encoding.UTF8;

            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded( object sender, RoutedEventArgs e )
        {
            if(!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        private void Button1_Click( object sender, RoutedEventArgs e )
        {
            queryProgress.Visibility = Visibility.Visible;
            queryProgress.IsIndeterminate = true;
            getResults(TextBox1.Text);
        }

        private void getResults( String searchterm )
        {
            System.Diagnostics.Debug.WriteLine("Searching "+searchterm);
            imageDownloadCounter = 0;
            urls.Clear();
            if(!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("Please connect to the internet");
            }
            int count = 0;
            System.Diagnostics.Debug.WriteLine("Removing " + (slideshowPivot.Items.Count - 1) + " items");
            while(slideshowPivot.Items.Count > 1)
            {
                //PanoramaItem pi = Pan.Items[1] as PanoramaItem;
                //Pan.Items.Remove(pi);
                PivotItem pi = slideshowPivot.Items[1] as PivotItem;
                /*Grid grid = pi.Content as Grid;
                Image img = grid.Children[0] as Image;
                BitmapImage bitmapImage = img.Source as BitmapImage;
                bitmapImage.UriSource = null;
                img.Source = null;
                GC.Collect();
                GC.SuppressFinalize(img);
                GC.WaitForPendingFinalizers();*/

                slideshowPivot.Items.RemoveAt(1);
                count++;
            }
            System.Diagnostics.Debug.WriteLine("Removed " + count + " items");
            String url = "http://api.bing.net/json.aspx?AppId=A34B1552C3B3DF826089895CCA0D868F0A81EF9D&Query=" + searchterm + "&Sources=Image&Image.Count=20&Image.Filters=Size:Medium";
            System.Diagnostics.Debug.WriteLine("Getting "+url);
            Uri uri = new Uri(url);
            wc.OpenReadAsync(uri);
        }

        private void loadImage( String url )
        {
            BitmapImage img = new BitmapImage(new Uri(url));


            PivotItem pi = new PivotItem();


            var image = new Image();
            image.ImageOpened += new EventHandler<RoutedEventArgs>(onImageLoaded);
            image.ImageFailed += new EventHandler<ExceptionRoutedEventArgs>(onImageError);
            image.Source = img;

            image.Stretch = Stretch.Uniform;

            pi.Content = image;

            slideshowPivot.Items.Add(pi);
        }

        private void onImageLoaded( Object sender, RoutedEventArgs e )
        {
            startDownload();
        }

        private void onImageError( Object sender, RoutedEventArgs e )
        {
            Image img = sender as Image;
            PivotItem pi = img.Parent as PivotItem;
            slideshowPivot.Items.Remove(pi);
            startDownload();
        }

        private void startDownload()
        {
            System.Diagnostics.Debug.WriteLine("Count: "+slideshowPivot.Items.Count());
            if(slideshowPivot.Items.Count() == 2)
            {
                System.Diagnostics.Debug.WriteLine("Changed index");
                slideshowPivot.SelectedIndex = 1;
            }
            if(imageDownloadCounter < urls.Count)
            {
                loadImage(urls.ElementAt(imageDownloadCounter));
                imageDownloadCounter++;
            }
        }



        private void wc_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e )
        {
            //"MediaUrl":"http:\/\/support.microsoft.com\/library\/images\/support\/kbgraphics\/Public\/EN-US\/XBOX\/360s\/opticalport360s.jpg"
            Regex pattern = new Regex("\"MediaUrl\":\"(.*?)\",", RegexOptions.IgnoreCase);
            String s;
            Stream response = e.Result;
            try
            {
                StreamReader sr = new StreamReader(response, System.Text.Encoding.UTF8);
                try
                {
                    s = sr.ReadToEnd();
                    s = s.Replace("\\/", "/");
                }
                finally
                {
                    sr.Close();
                }


                // Match the regular expression pattern against a text string.
                Match m = pattern.Match(s);
                //Dim urls(m.Length) As String
                int matchCount = 0;
                while(m.Success)
                {
                    Group g = m.Groups[1];
                    urls.Push(g.ToString());
                    matchCount += 1;
                    m = m.NextMatch();
                }

                System.Diagnostics.Debug.WriteLine("URLs matched");

                //urls holds an array of URLs
                //downloadImages(urls);
                startDownload();
                //step();

            }
            finally
            {
                response.Close();
                queryProgress.Visibility = Visibility.Collapsed;
                queryProgress.IsIndeterminate = false;
            }
        }
    }
}