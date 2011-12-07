using System;
using System.IO.IsolatedStorage;
using System.Windows;
using Microsoft.Phone.Controls;

namespace MusicBird
{
    public partial class Page1 : PhoneApplicationPage
    {
        public Page1()
        {
            InitializeComponent();
        }

        private void albumart_Checked( object sender, RoutedEventArgs e )
        {
            save("albumart", true);
        }

        private void albumart_Unchecked( object sender, RoutedEventArgs e )
        {
            save("albumart", false);
        }

        private void allowCellular_Checked( object sender, RoutedEventArgs e )
        {
            save("allowCellular", true);
        }

        private void allowCellular_Unchecked( object sender, RoutedEventArgs e )
        {
            save("allowCellular", false);
        }

        private void allowBattery_Unchecked( object sender, RoutedEventArgs e )
        {
            save("allowBattery", false);
        }

        private void allowBattery_Checked( object sender, RoutedEventArgs e )
        {
            save("allowBattery", true);
        }

        public static void save( string name, Boolean value ) {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if(settings.Contains(name))
            {
                settings.Remove(name);
            }
            settings.Add(name, value);
            //settings.Save();
        }

        public static bool read( string name ) {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            Boolean item;
            if(settings.Contains(name))
            {
                if(settings.TryGetValue<Boolean>(name, out item))
                {
                    return item;
                }
                else {
                    throw new IsolatedStorageException("could not read value of "+name+" from IsoStore. tryGetValue failed.");
                }
            }
            else
            {
                settings.Add(name, false);
                //settings.Save();
                return false;
            }
        }

        private void PhoneApplicationPage_Loaded( object sender, RoutedEventArgs e )
        {
            albumart.IsChecked = read("albumart");
            allowCellular.IsChecked = read("allowCellular");
            allowBattery.IsChecked = read("allowBattery");
        }

    }
}