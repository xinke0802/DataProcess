using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.NoiseRemoval
{
    class RemoveContentNoise
    {
        static string noisyTweetToken1 = "#N#";
        static string noisyTweetToken2 = "#R#";
        public static string RemoveTweetIndexNoise(string tweet)
        {
            if (tweet.Contains(noisyTweetToken1))
                tweet = tweet.Replace(noisyTweetToken1, " ");
            if (tweet.Contains(noisyTweetToken2))
                tweet = tweet.Replace(noisyTweetToken2, " ");
            return tweet;
        }

        static char[] wordSeperator = new char[] { ' ' };
        public static string RemoveTweetTokenizeNoise(string tweet)
        {
            if (tweet.Contains("http://"))
            {
                var tokens = tweet.Split(wordSeperator, StringSplitOptions.RemoveEmptyEntries);
                tweet = "";
                foreach (var token in tokens)
                {
                    if (!token.StartsWith("http://"))
                        tweet += token + " ";
                }
            }

            if (tweet.Contains("https://"))
            {
                var tokens = tweet.Split(wordSeperator, StringSplitOptions.RemoveEmptyEntries);
                tweet = "";
                foreach (var token in tokens)
                {
                    if (!token.StartsWith("https://"))
                        tweet += token + " ";
                }
            }
            return tweet;
        }
    }
}
