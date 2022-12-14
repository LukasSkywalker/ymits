using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;

namespace MusicBird
{
    public class DownloadOperationViewModel : ViewModelBase
    {
        private DownloadOperation _myDownloadOperation;

        public DownloadOperationViewModel(DownloadOperation dlop)
        {
            _myDownloadOperation = dlop;
        }

        public String ResultFile {
            get { return _myDownloadOperation.ResultFile.Name; }
        }

        public bool Running { get { return _myDownloadOperation.Progress.Status == BackgroundTransferStatus.Running; } }

        public BackgroundTransferStatus ProgressStatus
        {
            get { return _myDownloadOperation.Progress.Status; }
        }

        public ulong BytesReceived
        {
            get { return _myDownloadOperation.Progress.BytesReceived; }
        }

        public ulong TotalBytes
        {
            get { return _myDownloadOperation.Progress.TotalBytesToReceive; }
        }

        public DownloadOperation Dlop {
            get { return _myDownloadOperation; }
        }

        public void Pause() {
            try { _myDownloadOperation.Pause(); }
            catch (Exception ex) { Helper.DumpException(ex); }
        }

        public void Resume() {
            if (_myDownloadOperation.Progress.Status == BackgroundTransferStatus.PausedByApplication ||
                _myDownloadOperation.Progress.Status == BackgroundTransferStatus.PausedCostedNetwork ||
                _myDownloadOperation.Progress.Status == BackgroundTransferStatus.PausedNoNetwork) {
                try { _myDownloadOperation.Resume(); }
                catch (Exception ex) { Helper.DumpException(ex); }
            }
        }

        public void Cancel()
        {
            _myDownloadOperation.AttachAsync().Cancel();
        }

        protected override void RaisePropertyChanged(string propertyName)
        {
            base.RaisePropertyChanged(propertyName);
        }
        protected override void RaisePropertyChanged<T>(string propertyName, T oldValue, T newValue, bool broadcast)
        {
            base.RaisePropertyChanged<T>(propertyName, oldValue, newValue, broadcast);
        }

        public void RaisePropChanged(String propName) {
            this.RaisePropertyChanged(propName);
        }
    }
}
