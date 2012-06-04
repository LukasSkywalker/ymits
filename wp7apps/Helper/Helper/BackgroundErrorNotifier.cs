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

namespace Helper
{
    public static class BackgroundErrorNotifier
    {
        public static void addError( Exception ex )
        {
            if(ex != null)
            {
                Preferences.write("background-exception", ex.Message);
                System.Diagnostics.Debug.WriteLine("Adding error: " + ex.Message);
            }
            else Preferences.write("background-exception", null);
        }

        public static string getError()
        {
            return Preferences.read("background-exception");
        }
    }
}