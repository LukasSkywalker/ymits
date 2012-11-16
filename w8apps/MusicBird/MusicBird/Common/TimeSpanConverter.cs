using System;
using Windows.UI.Xaml.Data;

namespace MusicBird.Common
{
    /// <summary>
    /// Value converter that translates a <see cref="System.TimeSpan"/> to a string representation
    /// (00:12) and vice-versa.
    /// </summary>
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is double)
            {
                TimeSpan ts = TimeSpan.FromSeconds((double)value);
                return ts.ToString(@"mm\:ss");
            }
            else {
                return "00:00";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            if (value is String)
            {
                TimeSpan ts = TimeSpan.Parse((string)value);
                return ts.TotalSeconds;
            }
            else {
                return TimeSpan.FromSeconds(0);
            }
        }
    }
}
