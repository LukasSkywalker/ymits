using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DualNBack
{
    public partial class MainPage : PhoneApplicationPage
    {
        private readonly bool runTests = System.Diagnostics.Debugger.IsAttached;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            this.runTests = false;
        }
        
        void MainPage_Loaded( object sender, RoutedEventArgs e )
        {
            if(this.runTests)
            {
                System.Diagnostics.Debug.WriteLine("Running tests");
                SystemTray.IsVisible = false;

                var testPage = UnitTestSystem.CreateTestPage() as IMobileTestPage;

                BackKeyPress += ( x, xe ) => xe.Cancel = testPage.NavigateBack();
                (Application.Current.RootVisual as PhoneApplicationFrame).Margin = new System.Windows.Thickness(0, -32, 0, 0);
                (Application.Current.RootVisual as PhoneApplicationFrame).Content = testPage;
            }
            else {
                GameRunner gr = new GameRunner(3);
                gr.PositionChosen +=new GameRunner.PositionChosenEventHandler(gr_PositionChosen);
                gr.SoundChosen += new GameRunner.SoundChosenEventHandler(gr_SoundChosen);
            }
        }

        void gr_SoundChosen( object sender, GameRunner.SoundChosenEventArgs e )
        {
            playSound(e.index);
            System.Diagnostics.Debug.WriteLine("SND: "+e.index);
            if(e.isMatch) System.Diagnostics.Debug.WriteLine("SND MATCHED");
        }

        void gr_PositionChosen( object sender, GameRunner.PositionChosenEventArgs e )
        {
            System.Diagnostics.Debug.WriteLine("POS: "+e.index);
            if(e.isMatch) System.Diagnostics.Debug.WriteLine("POS MATCHED");
        }

        private void playSound( int index ) {
            mediaPlayer.Source = new Uri("Audio/letter"+index+".mp3", UriKind.Relative);
            mediaPlayer.Volume = 0.5;
            mediaPlayer.Play();
        }
    }
}