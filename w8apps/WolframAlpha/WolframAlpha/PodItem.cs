using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace WolframAlpha
{
    public class PodItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
		public ImageSource Image { get; set; }
		public string Subtitle { get; set; }

        public PodItem(String title, String subTitle, String description, ImageSource imageSource)
        {
            this.Title = title;
            this.Subtitle = subTitle;
            this.Description = description;
            this.Image = imageSource;
        }
    }
}
