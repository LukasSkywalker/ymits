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
using System.IO.IsolatedStorage;
using Microsoft.Phone.Controls;

namespace HangoverRescue
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
            loadPrefs();
        }

        private void loadPrefs() {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains("address")) textBox1.Text = (string)settings["address"];
            if(settings.Contains("location")) locationCheckbox.IsChecked = (bool)settings["location"];
        }

        private void saveAddress( object sender, RoutedEventArgs e )
        {
            String address = textBox1.Text;
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains("address")) settings.Remove("address");
            settings.Add("address", address);
            settings.Save();
        }

        private void saveAllowLocation(object sender, RoutedEventArgs e) {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains("location")) settings.Remove("location");
            bool? val = locationCheckbox.IsChecked;
            if(val.Value) settings.Add("location", true);
            else settings.Add("location", false);
            settings.Save();
        }

    }
}