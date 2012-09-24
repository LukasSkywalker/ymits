﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Devices.Geolocation;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
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
        private int flyoutOffset;
        public ApplicationDataContainer RoamingSettings = ApplicationData.Current.RoamingSettings;
        public bool LocationEnabled { get; set; }

        public Geocoordinate Coordinates { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = LocationEnabled;

            ApplicationData.Current.DataChanged += new TypedEventHandler<ApplicationData, object>(DataChangeHandler);
            SettingsPane.GetForCurrentView().CommandsRequested += CommandsRequested;

            int isKeyboardPresent = new KeyboardCapabilities().KeyboardPresent;
            int isMousePresent = new MouseCapabilities().MousePresent;
            int isTouchPresent = new TouchCapabilities().TouchPresent;

            // show additional buttons when keyboard is present
            // hide if there is no keyboard, but show when Virtual Keyboard is shown
            if(isKeyboardPresent == 1)
                AdditionalKeyboard.Visibility = Visibility.Visible;

            // add event handler
            // show keyboard when Virtual Keyboard is shown
            Windows.UI.ViewManagement.InputPane.GetForCurrentView().Showing += (s, args) =>
            {
                flyoutOffset = (int)args.OccludedRect.Height;
                // show buttons always
                AdditionalKeyboard.Margin = new Thickness(0, 0, 0, (double)flyoutOffset);
                AdditionalKeyboard.Visibility = Visibility.Visible;
            };

            // hide keyboard when Virtual Keys hidden AND no keyboard
            // let them stay when Virtual Keys hidden AND keyboard present
            Windows.UI.ViewManagement.InputPane.GetForCurrentView().Hiding += (s, args) =>
            {
                //downAnimation.Begin();
                // hide buttons when no keyboard
                AdditionalKeyboard.Margin = new Thickness(0, 0, 0, 0);
                if(new KeyboardCapabilities().KeyboardPresent == 0)
                    AdditionalKeyboard.Visibility = Visibility.Collapsed;
            };
        }

        private void CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            args.Request.ApplicationCommands.Add(new SettingsCommand("A", "About", (p) => { flyoutAbout.IsOpen = true; }));
            args.Request.ApplicationCommands.Add(new SettingsCommand("A", "Settings", (p) => { flyoutSettings.IsOpen = true; }));
        }

        private void DataChangeHandler(ApplicationData sender, object args)
        {
            //History.AddRange((List<HistoryItem>)sender.RoamingSettings.Values["History"]);
        }

        public void AddToHistory(String value)
        {
            List<HistoryItem> HistoryList = Deserialize((String)RoamingSettings.Values["History"]);
 
            int index = -1;
            //remove if exists
            do
            {
                index = HistoryList.FindIndex(x => x.Text.ToLower().Equals(value.ToLower()));
                if (index > -1)
                {
                    HistoryList.RemoveAt(index);
                }
            } while (index > -1);

            // create new
            HistoryItem Item = new HistoryItem();
            Item.Text = value;
            Item.DateTime = DateTime.Now;
            HistoryList.Insert(0, Item);

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
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            HistoryListBox.ItemsSource = Deserialize((String)RoamingSettings.Values["History"]);

            if (LocationEnabled)
            {
                Coordinates = await GeoLocator.GetGeolocation();
            }
        }

        private bool NullToFalse(object a) {
            if (a == null) return false;
            else return (bool)a;
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
            if (LocationEnabled) {
            
            }
            String QueryText = searchTextBox.Text;
            AddToHistory(QueryText);
            this.Frame.Navigate(typeof(ItemDetailPage), QueryText);
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

        private void InsertCharacter(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            String value = (String)btn.Content;
            searchTextBox.Text += value;
            searchTextBox.Select(searchTextBox.Text.Length, 0);
            searchTextBox.Focus(Windows.UI.Xaml.FocusState.Keyboard);
        }
    }
}
