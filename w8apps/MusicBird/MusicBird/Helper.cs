using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace MusicBird
{
    class Helper
    {
        public static string GetHresultFromErrorMessage(ExceptionRoutedEventArgs e)
        {
            String hr = String.Empty;
            String token = "HRESULT - ";
            const int hrLength = 10;     // eg "0xFFFFFFFF"

            int tokenPos = e.ErrorMessage.IndexOf(token, StringComparison.Ordinal);
            if (tokenPos != -1)
            {
                hr = e.ErrorMessage.Substring(tokenPos + token.Length, hrLength);
            }
            return hr;
        }

        public static double GetSliderFrequency(TimeSpan timevalue)
        {
            double stepfrequency = -1;

            double absvalue = (int)Math.Round(
                timevalue.TotalSeconds, MidpointRounding.AwayFromZero);

            stepfrequency = (int)(Math.Round(absvalue / 100));

            if (timevalue.TotalMinutes >= 10 && timevalue.TotalMinutes < 30)
            {
                stepfrequency = 10;
            }
            else if (timevalue.TotalMinutes >= 30 && timevalue.TotalMinutes < 60)
            {
                stepfrequency = 30;
            }
            else if (timevalue.TotalHours >= 1)
            {
                stepfrequency = 60;
            }

            if (stepfrequency == 0) stepfrequency += 1;

            if (stepfrequency == 1)
            {
                stepfrequency = absvalue / 100;
            }

            return stepfrequency;
        }

        public static Style GetStyle(string key)
        {
            return Application.Current.Resources[key] as Style;
        }

        private static int LevelCounter = 0;

        public static void DumpException(Exception ex)
        {
            WriteExceptionInfo(ex);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                WriteExceptionInfo(ex);
            }
            LevelCounter = 0;
        }

        private static void WriteExceptionInfo(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(LevelCounter + ": " + ex.Message + " " + ex.HResult);

            //at WolframAlpha.Helper.ParseResult(String src) in e:[...]\Helper.cs:line 57
            Regex pattern = new Regex("in (.*?)\\.cs\\:line (.*?)$", RegexOptions.IgnoreCase);
            Match m = pattern.Match(ex.StackTrace);
            bool found = false;
            while (m.Success)
            {
                found = true;
                Group g = m.Groups[1];
                Group h = m.Groups[2];
                String file = g.ToString();
                String filename = file.Substring(file.LastIndexOf("\\") + 1);
                String line = h.ToString();
                System.Diagnostics.Debug.WriteLine(filename + ".cs : line " + line);
                m = m.NextMatch();
            }

            if (!found) System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            LevelCounter++;
        }
    }
}
