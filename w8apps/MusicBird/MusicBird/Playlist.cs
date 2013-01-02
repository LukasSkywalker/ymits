using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace MusicBird
{
    public class Playlist : INotifyPropertyChanged
    {
        private int position = 0;

        public ObservableCollection<Track> Tracks { get; private set; }
        public int Position { get { return position; } set { position = value; RaisePropertyChanged("CurrentTrack"); } }
        public int Size { get { return Tracks.Count; } }

        public ObservableCollection<Track> Neighbors
        {
            get
            {
                const int AMOUNT = 3;
                int min = Position - (int)(AMOUNT/2);
                if (Position <= 1) min = 0;
                int max = min + AMOUNT;
                if (Size < max + 1) max = Size;
                ObservableCollection<Track> nb = new ObservableCollection<Track>();
                foreach (var track in Tracks.Skip(min).Take(max - min))
                {
                    nb.Add(track);
                }
                return nb;
            }
        }
        public Track CurrentTrack { get {
            int pos = mod(Position, Size);
            if(pos == 0 && Tracks.Count == 0)
                return null;
            else
                return Tracks[pos];
        } }

        public Playlist() {
            Tracks = new ObservableCollection<Track>();
            Position = 0;
        }

        public void Add(Track track) {
            Tracks.Add(track);
            RaisePropertyChanged();
        }

        public int GetPosition(Track track){
            return Tracks.IndexOf(track);
        }

        public void Remove(Track track) {
            Tracks.Remove(track);
            RaisePropertyChanged();
        }

        public void Remove(int index) {
            Tracks.RemoveAt(index);
            RaisePropertyChanged();
        }

        public void Clear() {
            Tracks.Clear();
            RaisePropertyChanged();
        }

        private int mod(int number, int divisor) {
            int r = divisor > 0 ? number % divisor : 0;
            return r < 0 ? r + divisor : r;
        }

        public void RaisePropertyChanged(string caller = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public enum PlaybackMode {
            Normal,
            TryPlay = 1,
        }
    }
}
