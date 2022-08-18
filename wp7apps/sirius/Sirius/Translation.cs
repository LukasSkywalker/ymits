using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Sirius
{
    public class Translation
    {
        private static string baseURL = "http://api.microsofttranslator.com/V2/Ajax.svc/Translate?appId=A34B1552C3B3DF826089895CCA0D868F0A25511E&from={0}&to={1}&text={2}";
        private static WebClient wc = new WebClient();

        public delegate void TranslatedHandler( String result );//ResultParsed
        public static event TranslatedHandler Translated;

        public static Dictionary<string, string> languages = new Dictionary<string, string>();

        static Translation() {
            createLanguageList();
        }

        public static void start( string from, string to, string text )
        {
            String[] data = new String[]{from, to, text};
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);
            wc.OpenReadAsync(new Uri(String.Format(baseURL, data)));
        }

        private static void wc_OpenReadCompleted( object sender, OpenReadCompletedEventArgs e )
        {
            StreamReader reader = new StreamReader(e.Result);
            string result = reader.ReadToEnd();

            result.Replace("\"","");

            System.Diagnostics.Debug.WriteLine("Translation: "+result);

            if(Translated != null) {
                Translated(result);
            }
        }

        private static void createLanguageList()
        {
            languages.Add("Arabic", "ar");
            languages.Add("Bulgarian", "bg");
            languages.Add("Catalan", "ca");
            languages.Add("Chinese Simplified", "zh-CHS");
            languages.Add("Chinese Traditional", "zh-CHT");
            languages.Add("Czech", "cs");
            languages.Add("Danish", "da");
            languages.Add("Dutch", "nl");
            languages.Add("English", "en");
            languages.Add("Estonian", "et");
            languages.Add("Finnish", "fi");
            languages.Add("French", "fr");
            languages.Add("German", "de");
            languages.Add("Greek", "el");
            languages.Add("Haitian Creole", "ht");
            languages.Add("Hebrew", "he");
            languages.Add("Hindi", "hi");
            languages.Add("Hungarian", "hu");
            languages.Add("Indonesian", "id");
            languages.Add("Italian", "it");
            languages.Add("Japanese", "ja");
            languages.Add("Korean", "ko");
            languages.Add("Latvian", "lv");
            languages.Add("Lithuanian", "lt");
            languages.Add("Norwegian", "no");
            languages.Add("Polish", "pl");
            languages.Add("Portuguese", "pt");
            languages.Add("Romanian", "ro");
            languages.Add("Russian", "ru");
            languages.Add("Slovak", "sk");
            languages.Add("Slovenian", "sl");
            languages.Add("Spanish", "es");
            languages.Add("Swedish", "sv");
            languages.Add("Thai", "th");
            languages.Add("Turkish", "tr");
            languages.Add("Ukrainian", "uk");
            languages.Add("Vietnamese", "vi");
        }
    }
}
