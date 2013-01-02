using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace MusicBird.Common
{
    /// <summary>
    /// Value converter that translates true to <see cref="Visibility.Visible"/> and false to
    /// <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return Visibility.Collapsed;
            bool comparer = true;
            if (parameter != null)
            {
                comparer = System.Convert.ToBoolean(parameter);
            }
            return System.Convert.ToBoolean(value) == comparer ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility && (Visibility)value == Visibility.Visible;
        }
    }
}
