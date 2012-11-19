
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Softbuild.Media;
using Windows.UI.Popups;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SimplePaint
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const double MAX_VALUE_EXTENT = 1.5;
        const double MAX_NORM = MAX_VALUE_EXTENT * MAX_VALUE_EXTENT;
        private Windows.UI.Core.CoreDispatcher dispatcher;
        private Color myColor;
        private WriteableBitmap bitmap;
        //DataTransferManager dataTransferManager;

        public MainPage()
        {
            this.InitializeComponent();
            this.dispatcher = Window.Current.CoreWindow.Dispatcher;
            //this.dataTransferManager = DataTransferManager.GetForCurrentView();
            //dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareImageHandler);
            RSlider.Value = 255;
            GSlider.Value = 120;
            BSlider.Value = 0;

            SaveButton.Visibility = Visibility.Collapsed;
            RedrawButton.Visibility = Visibility.Collapsed;
            SizeWarning.Visibility = Visibility.Collapsed;
            CPUWarning.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Smoothness.Items.Clear();
            for (int i = 5; i < 256; i += 5)
                Smoothness.Items.Add(i);
            Smoothness.SelectedIndex = 10;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String val = (string)FractalSelector.SelectedValue;
            GenerateFractal(val);
        }

        private async void GenerateFractal(String name)
        {
            bool showInvalidSize = false;

            int width = 1024;
            try{ width = Convert.ToInt32(ImageWidthBox.Text); }
            catch (FormatException ex) { showInvalidSize = true; }
            
            int height = 768;
            try { height = Convert.ToInt32(ImageHeightBox.Text); }
            catch (FormatException ex) { showInvalidSize = true; }

            if (showInvalidSize)
            {
                MessageDialog md = new MessageDialog("Invalid size, using 1024x768 pixels"); await md.ShowAsync();
            }

            byte[] buffer = new byte[width*height*4];

            double scale = 2 * MAX_VALUE_EXTENT / Math.Min(width, height);
            for (int i = 0; i < height; i++)
            {
                await dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    double prog = (double)i / (double)height;
                    Progress.Value = prog;
                    //ProgressText.Text = ((int)(prog * 100)).ToString()+"%";
                });
                double y = (height / 2 - i) * scale;
                for (int j = 0; j < width; j++)
                {
                    double x = (j - width / 2) * scale;
                    double color = 0;
                    if (name == "Mandelbrot") color = CalcMandelbrotSetColor(new ComplexNumber(x, y));
                    else if (name == "Julia") color = CalcJuliaSetColor(new ComplexNumber(x, y));
                    else color = 0;
                    int pos = 4 * j + 4 * width * i;
                    buffer[pos] = (byte)(Convert.ToInt32(myColor.B) * color);      //B
                    buffer[pos + 1] = (byte)(Convert.ToInt32(myColor.G) * color);  //G
                    buffer[pos + 2] = (byte)(Convert.ToInt32(myColor.R) * color);  //R
                    buffer[pos + 3] = 255;                  //A
                }
            }
            bitmap = new WriteableBitmap(width, height);
            bitmap.FromByteArray(buffer);
            FractalPanel.Source = bitmap;
            SaveButton.Visibility = Visibility.Visible;
            RedrawButton.Visibility = Visibility.Visible;
        }

        private double CalcMandelbrotSetColor(ComplexNumber c)
        {
            int MAX_ITERATIONS = (int)Smoothness.SelectedValue;
            int iteration = 0;
            ComplexNumber z = new ComplexNumber(0, 0);
            do
            {
                z = z * z + c;
                iteration++;
            } while (z.Norm() < MAX_NORM && iteration < MAX_ITERATIONS);
            if (iteration < MAX_ITERATIONS)
                return (double)iteration / MAX_ITERATIONS;
            else
                return 0;
        }

        private double CalcJuliaSetColor(ComplexNumber z) {
            int MAX_ITERATIONS = (int)Smoothness.SelectedValue;
            int iteration = 0;
            ComplexNumber c = new ComplexNumber(-1.125, 0.25);
            do
            {
                z = z * z + c;
                iteration++;
            } while (z.Norm() < MAX_NORM && iteration < MAX_ITERATIONS);
            if (iteration < MAX_ITERATIONS)
                return (double)iteration / MAX_ITERATIONS;
            else
                return 0;
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            byte R, G, B, A;

            A = 255;
            R = Convert.ToByte(RSlider.Value);
            G = Convert.ToByte(GSlider.Value);
            B = Convert.ToByte(BSlider.Value);

            myColor = Color.FromArgb(A, R, G, B);

            showColor.Fill = new SolidColorBrush(myColor);
        }

        private async void SaveButton_Click_1(object sender, RoutedEventArgs e)
        {
            String filename = "EasyFractal_" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")+".png";

            await bitmap.SaveAsync(ImageDirectories.PicturesLibrary, ImageFormat.Png, filename, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight);

            MessageDialog md = new MessageDialog("Image was saved to your pictures as " + filename, "Saved");
            await md.ShowAsync();
        }

        private void RedrawButton_Click_1(object sender, RoutedEventArgs e)
        {
            String val = (string)FractalSelector.SelectedValue;
            GenerateFractal(val);
        }

        private void SizeBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            try
            {
                if (Convert.ToInt32(ImageHeightBox.Text) > FractalPanel.Height || Convert.ToInt32(ImageWidthBox.Text) > FractalPanel.Width)
                    SizeWarning.Visibility = Visibility.Visible;
                else
                    SizeWarning.Visibility = Visibility.Collapsed;
                if (Convert.ToInt32(ImageHeightBox.Text) > 3500 || Convert.ToInt32(ImageWidthBox.Text) > 5000)
                    CPUWarning.Visibility = Visibility.Visible;
                else
                    CPUWarning.Visibility = Visibility.Collapsed;
            }
            catch (FormatException ex) {
                /*MessageDialog md = new MessageDialog("Invalid size");
                md.ShowAsync();*/
            }
        }

        /*private async void SaveButton_Click_1(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();

            
        }

        private async void ShareImageHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = "EasyFractals";
            request.Data.Properties.Description = "A beautiful fractal";

            InMemoryRandomAccessStream  ras = new InMemoryRandomAccessStream();
            using (Stream stream = await bitmap.PixelBuffer.OpenStreamForReadAsync())
            {
                await stream.CopyToAsync(ras.AsStreamForWrite());
            }

            request.Data.Properties.Thumbnail = bitmap.PixelBuffer.
        }*/
    }

    struct ComplexNumber
    {
        public double Re;
        public double Im;

        public ComplexNumber(double re, double im)
        {
            this.Re = re;
            this.Im = im;
        }

        public static ComplexNumber operator +(ComplexNumber x, ComplexNumber y)
        {
            return new ComplexNumber(x.Re + y.Re, x.Im + y.Im);
        }

        public static ComplexNumber operator *(ComplexNumber x, ComplexNumber y)
        {
            return new ComplexNumber(x.Re * y.Re - x.Im * y.Im,
                x.Re * y.Im + x.Im * y.Re);
        }

        public double Norm()
        {
            return Re * Re + Im * Im;
        }
    }
}
