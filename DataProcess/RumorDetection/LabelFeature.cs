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
    }
}
