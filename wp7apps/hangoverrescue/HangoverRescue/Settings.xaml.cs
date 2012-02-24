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
        }

        private void saveAddress( object sender, RoutedEventArgs e )
        {
            String address = textBox1.Text;
            var settings = IsolatedStorageSettings.ApplicationSettings;
            settings.Add("address", address);
        }

    }
}