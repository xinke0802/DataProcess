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
    }
}
