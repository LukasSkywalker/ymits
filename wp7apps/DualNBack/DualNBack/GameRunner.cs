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
using System.Collections.Generic;
using System.Windows.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.IO;


namespace DualNBack
{
    public class GameRunner
    {
        #region Event Handlers
        public event SoundChosenEventHandler SoundChosen;
        public delegate void SoundChosenEventHandler( object sender, SoundChosenEventArgs e );
        public class SoundChosenEventArgs
        {
            public SoundChosenEventArgs( int index, bool isMatch )
            {
                this.index = index;
                this.isMatch = isMatch;
            }
            public int index { get; private set; }
            public bool isMatch { get; private set; }
        }

        public event PositionChosenEventHandler PositionChosen;
        public delegate void PositionChosenEventHandler( object sender, PositionChosenEventArgs e );
        public class PositionChosenEventArgs
        {
            public PositionChosenEventArgs( int index, bool isMatch )
            {
                this.index = index;
                this.isMatch = isMatch;
            }
            public int index { get; private set; }
            public bool isMatch { get; private set; }
        } 
        #endregion

        private readonly double INTERVAL = 4;
        private readonly int n;

        private NumberChooser positionChooser;
        private NumberChooser soundChooser;
        private DispatcherTimer updateTimer;

        public GameRunner(int n) {
            this.n = n;
            this.positionChooser = new DualNBack.NumberChooser(n);
            this.soundChooser = new DualNBack.NumberChooser(n);
            this.updateTimer = new DispatcherTimer();
            this.updateTimer.Interval = TimeSpan.FromSeconds(INTERVAL);
            this.updateTimer.Tick += new EventHandler(updateTimer_Tick);
            this.updateTimer.Start();
        }

        private void updateTimer_Tick( object sender, EventArgs e ) {
            int position = positionChooser.getNew();
            int sound = soundChooser.getNew();

            PositionChosenEventArgs posE = new PositionChosenEventArgs(position, positionChooser.isMatch());
            OnPositionChosen(posE);

            SoundChosenEventArgs soundE = new SoundChosenEventArgs(sound, soundChooser.isMatch());
            OnSoundChosen(soundE);
        }

        protected virtual void OnPositionChosen( PositionChosenEventArgs e )
        {
            if(PositionChosen != null) PositionChosen(this, e);
        }

        protected virtual void OnSoundChosen( SoundChosenEventArgs e )
        {
            if(SoundChosen != null) SoundChosen(this, e);
        }
    }
}
