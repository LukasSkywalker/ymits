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
            DataContext = DownloadManager.ActiveDownloads;
        }

        private void DownloadPause_Click(object sender, RoutedEventArgs e)
        {
            DownloadOperation dlop = (DownloadOperation)(sender as FrameworkElement).DataContext;
            if(dlop.Progress.Status == BackgroundTransferStatus.PausedByApplication)
                dlop.Resume();
            else if(dlop.Progress.Status == BackgroundTransferStatus.Running)
                dlop.Pause();
        }

        private void DownloadCancel_Click(object sender, RoutedEventArgs e)
        {
            DownloadOperation dlop = (DownloadOperation)(sender as FrameworkElement).DataContext;
            dlop.AttachAsync().Cancel();
        }
    }
}
