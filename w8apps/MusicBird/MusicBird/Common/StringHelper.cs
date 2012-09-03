using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MusicBird.Common
{
    public class StringHelper
    {
        public static string[] getArtistAndTitle(string name)
        {
            String artist = "";
            String title = "";

            replaceNumbersAndExtension(name);

            name = WebUtility.HtmlDecode(name);

            name = name.Trim();

            int count = name.Length - name.Replace("-", "").Length + 1;     // count parts of string separated by dash

            if (count == 1)
            {                                           // John Wayne Heaven
                int len = name.Split((char)32).Length;                  // split at space
                if (len == 1)
                {                                         // Ex. JohnWayneHeaven
                    artist = name;
                    title = "";
                }
                else if (len > 1)                                      // Ex. JohnWayne Heaven or John Wayne Heaven
                {
                    string[] nameArray = name.Split((char)32);
                    for (int i = 0; i < len; i++)
                    {
                        if (i < len / 2)
                        {
                            artist += nameArray[i];
                        }
                        else
                        {
                            title += nameArray[i];
                        }
                    }
                }
            }
            else if (count == 2)
            {                                      // John Wayne - Heaven
                artist = name.Split((char)45)[0];
                title = name.Split((char)45)[1];
            }
            else if (count > 2)
            {                                        // John Wayne - Heaven - Live at Brixton Academy
                string[] parts = name.Split((char)45);
                int len = parts.Length;
                for (int i = 0; i < len; i++)
                {
                    if (i < len / 2)
                    {
                        artist += parts[i];
                    }
                    else
                    {
                        title += parts[i];
                    }
                }
            }

            artist = artist.Replace("_", " ");
            title = title.Replace("_", " ");

            artist = UppercaseWords(artist);
            title = UppercaseWords(title);

            artist = artist.Trim();
            title = title.Trim();

            artist = replaceNumbersAndExtension(artist);
            title = replaceNumbersAndExtension(title);

            artist = artist.Trim();
            title = title.Trim();

            if (artist.IndexOf(", The") == artist.Length - 5)
            {
                artist = "The " + artist.Substring(0, artist.Length - 5);
            }

            return new String[] { artist, title };
        }

        public static string replaceNumbersAndExtension(string name)
        {
            string pattern = "^[0-9]{1,3}?\\.(\\s+)?";                     // replace leading track number (01. or 1. or 12. )
            Regex rgx = new Regex(pattern);
            name = rgx.Replace(name, "");
            name = name.Trim();

            string pattern2 = "[0-9]{1,3}?[\\s]*-(\\s+)?";                 // replace leading track number (01- or 1 - or 12- )
            Regex rgx2 = new Regex(pattern2);
            name = rgx2.Replace(name, "");
            name = name.Trim();

            string pattern3 = "(\\.wma|\\.mp3|\\.mid)";                        // replace file ext. (.mp3)
            Regex rgx3 = new Regex(pattern3, RegexOptions.IgnoreCase);
            name = rgx3.Replace(name, "");
            name = name.Trim();

            string pattern4 = "\\([0-9]+[|\\.|-]?\\)";                  // replace number in brackets ((13.) (09) (11))
            Regex rgx4 = new Regex(pattern4);
            name = rgx4.Replace(name, "");
            name = name.Trim();

            string pattern1 = "^[0-9]{1,3}(\\s+)?";                     // replace leading track number (01 or 1 or 12 )
            Regex rgx1 = new Regex(pattern1);
            name = rgx1.Replace(name, "");
            name = name.Trim();


            return name;
        }

        public static string UppercaseWords(string value)
        {
            char[] array = value.ToCharArray();
            // Handle the first letter in the string.
            if (array.Length >= 1)
            {
                if (char.IsLower(array[0]))
                {
                    array[0] = char.ToUpper(array[0]);
                }
            }
            // Scan through the letters, checking for spaces.
            // ... Uppercase the lowercase letters following spaces.
            for (int i = 1; i < array.Length; i++)
            {
                if (array[i - 1] == ' ')
                {
                    if (char.IsLower(array[i]))
                    {
                        array[i] = char.ToUpper(array[i]);
                    }
                }
            }
            return new string(array);
        }
    }
}
