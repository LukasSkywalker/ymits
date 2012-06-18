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
using Microsoft.Phone.Controls;
using System.Windows.Navigation;

namespace FastFoodFinder
{
    public partial class List : PhoneApplicationPage
    {
        public List()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            ListElement.ItemsSource = (App.Current as App).searchResults;
        }

        private void mapButton_Click( object sender, RoutedEventArgs e )
        {
            Uri targetUri = new Uri("/Map.xaml", System.UriKind.Relative);
            NavigationService.Navigate(targetUri);
        }

        private void listItem_Click( object sender, RoutedEventArgs e ) {
            App.SearchResult result = (sender as FrameworkElement).DataContext as App.SearchResult;
            (App.Current as App).startNavigation(result.Name, result.Latitude, result.Longitude);
        }
    }
}