using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using System.Windows.Threading;
using System.IO;
using System.Net.NetworkInformation;
using Microsoft.Phone.Info;

namespace BingSlideshow
{
    public partial class MainPage : PhoneApplicationPage
    {
        private WebClient wc;

        //private DispatcherTimer myAnimation = new DispatcherTimer();
        private DispatcherTimer myStep = new DispatcherTimer();
        private Stack<String> urls = new Stack<String>();
        private double imageWidth = 0;
        private int imageWidthCounter = 0;
        private int imageDownloadCounter = 0;
        private bool panoramaAdvanced = false;
        

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;
            }

            const string total = "DeviceTotalMemory";
            const string current = "ApplicationCurrentMemoryUsage";
            const string peak = "ApplicationPeakMemoryUsage";

            long totalBytes =
              (long)DeviceExtendedProperties.GetValue(total);
            long currentBytes =
              (long)DeviceExtendedProperties.GetValue(current);
            long peakBytes =
              (long)DeviceExtendedProperties.GetValue(peak);

            textBlock2.Text = totalBytes.ToString();
            textBlock2.Text = currentBytes.ToString();
            textBlock2.Text = peakBytes.ToString();

            this.wc = new WebClient();
            this.wc.Encoding = System.Text.Encoding.UTF8;

            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);
            //AddHandler wc.OpenReadCompleted, New OpenReadCompletedEventHandler(AddressOf wc_OpenReadCompleted)

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            queryProgress.Visibility = Visibility.Visible;
            queryProgress.IsIndeterminate = true;
            getResults(TextBox1.Text);
        }

        private void getResults(String searchterm){
            panoramaAdvanced = false;
            imageDownloadCounter = 0;
            urls.Clear();
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                MessageBox.Show("Please connect to the internet");
            }
            int count = 0;
            System.Diagnostics.Debug.WriteLine("Removing " + (Pan.Items.Count-1) + " items");
            while (Pan.Items.Count > 1)
            {
                //PanoramaItem pi = Pan.Items[1] as PanoramaItem;
                //Pan.Items.Remove(pi);
                PanoramaItem pi = Pan.Items[1] as PanoramaItem;
                Grid grid = pi.Content as Grid;
                Image img = grid.Children[0] as Image;
                BitmapImage bitmapImage = img.Source as BitmapImage;
                bitmapImage.UriSource = null;
                img.Source = null;
                GC.Collect();
                GC.SuppressFinalize(img);
                GC.WaitForPendingFinalizers();
                
                Pan.Items.RemoveAt(1);
                count++;
            }
            System.Diagnostics.Debug.WriteLine("Removed " + count + " items");
            String url = "http://api.bing.net/json.aspx?AppId=A34B1552C3B3DF826089895CCA0D868F0A81EF9D&Query=" + searchterm + "&Sources=Image&Image.Count=20&Image.Filters=Size:Large";
            wc.OpenReadAsync(new Uri(url));
        }

        private void loadImage(String url){
            BitmapImage img = new BitmapImage(new Uri(url));
            

            PanoramaItem pi = new PanoramaItem();
            pi.Orientation = System.Windows.Controls.Orientation.Horizontal;

            Grid grid = new Grid();

            var image = new Image();
            image.ImageOpened += new EventHandler<RoutedEventArgs>(onImageLoaded);
            image.ImageFailed += new EventHandler<ExceptionRoutedEventArgs>(onImageError);
            image.Source = img;

            image.Stretch = Stretch.UniformToFill;
            grid.Children.Add(image);

            pi.Content = grid;

            Pan.Items.Add(pi);
            
        }

        private void onImageLoaded(Object sender, RoutedEventArgs e) {
            /*if (!panoramaAdvanced) {
                //Pan.DefaultItem = Pan.Items[1];
                panoramaAdvanced = true;
            }*/
            startDownload();
        }

        private void onImageError(Object sender, RoutedEventArgs e) {
            Image img = sender as Image;
            Grid grid = img.Parent as Grid;
            PanoramaItem pi = grid.Parent as PanoramaItem;
            Pan.Items.Remove(pi);
            startDownload();
        }

        /*private void downloadImages(Stack<String> urls) {
            foreach (string s in urls)
            {
                loadImage(s);
            }
            System.Diagnostics.Debug.WriteLine("Images downloaded");
        }*/

        private void startDownload(){
            //myStep.Interval = new TimeSpan( 0, 0, 0, 2, 0);
            /*myStep.Tick +=
            delegate(object s, EventArgs args)
            {
                */if (imageDownloadCounter < urls.Count) {
                    loadImage(urls.ElementAt(imageDownloadCounter));
                    imageDownloadCounter++;
                }
                /*else {
                    myStep.Stop();
                }
            };
            myStep.Start();*/
        }



        private void wc_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
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
                while (m.Success)
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