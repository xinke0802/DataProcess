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
    class ClusterGeneral
    {
        /// <summary>
        /// Cluster all the tweets with the representation (3-grams that often appear) of each signal tweet cluster.
        /// Actually, for each non-signal tweet, we compare its 3-grams set with representation of 
        /// each signal tweet cluster to decide which cluster the non-signal tweet will be added into.
        /// Output: generalCluster.txt
        /// </summary>
        /// <param name="fileName">Lucene index folder path of tweets</param>
        /// <param name="iDoc2rec">Dictionary from tweet ID # to 3-grams record list # of signal tweets</param>
        /// <param name="gramsClList">List of unigrams, bigrams and trigrams of signal tweets</param>
        /// <param name="gList">List of tweet ID # list of general tweets (non-signal tweets) in each tweet cluster</param>
        /// <param name="minTimeStr">Time stamp string of the earliest general tweets</param>
        /// <param name="maxTimeStr">Time stamp string of the latest general tweets</param>
        public static void cluster_ori(string fileName, Dictionary<int, int> iDoc2rec, List<List<HashSet<string>>> gramsClList, List<List<int>> gList, string minTimeStr = null, string maxTimeStr = null)
        {
            double jaccard_threshold = 0.6;
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            int signalClusterCount = gramsClList.Count;

            for (int i = 0; i < signalClusterCount; i++)
                gList.Add(new List<int>());

            for (int iDoc = 0; iDoc < indexReader.NumDocs(); iDoc++)
            {
                if (iDoc % 100 == 0)
                    Console.WriteLine(iDoc);
                if (iDoc2rec.ContainsKey(iDoc))
                    continue;
                Document inDoc = indexReader.Document(iDoc);

                if (minTimeStr != null && maxTimeStr != null)
                {
                    string timeStr = inDoc.Get("CreatedAt");
                    DateTime time = DateTime.Parse(timeStr);
                    DateTime minTime = DateTime.Parse(minTimeStr);
                    DateTime maxTime = DateTime.Parse(maxTimeStr);
                    if (DateTime.Compare(time, minTime) <= 0 || DateTime.Compare(time, maxTime) >= 0)
                        continue;
                }

                string text = inDoc.Get("Text").ToLower();
                text = Regex.Replace(text, @"\s+", " ");
                text = Regex.Replace(text, @"[^A-Za-z0-9_ ]+", "");

                string[] gramArray = Regex.Split(text, " ");
                List<HashSet<string>> grams = new List<HashSet<string>>();

                HashSet<string> unigram = new HashSet<string>();
                for (int i = 0; i < gramArray.Length; i++)
                    unigram.Add(gramArray[i]);
                grams.Add(unigram);

                HashSet<string> bigram = new HashSet<string>();
                for (int i = 0; i < gramArray.Length - 1; i++)
                    bigram.Add(gramArray[i] + " " + gramArray[i + 1]);
                grams.Add(bigram);

                HashSet<string> trigram = new HashSet<string>();
                for (int i = 0; i < gramArray.Length - 2; i++)
                    trigram.Add(gramArray[i] + " " + gramArray[i + 1] + " " + gramArray[i + 2]);
                grams.Add(trigram);

                for (int i = 0; i < signalClusterCount; i++)
                    if (jaccard(grams, gramsClList[i]) > jaccard_threshold)
                        gList[i].Add(iDoc);
            }

            FileStream fs = new FileStream("generalCluster.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            int count = 0;
            for (int i = 0; i < gList.Count; i++)
            {
                count += gList[i].Count;
                sw.WriteLine(i + " " + gList[i].Count + " " + count);
                for (int j = 0; j < gList[i].Count; j++)
                    sw.Write(gList[i][j] + " ");
                sw.WriteLine();
                sw.WriteLine();
            }

            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// Calculate Jaccard similarity between 3-grams sets of two tweets
        /// </summary>
        /// <param name="grams1">3-grams set of the first tweet</param>
        /// <param name="grams2">3-grams set of the second tweet</param>
        /// <returns>Jaccard similarity between 3-grams sets of two tweets</returns>
        public static double jaccard(List<HashSet<string>> grams1, List<HashSet<string>> grams2)
        {
            int intersect = 0, union = 0;
            for (int i = 0; i < grams1.Count; i++)
            {
                HashSet<string> g1 = grams1[i];
                HashSet<string> g2 = grams2[i];
                int count = 0;
                foreach (var gram in g1)
                    if (g2.Contains(gram))
                        count++;
                intersect += count;
                union += g1.Count + g2.Count - count;
            }
            return ((double)intersect / (double)union);
        }
    }
}
