using System;
using Windows.UI.Xaml.Data;

namespace MusicBird.Common
{

    /// <summary>
    /// Value converter that translates true to false and vice versa.
    /// </summary>
    /// 
    public sealed class PlayAccessibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return "Play " + (string)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((string)value).Replace("Play ", "");
        }
    }
}
