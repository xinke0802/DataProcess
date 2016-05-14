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
    class LabelFeature
    {
        // Merge parallel running result files into one
        public static void mergeGeneralTxt(int interval, int end)
        {
            StreamReader sr;
            string line1, line2;
            FileStream fs = new FileStream("generalCluster_merge.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            int count = 0;
            for (int i = interval; i <= end; i += interval)
            {
                sr = new StreamReader("generalCluster_" + i + ".txt", Encoding.Default);
                int j = 0;
                while ((line1 = sr.ReadLine()) != null)
                {
                    line2 = sr.ReadLine();
                    sr.ReadLine();
                    if (j < i - interval)
                    {
                        j++;
                        continue;
                    }
                    else if (j >= i)
                        break;

                    string[] strArray = Regex.Split(line1, " ");
                    count += int.Parse(strArray[1]);
                    sw.WriteLine(j + " " + strArray[1] + " " + count);
                    sw.WriteLine(line2);
                    sw.WriteLine();
                    j++;
                }
                sr.Close();
            }
            sw.Close();
            fs.Close();
        }

        // Input the general clusters
        // Input: generalCluster.txt
        public static void input_gList(List<List<int>> gList)
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
        }

        // Extract the feature of target general clusters
        // Output: featureCluster_readable.txt, featureCluster.txt
        public static void extractFeature_ori(string fileName, List<List<int>> rList, List<List<int>> gList, List<int> clList)
        {
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            FileStream fs = new FileStream("featureCluster_readable.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            FileStream fs1 = new FileStream("featureCluster.txt", FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                sw.WriteLine(clList[i]);
                sw1.Write(clList[i] + " ");
                List<int> sl = rList[clList[i]];
                List<int> gl = gList[clList[i]];

                //Percentage of signal tweets.
                double ratio = (double)sl.Count / (double)(sl.Count + gl.Count);
                sw.WriteLine("signalRatio " + ratio);
                sw1.Write(ratio + " ");

                //Tweet lengths, retweets, urls, hashtags, mentions.
                double sLength = 0.0;
                double aLength = 0.0;
                double lengthRatio;
                double sRetweet = 0.0;
                double aRetweet = 0.0;
                double sUrl = 0.0;
                double aUrl = 0.0;
                double sTag = 0.0;
                double aTag = 0.0;
                double sMention = 0.0;
                double aMention = 0.0;

                for (int j = 0; j < sl.Count; j++)
                {
                    Document inDoc = indexReader.Document(sl[j]);
                    if (inDoc.Get("IsRetweet") == "True")
                        sRetweet += 1.0;
                    string text = inDoc.Get("Text").ToLower();
                    MatchCollection mc;

                    mc = Regex.Matches(text, @"http:");
                    sUrl += mc.Count;

                    mc = Regex.Matches(text, @"#[A-Za-z0-9_]+");
                    sTag += mc.Count;
                    mc = Regex.Matches(text, @"#n#");
                    sTag -= mc.Count;

                    mc = Regex.Matches(text, @"@");
                    sMention += mc.Count;

                    text = Regex.Replace(text, @"\s+", " ");
                    text = Regex.Replace(text, @"[^A-Za-z0-9_ ]+", "");
                    string[] gramArray = Regex.Split(text, " ");
                    sLength += gramArray.Length;
                }

                aLength = sLength;
                aRetweet = sRetweet;
                aUrl = sUrl;
                aTag = sTag;
                aMention = sMention;

                for (int j = 0; j < gl.Count; j++)
                {
                    Document inDoc = indexReader.Document(gl[j]);
                    if (inDoc.Get("IsRetweet") == "True")
                        aRetweet += 1.0;
                    string text = inDoc.Get("Text").ToLower();
                    MatchCollection mc;

                    mc = Regex.Matches(text, @"http:");
                    aUrl += mc.Count;

                    mc = Regex.Matches(text, @"#[A-Za-z0-9_]+");
                    aTag += mc.Count;
                    mc = Regex.Matches(text, @"#n#");
                    aTag -= mc.Count;

                    mc = Regex.Matches(text, @"@");
                    aMention += mc.Count;

                    text = Regex.Replace(text, @"\s+", " ");
                    text = Regex.Replace(text, @"[^A-Za-z0-9_ ]+", "");
                    string[] gramArray = Regex.Split(text, " ");
                    aLength += gramArray.Length;
                }

                double sCount = (double)sl.Count;
                double aCount = (double)(sl.Count + gl.Count);
                sLength /= sCount;
                aLength /= aCount;
                lengthRatio = sLength / aLength;
                sRetweet /= sCount;
                aRetweet /= aCount;
                sUrl /= sCount;
                aUrl /= aCount;
                sTag /= sCount;
                aTag /= aCount;
                sMention /= sCount;
                aMention /= aCount;

                sw.WriteLine("signalLength " + sLength);
                sw1.Write(sLength + " ");
                sw.WriteLine("allLength " + aLength);
                sw1.Write(aLength + " ");
                sw.WriteLine("lengthRaio " + lengthRatio);
                sw1.Write(lengthRatio + " ");
                sw.WriteLine("signalRetweet " + sRetweet);
                sw1.Write(sRetweet + " ");
                sw.WriteLine("allRetweet " + aRetweet);
                sw1.Write(aRetweet + " ");
                sw.WriteLine("signalUrl " + sUrl);
                sw1.Write(sUrl + " ");
                sw.WriteLine("allUrl " + aUrl);
                sw1.Write(aUrl + " ");
                sw.WriteLine("signalTag " + sTag);
                sw1.Write(sTag + " ");
                sw.WriteLine("allTag " + aTag);
                sw1.Write(aTag + " ");
                sw.WriteLine("signalMention " + sMention);
                sw1.Write(sMention + " ");
                sw.WriteLine("allMention " + aMention);
                sw1.Write(aMention);
                
                sw.WriteLine();
                sw1.WriteLine();
                sw.Flush();
                sw1.Flush();
                Console.WriteLine(i);
            }

            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
        }

        // Input the clusters needed to be feature extracted
        // Input: label.txt
        public static void readTargetList(List<int> clList)
        {
            StreamReader sr = new StreamReader("label.txt", Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                clList.Add(int.Parse(iDocStrArray[0]));
            }
            Console.WriteLine(clList.Count + " lines read.");
            sr.Close();
        }







        public static string root = @"Feature\";
        public static string[] FeatureFileName = {
            @"RatioOfSignal.txt", @"AvgCharLength_Signal.txt", @"AvgCharLength_All.txt", @"AvgCharLength_Ratio.txt", @"AvgWordLength_Signal.txt", 
            @"AvgWordLength_All.txt", @"AvgWordLength_Ratio.txt"};

        public static List<List<int>> sList = new List<List<int>>();
        public static List<List<int>> gList = new List<List<int>>();
        public static List<List<int>> clList = new List<List<int>>();

        public static void LoadClusterList()
        {
            StreamReader sr = new StreamReader("signalCluster.txt", Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = sr.ReadLine();
                sr.ReadLine();
                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                if (iDocStrArray == null)
                {
                    sList.Add(iDocList);
                    continue;
                }
                for (int i = 0; i < iDocStrArray.Length - 1; i++)
                    iDocList.Add(int.Parse(iDocStrArray[i]));
                sList.Add(iDocList);
            }
            sr.Close();

            sr = new StreamReader("generalCluster.txt", Encoding.Default);
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

            sr = new StreamReader("label_clusterInverse.txt", Encoding.Default);
            while ((line = sr.ReadLine()) != null)
            {
                line = sr.ReadLine();
                string[] iDocStrArray = Regex.Split(line, " ");
                List<int> iDocList = new List<int>();
                if (iDocStrArray == null)
                {
                    clList.Add(iDocList);
                    sr.ReadLine();
                    continue;
                }
                for (int i = 0; i < iDocStrArray.Length; i++)
                {
                    iDocList.Add(int.Parse(iDocStrArray[i]));
                    sr.ReadLine();
                }
                clList.Add(iDocList);
                sr.ReadLine();
            }
            sr.Close();
        }

        // 0
        public static void RatioOfSignal()
        {
            string fileName = root + FeatureFileName[0];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                int sNum = 0;
                int gNum = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    sNum += s.Count;
                    gNum += g.Count;
                }
                sw.WriteLine((double)sNum / (sNum + gNum));
            }

            sw.Close();
            fs.Close();
        }

        // 1, 2, 3, 4, 5, 6
        public static void LengthAndRatio(string luceneFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);
            string fileName = root + FeatureFileName[1];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[2];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            fileName = root + FeatureFileName[3];
            FileStream fs2 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw2 = new StreamWriter(fs2, Encoding.Default);
            fileName = root + FeatureFileName[4];
            FileStream fs3 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw3 = new StreamWriter(fs3, Encoding.Default);
            fileName = root + FeatureFileName[5];
            FileStream fs4 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw4 = new StreamWriter(fs4, Encoding.Default);
            fileName = root + FeatureFileName[6];
            FileStream fs5 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw5 = new StreamWriter(fs5, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                double sCharLength = 0.0;
                double aCharLength = 0.0;
                double ratioCharLength;
                double sWordLength = 0.0;
                double aWordLength = 0.0;
                double ratioWordLength;
                int sNum = 0;
                int aNum = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    Document inDoc;
                    string text;
                    for (int k = 0; k < s.Count; k++)
                    {
                        inDoc = indexReader.Document(s[k]);
                        text = inDoc.Get("Text");
                        var deltaCharLength = text.Length;
                        var deltaWordLength = Regex.Split(text, " ").Length;
                        sCharLength += deltaCharLength;
                        sWordLength += deltaWordLength;
                        sNum++;
                        aCharLength += deltaCharLength;
                        aWordLength += deltaWordLength;
                        aNum++;
                    }
                    for (int k = 0; k < g.Count; k++)
                    {
                        inDoc = indexReader.Document(g[k]);
                        text = inDoc.Get("Text");
                        var deltaCharLength = text.Length;
                        var deltaWordLength = Regex.Split(text, " ").Length;
                        aCharLength += deltaCharLength;
                        aWordLength += deltaWordLength;
                        aNum++;
                    }
                }
                sCharLength /= sNum;
                sWordLength /= sNum;
                aCharLength /= aNum;
                aWordLength /= aNum;
                ratioCharLength = sCharLength / aCharLength;
                ratioWordLength = sWordLength / aWordLength;
                sw.WriteLine(sCharLength);
                sw1.WriteLine(aCharLength);
                sw2.WriteLine(ratioCharLength);
                sw3.WriteLine(sWordLength);
                sw4.WriteLine(aWordLength);
                sw5.WriteLine(ratioWordLength);
            }

            sw5.Close();
            fs5.Close();
            sw4.Close();
            fs4.Close();
            sw3.Close();
            fs3.Close();
            sw2.Close();
            fs2.Close();
            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
        }
    }
}
