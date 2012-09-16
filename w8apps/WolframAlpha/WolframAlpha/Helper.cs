using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

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

        public static void WriteExceptionInfo(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(LevelCounter+": "+ex.Message + " " + ex.HResult);
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

                if (!result.Success) {
                    String msg = "Error:" + result.Errors[0].Code + "; " + result.Errors[0].Message;
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
    }
}
