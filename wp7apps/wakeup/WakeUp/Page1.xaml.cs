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
using System.Windows.Threading;

namespace WakeUp
{
    public partial class Page1 : PhoneApplicationPage
    {
        public DispatcherTimer timer;
        public DispatcherTimer clock;
        public int counter = 0;

        public Page1()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            clock = new DispatcherTimer();
            startTimer();
            startClock();
        }

        public void startTimer()
        {
            timer.Interval = new TimeSpan(0, 0, 0, 300, 0);
            timer.Tick +=
            delegate( Object s, EventArgs args )
            {
                timer.Stop();
                MainPage.startup = false;
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            };
            timer.Start();
        }

        public void startClock()
        {
            clock.Interval = new TimeSpan(0, 0, 0, 1, 0);
            clock.Tick +=
            delegate( Object s, EventArgs args )
            {
                counter++;
                int time = 300 - counter;
                textBlock1.Value = time;
                
            };
            clock.Start();
        }
    }
}