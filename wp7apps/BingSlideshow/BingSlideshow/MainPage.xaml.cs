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

namespace BingSlideshow
{
    public partial class MainPage : PhoneApplicationPage
    {
        private WebClient wc;

        private DispatcherTimer myAnimation = new DispatcherTimer();
        private DispatcherTimer myStep = new DispatcherTimer();
        private Stack<String> urls = new Stack<String>();
        private double imageWidth = 0;
        private int imageWidthCounter = 0;

        // Constructor
        public MainPage()
        {
            InitializeComponent();


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
            urls = new Stack<String>();
            getResults(TextBox1.Text);
        }

        private void getResults(String searchterm){
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                MessageBox.Show("Please connect to the internet");
            }
            String url = "http://api.bing.net/json.aspx?AppId=A34B1552C3B3DF826089895CCA0D868F0A81EF9D&Query=" + searchterm + "&Sources=Image&Image.Count=20&Image.Filters=Size:Large";
            wc.OpenReadAsync(new Uri(url));
        }

        private void loadImage(String url){
            BitmapImage img = new BitmapImage(new Uri(url));

            PanoramaItem pi = new PanoramaItem();
            pi.Orientation = System.Windows.Controls.Orientation.Horizontal;

            Grid grid = new Grid();

            var image = new Image();
            image.Source = img;
            image.Stretch = Stretch.Uniform;
            grid.Children.Add(image);

            pi.Content = grid;

            //Pan.Items.RemoveAt(0);
            Pan.Items.Add(pi);
        }

        private void downloadImages(Stack<String> urls) {
            foreach (string s in urls)
            {
                loadImage(s);
            }
            step();
            startStep();
        }

        private void startStep(){
            myStep.Interval = new TimeSpan( 0, 0, 0, 4, 0);
            myStep.Tick +=
            delegate(object s, EventArgs args)
            {
                step();
            };
            myStep.Start();
        }

        private void step(){
            if (myAnimation.IsEnabled) {
                myAnimation.Stop();
            }
            int counter = Pan.SelectedIndex;
            
            if (Pan.Items.Count > counter + 1)
            {
                Pan.DefaultItem = Pan.Items[counter + 1];
            }
            else
            {
                Pan.DefaultItem = Pan.Items[0];
            }

            Image child;
            try
            {
                PanoramaItem item = Pan.DefaultItem as PanoramaItem;
                Grid grid = item.Content as Grid;
                child = grid.Children[0] as Image;
            }
            catch (Exception) {
                Pan.DefaultItem = Pan.Items[0];
                PanoramaItem item = Pan.DefaultItem as PanoramaItem;
                Grid grid = item.Content as Grid;
                child = grid.Children[1] as Image;
            }
            imageWidth = child.ActualWidth;

            startAnimation();
        }

        private void startAnimation()
        {
            imageWidthCounter = 0;
            myAnimation.Interval = new TimeSpan(0, 0, 0, 0, 10);
            myAnimation.Tick +=
            delegate(object s, EventArgs args)
            {
                animate();
            };
            myAnimation.Start();
        }

        private void animate()
        {
            double totalWidth = imageWidth;
            int currentWidth = imageWidthCounter;
            double step = (totalWidth / 400)*currentWidth;
            

            Thickness margin = new Thickness(-step,0,0,0);
            
            PanoramaItem item = Pan.DefaultItem as PanoramaItem;
            Grid grid = item.Content as Grid;
            Image child = grid.Children[0] as Image;
            child.Margin = margin;
            imageWidthCounter = currentWidth+1;
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

                //urls holds an array of URLs
                downloadImages(urls);

            }
            finally
            {
                response.Close();
            }
        }

        public bool hasValidImage(PanoramaItem item) {
            try
            {
                Grid grid = item.Content as Grid;
                Image child = grid.Children[0] as Image;
                double iWidth = child.ActualWidth;
                if (iWidth != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception){
                return false;
            }
        }
    }
}