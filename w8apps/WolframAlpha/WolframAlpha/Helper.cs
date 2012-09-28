using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.UI.Popups;

namespace WolframAlpha
{
    public class Helper
    {
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
                String filename = file.Substring(file.LastIndexOf("\\")+1);
                String line = h.ToString();
                System.Diagnostics.Debug.WriteLine(filename + ".cs : line " + line);
                m = m.NextMatch();
            }
            
            if (!found) System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            LevelCounter++;
        }

        public static async Task<String> GetResultAsync(String URL)
        {
            String result = "";

            HttpClient searchClient = new HttpClient();
            searchClient.CancelPendingRequests();
            HttpResponseMessage response = await searchClient.GetAsync(URL);
            response.EnsureSuccessStatusCode();
            result = await response.Content.ReadAsStringAsync();

            return result;
        }

        public static QueryResult ParseResult(String src)
        {
            try
            {
                XmlSerializer des = new XmlSerializer(typeof(QueryResult));
                var reader = new StringReader(src);
                var result = (QueryResult)des.Deserialize(reader);

                System.Diagnostics.Debug.WriteLine("Success:" + result.Success);

                if (!result.Success && result.Errors != null && result.Errors.Count > 0)
                {
                    String msg = "Error:" + result.Errors[0].Code + "; " + result.Errors[0].Message;
                    System.Diagnostics.Debug.WriteLine(msg);
                }
                else {
                    String msg = "Unknown error occured. Please try again.";
                    System.Diagnostics.Debug.WriteLine(msg);
                }

                return result;
            }
            catch (Exception ex)
            {
                Helper.DumpException(ex);
                return new QueryResult();
            }
        }

        public static async void DisplayErrors(QueryResult QueryResult) {
            if (QueryResult.Errors != null)
            {
                foreach (Error Error in QueryResult.Errors)
                {
                    MessageDialog md = new MessageDialog(Error.Code + " " + Error.Message, "Error");
                    await md.ShowAsync();
                }
           }
        }

        public static async void DisplayWarnings(QueryResult QueryResult) {
            if (QueryResult.Warnings != null)
            {
                foreach (Warning Warning in QueryResult.Warnings)
                {
                    MessageDialog md = new MessageDialog("Something bad happened. But we don't know what, so just go on.", "Warning");
                    if (Warning.Spellcheck != null)
                    {
                        md = new MessageDialog(Warning.Spellcheck[0].Text, "Warning");
                    }
                    if (Warning.Delimiters != null)
                    {
                        md = new MessageDialog(Warning.Delimiters[0].Text, "Warning");
                    }
                    if (Warning.Reinterpret != null)
                    {
                        md = new MessageDialog(Warning.Reinterpret[0].Text + " " + Warning.Reinterpret[0].New, "Warning");
                    }
                    if (Warning.Translation != null)
                    {
                        md = new MessageDialog(Warning.Translation[0].Phrase + ": " + Warning.Translation[0].Text, "Warning");
                    }
                    await md.ShowAsync();
                }
            }
        }
    }
}
