using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;

namespace Sirius
{
    public static class Weather
    {
        private static  WebClient wc = new WebClient();
        private static string requestURL = "http://www.google.com/ig/api?weather={0}";

        public delegate void WeatherReceivedHandler( List<WeatherData> result );//ResultParsed
        public static event WeatherReceivedHandler WeatherReceived;

        public static void getWeather( string location )
        {
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);
            wc.OpenReadAsync(new Uri(String.Format(requestURL, location)));
            System.Diagnostics.Debug.WriteLine(String.Format(requestURL, location));
        }

        private static void wc_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e )
        {
            System.Diagnostics.Debug.WriteLine("OpenReadCompleted");
            // You should always check to see if an error occurred. In this case, the application
            // simply returns.
            if(e.Error != null)
            {
                System.Diagnostics.Debug.WriteLine("Error");
                return;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Reading data");
                String xmlString = e.Result.ToString();

                //using(String s = e.Result.ToString())
                //{
                XElement xml = XElement.Load(e.Result, LoadOptions.None);
                    IEnumerable<XElement> weather = xml.Elements("weather");
                    foreach(XElement weat in weather){
                        System.Diagnostics.Debug.WriteLine("Reading weathers");
                        IEnumerable<XElement> foreCastInfo = weat.Elements("forecast_information");
                        foreach(XElement info in foreCastInfo){
                            IEnumerable<XElement> data = info.Elements("city");
                            foreach(XElement dat in data){
                                System.Diagnostics.Debug.WriteLine(dat.FirstAttribute.Value.ToString());
                            }
                        }

                        IEnumerable<XElement> currentConditions = weat.Elements("current_conditions");
                        foreach(XElement currentCondition in currentConditions){
                            System.Diagnostics.Debug.WriteLine("Reading current");
                            IEnumerable<XElement> data = currentCondition.Elements();
                        }

                        IEnumerable<XElement> forecastConditions = weat.Elements("forecast_conditions");
                        List<WeatherData> forecast = new List<WeatherData>();
                        foreach(XElement forecastCondition in forecastConditions) {
                            System.Diagnostics.Debug.WriteLine("Reading forecasts");
                            IEnumerable<XElement> data = forecastCondition.Elements();
                            List<String> values = new List<String>();
                            foreach(XElement dat in data) {
                                values.Add(dat.FirstAttribute.Value.ToString());
                            }
                            String day = values[0];
                            System.Diagnostics.Debug.WriteLine(values[1]);
                            int min = Convert.ToInt32(values[1]);
                            int max = Convert.ToInt32(values[2]);
                            string icon = values[3];
                            string condition  = values[4];
                            WeatherData fc = new WeatherData(day,min,max,icon,condition);
                            forecast.Add(fc);
                            System.Diagnostics.Debug.WriteLine("Read data: "+fc.ToString());
                        }
                        if(WeatherReceived != null) {
                            WeatherReceived(forecast);
                        }
                    }
                //}
            }
        }
    }

    public class WeatherData {
        public string day;
        public int min;
        public int max;
        public string icon;
        public string condition;
        public WeatherData(string day, int min, int max, string icon, string condition) {
            this.day = day;
            this.min = min;
            this.max = max;
            this.icon = icon;
            this.condition = condition;
        }

        public string ToString() {
            String str = "Day: {0}, Temp min: {1}, max: {2}, icon: {3}, condition: {4}.";
            String[] values = new String[]{day, min.ToString(), max.ToString(), icon, condition};
            str = String.Format(str, values);
            return str;
        }
    }
}
