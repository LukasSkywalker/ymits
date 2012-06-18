using System;
using System.Device.Location;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;

namespace FastFoodFinder
{
    public partial class Map : PhoneApplicationPage
    {
        public Map()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            double lat = (App.Current as App).lat;
            double lng = (App.Current as App).lng;
            updateMap(lat,lng);
        }

        private void listButton_Click( object sender, RoutedEventArgs e )
        {
            Uri targetUri = new Uri("/List.xaml", System.UriKind.Relative);
            NavigationService.Navigate(targetUri);
        }

        private void updateMap( double lat, double lon )
        {
            map1.Center = new GeoCoordinate(lat, lon);
            map1.ZoomLevel = 14;
            map1.ZoomBarVisibility = System.Windows.Visibility.Visible;
            map1.Children.Clear();

            foreach(App.SearchResult result in (App.Current as App).searchResults)
            {
                Pushpin pin = new Pushpin();
                pin.Location = new GeoCoordinate(result.Latitude, result.Longitude);
                pin.Content = new TextBlock() { Text = result.Name,  };
                pin.Tag = result;
                pin.MouseLeftButtonUp += Fastfood_MouseLeftButtonUp;
                map1.Children.Add(pin);
            }
        }

        private void Fastfood_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            var root = ((FrameworkElement)sender).Tag as App.SearchResult;
            if(root != null)
            {
                // You now have the original object from which the pushpin was created to derive your
                // required response.
                (App.Current as App).startNavigation(root.Name, root.Latitude, root.Longitude);
            }
        }

    }
}