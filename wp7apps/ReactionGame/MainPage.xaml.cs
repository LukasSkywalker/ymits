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
using System.Diagnostics;

namespace ReactionGame
{
    public partial class MainPage : PhoneApplicationPage
    {
        private int[] score = new int[] { 0,0 };
        private DispatcherTimer timer = new DispatcherTimer();
        private int startTime = 0;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            startTimer();
        }

        private void reset() {
            score[0] = score[1] = 0;
            setScore(0, 0);
            stopTimer();
            startTimer();
        }

        private void startMeasure() {
            startTime = Environment.TickCount;
        }

        private void startTimer()
        {
            Random rand = new Random();
            int interval = rand.Next(1, 10);
            timer.Interval = new TimeSpan(0, 0, 0, interval, 0);
            timer.Tick +=
            delegate(Object s, EventArgs args)
            {
                Random col = new Random();
                Byte[] r = BitConverter.GetBytes(col.Next(999));
                Byte[] g = BitConverter.GetBytes(col.Next(999));
                Byte[] b = BitConverter.GetBytes(col.Next(999));
                canvas1.Background = new SolidColorBrush(Color.FromArgb(255,r[0],g[0],b[0]));
                stopTimer();
                startMeasure();
            };
            timer.Start();
        }

        private void stopTimer()
        {
            timer.Stop();
        }

        private void setScore(int player1, int player2)
        {
            score[0] += player1;
            score[1] += player2;
            player1Score.Text = score[0].ToString();
            player2Score.Text = score[1].ToString();
        }

        private void player2Button_Click(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled)
            { //still running, too early
                setScore(0, -1);
                MessageBox.Show("Too early. Player 2 loses one point.");
            }
            else {
                setScore(0, 1);
                int elapsedTime = Environment.TickCount - startTime;
                player2Time.Text = elapsedTime.ToString();
                MessageBox.Show("Player 2 wins one point.");
            }
            startTimer();
        }

        private void player1Button_Click(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled)
            { //still running, too early
                setScore(-1, 0);
                MessageBox.Show("Too early. Player 1 loses one point.");
            }
            else
            {
                setScore(1, 0);
                int elapsedTime = Environment.TickCount - startTime;
                player1Time.Text = elapsedTime.ToString();
                MessageBox.Show("Player 1 wins one point.");
            }
            startTimer();
        }

        private void button1_Click( object sender, RoutedEventArgs e )
        {
            MessageBox.Show("Press your button as soon as the background changes color. The faster player gets a point. If you press too early, you will lose one point.");
        }
    }
}