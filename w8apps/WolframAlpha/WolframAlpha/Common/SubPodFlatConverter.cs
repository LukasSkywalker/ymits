using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace WolframAlpha.Common
{
    /// <summary>
    /// Value converter that translates true to <see cref="Visibility.Visible"/> and false to
    /// <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public sealed class SubPodFlatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            QueryResult qr = value as QueryResult;
            List<SubPod> spl = new List<SubPod>();
            for (int i = 0; i < qr.Pods.Length; i++ )
            {
                Pod Pod = qr.Pods[i];
                if (Pod.Error) continue;
                foreach (SubPod SubPod in Pod.SubPods)
                {
                    //SubPod.ImageSource = SubPod.Image.Src;
                    SubPod.Title = Pod.Title;
                    SubPod.States = Pod.States;
                    spl.Add(SubPod);
                }
            }
            return spl;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
