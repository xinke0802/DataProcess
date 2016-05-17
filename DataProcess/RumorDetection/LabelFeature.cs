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
            @"AvgWordLength_All.txt", @"AvgWordLength_Ratio.txt", @"RtRatio_Signal.txt", @"RtRatio_All.txt", @"AvgUrlNum_Signal.txt", 
            @"AvgUrlNum_All.txt", @"AvgHashtagNum_Signal.txt", @"AvgHashtagNum_All.txt", @"AvgMentionNum_Signal.txt", @"AvgMentionNum_All.txt", 
            @"AvgRegisterTime_All.txt", @"AvgEclipseTime_All.txt", @"AvgFavouritesNum_All.txt", @"AvgFollwersNum_All.txt", @"AvgFriendsNum_All.txt", 
            @"AvgReputation_All.txt", @"AvgTotalTweetNum_All.txt", @"AvgHasUrl_All.txt", @"AvgHasDescription_All.txt", @"AvgDescriptionCharLength_All.txt", 
            @"AvgDescriptionWordLength_All.txt", @"AvgUtcOffset_All.txt", @"OpinionLeaderNum_All.txt", @"NormalUserNum_All.txt", @"OpinionLeaderRatio_All.txt", 
            @"AvgQuestionMarkNum_All.txt", @"AvgExclamationMarkNum_All.txt", @"AvgUserRetweetNum_All.txt", @"AvgUserOriginalTweetNum_All.txt", @"AvgUserRetweetOriginalRatio_All.txt", 
            @"AvgSentimentScore_All.txt", @"PositiveTweetRatio_All.txt", @"NegativeTweetRatio_All.txt", @"AvgPositiveWordNum_All.txt", @"AvgNegativeWordNum_All.txt", 
            @"RetweetTreeRootNum_All.txt", @"RetweetTreeNonrootNum_All.txt", @"RetweetTreeMaxDepth_All.txt", @"RetweetTreeMaxBranchNum_All.txt", @"TotalTweetsCount_All.txt"};

        public static List<List<int>> sList = new List<List<int>>();
        public static List<List<int>> gList = new List<List<int>>();
        public static List<List<int>> clList = new List<List<int>>();
        public static Dictionary<Tuple<string, string>, int> userDic = new Dictionary<Tuple<string, string>, int>();
        public static Dictionary<string, int> userIdDic = new Dictionary<string, int>();

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

        public static void LoadUserDic(string luceneFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);

            for (int i = 0; i < indexReader.NumDocs(); i++)
            {
                Document user = indexReader.Document(i);
                string userName = user.Get("UserName");
                string screenName = user.Get("UserScreenName");
                string id = user.Get("UserId");
                if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(screenName))
                    userDic.Add(new Tuple<string, string>(userName, screenName), i);
                if (!string.IsNullOrEmpty(id))
                    userIdDic[id] = i;
            }
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

        // 7, 8
        public static void RtRatio(string luceneFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);
            string fileName = root + FeatureFileName[7];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[8];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                double sRtNum = 0.0;
                double aRtNum = 0.0;
                int sNum = 0;
                int aNum = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    Document inDoc;
                    for (int k = 0; k < s.Count; k++)
                    {
                        inDoc = indexReader.Document(s[k]);
                        if (inDoc.Get("IsRetweet") == "True")
                        {
                            sRtNum++;
                            aRtNum++;
                        }
                        sNum++;
                        aNum++;
                    }
                    for (int k = 0; k < g.Count; k++)
                    {
                        inDoc = indexReader.Document(g[k]);
                        if (inDoc.Get("IsRetweet") == "True")
                        {
                            aRtNum++;
                        }
                        aNum++;
                    }
                }
                sw.WriteLine(sRtNum / sNum);
                sw1.WriteLine(aRtNum / aNum);
            }

            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
        }

        // 9, 10, 11, 12, 13, 14
        public static void UrlHashtagMentionNum(string luceneFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);
            string fileName = root + FeatureFileName[9];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[10];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            fileName = root + FeatureFileName[11];
            FileStream fs2 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw2 = new StreamWriter(fs2, Encoding.Default);
            fileName = root + FeatureFileName[12];
            FileStream fs3 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw3 = new StreamWriter(fs3, Encoding.Default);
            fileName = root + FeatureFileName[13];
            FileStream fs4 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw4 = new StreamWriter(fs4, Encoding.Default);
            fileName = root + FeatureFileName[14];
            FileStream fs5 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw5 = new StreamWriter(fs5, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                double sUrlNum = 0.0;
                double aUrlNum = 0.0;
                double sHashtagNum = 0.0;
                double aHashtagNum = 0.0;
                double sMentionNum = 0.0;
                double aMentionNum = 0.0;
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
                        text = inDoc.Get("Text").ToLower();
                        text = Regex.Replace(text, @"#n#", "");
                        MatchCollection mc;

                        mc = Regex.Matches(text, @"http:");
                        sUrlNum += mc.Count;
                        aUrlNum += mc.Count;

                        mc = Regex.Matches(text, @"#[A-Za-z0-9_]+");
                        sHashtagNum += mc.Count;
                        aHashtagNum += mc.Count;

                        mc = Regex.Matches(text, @"@");
                        sMentionNum += mc.Count;
                        aMentionNum += mc.Count;
                        
                        sNum++;
                        aNum++;
                    }
                    for (int k = 0; k < g.Count; k++)
                    {
                        inDoc = indexReader.Document(g[k]);
                        text = inDoc.Get("Text").ToLower();
                        text = Regex.Replace(text, @"#n#", "");
                        MatchCollection mc;

                        mc = Regex.Matches(text, @"http:");
                        aUrlNum += mc.Count;

                        mc = Regex.Matches(text, @"#[A-Za-z0-9_]+");
                        aHashtagNum += mc.Count;

                        mc = Regex.Matches(text, @"@");
                        aMentionNum += mc.Count;

                        aNum++;
                    }
                }
                sUrlNum /= sNum;
                aUrlNum /= aNum;
                sHashtagNum /= sNum;
                aHashtagNum /= aNum;
                sMentionNum /= sNum;
                aMentionNum /= aNum;
                sw.WriteLine(sUrlNum);
                sw1.WriteLine(aUrlNum);
                sw2.WriteLine(sHashtagNum);
                sw3.WriteLine(aHashtagNum);
                sw4.WriteLine(sMentionNum);
                sw5.WriteLine(aMentionNum);
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

        // 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26
        public static void UserBaseFeature(string luceneFileName, string luceneUserFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);
            var indexUserReader = LuceneOperations.GetIndexReader(luceneUserFileName);
            string fileName = root + FeatureFileName[15];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[16];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            fileName = root + FeatureFileName[17];
            FileStream fs2 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw2 = new StreamWriter(fs2, Encoding.Default);
            fileName = root + FeatureFileName[18];
            FileStream fs3 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw3 = new StreamWriter(fs3, Encoding.Default);
            fileName = root + FeatureFileName[19];
            FileStream fs4 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw4 = new StreamWriter(fs4, Encoding.Default);
            fileName = root + FeatureFileName[20];
            FileStream fs5 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw5 = new StreamWriter(fs5, Encoding.Default);
            fileName = root + FeatureFileName[21];
            FileStream fs6 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw6 = new StreamWriter(fs6, Encoding.Default);
            fileName = root + FeatureFileName[22];
            FileStream fs7 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw7 = new StreamWriter(fs7, Encoding.Default);
            fileName = root + FeatureFileName[23];
            FileStream fs8 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw8 = new StreamWriter(fs8, Encoding.Default);
            fileName = root + FeatureFileName[24];
            FileStream fs9 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw9 = new StreamWriter(fs9, Encoding.Default);
            fileName = root + FeatureFileName[25];
            FileStream fs10 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw10 = new StreamWriter(fs10, Encoding.Default);
            fileName = root + FeatureFileName[26];
            FileStream fs11 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw11 = new StreamWriter(fs11, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                double registerTime = 0.0;
                double eclipseTime = 0.0;
                double favouritesNum = 0.0;
                double followersNum = 0.0;
                double friendsNum = 0.0;
                double reputation = 0.0;
                double totalTweetNum = 0.0;
                double hasUrlNum = 0.0;
                double hasDescriptionNum = 0.0;
                double descriptionCharLength = 0.0;
                double descriptionWordLength = 0.0;
                double utcOffset = 0.0;
                int registerTimeCount = 0;
                int eclipseTimeCount = 0;
                int favouritesNumCount = 0;
                int followersNumCount = 0;
                int friendsNumCount = 0;
                int reputationCount = 0;
                int totalTweetNumCount = 0;
                int hasUrlNumCount = 0;
                int hasDescriptionNumCount = 0;
                int descriptionCharLengthCount = 0;
                int descriptionWordLengthCount = 0;
                int utcOffsetCount = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    Document inDoc;
                    Document userDoc;
                    for (int k = 0; k < s.Count + g.Count; k++)
                    {
                        if (k < s.Count)
                            inDoc = indexReader.Document(s[k]);
                        else
                            inDoc = indexReader.Document(g[k - s.Count]);
                        string userName = inDoc.Get("UserName");
                        string screenName = inDoc.Get("UserScreenName");
                        var name = new Tuple<string, string>(userName, screenName);
                        if (!userDic.ContainsKey(name))
                            continue;
                        int iUser = userDic[name];
                        userDoc = indexUserReader.Document(iUser);

                        string rDateStr = userDoc.Get("CreatedAt");
                        if (!string.IsNullOrEmpty(rDateStr))
                        {
                            DateTime rDate = DateTime.Parse(rDateStr);
                            DateTime oDate = DateTime.Parse(@"7/1/2006 00:00:00");
                            registerTime += (rDate - oDate).Days;
                            registerTimeCount++;

                            string pDateStr = inDoc.Get("CreatedAt");
                            DateTime pDate = DateTime.Parse(pDateStr);
                            eclipseTime += (pDate - rDate).Days;
                            eclipseTimeCount++;
                        }

                        string favouriteStr = userDoc.Get("FavouritesCount");
                        if (!string.IsNullOrEmpty(favouriteStr))
                        {
                            favouritesNum += int.Parse(favouriteStr);
                            favouritesNumCount++;
                        }

                        string followerStr = userDoc.Get("FollowersCount");
                        if (!string.IsNullOrEmpty(followerStr))
                        {
                            followersNum += int.Parse(followerStr);
                            followersNumCount++;
                        }

                        string friendStr = userDoc.Get("FriendsCount");
                        if (!string.IsNullOrEmpty(friendStr))
                        {
                            friendsNum += int.Parse(friendStr);
                            friendsNumCount++;
                        }

                        if (!string.IsNullOrEmpty(followerStr) && !string.IsNullOrEmpty(friendStr))
                        {
                            reputation += (double)(int.Parse(followerStr)) / int.Parse(friendStr);
                            reputationCount++;
                        }

                        string TotalTweetStr = userDoc.Get("TotalTweetCount");
                        if (!string.IsNullOrEmpty(TotalTweetStr))
                        {
                            totalTweetNum += int.Parse(TotalTweetStr);
                            totalTweetNumCount++;
                        }

                        string UrlStr = userDoc.Get("Url");
                        if (!string.IsNullOrEmpty(UrlStr))
                            hasUrlNum++;
                        if (UrlStr != null)
                            hasUrlNumCount++;

                        string descriptionStr = userDoc.Get("UserDescription");
                        if (!string.IsNullOrEmpty(descriptionStr))
                            hasDescriptionNum++;
                        if (descriptionStr != null)
                            hasDescriptionNumCount++;

                        if (descriptionStr != null)
                        {
                            descriptionCharLength += descriptionStr.Length;
                            descriptionCharLengthCount++;
                            var wordArr = Regex.Split(descriptionStr, " ");
                            if (wordArr != null)
                            {
                                descriptionWordLength += wordArr.Length;
                                descriptionWordLengthCount++;
                            }
                        }

                        string utcStr = userDoc.Get("UtcOffset");
                        if (!string.IsNullOrEmpty(utcStr))
                        {
                            utcOffset += int.Parse(utcStr);
                            utcOffsetCount++;
                        }
                    }
                }
                sw.WriteLine(avg(registerTime, registerTimeCount));
                sw1.WriteLine(avg(eclipseTime, eclipseTimeCount));
                sw2.WriteLine(avg(favouritesNum, favouritesNumCount));
                sw3.WriteLine(avg(followersNum, followersNumCount));
                sw4.WriteLine(avg(friendsNum, friendsNumCount));
                sw5.WriteLine(avg(reputation, reputationCount));
                sw6.WriteLine(avg(totalTweetNum, totalTweetNumCount));
                sw7.WriteLine(avg(hasUrlNum, hasUrlNumCount));
                sw8.WriteLine(avg(hasDescriptionNum, hasDescriptionNumCount));
                sw9.WriteLine(avg(descriptionCharLength, descriptionCharLengthCount));
                sw10.WriteLine(avg(descriptionWordLength, descriptionWordLengthCount));
                sw11.WriteLine(avg(utcOffset, utcOffsetCount));
            }

            sw11.Close();
            fs11.Close();
            sw10.Close();
            fs10.Close();
            sw9.Close();
            fs9.Close();
            sw8.Close();
            fs8.Close();
            sw7.Close();
            fs7.Close();
            sw6.Close();
            fs6.Close();
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

        public static double avg(double sum, int num)
        {
            if (num == 0)
                return 0.0;
            else
                return sum / num;
        }

        // 27, 28, 29
        public static void LeaderNormalRatio(string luceneFileName, string luceneUserFileName)
        {
            double reputationThreshold = 1.0;
            int follwersNumThreshold = 1000;

            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);
            var indexUserReader = LuceneOperations.GetIndexReader(luceneUserFileName);
            string fileName = root + FeatureFileName[27];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[28];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            fileName = root + FeatureFileName[29];
            FileStream fs2 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw2 = new StreamWriter(fs2, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                int opinionLeaderNum = 0;
                int normalUserNum = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    Document inDoc;
                    Document userDoc;
                    double reputation;
                    int followersNum;
                    for (int k = 0; k < s.Count + g.Count; k++)
                    {
                        if (k < s.Count)
                            inDoc = indexReader.Document(s[k]);
                        else
                            inDoc = indexReader.Document(g[k - s.Count]);
                        string userName = inDoc.Get("UserName");
                        string screenName = inDoc.Get("UserScreenName");
                        var name = new Tuple<string, string>(userName, screenName);
                        if (!userDic.ContainsKey(name))
                            continue;
                        int iUser = userDic[name];
                        userDoc = indexUserReader.Document(iUser);

                        string followerStr = userDoc.Get("FollowersCount");
                        string friendStr = userDoc.Get("FriendsCount");

                        if (!string.IsNullOrEmpty(followerStr) && !string.IsNullOrEmpty(friendStr))
                        {
                            reputation = (double)(int.Parse(followerStr)) / int.Parse(friendStr);
                            followersNum = int.Parse(followerStr);
                            if (reputation > reputationThreshold && followersNum > follwersNumThreshold)
                                opinionLeaderNum++;
                            else
                                normalUserNum++;
                        }                      
                    }
                }
                
                sw.WriteLine(opinionLeaderNum);
                sw1.WriteLine(normalUserNum);
                if (opinionLeaderNum + normalUserNum == 0)
                    sw2.WriteLine("0");
                else
                    sw2.WriteLine((double)opinionLeaderNum / (opinionLeaderNum + normalUserNum));
            }

            sw2.Close();
            fs2.Close();
            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
        }

        // 30, 31
        public static void QuestionExclamationMark(string luceneFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);
            string fileName = root + FeatureFileName[30];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[31];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                double questionMarkNum = 0.0;
                double exclamationMarkNum = 0.0;
                int num = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    Document inDoc;
                    for (int k = 0; k < s.Count + g.Count; k++)
                    {
                        if (k < s.Count)
                            inDoc = indexReader.Document(s[k]);
                        else
                            inDoc = indexReader.Document(g[k - s.Count]);
                        string text = inDoc.Get("Text");
                        MatchCollection mc;
                        mc = Regex.Matches(text, @"\?");
                        questionMarkNum += mc.Count;
                        mc = Regex.Matches(text, @"!");
                        exclamationMarkNum += mc.Count;
                        num++;
                    }
                }
                sw.WriteLine(questionMarkNum / num);
                sw1.WriteLine(exclamationMarkNum / num);
            }

            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
        }

        // 32, 33, 34
        public static void UserRtOriRatio(string luceneFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);
            string fileName = root + FeatureFileName[32];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[33];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            fileName = root + FeatureFileName[34];
            FileStream fs2 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw2 = new StreamWriter(fs2, Encoding.Default);

            var rtNumDic = new Dictionary<Tuple<string, string>, int>();
            var oriNumDic = new Dictionary<Tuple<string, string>, int>();

            for (int i = 0; i < indexReader.NumDocs(); i++)
            {
                Document indoc = indexReader.Document(i);
                string userName = indoc.Get("UserName");
                string screenName = indoc.Get("UserScreenName");
                var name = new Tuple<string, string>(userName, screenName);
                if (indoc.Get("IsRetweet") == "True")
                {
                    if (rtNumDic.ContainsKey(name))
                        rtNumDic[name]++;
                    else
                        rtNumDic.Add(name, 0);
                }
                else
                {
                    if (oriNumDic.ContainsKey(name))
                        oriNumDic[name]++;
                    else
                        oriNumDic.Add(name, 0);
                }
            }

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                double userRtNum = 0.0;
                double userOriNum = 0.0;
                double userRtOriRatio = 0.0;
                int userNum = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    Document inDoc;
                    for (int k = 0; k < s.Count + g.Count; k++)
                    {
                        if (k < s.Count)
                            inDoc = indexReader.Document(s[k]);
                        else
                            inDoc = indexReader.Document(g[k - s.Count]);
                        string userName = inDoc.Get("UserName");
                        string screenName = inDoc.Get("UserScreenName");
                        var name = new Tuple<string, string>(userName, screenName);

                        int rtNum = 0;
                        int oriNum = 0;
                        if (rtNumDic.ContainsKey(name))
                        {
                            rtNum += rtNumDic[name];
                            userRtNum += rtNum;
                        } 
                        if (oriNumDic.ContainsKey(name))
                        {
                            oriNum += oriNumDic[name];
                            userOriNum += oriNum;
                        }
                        if (rtNum + oriNum != 0)
                            userRtOriRatio += (double)rtNum / (rtNum + oriNum);
                        userNum++;
                    }
                }
                sw.WriteLine(userRtNum / userNum);
                sw1.WriteLine(userOriNum / userNum);
                sw2.WriteLine(userRtOriRatio / userNum);
            }

            sw2.Close();
            fs2.Close();
            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
        }

        // 35, 36, 37
        public static void TweetSentiment(string luceneFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);
            string fileName = root + FeatureFileName[35];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[36];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            fileName = root + FeatureFileName[37];
            FileStream fs2 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw2 = new StreamWriter(fs2, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                double sentiment = 0.0;
                double positive = 0.0;
                double negative = 0.0;
                int userNum = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    Document inDoc;
                    for (int k = 0; k < s.Count + g.Count; k++)
                    {
                        if (k < s.Count)
                            inDoc = indexReader.Document(s[k]);
                        else
                            inDoc = indexReader.Document(g[k - s.Count]);
                        string score = inDoc.Get("SentimentScore");
                        string type = inDoc.Get("Sentiment");

                        sentiment += double.Parse(score);
                        if (type == "+")
                            positive += 1;
                        else if (type == "-")
                            negative += 1;
                        userNum++;
                    }
                }
                sw.WriteLine(sentiment / userNum);
                sw1.WriteLine(positive / userNum);
                sw2.WriteLine(negative / userNum);
            }

            sw2.Close();
            fs2.Close();
            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
        }

        // 38, 39
        public static void PositiveNegativeWordNum(string luceneFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);

            HashSet<string> posSet = new HashSet<string>();
            HashSet<string> negSet = new HashSet<string>();

            string line;
            StreamReader sr = new StreamReader("positive-words.txt", Encoding.Default);
            while ((line = sr.ReadLine()).StartsWith(";"))
            {
            }
            while ((line = sr.ReadLine()) != null)
            {
                posSet.Add(line);
            }
            sr.Close();
            sr = new StreamReader("negative-words.txt", Encoding.Default);
            while ((line = sr.ReadLine()).StartsWith(";"))
            {
            }
            while ((line = sr.ReadLine()) != null)
            {
                negSet.Add(line);
            }
            sr.Close();

            string fileName = root + FeatureFileName[38];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[39];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                double positive = 0.0;
                double negative = 0.0;
                int num = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    Document inDoc;
                    string text;
                    for (int k = 0; k < s.Count + g.Count; k++)
                    {
                        if (k < s.Count)
                            inDoc = indexReader.Document(s[k]);
                        else
                            inDoc = indexReader.Document(g[k - s.Count]);
                        text = inDoc.Get("Text").ToLower();
                        var wordArray = Regex.Split(text, " ");
                        for (int l = 0; l < wordArray.Length; l++)
                        {
                            if (posSet.Contains(wordArray[l]))
                                positive += 1;
                            if (negSet.Contains(wordArray[l]))
                                negative += 1;
                        }
                        num++;
                    }
                    
                }
                sw.WriteLine(positive / num);
                sw1.WriteLine(negative / num);
            }

            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
        }

        // 40, 41, 42, 43
        public static void NetworkBasedFeature(string luceneFileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(luceneFileName);
            string fileName = root + FeatureFileName[40];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            fileName = root + FeatureFileName[41];
            FileStream fs1 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
            fileName = root + FeatureFileName[42];
            FileStream fs2 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw2 = new StreamWriter(fs2, Encoding.Default);
            fileName = root + FeatureFileName[43];
            FileStream fs3 = new FileStream(fileName, FileMode.Create);
            StreamWriter sw3 = new StreamWriter(fs3, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                int rootNum = 0;
                int nodeNum = 0;
                int depth = 0;
                int maxBranchNum = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    Document inDoc;
                    string text;
                    string cName;
                    string pName;
                    var childrenDic = new Dictionary<string, HashSet<string>>();
                    var parentDic = new Dictionary<string, string>();
                    var rootSet = new HashSet<string>();
                    for (int k = 0; k < s.Count + g.Count; k++)
                    {
                        if (k < s.Count)
                            inDoc = indexReader.Document(s[k]);
                        else
                            inDoc = indexReader.Document(g[k - s.Count]);
                        text = inDoc.Get("Text");
                        cName = inDoc.Get("UserScreenName");
                        MatchCollection mc = Regex.Matches(text, @"RT @[A-Za-z0-9_]+");
                        var it = mc.GetEnumerator();
                        for (int l = 0; l < mc.Count; l++)
                        {
                            it.MoveNext();
                            pName = it.Current.ToString().Substring(4);
                            if (childrenDic.ContainsKey(pName))
                            {
                                childrenDic[pName].Add(cName);
                            }
                            else
                            {
                                var childrenSet = new HashSet<string>();
                                childrenSet.Add(cName);
                                childrenDic.Add(pName, childrenSet);
                            }
                            parentDic[cName] = pName;
                            rootSet.Remove(cName);
                            if (!parentDic.ContainsKey(pName))
                                rootSet.Add(pName);
                            cName = pName;
                        }
                    }
                    rootNum += rootSet.Count;
                    var iter = rootSet.GetEnumerator();
                    for (int k = 0; k < rootSet.Count; k++)
                    {
                        iter.MoveNext();
                        var res = exploreTree(childrenDic, iter.Current);
                        nodeNum += res.Item1;
                        if (res.Item2 > depth)
                            depth = res.Item2;
                        if (res.Item3 > maxBranchNum)
                            maxBranchNum = res.Item3;
                    }
                }
                sw.WriteLine(rootNum);
                sw1.WriteLine(nodeNum - rootNum);
                sw2.WriteLine(depth);
                sw3.WriteLine(maxBranchNum);
            }

            sw3.Close();
            fs3.Close();
            sw2.Close();
            fs2.Close();
            sw1.Close();
            fs1.Close();
            sw.Close();
            fs.Close();
        }

        public static Tuple<int, int, int> exploreTree(Dictionary<string, HashSet<string>> childrenDic, string root)
        {
            if (!childrenDic.ContainsKey(root))
            {
                return new Tuple<int, int, int>(1, 1, 1);
            }
            var childrenSet = childrenDic[root];
            int nodeNum = 1;
            int depth = 1;
            int maxBranchNum = childrenSet.Count;
            var it = childrenSet.GetEnumerator();
            for (int i = 0; i < childrenSet.Count; i++)
            {
                it.MoveNext();
                var res = exploreTree(childrenDic, it.Current);
                nodeNum += res.Item1;
                if (1 + res.Item2 > depth)
                    depth = 1 + res.Item2;
                if (res.Item3 > maxBranchNum)
                    maxBranchNum = res.Item3;
            }
            return new Tuple<int, int, int>(nodeNum, depth, maxBranchNum);
        }

        // 44
        public static void TotalTweetsCount()
        {
            string fileName = root + FeatureFileName[44];
            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            for (int i = 0; i < clList.Count; i++)
            {
                List<int> cl = clList[i];  // cl: List of original clusters of a newly built cluster
                int num = 0;
                for (int j = 0; j < cl.Count; j++)
                {
                    List<int> s = sList[cl[j] - 1];  // s: List of signal tweets of a original cluster
                    List<int> g = gList[cl[j] - 1];  // g: List of general tweets of a original cluster
                    num += s.Count;
                    num += g.Count;
                }
                sw.WriteLine(num);
            }

            sw.Close();
            fs.Close();
        }
    }
}
