using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MusicBird.Common
{
    /// <summary>
    /// Value converter that translates true to false and vice versa.
    /// </summary>
    public sealed class NBSpaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) { return null; }
            else
            {
                string str = ((string)value).Replace("&#00a0", "\x00a0");
                return str;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null) { return null; }
            else
            {
                string str = ((string)value).Replace("\x00a0", "&#00a0");
                return str;
            }
        }
    }
}
