using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
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
    public sealed partial class DownloadPage : Page
    {
        private RootPage RootPage { get { return (RootPage)((App)Application.Current).RootFrame.Content; } }

        public DownloadPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = DownloadManager.AllDownloads;
        }

        private void DownloadPause_Click(object sender, RoutedEventArgs e)
        {
            DownloadOperationViewModel dlopvm = (DownloadOperationViewModel)(sender as FrameworkElement).DataContext;
            dlopvm.PauseResume();
        }

        private void DownloadCancel_Click(object sender, RoutedEventArgs e)
        {
            DownloadOperationViewModel dlopvm = (DownloadOperationViewModel)(sender as FrameworkElement).DataContext;
            dlopvm.Cancel();
        }

        private void PauseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (DownloadOperationViewModel dlopvm in DownloadManager.AllDownloads)
                dlopvm.PauseResume();
        }

        private void RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (DownloadOperationViewModel dlopvm in DownloadManager.AllDownloads)
                dlopvm.Cancel();
        }
    }
}
