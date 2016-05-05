using DataProcess.Utils;
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
    }
}
