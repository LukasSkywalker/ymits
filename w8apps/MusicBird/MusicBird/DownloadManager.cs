using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace MusicBird
{
    public class DownloadManager : INotifyPropertyChanged
    {
        public static ObservableCollection<DownloadOperation> ActiveDownloads { get; set; }
        public static ObservableCollection<DownloadOperationViewModel> AllDownloads { get; set; }
        private CancellationTokenSource cts = new CancellationTokenSource();

        public static int DownloadCount { get { return ActiveDownloads.Count; } }

        public DownloadManager() {
            ActiveDownloads = new ObservableCollection<DownloadOperation>();
            AllDownloads = new ObservableCollection<DownloadOperationViewModel>();
        }

        public async void Add(Track track) {
            string url = track.Url;

            Uri source = new Uri(url);

            StorageFolder musicLibrary = KnownFolders.MusicLibrary;
            StorageFile destinationFile = await musicLibrary.CreateFileAsync(track.Artist.Replace("\"", "") + " - " + track.Title.Replace("\"", "") + ".mp3",
                CreationCollisionOption.ReplaceExisting);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation dlop = downloader.CreateDownload(source, destinationFile);

            await HandleDownloadAsync(dlop, true);
        }

        private async Task HandleDownloadAsync(DownloadOperation download, bool start)
        {
            DownloadOperationViewModel dlopvm = new DownloadOperationViewModel(download);

            try
            { 
                ActiveDownloads.Add(download);
                AllDownloads.Add(dlopvm);
                RaisePropertyChanged("ActiveDownloads");
                RaisePropertyChanged("DownloadCount");
                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgressHandler);
                if (start)
                {
                    await download.StartAsync().AsTask(cts.Token, progressCallback);
                }
                else
                {
                    await download.AttachAsync().AsTask(cts.Token, progressCallback);
                }
                ResponseInformation response = download.GetResponseInformation();
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception)
            {
            }
            finally
            {
                ActiveDownloads.Remove(download);
                AllDownloads.Remove(dlopvm);
                RaisePropertyChanged("ActiveDownloads");
                RaisePropertyChanged("DownloadCount");
            }
        }

        private void DownloadProgressHandler(DownloadOperation dlop)
        {
            RaisePropertyChanged("ActiveDownloads");
            foreach (DownloadOperationViewModel dlopvm in AllDownloads)
            {
                if (dlopvm.Dlop == dlop)
                {
                    dlopvm.RaisePropChanged("BytesReceived");
                    dlopvm.RaisePropChanged("TotalBytes");
                    dlopvm.RaisePropChanged("ProgressStatus");
                    break;
                }
            }
            System.Diagnostics.Debug.WriteLine(dlop.ResultFile.Name+" : "+dlop.Progress.Status+", "+dlop.Progress.BytesReceived+" of "+dlop.Progress.TotalBytesToReceive);
        }

        public async Task ResumeDownloads()
        {
            ActiveDownloads = new ObservableCollection<DownloadOperation>();
            IReadOnlyList<DownloadOperation> downloads = null;
            try
            {
                downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            }
            catch (Exception)
            {
                if (false)
                {
                    throw;
                }
                return;
            }
            System.Diagnostics.Debug.WriteLine("Loading background downloads: " + downloads.Count);
            if (downloads.Count > 0)
            {
                List<Task> tasks = new List<Task>();
                foreach (DownloadOperation download in downloads)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Discovered background download: {0}, Status: {1}", download.Guid,
                        download.Progress.Status));
                    tasks.Add(HandleDownloadAsync(download, false));
                }
                await Task.WhenAll(tasks);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string caller = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(caller);
            return true;
        }

        private void RaisePropertyChanged(String caller)
        {
            if (PropertyChanged != null)
            {
                System.Diagnostics.Debug.WriteLine("Prop Changed");
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }
    }
}
