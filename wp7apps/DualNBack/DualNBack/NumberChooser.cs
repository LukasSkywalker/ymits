using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DualNBack
{
    public class NumberChooser
    {
        private readonly int n;
        private DualNBack.Queue<int> queue;
        private readonly Random randomNumber;

        private readonly int repetitionInterval = 2;

        public NumberChooser( int n ) {
            this.n = n;
            this.queue = new DualNBack.Queue<int>(n+1);
            this.randomNumber = new Random((int)DateTime.Now.Ticks);
        }

        public int getNew()
        {
            bool shouldRepeat = true; //randomNumber.Next(1, repetitionInterval+1) == 1;
            if(this.queue.IsFull() && shouldRepeat)
            {
                int val = this.queue.First();
                this.queue.Add(val);
                return val;
            }
            else
            {
                int val = randomNumber.Next(1, 9);
                // TODO Only 8 sound available, but 9 positions!
                this.queue.Add(val);
                return val;
            }
        }

        public bool isMatch() {
            if(this.queue.IsFull())
                return this.queue.First().Equals(this.queue.Last());
            else
                return false;
        }
    }
}
