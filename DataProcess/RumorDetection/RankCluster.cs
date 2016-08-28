using DataProcess.Utils;
using Lucene.Net.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace DataProcess.RumorDetection
{
    class RankCluster
    {
        /// <summary>
        /// Rank general clusters with naive algorithm to find the most likely rumors
        /// Output: rankCluster.txt
        /// </summary>
        /// <param name="fileName">Lucene index folder path of tweets</param>
        /// <param name="rList">List of tweet ID # list of signal tweets in each tweet cluster</param>
        /// <param name="gList">List of tweet ID # list of non-signal tweets in each tweet cluster</param>
        public static void rank_naive(string fileName, List<List<int>> rList, List<List<int>> gList)
        {
            StreamReader sr = new StreamReader("generalCluster.txt", Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = sr.ReadLine();
                sr.ReadLine();
                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                if (iDocStrArray == null)
                {
                    gList.Add(iDocList);
                    continue;
                }
                for (int i = 0; i < iDocStrArray.Length - 1; i++)
                    iDocList.Add(int.Parse(iDocStrArray[i]));
                gList.Add(iDocList);
            }
            sr.Close();

            List<ScoreRec> scoreList = new List<ScoreRec>();
            var indexReader = LuceneOperations.GetIndexReader(fileName);

            MatchCollection mc;
            int count;
            for (int i = 0; i < gList.Count; i++)
            {
                if (i % 10 == 0)
                    Console.WriteLine(i);
                double score = 0.0;
                double count_popularity = 0.2 * Math.Log10((double)(rList[i].Count + gList[i].Count));
                double count_signal = 0.3 * (double)rList[i].Count / (double)(rList[i].Count + gList[i].Count);
                double count_url = 0.0;
                double count_mention = 0.0;
                double count_length = 0.0;

                for (int j = 0; j < rList[i].Count; j++)
                {
                    int iDoc = rList[i][j];
                    Document inDoc = indexReader.Document(iDoc);
                    string text = inDoc.Get("Text").ToLower();

                    mc = Regex.Matches(text, @"http:");
                    count = mc.Count;
                    if (count > 2)
                        count = 2;
                    count_url += count;

                    mc = Regex.Matches(text, @"@");
                    count = mc.Count;
                    if (count > 5)
                        count = 5;
                    count_mention += count;

                    text = Regex.Replace(text, @"\s+", " ");
                    text = Regex.Replace(text, @"[^A-Za-z0-9_ ]+", "");
                    string[] gramArray = Regex.Split(text, " ");
                    count_length += gramArray.Length;
                }

                for (int j = 0; j < gList[i].Count; j++)
                {
                    int iDoc = gList[i][j];
                    Document inDoc = indexReader.Document(iDoc);
                    string text = inDoc.Get("Text").ToLower();

                    mc = Regex.Matches(text, @"http:");
                    count = mc.Count;
                    if (count > 2)
                        count = 2;
                    count_url += count;

                    mc = Regex.Matches(text, @"@");
                    count = mc.Count;
                    if (count > 5)
                        count = 5;
                    count_mention += count;

                    text = Regex.Replace(text, @"\s+", " ");
                    text = Regex.Replace(text, @"[^A-Za-z0-9_ ]+", "");
                    string[] gramArray = Regex.Split(text, " ");
                    count_length += gramArray.Length;
                }

                count_url /= (double)(rList[i].Count + gList[i].Count);
                count_mention /= (double)(rList[i].Count + gList[i].Count);
                count_length /= (double)(rList[i].Count + gList[i].Count);

                count_url = (2 - count_url) * 0.1;
                count_mention = (5 - count_mention) * 0.05;
                count_length = (140 / count_length > 10 ? 10 : 140 / count_length) * 0.02;

                score = count_popularity + count_signal + count_url + count_mention + count_length;
                scoreList.Add(new ScoreRec(score, i));
            }
            scoreList.Sort(new ScoreRecComparer());
            FileStream fs = new FileStream("rankCluster.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            for (int i = 0; i < gList.Count; i++)
            {
                Console.WriteLine(i + ": " + scoreList[i].score + " " + scoreList[i].rec);
                sw.WriteLine(i + ": " + scoreList[i].score + " " + scoreList[i].rec);
            }
            sw.Close();
            fs.Close();
        }
    }

    /// <summary>
    /// Data structure to help ranking tweet clusters
    /// </summary>
    class ScoreRec
    {
        /// <summary>
        /// Rumor likelihood score of tweet
        /// </summary>
        public double score;

        /// <summary>
        /// Tweet cluster # in the record list
        /// </summary>
        public int rec;

        public ScoreRec()
        {
            this.score = 0;
            this.rec = 0;
        }

        public ScoreRec(double s, int r)
        {
            this.score = s;
            this.rec = r;
        }
    }

    /// <summary>
    /// Sorting class for rumor ranking
    /// </summary>
    class ScoreRecComparer : IComparer<ScoreRec>
    {
        public int Compare(ScoreRec x, ScoreRec y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return 1;
            if (y == null)
                return -1;

            if (x.score > y.score)
                return -1;
            else if (x.score < y.score)
                return 1;
            else
                return 0;
        }
    }
}
