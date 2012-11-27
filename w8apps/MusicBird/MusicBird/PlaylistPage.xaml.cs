using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MusicBird
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlaylistPage : Page
    {
        public RootPage RootPage { get { return (RootPage)((App)Application.Current).RootFrame.Content; } }

        public PlaylistPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = Playlist.Tracks;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Track track = (Track)(sender as FrameworkElement).DataContext;
            RootPage.Playlist.Remove(track);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Track track = (Track)(sender as FrameworkElement).DataContext;
            RootPage.PlayPosition(RootPage.Playlist.GetPosition(track));
        }
    }
}
