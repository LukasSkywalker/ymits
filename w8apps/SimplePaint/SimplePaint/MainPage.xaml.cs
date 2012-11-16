
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SimplePaint
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const double MAX_VALUE_EXTENT = 2.0;
        const double MAX_NORM = MAX_VALUE_EXTENT * MAX_VALUE_EXTENT;
        private Windows.UI.Core.CoreDispatcher dispatcher;
        private Color myColor;
        

        public MainPage()
        {
            this.InitializeComponent();
            this.dispatcher = Window.Current.CoreWindow.Dispatcher;
            RSlider.Value = 250;
            GSlider.Value = 120;
            BSlider.Value = 0;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String val = (string)FractalSelector.SelectedValue;
            GenerateFractal(val);
        }

        private async void GenerateFractal(String name)
        {
            int width = (int)(FractalPanel.Width);
            int height = (int)(FractalPanel.Height);

            byte[] buffer = new byte[width*height*4];

            double scale = 2 * MAX_VALUE_EXTENT / Math.Min(width, height);
            for (int i = 0; i < height; i++)
            {
                await dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    double prog = (double)i / (double)height;
                    Progress.Value = prog;
                    ProgressText.Text = ((int)(prog * 100)).ToString()+"%";
                });
                double y = (height / 2 - i) * scale;
                for (int j = 0; j < width; j++)
                {
                    double x = (j - width / 2) * scale;
                    double color = 0;
                    if (name == "Mandelbrot") color = CalcMandelbrotSetColor(new ComplexNumber(x, y));
                    if (name == "Julia") color = CalcJuliaSetColor(new ComplexNumber(x, y));
                    else color = 0;
                    int pos = 4 * j + 4 * width * i;
                    buffer[pos] = (byte)(Convert.ToInt32(myColor.B) * color);      //B
                    buffer[pos + 1] = (byte)(Convert.ToInt32(myColor.G) * color);  //G
                    buffer[pos + 2] = (byte)(Convert.ToInt32(myColor.R) * color);  //R
                    buffer[pos + 3] = 255;                  //A
                }
            }
            WriteableBitmap bitmap = new WriteableBitmap(width, height);
            bitmap.FromByteArray(buffer);
            FractalPanel.Source = bitmap;
        }

        private double CalcMandelbrotSetColor(ComplexNumber c)
        {
            const int MAX_ITERATIONS = 50;
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
            const int MAX_ITERATIONS = 50;
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
