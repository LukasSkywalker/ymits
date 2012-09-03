using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MusicBird.Common
{
    /// <summary>
    /// Value converter that translates true to <see cref="Visibility.Visible"/> and false to
    /// <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public sealed class BytesMegabytesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Int32)
            {
                int val = (Int32)value;
                double d = val / 1024 / 1024;
                decimal rounded = Math.Round((decimal)d, 2);
                string text = rounded.ToString("0 MB");
                return text;
            }
            else {
                return "0 MB";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string) {
                string numeric = ((string)value).Replace(" MB", "");
                double dbl = double.Parse(numeric);
                return dbl*1024*1024;
            } else {
                return 0.0;
            }
        }
    }
}