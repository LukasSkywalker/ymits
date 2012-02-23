using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace MakeMyDecision
{
    public partial class MainPage : PhoneApplicationPage
    {
        Random rand;
        int counter = 0;
        DispatcherTimer timer;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            rand = new Random();
        }

        private void button1_Click( object sender, RoutedEventArgs e )
        {
            // yes-no
            int value = rand.Next(2);
            if(value == 0)
            {
                textBlock3.Text = "Yes";
            }
            else {
                textBlock3.Text = "No";
            }
        }

        private void button2_Click( object sender, RoutedEventArgs e )
        {
            // left-right
            int value = rand.Next(2);
            if(value == 0)
            {
                textBlock3.Text = "Left";
            }
            else
            {
                textBlock3.Text = "Right";
            }
        }

        private void button3_Click( object sender, RoutedEventArgs e )
        {
            // yes-no
            int min = Convert.ToInt32(textBox1.Text);
            int max = Convert.ToInt32(textBox2.Text)+1;
            int lower = Math.Min(min, max);
            int higher = Math.Max(min, max);
            int value = rand.Next(lower, higher);
            textBlock3.Text = value.ToString();
            
        }

        private void button4_Click( object sender, RoutedEventArgs e )
        {
            if(counter < 10) {
                counter++;
            }else{
                button5.Visibility = Visibility.Visible;
            }
        }

        private void button5_Click( object sender, RoutedEventArgs e )
        {
            textBlock3.Text = "Burn it.\nEat it.";
        }
    }
}