using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

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
            (App.Current as App).searchResults.Sort(( x, y ) => String.Compare(x.Distance, y.Distance));
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