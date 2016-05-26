using DataProcess.DataAnalysis;
using DataProcess.DataTransform;
using DataProcess.Utils;
using DataProcess.RumorDetection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using DataProcess.NoiseRemoval;
using System.Xml.Serialization;

using edu.stanford.nlp.ie.crf;

namespace DataProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            //// Path to the folder with classifiers models
            //var jarRoot = @"..\..\..\..\stanford-ner-2015-12-09";
            //var classifiersDirecrory = jarRoot + @"\classifiers";

            //// Loading 3 class classifier model
            //var classifier = CRFClassifier.getClassifierNoExceptions(
            //    classifiersDirecrory + @"\english.all.3class.distsim.crf.ser.gz");

            //var s2 = "I go to school at Stanford University, which is located in California.";
            //Console.WriteLine("{0}\n", classifier.classifyWithInlineXML(s2));

            //Console.WriteLine("{0}\n", classifier.classifyToString(s2, "xml", true));
            //Console.ReadLine();

            string tweetPath = @"..\..\..\..\EbolaTweetIndex";
            string userPath = @"..\..\..\..\EbolaProfileIndex";
            //MatchSignal.match_ori(tweetPath);
            //FilterTweets.filterTimeRange(tweetPath, @"signal.txt", @"11/1/2014 00:00:00", @"12/1/2014 00:00:00");
            List<List<HashSet<string>>> gramsList = new List<List<HashSet<string>>>();
            Dictionary<int, int> rec2iDoc = new Dictionary<int, int>();
            Dictionary<int, int> iDoc2rec = new Dictionary<int, int>();
            ClusterSignal.preCluster_ori(tweetPath, gramsList, rec2iDoc, iDoc2rec);
            //Console.WriteLine(rec2iDoc[gramsList.Count-1]);
            //Console.WriteLine((gramsList.Last())[2].Last());
            //List<List<int>> rList = ClusterSignal.cluster_ori(gramsList, rec2iDoc, iDoc2rec);
            List<List<HashSet<string>>> gramsClList = new List<List<HashSet<string>>>();
            List<List<int>> rList = new List<List<int>>();
            ClusterSignal.extract_ori(gramsList, rec2iDoc, iDoc2rec, gramsClList, rList);
            List<List<int>> gList = new List<List<int>>();
            //ClusterGeneral.cluster_ori(tweetPath, iDoc2rec, gramsClList, gList, @"11/1/2014 00:00:00", @"12/1/2014 00:00:00");
            //RankCluster.rank_naive(tweetPath, rList, gList);
            //LabelFeature.mergeGeneralTxt(1000, 14000);

            LabelFeature.input_gList(gList);
            //ProcessCluster.selectRepresentative(tweetPath, gramsList, iDoc2rec);
            //ProcessCluster.averageTime(tweetPath);
            //ProcessCluster.hashtagSet(tweetPath);
            //ProcessCluster.nameEntitySet(tweetPath);
            //ProcessCluster.timeSimilarity();
            //ProcessCluster.hashtagSimilarity();
            //ProcessCluster.nameEntitySimilarity();
            //ProcessCluster.changeDateFormat();
            //ProcessCluster.checkLabelClusterInverse(939);
            //ProcessCluster.wordJaccardSimilarity();
            //ProcessCluster.tfIdfSimilarity();
            //ProcessCluster.mentionSimilarity(tweetPath);
            //ProcessCluster.ouputRepresentativeOriginalText(tweetPath);

            LabelFeature.LoadClusterList_all();
            
            //LabelFeature.RatioOfSignal();
            //LabelFeature.LengthAndRatio(tweetPath);
            //LabelFeature.RtRatio(tweetPath);
            //LabelFeature.UrlHashtagMentionNum(tweetPath);

            LabelFeature.LoadUserDic(userPath);

            //LabelFeature.UserBaseFeature(tweetPath, userPath);
            //LabelFeature.LeaderNormalRatio(tweetPath, userPath);
            //LabelFeature.QuestionExclamationMark(tweetPath);
            //LabelFeature.UserRtOriRatio(tweetPath);
            //LabelFeature.TweetSentiment(tweetPath);
            //LabelFeature.PositiveNegativeWordNum(tweetPath);
            LabelFeature.NetworkBasedFeature(tweetPath);
            LabelFeature.TotalTweetsCount();

            //LabelFeature.countNonNoiseClusterNum();

            //List<int> clList = new List<int>();
            //LabelFeature.readTargetList(clList);
            //LabelFeature.extractFeature_ori(tweetPath, rList, gList, clList);
            //List<int> tList = new List<int>();
            //for (int i = 0; i < 13974; i++)
            //    tList.Add(i);
            //LabelFeature.extractFeature_ori(tweetPath, rList, gList, tList);



        //BRTAnalysis.AnalyzeTreeStructure();
        //BRTAnalysis.GenerateCopiedData();
        //new TopicStreamConfigure().TestReadWrite();
        //if (args == null || args.Length == 0)
        //{
        //    Console.WriteLine("Error! Usage: focusSeeds (int[])");
        //}
        //else
        //{
        //    new TopicStreamExperiment().Start(args.ToList().ConvertAll(str => int.Parse(str)).ToArray());
        //}

        //new TopicStreamExperiment().Start();
        //Console.WriteLine((new DateTime(2014, 9, 27).Subtract(new DateTime(2014, 7, 27)).TotalDays + 1)/7.0);
        //Console.WriteLine((new DateTime(2015, 2, 21).Subtract(new DateTime(2014, 9, 28)).TotalDays + 1)/7.0);
        //var date = new DateTime(2014, 7, 27);
        //for (int i = 0; i < 10; i++)
        //{
        //    Console.WriteLine(i + "\t" + date.ToShortDateString());
        //    date = date.AddDays(7);
        //}

        //new TopicStreamExperiment().AnalyzeResultsTreeCounts();
        //new TopicStreamExperiment().AnalyzeResultsTopicNumber();
        //BRTAnalysis.AnalyzeScalabilityExperimentResults();
        //var color = System.Windows.Media.Color.FromRgb(252, 141, 89);
        //Console.WriteLine(color.R + " " + color.G + " " + color.B);
        //var color2 = WPFUtils.GetDarkerColor(color);
        //Console.WriteLine(color2.R + " " + color2.G + " " + color2.B);
        //var color3 = WPFUtils.GetDarkerColor(color2);
        //Console.WriteLine(color3.R + " " + color3.G + " " + color3.B);
        //            DataAnalysisMiscs.TestReadLargeFiles(@"D:\Project\IdeaFlowCode\IdeaFlowVis\Data\NewCongressWordFrequencyMonth678Interval2days\Tensor\3_1day_2days_leadlag_filter_2\ContentTensor\ContentTensorForIdeaClustering.txt");
        //            Util.ProgramFinishHalt();


        //var fields = new[] {BingNewsFields.DiscoveryStringTime, BingNewsFields.NewsArticleDescription, BingNewsFields.User_ScreenName};
        //int unqiueEffTweetCount = 0;
        //HashSet<string> contentHash = new HashSet<string>();

        //int docNum = LuceneOperations.EnumerateIndexReaderWriter(@"D:\Data\TweetIndex\EbolaTwitter3", @"D:\Data\TweetIndex\EbolaTwitter3_Part0220to0222",
        //    (document, indexWriter) =>
        //    {
        //        string content = "";
        //        foreach (var field in fields)
        //        {
        //            content += document.Get(field) + " | ";
        //        }
        //        if (!contentHash.Contains(content))
        //        {
        //            contentHash.Add(content);

        //            var date =
        //                StringOperations.ParseDateTimeString(document.Get(BingNewsFields.DiscoveryStringTime),
        //                    BingNewsFields.TimeFormat);
        //            if (date.Year == 2015 && date.Month == 2 && date.Day <= 22 && date.Day >= 20)
        //            {
        //                indexWriter.AddDocument(document);
        //                unqiueEffTweetCount++;
        //            }
        //        }
        //    });

        //Console.WriteLine("Unique tweets: {0} out of {1} ({2}%)", contentHash.Count, docNum, 100 * contentHash.Count / docNum);
        //Console.WriteLine("unqiueEffTweetCount = " + unqiueEffTweetCount);
        //Console.Read();

        //var tokenizeConfig = new TokenizeConfig(TokenizerType.Standard, StopWordsFile.EN,
        //    StopWordsFile.SpecENMicrosoft);
        //var words = NLPOperations.Tokenize("List of mergers and acquisitions by Microsoft", tokenizeConfig);

        //foreach (var word in words)
        //{
        //    Console.WriteLine(word);
        //}

        //LuceneIndexAnalysis.AnalyzeSearchWordSentiment(
        //    @"D:\Project\StreamingRoseRiver\Ebola\Raw_EbolaEnBingNews_Ebola_0_1_RS_R-1",
        //    BingNewsFields.NewsArticleHeadline,
        //    new []{"chinese","china"},
        //    50,
        //    BingNewsFields.NewsSource
        //    );

        //LuceneIndexAnalysis.AnalyzeSearchWordSentiment(
        //    @"D:\DataProcess\TweetIndex\EbolaTwitter3",
        //    BingNewsFields.NewsArticleDescription,
        //    new[] { "chinese", "china" },
        //    50
        //    );

        //var sentiAnalyze = new SentimentAnalyzer();
        //SentimentType type;
        //double score;
        //sentiAnalyze.GetSentiment("I feel bad", out type, out score);
        //Console.WriteLine(type);
        //Console.WriteLine(score);
        //Console.ReadLine();

        //LuceneIndexAnalysis.AnalyzeTwitterWordDistribution(@"D:\DataProcess\TweetIndex\EbolaTwitter3_RS", new TokenizeConfig(TokenizerType.FeatureVector, StopWordsFile.ENTwitter,
        //    StopWordsFile.SpecENEbolaNews));
        //LuceneIndexAnalysis.AnalyzeTwitterWordDistribution(@"D:\DataProcess\Index\Raw_NSA_ENBingNews_NSA_0_1_RS_R-1", new TokenizeConfig(TokenizerType.Standard, StopWordsFile.EN,
        //    StopWordsFile.SpecENEbolaNews));

        //new CTMAnalyzer().Start();
        //DataAnalysisMiscs.TestReadLargeFiles(@"D:\Project\TopicPanorama\data\TopicGraphs\NewCode-Ebola-Test9\Raw\news\result\lda.ttc.json");

        //var configure = new LuceneToTextConfigure();
        //configure.TestReadWrite();
        //new LuceneToText(true).Start();

        //new RemoveSimilarDocuments(true).Start();

        //var configure = new BingNewsBuildLuceneIndexConfigure();
        //configure.TestReadWrite();

        //new BuildLuceneIndex(BuildLuceneIndexType.BingNews, true).Start();

        //new EvoBRTSelector().Start();

        //TwitterProcess.TweetToRawIndex(
        //    @"D:\DataProcess\TweetIndex\tweets-Ebola-20150101-20150228.txt",
        //    @"D:\DataProcess\TweetIndex\tweets-Ebola-20150101-20150228\"
        //    );
        //TwitterProcess.RawIndexToIndex(
        //    @"D:\DataProcess\TweetIndex\tweets-Ebola-20150101-20150228\",
        //    @"D:\DataProcess\TweetIndex\tweets-Ebola-20150101-20150228_dedup\"
        //    );

        //new MergeLuceneIndex(
        //    (new string[] { @"D:\DataProcess\TweetIndex\EbolaTwitter1\", @"D:\DataProcess\TweetIndex\EbolaTwitter2\" }).ToList(),
        //    @"D:\DataProcess\TweetIndex\").Start();

        //new LuceneIndexTransform().StartTransformTweetIndexForStreamingRoseRiver();

        //DateTime dateTime = new DateTime(2014, 3, 22);
        //Trace.Write(dateTime.DayOfWeek);

        /// build rose trees
        //string indicespath = @"D:\DataProcess\Index\Raw_EbolaEnBingNews_Ebola_0_1_RS_R-1_S\";
        //foreach(var directory in Directory.GetDirectories(indicespath))
        //{
        //    ProcessStartInfo startInfo = new ProcessStartInfo();
        //    startInfo.CreateNoWindow = false;
        //    startInfo.UseShellExecute = false;
        //    startInfo.FileName = @"C:\Program Files\Microsoft MPI\Bin\mpiexec.exe";
        //    startInfo.WindowStyle = ProcessWindowStyle.Normal;

        //    startInfo.Arguments = @"-np 3 D:\Project\RoseTreeTaxonomyMergeTreesKnnSU\MpiBRT\bin\x64\Release\MpiBRT.exe -datasetindex 1 -samplenum 5000 -standalone false -alpha 0.08 -gama 0.4 -knnPartition 5 -knnBRT 25 -threadspernode 6 -datapath " +
        //        directory;
        //    using (Process exeProcess = Process.Start(startInfo))
        //    {
        //        exeProcess.WaitForExit();
        //    }
        //}
        //-np 3 MpiBRT -datasetindex 1 -samplenum 5000 -standalone false -alpha 0.08 -gama 0.4 -knnPartition 5 -knnBRT 25 -threadspernode 6 -datapath D:\DataProcess\Index\Raw_EbolaEnBingNews_Ebola_0_1_RS_R-1_S\2014-07-20\


        //var wordOccurrence = new WordOccurrence();
        //wordOccurrence.Configure.InputPath = @"D:\DataProcess\Index\Raw_EbolaEnBingNews_Ebola_0_1_RS_R-1\";
        //wordOccurrence.Start();
        //Trace.WriteLine("");



        //new SelectIndexDocuments(true).Start();
        //new SelectIndexDocumentsConfigure().TestReadWrite();
        //var selectIndexDocs = new SelectIndexDocuments();
        //selectIndexDocs.Configure.InputPath = @"D:\Project\TopicPanorama\data\Index\BoardReader\text_index_cleantime_Lucene_RemoveSimilar_RemoveNoise";
        //selectIndexDocs.Configure.OutputPath = selectIndexDocs.Configure.InputPath + "_Board";
        //selectIndexDocs.Configure.IsSelectByExactMatch = true;
        //selectIndexDocs.Configure.FieldMatchDict = new Dictionary<string, string> { { "DocumentType", "message_board" } };
        //selectIndexDocs.Start();


        //selectIndexDocs.Configure.InputPath = @"D:\DataProcess\TweetIndex\EbolaTwitter3";
        //selectIndexDocs.Configure.OutputPath = @"D:\DataProcess\TweetIndex\EbolaTwitter3_Sample0.1";
        //selectIndexDocs.Configure.IsSampling = true;
        //selectIndexDocs.Configure.SampleRatio = 0.1;
        //selectIndexDocs.Configure.IsSelectByExactMatch = false;
        //selectIndexDocs.Configure.IsSelectByTime = false;
        //////selectIndexDocs.Configure.StartDate = "2011-10-1";
        //////selectIndexDocs.Configure.EndDate = "2011-10-16";
        //////selectIndexDocs.Configure.IsSplitByTime = true;
        //////selectIndexDocs.Configure.SplitDayCount = 7;
        ////selectIndexDocs.Configure.StartDate = "2014-7-26";
        ////selectIndexDocs.Configure.EndDate = "2015-2-16";
        //selectIndexDocs.Configure.IsSplitByTime = false;
        ////selectIndexDocs.Configure.SplitDayCount = 7;
        //selectIndexDocs.Start();

        //var date = new DateTime(2013, 3, 22);
        //date = date.AddDays(48 * 7 - 1);
        //Trace.WriteLine(date.ToShortDateString());

        //BRTAnalysis.VisualizeTree(
        //    @"D:\Project\TopicPanorama\data\TopicGraphs\NewCode-Ebola-NT400Kmeans\Cache\Hierarchy\Hierarchy\12_news.word.gv"
        //    );
        //    //Util.GetIntArray(0, 61).ToList().ConvertAll(i => @"D:\Project\StreamingRoseRiver\Obama\Trees2\" + i + ".gv"),
        //    //Util.GetIntArray(9, 29).ToList().ConvertAll(i => @"D:\Project\StreamingRoseRiver\Ebola\Trees3\" + i + ".gv"),
        //    @"D:\Project\TopicPanorama\data\TopicGraphs\NewCode-Ebola-Test1\Cache\Hierarchy\Hierarchy\0_twitter.word.gv",
        //    //@"D:\Project\StreamingRoseRiver\Obama\Tree1-5000-1e-100\13.gv",
        //    //@"D:\Project\StreamingRoseRiver\Ebola\Trees\3.gv",
        //    //@"D:\Project\EvolutionaryRoseTreeData\ObamaData\Evolutionary\start[2012-9-30]span7slot125sample10000\0313_201107_gamma0.4alpha0.003KNN100merge1E-100split1E-100cos0.25newalpha1E-20_LooseOrder0.2_OCM_CSW0.65_520\100.gv",
        //    //@"D:\DataProcess\Index\Raw_EbolaEnBingNews_Ebola_0_1_RS_R-1_S\2014-07-27g.gv",
        //    //@"D:\DataProcess\Index\Raw_NSABingNews_NSA_EN_0_20_RS",
        //    //@"D:\DataProcess\Index\Raw_EbolaEnBingNews_Ebola_0_1_RS_R-1\",
        //    //@"D:\Project\StreamingRoseRiver\Obama\",
        //    null,
        //    //new string[] { "Barack", "Obama" });
        //    new string[] { "Ebola", "Ebolavirus", "Ebolaviruses", "BDBV", "SUDV", "TAFV", "EBOV", "RESTV", "EVD", "EHF" },
        //    false);

        //var removeSimilarDocuments = new NoiseRemoval.RemoveSimilarDocuments(true);
        //removeSimilarDocuments.Start();

        //LuceneIndexAnalysis.AnalyzeFieldValues(@"D:\Project\StreamingRoseRiver\Ebola\EbolaTwitter3_Sample0.01_MOD\", BingNewsFields.DiscoveryStringTime,
        //    str =>
        //    {
        //        var dateTime = StringOperations.ParseDateTimeString(str, BingNewsFields.TimeFormat);
        //        dateTime = dateTime.Subtract(TimeSpan.FromDays((int)dateTime.DayOfWeek));
        //        return dateTime.ToString("yyyy-MM-dd");
        //    });

        //NoiseRemoval.RemoveSimilarDocumentsConfigure config = new NoiseRemoval.RemoveSimilarDocumentsConfigure();
        //config.TestReadWrite();

        //BingNewsXMLAnalysis.AnalyzeLanguageDistribution(@"D:\AllTime_EN_NSA\");

        //BuildLuceneIndex buildLuceneIndex = new BuildLuceneIndex(BuildLuceneIndexType.BingNews, true);
        //buildLuceneIndex.Start();

        //var val = "我想测测自己定制的中文分词";
        //string outputPath = @"d:\Temp\TextIndex\";
        //string field = "Field1";
    }
    }
}
