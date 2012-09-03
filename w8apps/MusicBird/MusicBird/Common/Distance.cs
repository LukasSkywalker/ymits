using System;
using System.Collections.Generic;

namespace MusicBird.Common
{
    class Distance
    {
        public static List<String> letterPairs( String str ) {
            int numPairs = str.Length-1;
            List<String> pairs = new List<String>();
            for (int i=0; i<numPairs; i++) {
                pairs.Add(str.Substring(i,2));
            }
            return pairs;
        }

        public static List<String> wordLetterPairs( String str ) {
            List<String> allPairs = new List<String>();
            // Tokenize the string and put the tokens/words into an array
            String[] words = str.Split('s');
            // For each word
            for (int w=0; w < words.Length; w++) {
                // Find the pairs of characters
                List<String> pairsInWord = letterPairs(words[w]);
                for (int p=0; p < pairsInWord.Count; p++) {
                    allPairs.Add(pairsInWord[p]);
                }
            }
            return allPairs;
        }

        public static double compareStrings( String str1, String str2 ) {
            List<String> pairs1 = wordLetterPairs(str1.ToUpper());
            List<String> pairs2 = wordLetterPairs(str2.ToUpper());
            int intersection = 0;
            int union = pairs1.Count + pairs2.Count;
            for (int i=0; i<pairs1.Count; i++) {
                String pair1 = pairs1[i];
                for(int j=0; j<pairs2.Count; j++) {
                    String pair2 = pairs2[j];
                    if (pair1.Equals(pair2)) {
                        intersection++;
                        pairs2.RemoveAt( j );
                        break;
                    }
                }
            }
            return (2.0*intersection)/union;
        }
    }
}
