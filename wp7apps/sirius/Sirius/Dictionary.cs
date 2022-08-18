using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sirius
{
    public class Dictionary
    {
        public delegate void ResultParsedHandler( parseResult result );//ResultParsed
        public event ResultParsedHandler ResultParsed;


        private static Dictionary<string, string> actionDictionary = new Dictionary<string, string>();
        private static Dictionary<string, string> dateDictionary = new Dictionary<string, string>();

        static Dictionary()
        {
            actionDictionary.Add("calendar", "calendar");
            actionDictionary.Add("to do", "calendar");
            actionDictionary.Add("todo", "calendar");
            actionDictionary.Add("action", "calendar");
            actionDictionary.Add("task", "calendar");
            actionDictionary.Add("event", "calendar");
            
            actionDictionary.Add("weather", "weather");
            actionDictionary.Add("temperature", "weather");
            actionDictionary.Add("rain", "weather");
            actionDictionary.Add("snow", "weather");
            actionDictionary.Add("sun", "weather");
            actionDictionary.Add("cold", "weather");
            actionDictionary.Add("warm", "weather");
            actionDictionary.Add("outside", "weather");

            actionDictionary.Add("map", "map");
            actionDictionary.Add("location", "map");
            actionDictionary.Add("where", "map");

            dateDictionary.Add("today", "currentDay");
            dateDictionary.Add("now", "currentDay");
            dateDictionary.Add("tomorrow", "nextDay");
            dateDictionary.Add("this week", "currentWeek");
            dateDictionary.Add("weekend", "weekEnd");
            
        }

        public void getActionAndTime(String sentence){
            String[] words = sentence.Split((" ").ToCharArray());
            Regex translationPattern = new Regex("^translate (.*?) to (.*?)$", RegexOptions.IgnoreCase);

            String action = "";
            String time = "today";
            String data = "";

            Match m = translationPattern.Match(sentence);
            if(m.Success)
            {
                action = "translate";
                time = m.Groups[2].ToString();
                data = m.Groups[1].ToString();

                parseResult res = new parseResult(action, time, data);

                if(ResultParsed != null) {
                    ResultParsed(res);
                }
                return;
            }
            
            //loop through keys and try to match...
            foreach(String word in words)
            {
                //System.Diagnostics.Debug.WriteLine("Searching key: " + word);
                foreach(String key in actionDictionary.Keys)
                {
                    if(LevenshteinDistance.Compute(word, key) < word.Length/2)
                    {
                        System.Diagnostics.Debug.WriteLine("Action found: " + key);
                        String tempAction = "";
                        if(actionDictionary.TryGetValue(key, out tempAction))
                        {
                            action = tempAction;
                        }
                    }
                }

                foreach(String key in dateDictionary.Keys)
                {
                    if(LevenshteinDistance.Compute(word, key) < word.Length / 2)
                    {
                        System.Diagnostics.Debug.WriteLine("Time found: " + key);
                        String tempTime = "";
                        if(actionDictionary.TryGetValue(key, out tempTime))
                        {
                            time = tempTime;
                        }
                    }
                }
            }

            if(ResultParsed != null) {
                parseResult result = new parseResult(action, time, data);
                ResultParsed(result);
            }
        }
    }

    public class parseResult {
        public string action { get; set; }
        public string time { get; set; }
        public string data { get; set; }

        public parseResult( String action, String time )
        {
            this.action = action;
            this.data = null;
            this.time = time;
        }
        
        public parseResult( String action, String time, String data ) {
            this.action = action;
            this.data = data;
            this.time = time;
        }
    }

    static class LevenshteinDistance
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute( string s, string t )
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if(n == 0)
            {
                return m;
            }

            if(m == 0)
            {
                return n;
            }

            // Step 2
            for(int i = 0 ; i <= n ; d[i, 0] = i++)
            {
            }

            for(int j = 0 ; j <= m ; d[0, j] = j++)
            {
            }

            // Step 3
            for(int i = 1 ; i <= n ; i++)
            {
                //Step 4
                for(int j = 1 ; j <= m ; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}