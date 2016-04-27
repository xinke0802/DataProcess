using DataProcess.Utils;
using Lucene.Net.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace DataProcess.RumorDetection
{
    class ClusterSignal
    {
        public static void preCluster_ori(string fileName, List<List<HashSet<string>>> gramsList, Dictionary<int, int> rec2iDoc, Dictionary<int, int> iDoc2rec)
        {
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            StreamReader sr = new StreamReader("signal.txt", Encoding.Default);
            
            string line;
            int recNum = 0;
            while ((line = sr.ReadLine()) != null)
            {
                int iDoc = int.Parse(line);
                Document inDoc = indexReader.Document(iDoc);
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

                if (recNum % 1000 == 0)
                    Console.WriteLine(recNum);
                gramsList.Add(grams);
                rec2iDoc.Add(recNum, iDoc);
                iDoc2rec.Add(iDoc, recNum);
                recNum++;
            }
            sr.Close();
        }

        public static List<List<int>> cluster_ori(List<List<HashSet<string>>> gramsList, Dictionary<int, int> rec2iDoc, Dictionary<int, int> iDoc2rec)
        {
            List<int> uList = new List<int>();
            List<int> cList = new List<int>();
            List<List<int>> rList = new List<List<int>>();
            double jaccard_threshold = 0.6;

            FileStream fs = new FileStream("signalCluster.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            for (int i = 0; i < rec2iDoc.Count; i++)
                uList.Add(i);
            int c = 0;
            int cc = 0;
            while (uList.Count != 0)
            {
                List<int> cl = new List<int>();
                rList.Add(cl);
                int h = uList[0];
                uList.RemoveAt(0);
                cList.Add(h);
                while (cList.Count != 0)
                {
                    int head = cList[0];
                    cList.RemoveAt(0);
                    cl.Add(rec2iDoc[head]);
                    int i = 0;
                    while (i < uList.Count)
                    {
                        int body = uList[i];
                        if (jaccard(gramsList, head, body) > jaccard_threshold)
                        {
                            uList.RemoveAt(i);
                            cList.Add(body);
                        }
                        else
                            i++;
                    }
                }
                cc += cl.Count;
                Console.WriteLine(c + " " + cl.Count + " " + cc);
                sw.WriteLine(c + " " + cl.Count + " " + cc);
                c++;
                for (int d = 0; d < cl.Count; d++)
                    Console.Write(cl[d] + " ");
                Console.WriteLine();
                Console.WriteLine();
                for (int d = 0; d < cl.Count; d++)
                    sw.Write(cl[d] + " ");
                sw.WriteLine();
                sw.WriteLine();
                sw.Flush();
            }
            sw.Close();
            fs.Close();
            return rList;
        }

        public static double jaccard(List<List<HashSet<string>>> gramsList, int head, int body)
        {
            List<HashSet<string>> hh = gramsList[head];
            List<HashSet<string>> bb = gramsList[body];
            int intersect = 0, union = 0;
            for (int i = 0; i < 3; i++)
            {
                HashSet<string> h = hh[i];
                HashSet<string> b = bb[i];
                int count = 0;
                foreach (var gram in h)
                    if (b.Contains(gram))
                        count++;
                intersect += count;
                union += h.Count + b.Count - count;
            }
            return ((double)intersect / (double)union);
        }

        public static void extract_ori(List<List<HashSet<string>>> gramsList, Dictionary<int, int> rec2iDoc, Dictionary<int, int> iDoc2rec, List<List<HashSet<string>>> gramsClList, List<List<int>> rList)
        {
            StreamReader sr = new StreamReader("signalCluster.txt", Encoding.Default);
            
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = sr.ReadLine();
                sr.ReadLine();
                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                for (int i = 0; i < iDocStrArray.Length - 1; i++)
                    iDocList.Add(int.Parse(iDocStrArray[i]));
                rList.Add(iDocList);
                Dictionary<string, int>[] gramCount = new Dictionary<string, int>[gramsList[0].Count];
                for (int i = 0; i < gramCount.Length; i++)
                    gramCount[i] = new Dictionary<string, int>();
                for (int i = 0; i < iDocList.Count; i++)
                {
                    List<HashSet<string>> grams = gramsList[iDoc2rec[iDocList[i]]];
                    for (int j = 0; j < grams.Count; j++)
                    {
                        foreach (var gram in grams[j])
                        {
                            if (gramCount[j].ContainsKey(gram))
                                gramCount[j][gram]++;
                            else
                                gramCount[j].Add(gram, 1);

                        }
                    }
                }
                HashSet<string>[] gramClList = new HashSet<string>[gramsList[0].Count];
                for (int i = 0; i < gramClList.Length; i++)
                    gramClList[i] = new HashSet<string>();
                List<HashSet<string>> gramClList_ = new List<HashSet<string>>();
                for (int i = 0; i < gramsList[0].Count; i++)
                {
                    foreach (var item in gramCount[i])
                    {
                        if ((double)item.Value / iDocList.Count > 0.8)
                        {
                            gramClList[i].Add(item.Key);
                        }
                    }
                    gramClList_.Add(gramClList[i]);
                }
                gramsClList.Add(gramClList_);
            }
            sr.Close();
        }
    }
}
