using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace CobaltGamma.Common
{
    public sealed class LengthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return Visibility.Collapsed;
            try
            {
                int length = ((ObservableCollection<Assumption>)value).Count;
                if (length > 0) return Visibility.Visible;
                else return Visibility.Collapsed;
            }catch(Exception e){
                Helper.DumpException(e);
                try
                {
                    int length = ((ObservableCollection<Source>)value).Count;
                    if (length > 0) return Visibility.Visible;
                    else return Visibility.Collapsed;
                }
                catch (Exception ex) {
                    Helper.DumpException(ex);

                    try
                    {
                        int length = ((ObservableCollection<Info>)value).Count;
                        if (length > 0) return Visibility.Visible;
                        else return Visibility.Collapsed;
                    }
                    catch (Exception exc)
                    {
                        Helper.DumpException(exc);
                    }
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
