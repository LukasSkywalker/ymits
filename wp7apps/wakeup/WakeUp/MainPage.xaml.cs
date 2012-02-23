using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace WakeUp
{
    public partial class MainPage : PhoneApplicationPage
    {
        public static bool startup = true;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            if(!startup) {
                textBlock1.Text = "If you still feel tired, tap the screen again. Otherwise, have a nice day!";
            }
        }

        private void button1_Click( object sender, RoutedEventArgs e )
        {
            NavigationService.Navigate(new Uri("/Page1.xaml", UriKind.Relative));
        }

        private void button2_Click( object sender, RoutedEventArgs e )
        {
            MessageBox.Show("Light therapy is an accepted method of scientific medicine to fight various diseases. Depression and the often associated sleep disorders are often treated with it. Blue light with a specific wavelength has an ergotropic effect on the sympathetic nervous system, thus waking you up. Phototherapy light treatment is even used to cure severe damage to the skin such as acne and psoriasis.");
        }
    }
}