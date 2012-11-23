using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace AnnotatePDF
{
    public sealed partial class StartPage : Page
    {
        public StartPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BaseFont bfHelvetica = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);
            Font helvetica = new Font(bfHelvetica, 12, Font.NORMAL, iTextSharp.text.BaseColor.RED);

            Document doc = new Document();

            PdfWriter.GetInstance(doc, new FileStream(path + "/Font.pdf", FileMode.Create));
            doc.Open();
            doc.Add(new Paragraph("This is a Red Font Test using Times Roman", times));
            doc.Close();
        }
    }
}
