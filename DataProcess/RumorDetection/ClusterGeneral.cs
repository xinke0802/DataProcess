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
        // Cluster all the tweets with the representation of each signal tweet cluster
        // Output: generalCluster.txt
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
