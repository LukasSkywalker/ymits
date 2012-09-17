using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WolframAlpha.Common;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WolframAlpha
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ApplicationDataContainer RoamingSettings = ApplicationData.Current.RoamingSettings;

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationData.Current.DataChanged += new TypedEventHandler<ApplicationData, object>(DataChangeHandler);
        }

        private void DataChangeHandler(ApplicationData sender, object args)
        {
            //History.AddRange((List<HistoryItem>)sender.RoamingSettings.Values["History"]);
        }

        public void AddToHistory(String value)
        {
            List<HistoryItem> HistoryList = Deserialize((String)RoamingSettings.Values["History"]);

            HistoryItem Item = new HistoryItem();
            Item.Text = value;
            Item.DateTime = DateTime.Now;
            HistoryList.Insert(0,Item);

            string result = Serialize(HistoryList);

            RoamingSettings.Values["History"] = result;
            HistoryListBox.ItemsSource = HistoryList;
            
            return;
        }

        private List<HistoryItem> Deserialize(String xmlData)
        {
            System.Diagnostics.Debug.WriteLine("Deserializing old list...");
            if (xmlData == null) {
                return new List<HistoryItem>();
            }
            using (Stream stream = new MemoryStream())
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xmlData);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                XmlSerializer deserializer = new XmlSerializer(typeof(List<HistoryItem>));
                List<HistoryItem> list = (List<HistoryItem>)deserializer.Deserialize(stream);
                System.Diagnostics.Debug.WriteLine("Deserializing completed.");
                return list;
            }
        }

        private string Serialize(List<HistoryItem> HistoryList)
        {
            System.Diagnostics.Debug.WriteLine("Serializing new list...");
            MemoryStream memStm = new MemoryStream();
            var serializer = new XmlSerializer(typeof(List<HistoryItem>));
            serializer.Serialize(memStm, HistoryList);

            memStm.Seek(0, SeekOrigin.Begin);
            string result = new StreamReader(memStm).ReadToEnd();
            System.Diagnostics.Debug.WriteLine("Serializing completed.");
            return result;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HistoryListBox.ItemsSource = Deserialize((String)RoamingSettings.Values["History"]);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            startSearch();
        }

        private void Search_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                startSearch();
            }
        }

        private void startSearch()
        {
            String QueryText = searchTextBox.Text;
            AddToHistory(QueryText);
            this.Frame.Navigate(typeof(SearchResultsPage), QueryText);
        }

        private void History_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = ((ListBox)sender);
            if (lb.SelectedItem == null)
                return;
            String Text = (string)lb.SelectedValue;
            searchTextBox.Text = Text;
        }

        private void ClearHistory(object sender, RoutedEventArgs e)
        {
            RoamingSettings.Values["History"] = null;
            HistoryListBox.ItemsSource = null;
        }
    }
}
