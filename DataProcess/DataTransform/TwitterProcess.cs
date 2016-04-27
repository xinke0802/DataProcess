using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;
using DataProcess.Utils;
using System.Diagnostics;

namespace DataProcess
{
    public class TwitterProcess
    {
        public static int fieldCount = 19;

        public static void RemoveDeplicate(string inputFile, string outputFile)
        {
            Dictionary<string, string> idToTweet = new Dictionary<string, string>();
            HashSet<string> ids = new HashSet<string>();
            List<string> output = new List<string>();
            var sr = new StreamReader(inputFile);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] items = line.Split('\t');
                if (items.Count() != fieldCount) continue;
                //Check
                //if (idToTweet.ContainsKey(items[0]))
                //{
                //    string s1 = idToTweet[items[0]];
                //    string s2 = line;
                //}
                //else
                //{
                //    idToTweet.Add(items[0], line);
                //}
                if (ids.Contains(items[0])) continue;
                ids.Add(items[0]);
                output.Add(line);
                if (output.Count >= 1000)
                {
                    File.AppendAllLines(outputFile, output);
                    output.Clear();
                }
            }
            File.AppendAllLines(outputFile, output);
        }

        public static void TweetToRawIndex(string inputFile, string outputFolder)
        {
            IndexWriter tweetWriter = new IndexWriter(new SimpleFSDirectory(new DirectoryInfo(outputFolder)),
                                                 new StandardAnalyzer(Version.LUCENE_29), new IndexWriter.MaxFieldLength(int.MaxValue));
            var sr = new StreamReader(inputFile);
            string line;
            int i = 0;
            while ((line = sr.ReadLine()) != null)
            {
                i++;
                if (i % 1000 == 0) Console.WriteLine(i);
                string[] items = line.Split('\t');
                Document doc = new Document();
                if (ItemsToDocument_6(items, doc))
                    //if (ItemsToDocument_11(items, doc))
                    tweetWriter.AddDocument(doc);
                else
                    throw new ArgumentException();
            }
            tweetWriter.Optimize();
            tweetWriter.Close();
        }

        private static bool ItemsToDocument_6(string[] items, Document doc)
        {
            if (items.Count() != 6) return false;
            if (items[5] != "en") return false;
            doc.Add(new Field("Text", items[1], Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("IsRetweet", items[2], Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("RetweetCount", items[3], Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("CreateAt", items[4], Field.Store.YES, Field.Index.NOT_ANALYZED));
            return true;
        }

        private static bool ItemsToDocument_19(string[] items, Document doc)
        {
            if (items.Count() != 19) return false;
            if (items[5] != "en") return false;
            doc.Add(new Field("Text", items[4], Field.Store.YES, Field.Index.ANALYZED));
            //doc.Add(new Field("IsRetweet", items[13], Field.Store.YES, Field.Index.ANALYZED));//
            //doc.Add(new Field("RetweetCount", items[3], Field.Store.YES, Field.Index.NOT_ANALYZED));//
            doc.Add(new Field("CreateAt", items[6], Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("User_Name", items[1], Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("User_ScreenName", items[2], Field.Store.YES, Field.Index.ANALYZED));//not sure
            doc.Add(new Field("User_FollowersCount", items[11], Field.Store.YES, Field.Index.NOT_ANALYZED));//
            return true;
        }

        private static bool ItemsToDocument_11(string[] items, Document doc)
        {
            if (items.Count() != 11) return false;
            if (items[5] != "en") return false;
            doc.Add(new Field("Text", items[1], Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("IsRetweet", items[2], Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("RetweetCount", items[3], Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("CreateAt", items[4], Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("User_Name", items[7], Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("User_ScreenName", items[8], Field.Store.YES, Field.Index.ANALYZED));//not sure
            doc.Add(new Field("User_FollowersCount", items[10], Field.Store.YES, Field.Index.NOT_ANALYZED));//
            return true;
        }

        public static void RawIndexToIndex(string inputFolder, string outputFolder)
        {
            IndexWriter tweetWriter = new IndexWriter(new SimpleFSDirectory(new DirectoryInfo(outputFolder)),
                                                 new StandardAnalyzer(Version.LUCENE_29), new IndexWriter.MaxFieldLength(int.MaxValue));
            HashSet<string> stopwords = new HashSet<string>(Stopwords);

            var iodir = new DirectoryInfo(inputFolder);
            var directory = FSDirectory.Open(iodir);
            IndexSearcher searcher = new IndexSearcher(directory);
            for (int i = 0; i < searcher.MaxDoc(); i++)
            {
                if (i % 10000 == 0) Console.Out.WriteLine(i);
                Document doc = searcher.Doc(i);
                string text = doc.Get("Text");
                //bool isRetweet = bool.Parse(doc.Get("IsRetweet"));
                var type = AnalyzeTweet(text, stopwords);
                var dic = RefineTweet(text, type, stopwords);
                string[] items = text.Split(' ');
                List<string> words = new List<string>();
                List<string> hashtags = new List<string>();
                List<string> mentions = new List<string>();
                List<string> retweets = new List<string>();
                for (int j = 0; j < items.Length; j++)
                {
                    if (dic.ContainsKey(j))
                    {
                        if (type[j] == WordType.Hashtag) hashtags.Add(dic[j]);
                        if (type[j] == WordType.Mention) mentions.Add(dic[j]);
                        if (type[j] == WordType.Retweet) retweets.Add(dic[j]);
                        if (type[j] == WordType.Word) words.Add(dic[j]);
                    }
                }

                if (hashtags.Count > 0)
                    doc.Add(new Field("Hashtag", hashtags.Aggregate("", (a, b) => a + " " + b).Substring(1),
                        Field.Store.YES, Field.Index.ANALYZED));
                else
                    doc.Add(new Field("Hashtag", "", Field.Store.YES, Field.Index.ANALYZED));
                if (mentions.Count > 0)
                    doc.Add(new Field("Mention", mentions.Aggregate("", (a, b) => a + " " + b).Substring(1),
                        Field.Store.YES, Field.Index.ANALYZED));
                else
                    doc.Add(new Field("Mention", "", Field.Store.YES, Field.Index.ANALYZED));
                if (retweets.Count > 0)
                    doc.Add(new Field("Retweet", retweets.Aggregate("", (a, b) => a + " " + b).Substring(1),
                        Field.Store.YES, Field.Index.ANALYZED));
                else
                    doc.Add(new Field("Retweet", "", Field.Store.YES, Field.Index.ANALYZED));
                if (words.Count > 0)
                    doc.Add(new Field("Word", words.Aggregate("", (a, b) => a + " " + b), Field.Store.YES,
                        Field.Index.ANALYZED));
                else
                    doc.Add(new Field("Word", "", Field.Store.YES, Field.Index.ANALYZED));

                if (hashtags.Count < 5 && words.Count > 3)
                    tweetWriter.AddDocument(doc);
            }
            tweetWriter.Optimize();
            tweetWriter.Close();
        }

        public static Dictionary<int, string> RefineTweet(string text, Dictionary<int, WordType> type,
            HashSet<string> stopwords)
        {
            Regex wordRegex = new Regex("\\p{S}|\\p{P}|\\p{N}");
            var dic = new Dictionary<int, string>();
            text = text.ToLower();
            string[] items = text.Split(' ');
            for (int i = 0; i < items.Length; i++)
            {
                string item = items[i];
                if (!type.ContainsKey(i)) continue;
                if (type[i] == WordType.Hashtag)
                {
                    if (item.EndsWith("...")) continue;
                    int n = 1;
                    while (n < item.Length && ((item[n] <= 'z' && item[n] >= 'a') || (item[n] <= '9' && item[n] >= '0')))//is num or zimu
                    {
                        n++;
                    }
                    string tag = item.Substring(1, n - 1);
                    dic.Add(i, tag);
                }
                if (type[i] == WordType.Mention || type[i] == WordType.Retweet)
                {
                    int n = 1;
                    while (n < item.Length && ((item[n] <= 'z' && item[n] >= 'a') || (item[n] <= '9' && item[n] >= '0') || item[n] == '_'))//is num or zimu
                    {
                        n++;
                    }
                    string name = item.Substring(1, n - 1);
                    dic.Add(i, name);
                }
                if (type[i] == WordType.Word)
                {
                    List<int> list = new List<int>();
                    list.Add(-1);
                    for (int n = 0; n < item.Length; n++)
                    {
                        if (!((item[n] <= 'z' && item[n] >= 'a') || (item[n] <= '9' && item[n] >= '0')))
                        {
                            list.Add(n);
                        }
                    }
                    list.Add(item.Length);
                    List<int> diff = new List<int>();
                    for (int j = 0; j < list.Count - 1; j++)
                    {
                        diff.Add(list[j + 1] - list[j]);
                    }
                    int max = -1;
                    int maxIndex = -1;
                    for (int j = 0; j < diff.Count; j++)
                    {
                        if (max < diff[j])
                        {
                            max = diff[j];
                            maxIndex = j;
                        }
                    }
                    int end = list[maxIndex + 1] - 1;
                    int start = list[maxIndex] + 1;
                    string word = item.Substring(start, end - start + 1);
                    if (wordRegex.Replace(word, "").Length <= 1) continue;
                    if (stopwords.Contains(word)) continue;
                    dic.Add(i, word);
                }
            }
            return dic;
        }


        public static Dictionary<int, WordType> AnalyzeTweet(string text, HashSet<string> stopwords, bool isRetweet = false)
        {
            text = text.ToLower();
            Regex numRegex = new Regex("\\p{S}|\\p{P}|\\p{N}");
            Regex tagRegex = new Regex("\\p{S}|\\p{P}");
            Regex noneRegex = new Regex("\\p{S}|\\p{P}|\\p{C}");
            // L：字母；
            // M：标记符号（一般不会单独出现）；
            // Z：分隔符（比如空格、换行等）；
            // S：符号（比如数学符号、货币符号等）；
            // N：数字（比如阿拉伯数字、罗马数字等）；
            // C：其他字符
            // P:标点符号
            string[] items = text.Split(' ');
            Dictionary<int, WordType> type = new Dictionary<int, WordType>();
            if (text.StartsWith("rt @"))
                isRetweet = true;
            for (int i = 0; i < items.Length; i++)
            {
                string item = items[i];
                if (i == 0 && item == "rt" && isRetweet)
                {
                    type.Add(0, WordType.RT);
                    continue;
                }
                if (item.Contains("&amp;"))
                {
                    type.Add(i, WordType.None);
                    continue;
                }
                if (item.Contains("#n#"))
                {
                    type.Add(i, WordType.None);
                    continue;
                }
                if (item.Length >= 15)
                {
                    type.Add(i, WordType.None);
                    continue;
                }
                if (item.Length <= 1)
                {
                    type.Add(i, WordType.None);
                    continue;
                }
                if (item.StartsWith("http"))
                {
                    type.Add(i, WordType.Url);
                    continue;
                }
                if (numRegex.Replace(item, "").Length == 0)
                {
                    type.Add(i, WordType.None);
                    continue;
                }
                if (item.StartsWith("@"))
                {
                    if (isRetweet && i == 1 && items[0] == "rt") type.Add(i, WordType.Retweet);
                    else type.Add(i, WordType.Mention);
                    continue;
                }
                if (item.StartsWith("#"))
                {
                    if (tagRegex.Replace(item, "").Length > 1) type.Add(i, WordType.Hashtag);
                    else type.Add(i, WordType.None);
                    continue;
                }
                if (stopwords.Contains(item))
                {
                    type.Add(i, WordType.None);
                    continue;
                }
                if (noneRegex.Replace(item, "").Length <= 1)
                {
                    type.Add(i, WordType.None);
                    continue;
                }
                if (item.Contains("@") || item.Contains("#"))
                {
                    type.Add(i, WordType.None);
                    continue;
                }
                type.Add(i, WordType.Word);
            }
            return type;
        }

        public static void AggregateSingleText(string inputFolder, string outputFile)
        {
            FileStream output = new FileStream(outputFile, FileMode.Create);
            StreamWriter writer = new StreamWriter(output);
            var directory = new DirectoryInfo(inputFolder);
            foreach (var file in directory.GetFiles())
            {
                List<string> buffer = new List<string>();
                FileStream fs_in = new FileStream(file.FullName, FileMode.Open);
                StreamReader reader = new StreamReader(fs_in);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    writer.WriteLine(line);
                }
            }
        }

        #region stopwords
        public static string[] Stopwords = new string[]
        {
            "retweet",
"rt",
"gt",
"&amp;",
"a",
"about",
"above",
"after",
"again",
"against",
"all",
"am",
"an",
"and",
"any",
"are",
"aren't",
"as",
"at",
"be",
"because",
"been",
"before",
"being",
"below",
"between",
"both",
"but",
"by",
"can't",
"cannot",
"could",
"couldn't",
"did",
"didn't",
"do",
"does",
"doesn't",
"doing",
"don't",
"down",
"during",
"each",
"few",
"for",
"from",
"further",
"had",
"hadn't",
"has",
"hasn't",
"have",
"haven't",
"having",
"he",
"he'd",
"he'll",
"he's",
"her",
"here",
"here's",
"hers",
"herself",
"him",
"himself",
"his",
"how",
"how's",
"i",
"i'd",
"i'll",
"i'm",
"i've",
"if",
"in",
"into",
"is",
"isn't",
"it",
"it's",
"its",
"itself",
"let's",
"me",
"more",
"most",
"mustn't",
"my",
"myself",
"no",
"nor",
"not",
"of",
"off",
"on",
"once",
"only",
"or",
"other",
"ought",
"our",
"ours ",
"ourselves",
"out",
"over",
"own",
"same",
"shan't",
"she",
"she'd",
"she'll",
"she's",
"should",
"shouldn't",
"so",
"some",
"such",
"than",
"that",
"that's",
"the",
"their",
"theirs",
"them",
"themselves",
"then",
"there",
"there's",
"these",
"they",
"they'd",
"they'll",
"they're",
"they've",
"this",
"those",
"through",
"to",
"too",
"under",
"until",
"up",
"very",
"was",
"wasn't",
"we",
"we'd",
"we'll",
"we're",
"we've",
"were",
"weren't",
"what",
"what's",
"when",
"when's",
"where",
"where's",
"which",
"while",
"who",
"who's",
"whom",
"why",
"why's",
"with",
"won't",
"would",
"wouldn't",
"you",
"you'd",
"you'll",
"you're",
"you've",
"your",
"yours",
"yourself",
"yourselves"
        };
        #endregion
    }

    public enum WordType
    {
        RT,
        Retweet,
        Mention,
        Hashtag,
        Url,
        Number,
        Word,
        None
    }
}
