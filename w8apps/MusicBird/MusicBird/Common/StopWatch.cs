using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBird.Common
{


    class StopWatch
    {
        Stopwatch watch;

        public StopWatch(bool startNow){
            watch = new Stopwatch();
            if (startNow)
                this.Start();
        }

        public void Start() {
            watch.Reset();
            watch.Start();
        }

        public string Split(string message) {
            return message + ": " + watch.ElapsedMilliseconds + " ms";
        }

        public string Stop(string message) {
            watch.Stop();
            return message+": "+watch.ElapsedMilliseconds+" ms";
        }

        public void Reset() {
            watch.Reset();
        }

        public void Restart() {
            watch.Reset();
            watch.Start();
        }
    }
}
