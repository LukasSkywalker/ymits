using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace CobaltGamma.Common
{
    /// <summary>
    /// Value converter that translates true to <see cref="Visibility.Visible"/> and false to
    /// <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public sealed class DateTimeConverter : IValueConverter
    {
        private bool USFormat = false;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime dt = (DateTime)value;
            if(!USFormat)
                return String.Format("{0:d/M/yy HH:mm}", dt);
            else
                return String.Format("{0:g}",dt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            String dt = (String)value;
            return DateTime.Parse(dt);
        }
    }
}
