﻿using DataProcess.Utils;
using Lucene.Net.Documents;
using edu.stanford.nlp.ie.crf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace DataProcess.RumorDetection
{
    class ProcessCluster
    {
        /// <summary>
        /// Output representative tweet text of each tweet cluster
        /// Need executing selectRepresentative() first
        /// Output: clusterRepOriginalText.txt
        /// </summary>
        /// <param name="fileName">Lucene index folder path of tweets</param>
        public static void ouputRepresentativeOriginalText(string fileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            StreamReader sr = new StreamReader("clusterRepIDoc.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterRepOriginalText.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                Document inDoc = indexReader.Document(int.Parse(line));
                string text = inDoc.Get("Text");
                text = Regex.Replace(text, @"#N#", "");
                text = Regex.Replace(text, @"#n#", "");
                text = Regex.Replace(text, @"\s+", " ");
                sw.WriteLine(text);
            }

            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// Select a representative tweet for each tweet cluster
        /// Output: clusterRepIDoc.txt, clusterRepText.txt, clusterRepWords.txt
        /// </summary>
        /// <param name="fileName">Lucene index folder path of tweets</param>
        /// <param name="gramsList">List of 3-grams sets of signal tweets in each signal tweet cluster</param>
        /// <param name="iDoc2rec">Dictionary from tweet ID # to 3-grams record list #</param>
        public static void selectRepresentative(string fileName, List<List<HashSet<string>>> gramsList, Dictionary<int, int> iDoc2rec)
        {
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            StreamReader sr = new StreamReader("signalCluster.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterRepIDoc.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            FileStream fs1 = new FileStream("clusterRepText.txt", FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            FileStream fs2 = new FileStream("clusterRepWords.txt", FileMode.Create);
            StreamWriter sw2 = new StreamWriter(fs2, Encoding.Default);
            
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = sr.ReadLine();
                sr.ReadLine();
                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                for (int i = 0; i < iDocStrArray.Length - 1; i++)
                    iDocList.Add(int.Parse(iDocStrArray[i]));

                double[] simArr = new double[iDocList.Count];
                for (int i = 0; i < iDocList.Count; i++)
                    simArr[i] = 0.0;

                for (int i = 0; i < iDocList.Count; i++)
                {
                    int rec1 = iDoc2rec[iDocList[i]];
                    for (int j = i + 1; j < iDocList.Count; j++)
                    {
                        int rec2 = iDoc2rec[iDocList[j]];
                        double sim = ClusterGeneral.jaccard(gramsList[rec1], gramsList[rec2]);
                        simArr[i] += sim;
                        simArr[j] += sim;
                    }
                }

                if (iDocList.Count > 1)
                {
                    for (int i = 0; i < iDocList.Count; i++)
                        simArr[i] /= (iDocList.Count - 1);
                }

                double maxSim = -1.0;
                int maxSimIndex = -1;
                for (int i = 0; i < iDocList.Count; i++)
                {
                    if (simArr[i] > maxSim)
                    {
                        maxSim = simArr[i];
                        maxSimIndex = i;
                    }
                }

                int iDoc = iDocList[maxSimIndex];
                Document inDoc = indexReader.Document(iDoc);
                string text = inDoc.Get("Text").ToLower();
                text = Regex.Replace(text, @"\s+", " ");
                text = Regex.Replace(text, @"#n#", "");
                string words = Regex.Replace(text, @"[^A-Za-z0-9_ ]+", "");
                sw.WriteLine(iDoc);
                sw1.WriteLine(text);
                sw2.WriteLine(words);
            }

            sw2.Close();
            fs2.Close();
            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
            sr.Close();
        }

        /// <summary>
        /// Calculate the average published time of each tweet cluster
        /// Output: clusterAverageTime.txt
        /// </summary>
        /// <param name="fileName">Lucene index folder path of tweets</param>
        public static void averageTime(string fileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            StreamReader sr = new StreamReader("signalCluster.txt", Encoding.Default);
            StreamReader sr1 = new StreamReader("generalCluster.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterAverageTime.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            string line;
            string line1;
            while ((line = sr.ReadLine()) != null && (line1 = sr1.ReadLine()) != null)
            {
                line = sr.ReadLine();
                line1 = sr1.ReadLine();
                sr.ReadLine();
                sr1.ReadLine();

                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                for (int i = 0; i < iDocStrArray.Length - 1; i++)
                    iDocList.Add(int.Parse(iDocStrArray[i]));

                string[] iDocStrArray1 = Regex.Split(line1, " ");
                List<int> iDocList1 = new List<int>();
                for (int i = 0; i < iDocStrArray1.Length - 1; i++)
                    iDocList1.Add(int.Parse(iDocStrArray1[i]));

                int count = iDocList.Count + iDocList1.Count;
                double temp = 0.0;
                for (int i = 0; i < iDocList.Count; i++)
                {
                    Document inDoc = indexReader.Document(iDocList[i]);
                    string timeStr = inDoc.Get("CreatedAt");
                    DateTime time = DateTime.Parse(timeStr);
                    temp += (double)time.Ticks / count;
                }
                for (int i = 0; i < iDocList1.Count; i++)
                {
                    Document inDoc = indexReader.Document(iDocList1[i]);
                    string timeStr = inDoc.Get("CreatedAt");
                    DateTime time = DateTime.Parse(timeStr);
                    temp += (double)time.Ticks / count;
                }
                DateTime timeAvg = new DateTime((long)temp);

                sw.WriteLine(timeAvg.ToString());
            }

            sw.Close();
            fs.Close();
            sr1.Close();
            sr.Close();
        }

        /// <summary>
        /// Output hashtag set of each tweet cluster
        /// Output: clusterHashtagSet.txt
        /// </summary>
        /// <param name="fileName">Lucene index folder path of tweets</param>
        public static void hashtagSet(string fileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            StreamReader sr = new StreamReader("signalCluster.txt", Encoding.Default);
            StreamReader sr1 = new StreamReader("generalCluster.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterHashtagSet.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            string line;
            string line1;
            while ((line = sr.ReadLine()) != null && (line1 = sr1.ReadLine()) != null)
            {
                line = sr.ReadLine();
                line1 = sr1.ReadLine();
                sr.ReadLine();
                sr1.ReadLine();

                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                for (int i = 0; i < iDocStrArray.Length - 1; i++)
                    iDocList.Add(int.Parse(iDocStrArray[i]));

                string[] iDocStrArray1 = Regex.Split(line1, " ");
                List<int> iDocList1 = new List<int>();
                for (int i = 0; i < iDocStrArray1.Length - 1; i++)
                    iDocList1.Add(int.Parse(iDocStrArray1[i]));

                HashSet<string> hashtagSet = new HashSet<string>();

                for (int i = 0; i < iDocList.Count; i++)
                {
                    Document inDoc = indexReader.Document(iDocList[i]);
                    string text = inDoc.Get("Text").ToLower();
                    text = Regex.Replace(text, @"\s+", " ");
                    text = Regex.Replace(text, @"#n#", "");
                    MatchCollection mc;
                    mc = Regex.Matches(text, @"#[A-Za-z0-9_]+");
                    var it = mc.GetEnumerator();
                    for (int j = 0; j < mc.Count; j++)
                    {
                        it.MoveNext();
                        hashtagSet.Add(it.Current.ToString());
                    }
                }

                for (int i = 0; i < iDocList1.Count; i++)
                {
                    Document inDoc = indexReader.Document(iDocList1[i]);
                    string text = inDoc.Get("Text").ToLower();
                    text = Regex.Replace(text, @"\s+", " ");
                    text = Regex.Replace(text, @"#n#", "");
                    MatchCollection mc;
                    mc = Regex.Matches(text, @"#[A-Za-z0-9_]+");
                    var it = mc.GetEnumerator();
                    for (int j = 0; j < mc.Count; j++)
                    {
                        it.MoveNext();
                        hashtagSet.Add(it.Current.ToString());
                    }
                }

                var iter = hashtagSet.GetEnumerator();
                for (int i = 0; i < hashtagSet.Count; i++)
                {
                    iter.MoveNext();
                    if (iter.Current != "#ebola")
                        sw.Write(iter.Current.ToString() + " ");
                }

                sw.WriteLine();
            }

            sw.Close();
            fs.Close();
            sr1.Close();
            sr.Close();
        }

        /// <summary>
        /// Output name entity set of each tweet cluster
        /// Output: clusterNameEntitySet.txt
        /// </summary>
        /// <param name="fileName">Lucene index folder path of tweets</param>
        public static void nameEntitySet(string fileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            StreamReader sr = new StreamReader("signalCluster.txt", Encoding.Default);
            StreamReader sr1 = new StreamReader("generalCluster.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterNameEntitySet.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            // Path to the folder with classifiers models
            var jarRoot = @"..\..\..\..\stanford-ner-2015-12-09";
            var classifiersDirecrory = jarRoot + @"\classifiers";

            // Loading 3 class classifier model
            var classifier = CRFClassifier.getClassifierNoExceptions(
                classifiersDirecrory + @"\english.all.3class.distsim.crf.ser.gz");

            string line;
            string line1;
            while ((line = sr.ReadLine()) != null && (line1 = sr1.ReadLine()) != null)
            {
                line = sr.ReadLine();
                line1 = sr1.ReadLine();
                sr.ReadLine();
                sr1.ReadLine();

                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                for (int i = 0; i < iDocStrArray.Length - 1; i++)
                    iDocList.Add(int.Parse(iDocStrArray[i]));

                string[] iDocStrArray1 = Regex.Split(line1, " ");
                List<int> iDocList1 = new List<int>();
                for (int i = 0; i < iDocStrArray1.Length - 1; i++)
                    iDocList1.Add(int.Parse(iDocStrArray1[i]));

                HashSet<string> nameEntitySet = new HashSet<string>();

                for (int i = 0; i < iDocList.Count; i++)
                {
                    Document inDoc = indexReader.Document(iDocList[i]);
                    string text = inDoc.Get("Text");
                    text = Regex.Replace(text, @"\s+", " ");
                    text = Regex.Replace(text, @"#n#|#N#", "");
                    text = Regex.Replace(text, @"#", "");
                    text = Regex.Replace(text, @"@", "");
                    text = classifier.classifyWithInlineXML(text);
                    MatchCollection mc;
                    mc = Regex.Matches(text, @"<PERSON>[^<>]+</PERSON>");
                    var it = mc.GetEnumerator();
                    for (int j = 0; j < mc.Count; j++)
                    {
                        it.MoveNext();
                        string str = it.Current.ToString();
                        nameEntitySet.Add(str.Substring(8, str.Length - 17));
                    }
                    mc = Regex.Matches(text, @"<ORGANIZATION>[^<>]+</ORGANIZATION>");
                    it = mc.GetEnumerator();
                    for (int j = 0; j < mc.Count; j++)
                    {
                        it.MoveNext();
                        string str = it.Current.ToString();
                        nameEntitySet.Add(str.Substring(14, str.Length - 29));
                    }
                    mc = Regex.Matches(text, @"<LOCATION>[^<>]+</LOCATION>");
                    it = mc.GetEnumerator();
                    for (int j = 0; j < mc.Count; j++)
                    {
                        it.MoveNext();
                        string str = it.Current.ToString();
                        nameEntitySet.Add(str.Substring(10, str.Length - 21));
                    }
                }

                for (int i = 0; i < iDocList1.Count; i++)
                {
                    Document inDoc = indexReader.Document(iDocList1[i]);
                    string text = inDoc.Get("Text");
                    text = Regex.Replace(text, @"\s+", " ");
                    text = Regex.Replace(text, @"#n#|#N#", "");
                    text = Regex.Replace(text, @"#", "");
                    text = Regex.Replace(text, @"@", "");
                    text = classifier.classifyWithInlineXML(text);
                    MatchCollection mc;
                    mc = Regex.Matches(text, @"<PERSON>[^<>]+</PERSON>");
                    var it = mc.GetEnumerator();
                    for (int j = 0; j < mc.Count; j++)
                    {
                        it.MoveNext();
                        string str = it.Current.ToString();
                        nameEntitySet.Add(str.Substring(8, str.Length - 17));
                    }
                    mc = Regex.Matches(text, @"<ORGANIZATION>[^<>]+</ORGANIZATION>");
                    it = mc.GetEnumerator();
                    for (int j = 0; j < mc.Count; j++)
                    {
                        it.MoveNext();
                        string str = it.Current.ToString();
                        nameEntitySet.Add(str.Substring(14, str.Length - 29));
                    }
                    mc = Regex.Matches(text, @"<LOCATION>[^<>]+</LOCATION>");
                    it = mc.GetEnumerator();
                    for (int j = 0; j < mc.Count; j++)
                    {
                        it.MoveNext();
                        string str = it.Current.ToString();
                        nameEntitySet.Add(str.Substring(10, str.Length - 21));
                    }
                }

                var iter = nameEntitySet.GetEnumerator();
                for (int i = 0; i < nameEntitySet.Count; i++)
                {
                    iter.MoveNext();
                    sw.Write(iter.Current.ToString() + "; ");
                }

                sw.WriteLine();
            }

            sw.Close();
            fs.Close();
            sr1.Close();
            sr.Close();
        }

        /// <summary>
        /// Calculate time similarity matrix of tweet clusters
        /// Output: clusterTimeSimilarity.txt
        /// </summary>
        public static void timeSimilarity()
        {
            StreamReader sr = new StreamReader("clusterAverageTime.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterTimeSimilarity.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            List<DateTime> timeList = new List<DateTime>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                timeList.Add(DateTime.Parse(line));
            }

            double lamda = Math.Log(2) / (60 * 60 * 24);
            for (int i = 0; i < timeList.Count; i++)
            {
                DateTime t1 = timeList[i];
                for (int j = 0; j < timeList.Count; j++)
                {
                    if (i == j)
                        sw.Write("1.0 ");
                    else
                    {
                        DateTime t2 = timeList[j];
                        var dt = Math.Abs((t1 - t2).TotalSeconds);
                        sw.Write(Math.Exp(-lamda * dt) + " ");
                    }
                }
                sw.WriteLine();
            }

            sw.Close();
            fs.Close();
            sr.Close();
        }

        /// <summary>
        /// Calculate hashtag similarity matrix of tweet clusters
        /// Output: clusterHashTagSimilarity.txt
        /// </summary>
        public static void hashtagSimilarity()
        {
            StreamReader sr = new StreamReader("clusterHashtagSet.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterHashTagSimilarity.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            List<HashSet<string>> hashtagSetList = new List<HashSet<string>>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] hashtagArray = Regex.Split(line, " ");
                HashSet<string> hashtagSet = new HashSet<string>();
                for (int i = 0; i < hashtagArray.Length - 1; i++)
                    hashtagSet.Add(hashtagArray[i]);
                hashtagSetList.Add(hashtagSet);
            }

            for (int i = 0; i < hashtagSetList.Count; i++)
            {
                var set1 = hashtagSetList[i];
                for (int j = 0; j < hashtagSetList.Count; j++)
                {
                    var set2 = hashtagSetList[j];
                    sw.Write(jaccard(set1, set2) + " ");
                }
                sw.WriteLine();
            }

            sw.Close();
            fs.Close();
            sr.Close();
        }

        /// <summary>
        /// Calculate name entity similarity matrix of tweet clusters
        /// Output: clusterNameEntitySetSimilarity.txt
        /// </summary>
        public static void nameEntitySimilarity()
        {
            StreamReader sr = new StreamReader("clusterNameEntitySet.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterNameEntitySetSimilarity.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            List<HashSet<string>> nameEntitySetList = new List<HashSet<string>>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] nameEntityArray = Regex.Split(line, "; ");
                HashSet<string> nameEntitySet = new HashSet<string>();
                for (int i = 0; i < nameEntityArray.Length - 1; i++)
                    nameEntitySet.Add(nameEntityArray[i]);
                nameEntitySetList.Add(nameEntitySet);
            }

            for (int i = 0; i < nameEntitySetList.Count; i++)
            {
                var set1 = nameEntitySetList[i];
                for (int j = 0; j < nameEntitySetList.Count; j++)
                {
                    var set2 = nameEntitySetList[j];
                    sw.Write(jaccard(set1, set2) + " ");
                }
                sw.WriteLine();
            }

            sw.Close();
            fs.Close();
            sr.Close();
        }

        /// <summary>
        /// Calculate Jaccard similarity between two string sets
        /// </summary>
        /// <param name="set1">The first set</param>
        /// <param name="set2">The second set</param>
        /// <returns>Jaccard similarity between two string sets</returns>
        public static double jaccard(HashSet<string> set1, HashSet<string> set2)
        {
            int intersect = 0, union = 0;

            foreach (var element in set1)
                if (set2.Contains(element))
                    intersect++;
            union += set1.Count + set2.Count - intersect;

            if (union == 0)
                return 0;
            return ((double)intersect / (double)union);
        }

        /// <summary>
        /// Calculate charge similarity between two string sets
        /// </summary>
        /// <param name="set1">The first set</param>
        /// <param name="set2">The second set</param>
        /// <returns>Charge similarity between two string sets</returns>
        public static double chargeSimilarity(HashSet<string> set1, HashSet<string> set2)
        {
            int intersect = 0;

            foreach (var element in set1)
                if (set2.Contains(element))
                    intersect++;

            double delta = 1.0;
            for (int i = 0; i < intersect; i++)
                delta *= 0.5;

            return (1 - delta);
        }

        /// <summary>
        /// Transfer the date format of average published times of each tweet cluster and output them
        /// Output: clusterAverageTime_format.txt
        /// </summary>
        public static void changeDateFormat()
        {
            StreamReader sr = new StreamReader("clusterAverageTime.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterAverageTime_format.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            List<DateTime> timeList = new List<DateTime>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                timeList.Add(DateTime.Parse(line));
            }

            double lamda = Math.Log(2) / (60 * 60 * 24);
            for (int i = 0; i < timeList.Count; i++)
            {
                DateTime t = timeList[i];
                sw.WriteLine(t.Year + "/" + t.Month + "/" + t.Day + " " + t.Hour + ":" + t.Minute + ":" + t.Second);
            }

            sw.Close();
            fs.Close();
            sr.Close();
        }

        /// <summary>
        /// Process the ground-truth label file of the second time clustring of tweet clusters
        /// Input: label_clusterInverse.txt
        /// Output: label_clusterInverse_new.txt (readable file), label_cluster.txt (label vector file)
        /// </summary>
        /// <param name="count"></param>
        public static void checkLabelClusterInverse(int count)
        {
            StreamReader sr = new StreamReader("label_clusterInverse.txt", Encoding.Default);
            StreamReader sr1 = new StreamReader("clusterRepText.txt", Encoding.Default);
            FileStream fs = new FileStream("label_clusterInverse_new.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            FileStream fs1 = new FileStream("label_cluster.txt", FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);

            string line;
            string[] repText = new string[count];
            int textIndex = 0;
            while ((line = sr1.ReadLine()) != null)
                repText[textIndex++] = line;

            int[] mark = new int[count];
            for (int i = 0; i < count; i++)
                mark[i] = 0;
            int[] clusterLabel = new int[count];
            for (int i = 0; i < count; i++)
                clusterLabel[i] = 0;
            int num = 0;
            int clusterIndex = 0;
            while (true)
            {
                sr.ReadLine();
                num++;
                line = sr.ReadLine();
                num++;

                sw.WriteLine(++clusterIndex);
                string[] iDocStrArray = Regex.Split(line, " ");
                for (int i = 0; i < iDocStrArray.Length; i++)
                {
                    mark[int.Parse(iDocStrArray[i]) - 1]++;
                    clusterLabel[int.Parse(iDocStrArray[i]) - 1] = clusterIndex;
                    if (i != iDocStrArray.Length - 1)
                        sw.Write(iDocStrArray[i] + " ");
                    else
                        sw.WriteLine(iDocStrArray[i]);
                }

                for (int i = 0; i < iDocStrArray.Length; i++)
                {
                    sw.WriteLine(repText[int.Parse(iDocStrArray[i]) - 1]);
                }

                do
                {
                    line = sr.ReadLine();
                    num++;
                }
                while (line != "" && line != null);

                if (line == null)
                    break;

                sw.WriteLine();
            }

            for (int i = 0; i < count; i++)
                if (mark[i] != 1)
                    Console.WriteLine((i + 1) + ": " + mark[i]);

            for (int i = 0; i < count; i++)
                sw1.WriteLine(clusterLabel[i]);

            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
            sr1.Close();
            sr.Close();
        }

        /// <summary>
        /// Calculate word sets jaccard similarity matrix of tweet clusters
        /// </summary>
        public static void wordJaccardSimilarity()
        {
            StreamReader sr = new StreamReader("clusterRepWords.txt", Encoding.Default);

            List<HashSet<string>> repWordList = new List<HashSet<string>>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] wordArray = Regex.Split(line, " ");
                HashSet<string> repWord = new HashSet<string>();
                for (int i = 0; i < wordArray.Length; i++)
                    if (!string.IsNullOrEmpty(wordArray[i]))
                        repWord.Add(wordArray[i]);
                repWordList.Add(repWord);
            }

            sr.Close();

            FileStream fs = new FileStream("clusterWordJaccardSimilarity.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            for (int i = 0; i < repWordList.Count; i++)
            {
                var repWord1 = repWordList[i];
                for (int j = 0; j < repWordList.Count; j++)
                {
                    var repWord2 = repWordList[j];
                    double sim = jaccard(repWord1, repWord2);
                    sw.Write(sim + " ");
                }
                sw.WriteLine();
            }
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// Calculate tf-idf similarity matrix of tweet clusters
        /// </summary>
        public static void tfIdfSimilarity()
        {
            StreamReader sr = new StreamReader("clusterRepWords.txt", Encoding.Default);

            List<Dictionary<string, double>> tfList = new List<Dictionary<string, double>>();
            Dictionary<string, double> idfDic = new Dictionary<string, double>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] wordArray = Regex.Split(line, " ");
                Dictionary<string, double> tf = new Dictionary<string, double>();
                int count = 0;
                for (int i = 0; i < wordArray.Length; i++)
                {
                    string word = wordArray[i];
                    if (!string.IsNullOrEmpty(word))
                    {
                        count++;
                        if (tf.ContainsKey(word))
                            tf[word] += 1.0;
                        else
                            tf.Add(word, 1.0);
                    }
                }
                Dictionary<string, double> tfCopy = new Dictionary<string, double>();
                foreach (var word in tf.Keys)
                {
                    tfCopy[word] = tf[word] / count;
                    if (idfDic.ContainsKey(word))
                        idfDic[word] += 1.0;
                    else
                        idfDic.Add(word, 1.0);
                }
                tfList.Add(tfCopy);
            }
            sr.Close();

            FileStream fs = new FileStream("wordCount.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            foreach (var word in idfDic.Keys)
            {
                sw.WriteLine(word + " " + (int)idfDic[word]);
            }
            sw.Close();
            fs.Close();

            fs = new FileStream("wordIdf.txt", FileMode.Create);
            sw = new StreamWriter(fs, Encoding.Default);
            Dictionary<string, double> idfDicFinal = new Dictionary<string, double>();
            foreach (var word in idfDic.Keys)
            {
                idfDicFinal[word] = Math.Log(tfList.Count / idfDic[word]);
                sw.WriteLine(word + " " + idfDicFinal[word]);
            }
            sw.Close();
            fs.Close();

            List<Dictionary<string, double>> tfIdfList = new List<Dictionary<string, double>>();
            for (int i = 0; i < tfList.Count; i++)
            {
                var tfIdf = new Dictionary<string, double>();
                var tf = tfList[i];
                foreach(var word in tf.Keys)
                {
                    tfIdf.Add(word, tf[word] * idfDicFinal[word]);
                }
                double factor = 0.0;
                foreach (var word in tfIdf.Keys)
                {
                    factor += tfIdf[word] * tfIdf[word];
                }
                factor = Math.Sqrt(factor);
                var tfIdfCopy = new Dictionary<string, double>();
                foreach (var word in tfIdf.Keys)
                {
                    tfIdfCopy[word] = tfIdf[word] / factor;
                }
                tfIdfList.Add(tfIdfCopy);
            }

            fs = new FileStream("clusterTfIdfSimilarity.txt", FileMode.Create);
            sw = new StreamWriter(fs, Encoding.Default);
            for (int i = 0; i < tfIdfList.Count; i++)
            {
                var tfIdf1 = tfIdfList[i];
                for (int j = 0; j < tfIdfList.Count; j++)
                {
                    var tfIdf2 = tfIdfList[j];
                    double sim = 0.0;
                    foreach (var word in tfIdf1.Keys)
                    {
                        if (tfIdf2.ContainsKey(word))
                        {
                            sim += tfIdf1[word] * tfIdf2[word];
                        }
                    }
                    sw.Write(sim + " ");
                }
                sw.WriteLine();
            }
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// Calculate mention similarity matrix of tweet clusters
        /// </summary>
        public static void mentionSimilarity(string fileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            StreamReader sr = new StreamReader("signalCluster.txt", Encoding.Default);
            StreamReader sr1 = new StreamReader("generalCluster.txt", Encoding.Default);
            FileStream fs = new FileStream("clusterMentionSimilarity.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            var mentionList = new List<HashSet<string>>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = sr.ReadLine();
                sr.ReadLine();
                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                for (int i = 0; i < iDocStrArray.Length - 1; i++)
                    iDocList.Add(int.Parse(iDocStrArray[i]));
                sr1.ReadLine();
                line = sr1.ReadLine();
                sr1.ReadLine();
                iDocStrArray = Regex.Split(line, " ");
                for (int i = 0; i < iDocStrArray.Length - 1; i++)
                    iDocList.Add(int.Parse(iDocStrArray[i]));

                var mention = new HashSet<string>();
                for (int i = 0; i < iDocList.Count; i++)
                {
                    Document inDoc = indexReader.Document(iDocList[i]);
                    string userSrnName = inDoc.Get("UserScreenName");
                    mention.Add(userSrnName);
                    string text = inDoc.Get("Text");
                    MatchCollection mc;
                    mc = Regex.Matches(text, @"@[A-Za-z0-9_]+");
                    var it = mc.GetEnumerator();
                    for (int j = 0; j < mc.Count; j++)
                    {
                        it.MoveNext();
                        string str = it.Current.ToString();
                        mention.Add(str.Substring(1));
                    }
                }
                mentionList.Add(mention);
            }

            for (int i = 0; i < mentionList.Count; i++)
            {
                var mention1 = mentionList[i];
                for (int j = 0; j < mentionList.Count; j++)
                {
                    var mention2 = mentionList[j];
                    int sim = 0;
                    foreach(var name in mention1)
                    {
                        if (mention2.Contains(name))
                        {
                            sim = 1;
                            break;
                        }
                    }
                    sw.Write(sim + " ");
                }
                sw.WriteLine();
            }

            sw.Close();
            fs.Close();
            sr1.Close();
            sr.Close();
        }
    }
}
