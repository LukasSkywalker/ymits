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
            long bytes = System.Convert.ToInt64(value);
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB" };
            int place = (int)(Math.Floor(Math.Log(bytes, 1024)));
            if (place < 0) place = 0;
            if (place > 5) place = 5;
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return String.Format("{0:0.##} {1}", num, suf[place]);
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