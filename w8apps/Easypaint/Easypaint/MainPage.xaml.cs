using System;
using Softbuild.Media;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Easypaint
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Windows.Foundation.Collections.IPropertySet appSettings;
        private const String photoKey = "capturedPhoto";

        public WriteableBitmap BaseImage;

        public MainPage()
        {
            this.InitializeComponent();
            appSettings = ApplicationData.Current.LocalSettings.Values;
            CapturePhoto.Visibility = Visibility.Visible;
            CapturePhoto2.Visibility = Visibility.Collapsed;
            ResetButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            CapturePhoto.Visibility = Visibility.Visible;
        }

        private async void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            CapturePhoto.Visibility = Visibility.Collapsed;
            try
            {
                // Using Windows.Media.Capture.CameraCaptureUI API to capture a photo
                CameraCaptureUI dialog = new CameraCaptureUI();
                Size aspectRatio = new Size(16, 9);
                dialog.PhotoSettings.CroppedAspectRatio = aspectRatio;

                StorageFile file = await dialog.CaptureFileAsync(CameraCaptureUIMode.Photo);
                if (file != null)
                {
                    WriteableBitmap bitmapImage = new WriteableBitmap(1600,900);
                    using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        //.SetSource(fileStream);
                        bitmapImage = await Softbuild.Media.WriteableBitmapExtensions.FromStreamAsync(fileStream);
                    }

                    SaveButton.Visibility = Visibility.Visible;
                    ResetButton.Visibility = Visibility.Visible;
                    Filtername.Visibility = Visibility.Visible;
                    CapturePhoto.Visibility = Visibility.Collapsed;

                    CapturedPhoto.Source = bitmapImage;
                    //ResetButton.Visibility = Visibility.Visible;

                    // Store the file path in Application Data
                    appSettings[photoKey] = file.Path;

                    LoadFilters(bitmapImage);
                }
                else
                {
                    CapturePhoto.Visibility = Visibility.Visible;   
                }
            }
            catch (Exception ex)
            {
                
            }
        }

        private async void LoadFilters(WriteableBitmap img) {
            img = CapturedPhoto.Source as WriteableBitmap;
            BaseImage = img;
            List<FilterDisplay> filterList = new List<FilterDisplay>();

            Image imgNone = new Image();
            imgNone.Source = img;
            filterList.Add(new FilterDisplay("None", imgNone));

            Image imgGrayscale = new Image();
            imgGrayscale.Source = img.EffectGrayscale();
            filterList.Add(new FilterDisplay("Grayscale", imgGrayscale));

            Image imgNegative = new Image();
            imgNegative.Source = img.EffectNegative();
            filterList.Add(new FilterDisplay("Negative", imgNegative));

            Image imgSaturation = new Image();
            imgSaturation.Source = img.EffectSaturation(1.0);
            filterList.Add(new FilterDisplay("Saturation", imgSaturation, true, "d", 1.0));

            Image imgContrast = new Image();
            imgContrast.Source = img.EffectContrast(1.0);
            filterList.Add(new FilterDisplay("Contrast", imgContrast, true, "d", 1.0));

            /*Image imgVignetting = new Image();
            imgVignetting.Source = await img.EffectVignettingAsync(1.0);
            filterList.Add(new FilterDisplay("Vignetting", imgVignetting, true, "d", 1.0));*/

            Image imgSepia = new Image();
            imgSepia.Source = img.EffectSepia();
            filterList.Add(new FilterDisplay("Sepia", imgSepia));

            /*Image imgPosterize = new Image();
            imgPosterize.Source = img.EffectPosterize(10);
            filterList.Add(new FilterDisplay("Posterize", imgPosterize, true, "b", 150));*/

            Image imgBaku = new Image();
            imgBaku.Source = await img.EffectBakumatsuAsync();
            filterList.Add(new FilterDisplay("Bakumatu", imgBaku));

            Image imgToycam = new Image();
            imgToycam.Source = await img.EffectToycameraAsync();
            filterList.Add(new FilterDisplay("Toy Camera", imgToycam));

            FilterList.ItemsSource = filterList;
            FilterList.SelectedIndex = 0;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetButton.Visibility = Visibility.Collapsed;
            Filtername.Visibility = Visibility.Collapsed;
            CapturePhoto.Visibility = Visibility.Collapsed;
            CapturePhoto2.Visibility = Visibility.Visible;

            // Clear file path in Application Data
            appSettings.Remove(photoKey);

            CapturedPhoto.Source = ((FilterList.ItemsSource) as List<FilterDisplay>)[0].Image;
        }

        public class FilterDisplay {
            public String Title { get; set; }
            public ImageSource Image { get; set; }
            public Boolean Slider { get; set; }
            public String Type { get; set; }
            //public object Val { get; set; }

            public FilterDisplay(String name, Image image) {
                this.Title = name;
                this.Image = image.Source;
                this.Slider = false;
            }

            public FilterDisplay(String name, Image image, Boolean slider, String type, object val) {
                this.Title = name;
                this.Image = image.Source;
                this.Slider = slider;
                this.Type = type;
                //this.Val = val;
            }
        }

        private void FilterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterDisplay currentSelection = FilterList.SelectedItem as FilterDisplay;

            if (currentSelection.Slider)
            {
                SettingsSlider.Visibility = Visibility.Visible;
                switch (currentSelection.Title){
                    case "Saturation":
                        SettingsSlider.Maximum = 3.0;
                        SettingsSlider.Minimum = 0.0;
                        SettingsSlider.StepFrequency = 0.1;
                        SettingsSlider.Value = 1.0;
                        //SettingsSlider.Value = (double)currentSelection.Val;
                        SettingsSlider.ValueChanged += (s, ev) => {
                            Image temp = new Image();
                            System.Diagnostics.Debug.WriteLine("Sat:"+ev.NewValue);
                            CapturedPhoto.Source = BaseImage.EffectSaturation(SettingsSlider.Value);
                            //currentSelection = new FilterDisplay("Saturation", temp, true, "d", ev.NewValue);
                        };
                        break;
                    case "Contrast":
                        SettingsSlider.Maximum = 3.0;
                        SettingsSlider.Minimum = 0.0;
                        SettingsSlider.StepFrequency = 0.1;
                        SettingsSlider.Value = 1.0;
                        //SettingsSlider.Value = (double)currentSelection.Val;
                        SettingsSlider.ValueChanged += (s, ev) =>
                        {
                            Image temp = new Image();
                            System.Diagnostics.Debug.WriteLine("Con:" + ev.NewValue);
                            CapturedPhoto.Source = BaseImage.EffectContrast(SettingsSlider.Value);
                            //currentSelection = new FilterDisplay("Saturation", temp, true, "d", ev.NewValue);
                        };
                        break;
                    case "Vignetting":
                        SettingsSlider.Maximum = 3.0;
                        SettingsSlider.Minimum = 0.0;
                        SettingsSlider.StepFrequency = 0.5;
                        SettingsSlider.Value = 1.0;
                        //SettingsSlider.Value = (double)currentSelection.Val;
                        SettingsSlider.ValueChanged += async (s, ev) =>
                        {
                            Image temp = new Image();
                            System.Diagnostics.Debug.WriteLine("Vig:" + ev.NewValue);
                            CapturedPhoto.Source = await BaseImage.EffectVignettingAsync(0.5);
                            //currentSelection = new FilterDisplay("Saturation", temp, true, "d", ev.NewValue);
                        };
                        break;
                    case "Posterize":
                        SettingsSlider.Maximum = 255;
                        SettingsSlider.Minimum = 0;
                        SettingsSlider.StepFrequency = 1;
                        SettingsSlider.Value = 1;
                        //SettingsSlider.Value = (double)currentSelection.Val;
                        SettingsSlider.ValueChanged += (s, ev) =>
                        {
                            Image temp = new Image();
                            System.Diagnostics.Debug.WriteLine("Post:" + ev.NewValue);
                            CapturedPhoto.Source = BaseImage.EffectPosterize(1);
                            //currentSelection = new FilterDisplay("Saturation", temp, true, "d", ev.NewValue);
                        };
                        break;
                    //3 Saturation 4 Contrast 5 Vignetting . 7 Posterize
                    
                }
            }
            else
            {
                SettingsSlider.Visibility = Visibility.Collapsed;
            }

            Filtername.Text = currentSelection.Title;
            CapturedPhoto.Source = currentSelection.Image;
            
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ImageSource img = CapturedPhoto.Source;
            var bitmap = CapturedPhoto.Source as WriteableBitmap;
            string format = "yyyy-MM-dd HH.mm.ss";
            await bitmap.SaveAsync(ImageFormat.Jpeg, ImageDirectories.PicturesLibrary, "InstaTon " + DateTime.Today.ToString(format), (uint)BaseImage.PixelWidth, (uint)BaseImage.PixelHeight);
            MessageDialog md = new MessageDialog("Image has been saved to your Picture Library as 'InstaTon " + DateTime.Now.ToString(format)+".jpg'", "Complete");
            await md.ShowAsync();
        }
    }
}
