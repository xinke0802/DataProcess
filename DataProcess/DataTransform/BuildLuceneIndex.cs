using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataProcess.NoiseRemoval;
using HtmlAgilityPack;
using Lucene.Net.Documents;
using DataProcess.Utils;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Directory = System.IO.Directory;
using Ionic.Zip;
using System.Xml;
using System.Threading;

namespace DataProcess.DataTransform
{
    public enum BuildLuceneIndexType { Twitter, Weibo, BingNews };

    public class TwitterBuildLuceneIndexConfigure : AbstractConfigure
    {
        private static readonly string _configureFileName = "configBuildLuceneIndexTwitter.txt";
        public TwitterBuildLuceneIndexConfigure()
            : base(_configureFileName)
        {
        }

        public string InputPath = "D:\\TwitterTxt.txt";
        public string OutputPath = "D:\\TwitterIndex";
        public string[] TwitterSchema = new string[] { "Field1", "Field2" };
        public string TwitterBodyField = "TwitterBodyField";
    }

    public class WeiboBuildLuceneIndexConfigure : AbstractConfigure
    {
        private static readonly string _configureFileName = "configBuildLuceneIndexWeibo.txt";
        public WeiboBuildLuceneIndexConfigure()
            : base(_configureFileName)
        {
        }

        public string InputPath = "D:\\Weibo";
        public string OutputPath = "D:\\WeiboIndex";
    }

    public class BingNewsBuildLuceneIndexConfigure : AbstractConfigure
    {
        private static readonly string _configureFileName = "configBuildLuceneIndexBingNews.txt";

        public BingNewsBuildLuceneIndexConfigure()
            : base(_configureFileName)
        {

        }

        public List<string> BingNewsPaths = new string[] { "D:\\BingNewsPath1", "F:\\BingNewsPath2" }.ToList();
        public List<string> IndexPaths = new string[] { "D:\\IndexPath1", "D:\\IndexPath2" }.ToList();
        public List<string[]> KeywordLists = new string[][] { new string[] { "Keyword11", "Keyword12" }, new string[] { "Keyword21", "Keyword22", "Keyword32" } }.ToList();
        public string StartDate = "2010-01-01";
        public string EndDate = DateTime.Now.ToString("yyyy-MM-dd");
        public List<string> Languages = new string[] { "en", "zh" }.ToList();
        public int iProcessor = 0;
        public int ProcessorNum = 1;
        public int MaxThreadNum = 10;
    }

    /// <summary>
    /// Build a lucene index from raw data
    /// </summary>
    public class BuildLuceneIndex
    {
        public BuildLuceneIndexType BuildLuceneIndexType { get; protected set; }

        public TwitterBuildLuceneIndexConfigure TwitterConfigure { get; protected set; }
        public WeiboBuildLuceneIndexConfigure WeiboConfigure { get; protected set; }
        public BingNewsBuildLuceneIndexConfigure BingNewsConfigure { get; protected set; }


        public BuildLuceneIndex(BuildLuceneIndexType type, bool isLoadFromFile = false)
        {
            BuildLuceneIndexType = type;

            switch (type)
            {
                case BuildLuceneIndexType.Twitter:
                    TwitterConfigure = new TwitterBuildLuceneIndexConfigure();
                    if (isLoadFromFile)
                        TwitterConfigure.Read();
                    break;
                case BuildLuceneIndexType.Weibo:
                    WeiboConfigure = new WeiboBuildLuceneIndexConfigure();
                    if (isLoadFromFile)
                        WeiboConfigure.Read();
                    break;
                case BuildLuceneIndexType.BingNews:
                    BingNewsConfigure = new BingNewsBuildLuceneIndexConfigure();
                    if (isLoadFromFile)
                        BingNewsConfigure.Read();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void Start()
        {
            switch (BuildLuceneIndexType)
            {
                case BuildLuceneIndexType.Twitter:
                    BuildFromTwitterTxt();
                    break;
                case BuildLuceneIndexType.Weibo:
                    BuildFromWeiboWebPages();
                    break;
                case BuildLuceneIndexType.BingNews:
                    BuildFromBingNewsXMLs();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Twitter data: from cosmos, each line represents a Tweet. 
        /// Different fields are seperated by '\t'. The schema is the name for each field
        /// </summary>
        private void BuildFromTwitterTxt()
        {
            string inputpath = TwitterConfigure.InputPath;
            string outputpath = TwitterConfigure.OutputPath;
            var schema = TwitterConfigure.TwitterSchema;
            string bodyField = TwitterConfigure.TwitterBodyField;

            var indexwriter = LuceneOperations.GetIndexWriter(outputpath);

            StreamReader sr = new StreamReader(inputpath);
            string line;
            int lineCnt = 0;
            while ((line = sr.ReadLine()) != null)
                lineCnt++;
            //Console.WriteLine("Total Lines: " + lineCnt);
            sr.Close();

            sr = new StreamReader(inputpath);
            var seperator = new char[] { '\t' };
            int lineIndex = 0;
            var progress = new ProgramProgress(lineCnt);
            while ((line = sr.ReadLine()) != null)
            {
                //if (lineIndex % 100000 == 0)
                //    Console.WriteLine("{0} out of {1} ({2}%)", lineIndex, lineCnt, 100 * lineIndex / lineCnt);

                var tokens = line.Split(seperator);//, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != schema.Length)
                    throw new Exception("Unmatch schema");
                var document = new Document();
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (schema[i] == bodyField)
                        tokens[i] = RemoveContentNoise.RemoveTweetIndexNoise(tokens[i]);
                    document.Add(new Field(schema[i], tokens[i], Field.Store.YES, Field.Index.ANALYZED));
                }
                indexwriter.AddDocument(document);

                lineIndex++;
                progress.PrintIncrementExperiment();
            }
            progress.PrintTotalTime();

            sr.Close();

            indexwriter.Optimize();
            indexwriter.Close();
        }


        private void BuildFromWeiboWebPages()
        {
            var indexWriter = LuceneOperations.GetIndexWriter(WeiboConfigure.OutputPath);
            //int totalWeiboCount = 0;
            //int totalFileCount = 0;
            foreach (var filename in Directory.EnumerateFiles(WeiboConfigure.InputPath, "*.txt", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(filename).StartsWith("_"))
                    continue;
                var parser = new WeiboParser(filename);
                foreach (var weibo in parser.GetContainedWeibo())
                {
                    Document doc = new Document();
                    doc.Add(new Field(WeiboLuceneFields.UserNickName, weibo.UserNickName, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field(WeiboLuceneFields.UserID, weibo.UserID, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field(WeiboLuceneFields.NewsArticleDescription, weibo.Content, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field(WeiboLuceneFields.DiscoveryStringTime, weibo.Time, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field(WeiboLuceneFields.Source, weibo.Source, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field(WeiboLuceneFields.UpCount, weibo.UpCount.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field(WeiboLuceneFields.ForwardCount, weibo.ForwardCount.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field(WeiboLuceneFields.CollectCount, weibo.CollectCount.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field(WeiboLuceneFields.ReplyCount, weibo.ReplyCount.ToString(), Field.Store.YES, Field.Index.ANALYZED));

                    indexWriter.AddDocument(doc);
                }
                //Console.WriteLine(filename);
                //var cnt = parser.GetContainedWeibo().Count;
                //Console.WriteLine(cnt);
                //totalWeiboCount += cnt;
                //totalFileCount++;
            }

            //Console.WriteLine("Total count:" + totalWeiboCount);
            //Console.WriteLine("Total file count: " + totalFileCount);

            indexWriter.Optimize();
            indexWriter.Close();
        }

        #region WeiboParser
        class WeiboLuceneFields
        {
            public static readonly string UserNickName = "UserNickName";
            public static readonly string UserID = "UserID";
            public static readonly string NewsArticleDescription = "NewsArticleDescription";
            public static readonly string DiscoveryStringTime = "DiscoveryStringTime";
            public static readonly string TimeTicks = "TimeTicks";
            public static readonly string Source = "Source";
            public static readonly string UpCount = "UpCount";
            public static readonly string ForwardCount = "ForwardCount";
            public static readonly string CollectCount = "CollectCount";
            public static readonly string ReplyCount = "ReplyCount";

        }

        class WeiboParser
        {
            static int globalid = 0;
            string filename;
            int id;

            public WeiboParser(string filename)
            {
                this.filename = filename;
                this.id = globalid++;
            }


            public List<Weibo> GetContainedWeibo()
            {
                var scriptDoc = GetInsideScriptDocument();

                var weiboNodes = GetWeiboNodes(scriptDoc);
                var weibos = new List<Weibo>();
                foreach (var weiboNode in weiboNodes)
                    weibos.Add(new Weibo(weiboNode));

                return weibos;
            }

            private List<HtmlNode> GetWeiboNodes(HtmlDocument scriptDoc)
            {
                var weiboNodes = new List<HtmlNode>();
                foreach (var node in scriptDoc.DocumentNode.SelectNodes("/div/div/dl"))
                {
                    weiboNodes.Add(node);
                    //Trace.WriteLine(node.InnerText);
                    //Trace.WriteLine("----------------------------------");
                }
                return weiboNodes;
            }

            private HtmlDocument GetInsideScriptDocument()
            {
                HtmlDocument doc = new HtmlDocument();
                doc.Load(filename, Encoding.GetEncoding("utf-8"));
                List<HtmlNode> nodes = new List<HtmlNode>();
                foreach (var node in doc.DocumentNode.SelectNodes("/html/script"))
                {
                    if (node.HasChildNodes && node.ChildNodes.Count == 1)
                    {
                        var child = node.ChildNodes[0];
                        var text = child.InnerText;
                        if (text.Contains("转发") && text.Contains("收藏") && text.Contains("评论"))
                        {
                            nodes.Add(child);
                        }
                    }
                }

                if (nodes.Count != 1)
                    throw new Exception("Cannot find appropriate node!");

                var doc2 = new HtmlDocument();
                doc2.LoadHtml(nodes[0].InnerHtml);

                return doc2;
            }
        }

        class Weibo
        {
            public string UserNickName { get; protected set; }
            public string UserID { get; protected set; }
            public string Content { get; protected set; }
            //public string ForwardContent { get; protected set; }
            public string Time { get; protected set; }
            //public long TimeTicks { get; protected set; }
            public string Source { get; protected set; }
            public int UpCount { get; protected set; }
            public int ForwardCount { get; protected set; }
            public int CollectCount { get; protected set; }
            public int ReplyCount { get; protected set; }

            HtmlNode weiboNode;
            public Weibo(HtmlNode weiboNode)
            {
                this.weiboNode = weiboNode;

                var paraNodes = GetParaNodes();

                var p1Node = paraNodes[0];

                var userInfoNode = getUserNode(p1Node);

                //user name
                UserNickName = userInfoNode.InnerText;

                //user id
                var userCard = userInfoNode.GetAttributeValue("usercard", "");
                if (userCard != null && userCard.Length > 0)
                {
                    var index0 = userCard.IndexOf("id=") + 3;
                    var index1 = userCard.IndexOf('&', index0);
                    UserID = userCard.Substring(index0, index1 - index0);
                }

                //content
                var contentNode = getContentNode(p1Node);
                Content = contentNode.InnerText;


                var p2Node = paraNodes[1];

                //time
                var timeNode = GetTimeNode(p2Node);
                string time;
                long ticks;
                ParseTimeString(timeNode.InnerText, out time, out ticks);
                this.Time = time;
                //this.TimeTicks = ticks;

                //source
                var sourceNode = GetSourceNode(p2Node);
                Source = sourceNode == null ? "" : sourceNode.InnerText;

                //赞，收藏，转发，评论
                var spanNode = GetSpanNode(p2Node);
                foreach (var node in spanNode.SelectNodes("a"))
                {
                    var text = node.InnerText;
                    if (text.StartsWith("赞"))
                    {
                        if (text.Contains("("))
                        {
                            var index1 = text.IndexOf("(") + 1;
                            var index2 = text.IndexOf(")", index1 + 1);
                            UpCount = int.Parse(text.Substring(index1, index2 - index1));
                        }
                    }
                    else if (text.StartsWith("转发"))
                    {
                        if (text.Contains("("))
                        {
                            var index1 = text.IndexOf("(") + 1;
                            var index2 = text.IndexOf(")", index1 + 1);
                            ForwardCount = int.Parse(text.Substring(index1, index2 - index1));
                        }
                    }
                    else if (text.StartsWith("收藏"))
                    {
                        if (text.Contains("("))
                        {
                            var index1 = text.IndexOf("(") + 1;
                            var index2 = text.IndexOf(")", index1 + 1);
                            CollectCount = int.Parse(text.Substring(index1, index2 - index1));
                        }
                    }
                    else if (text.StartsWith("评论"))
                    {
                        if (text.Contains("("))
                        {
                            var index1 = text.IndexOf("(") + 1;
                            var index2 = text.IndexOf(")", index1 + 1);
                            ReplyCount = int.Parse(text.Substring(index1, index2 - index1));
                        }
                    }
                    else
                        throw new Exception("Unexpected!");
                }
            }

            private HtmlNode GetSpanNode(HtmlNode p2Node)
            {
                var nodes = new List<HtmlNode>();
                foreach (var node in p2Node.SelectNodes("span"))
                {
                    nodes.Add(node);
                }
                if (nodes.Count != 1)
                    throw new Exception("Expect 1 such node!");

                return nodes[0];
            }

            private void ParseTimeString(string time, out string Time, out long TimeTicks)
            {
                //var dateTime = new DateTime(DateTime.Now.Ticks);

                var tokens = time.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int year = DateTime.Now.Year;
                int month = DateTime.Now.Month;
                int day = DateTime.Now.Day;
                int hour = DateTime.Now.Hour;
                int minute = DateTime.Now.Minute;

                var tokens0 = tokens[0].Split(new string[] { "年", "月", "日" }, StringSplitOptions.RemoveEmptyEntries);
                var basei = 3 - tokens.Length;
                for (int i = basei; i < 3; i++)
                {
                    switch (i)
                    {
                        case 0:
                            year = int.Parse(tokens0[i - basei]);
                            break;
                        case 1:
                            month = int.Parse(tokens0[i - basei]);
                            break;
                        case 2:
                            day = int.Parse(tokens0[i - basei]);
                            break;
                    }
                }

                var tokens1 = tokens[1].Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                hour = int.Parse(tokens1[0]);
                minute = int.Parse(tokens1[1]);

                var dateTime = new DateTime(year, month, day, hour, minute, 0);
                Time = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                TimeTicks = dateTime.Ticks;
            }

            private HtmlNode GetTimeNode(HtmlNode p2Node)
            {
                var nodes = new List<HtmlNode>();
                foreach (var node in p2Node.SelectNodes("a"))
                {
                    nodes.Add(node);
                }
                if (nodes.Count != 1 && nodes.Count != 2)
                    throw new Exception("Expect 2 such node!");

                return nodes[0];
            }

            private HtmlNode GetSourceNode(HtmlNode p2Node)
            {
                var nodes = new List<HtmlNode>();
                foreach (var node in p2Node.SelectNodes("a"))
                {
                    nodes.Add(node);
                }
                if (nodes.Count != 1 && nodes.Count != 2)
                    throw new Exception("Expect 2 such node!");

                if (nodes.Count == 2)
                    return nodes[1];
                else
                    return null;
            }


            private HtmlNode getContentNode(HtmlNode p1Node)
            {
                var nodes = new List<HtmlNode>();
                foreach (var node in p1Node.SelectNodes("em"))
                {
                    nodes.Add(node);
                }
                if (nodes.Count != 1)
                    throw new Exception("Expect 1 content node!");

                return nodes[0];
            }

            private HtmlNode getUserNode(HtmlNode p1Node)
            {
                var nodes = new List<HtmlNode>();
                foreach (var node in p1Node.SelectNodes("a"))
                {
                    nodes.Add(node);
                }
                if (nodes.Count != 1)
                    throw new Exception("Expect 1 user node!");

                return nodes[0];
            }

            private List<HtmlNode> GetParaNodes()
            {
                var nodes = new List<HtmlNode>();
                foreach (var node in weiboNode.SelectNodes("dd/p"))
                {
                    if (node.HasAttributes && node.Attributes.Count == 1)
                    {
                        var attribute = node.Attributes[0];
                        if (attribute.Name == "node-type" && attribute.Value == "feed_list_content" ||
                            attribute.Name == "class" && attribute.Value == "info W_linkb W_textb")
                            nodes.Add(node);
                    }
                }
                if (nodes.Count != 2)
                    throw new Exception("Expect 2 para nodes!");

                return nodes;
            }

            public override string ToString()
            {
                string str = "";
                str += string.Format("{0},{1}\n", "UserNickName", UserNickName);
                str += string.Format("{0},{1}\n", "UserID", UserID);
                str += string.Format("{0},{1}\n", "Content", Content);
                //str += string.Format("{0},{1}", "ForwardContent", ForwardContent);
                str += string.Format("{0},{1}\n", "Time", Time);
                str += string.Format("{0},{1}\n", "UpCount", UpCount);
                str += string.Format("{0},{1}\n", "ForwardCount", ForwardCount);
                str += string.Format("{0},{1}\n", "CollectCount", CollectCount);
                str += string.Format("{0},{1}\n", "CommentCount", ReplyCount);
                return str;
            }
        }
        #endregion

        private void BuildFromBingNewsXMLs()
        {
            string[] selectedFields = new string[] { 
                "DocumentURL", "DocumentUrl", "Country", "NewsArticleCategoryData",
                "NewsArticleHeadline", "NewsArticleDescription", 
                "DiscoveryStringTime", "PublishedDateTime",
                "DownloadStringTime", "PublishedDateTime", "NewsSource"}; //NewsArticleBodyNEMap, RealTimeType

            List<string> bingnewspaths = BingNewsConfigure.BingNewsPaths;
            int iProcessor = BingNewsConfigure.iProcessor;
            int processorNum = BingNewsConfigure.ProcessorNum;
            string startdate = BingNewsConfigure.StartDate;
            string enddate = BingNewsConfigure.EndDate;
            List<string[]> keywordLists = BingNewsConfigure.KeywordLists;
            List<string> indexpaths = BingNewsConfigure.IndexPaths;
            List<string> languages = BingNewsConfigure.Languages;
            int maxThreadNum = BingNewsConfigure.MaxThreadNum;

            //LoadExtractBingNewsDataConfig_KeyWordList(out bingnewspaths,
            //    out iProcessor, out processorNum, out startdate, out enddate,
            //    out keywordLists, out languages, out indexpaths);

            List<string> outputdirs = new List<string>();
            List<string> infofilenames = new List<string>();
            int ikeyword2 = 0;
            foreach (string indexpath in indexpaths)
            {
                string outputdir = indexpath + "BingNews_" + keywordLists[ikeyword2][0] + "_" + iProcessor + "_" + processorNum;
                if (!Directory.Exists(outputdir))
                    Directory.CreateDirectory(outputdir);
                infofilenames.Add(indexpath + "BingNews_" + keywordLists[ikeyword2][0] + "_" + iProcessor + "_" + processorNum + ".dat");
                outputdirs.Add(outputdir);
                ikeyword2++;
            }

            List<IndexWriter> indexwriters = new List<IndexWriter>();
            List<StreamWriter> infofiles = new List<StreamWriter>();
            for (ikeyword2 = 0; ikeyword2 < keywordLists.Count; ikeyword2++)
            {
                IndexWriter indexwriter = LuceneOperations.GetIndexWriter(outputdirs[ikeyword2]);
                StreamWriter infofile = new StreamWriter(infofilenames[ikeyword2]);
                indexwriters.Add(indexwriter);
                infofiles.Add(infofile);
            }

            List<string> allfilenames = new List<string>();
            foreach (var bingnewpath in bingnewspaths)
                allfilenames.AddRange(Directory.GetFiles(bingnewpath, "*.*", System.IO.SearchOption.AllDirectories));
            allfilenames = FilterDates(allfilenames, startdate, enddate).ToList();
            List<string> filenames = new List<string>();
            for (int i = iProcessor; i < allfilenames.Count; i += processorNum)
            {
                filenames.Add(allfilenames[i]);
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ProgramProgress progress = new ProgramProgress(filenames.Count);
            //ProgramProgress progress = new ProgramProgress(filenames.Count / processorNum);

            int[] newsfoundcnts = new int[keywordLists.Count];

            DateTime time_begin_1 = DateTime.Now;
            //for (int ifilename = iProcessor; ifilename < filenames.Count; ifilename += processorNum)

            if (maxThreadNum == 1)
            {
                foreach (var filename in filenames)
                    BuildLuceneFromFile(filename, keywordLists, indexwriters, languages, selectedFields, newsfoundcnts, infofiles, progress);
            }
            else
            {
                ParallelOptions options = new ParallelOptions();
                options.MaxDegreeOfParallelism = maxThreadNum;
                object obj = new Object();

                Parallel.ForEach(filenames, options, filename => BuildLuceneFromFile(filename, keywordLists, indexwriters, languages, selectedFields, newsfoundcnts, infofiles, progress));
            }

            for (ikeyword2 = 0; ikeyword2 < keywordLists.Count; ikeyword2++)
            {
                infofiles[ikeyword2].WriteLine("Extract xml time\t" + stopwatch.Elapsed);
            }

            Console.WriteLine("Start writing to lucene index...");

            Stopwatch stopwatch2 = new Stopwatch();
            stopwatch2.Start();

            for (ikeyword2 = 0; ikeyword2 < keywordLists.Count; ikeyword2++)
            {
                indexwriters[ikeyword2].Optimize();
                indexwriters[ikeyword2].Close();
            }

            for (ikeyword2 = 0; ikeyword2 < keywordLists.Count; ikeyword2++)
            {
                infofiles[ikeyword2].WriteLine("Write to lucene index time\t" + stopwatch2.Elapsed);
                infofiles[ikeyword2].WriteLine("Total time\t" + stopwatch.Elapsed);
                infofiles[ikeyword2].Flush();
                infofiles[ikeyword2].Close();
            }
        }

        private void BuildLuceneFromFile(string filename, List<string[]> keywordLists, List<IndexWriter> indexwriters, List<string> languages, string[] selectedFields,
            int[] newsfoundcnts, List<StreamWriter> infofiles, ProgramProgress progress)
        {
            //string filename = filenames[ifilename];
            int deltanewsfoundcnt = 0;

            ZipFile zipfile = null;
            List<XmlDocument> xmldocs = new List<XmlDocument>();
            if (filename.EndsWith(".zip"))
            {
                zipfile = new ZipFile(filename);
                MemoryStream ms = new MemoryStream();
                foreach (ZipEntry entry in zipfile.Entries)
                {
                    entry.Extract(ms);
                    ms.Position = 0;
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(ms);
                    xmldocs.Add(xmldoc);
                    ms.Dispose();
                }
            }
            else
            {
                try
                {
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(filename);
                    xmldocs.Add(xmldoc);
                }
                catch
                {
                    var xmldoclist = GetXMLDocList(filename);
                    xmldocs.AddRange(xmldoclist);
                }
            }
            foreach (XmlDocument xmldoc in xmldocs)
            {
                XmlNodeList list = xmldoc.GetElementsByTagName("NewsArticleDescription");
                foreach (XmlNode bodynemapnode in list)
                {
                    for (int ikeyword = 0; ikeyword < keywordLists.Count; Interlocked.Increment(ref ikeyword))
                    {
                        var keywords = keywordLists[ikeyword];
                        IndexWriter indexwriter = indexwriters[ikeyword];
                        string str = bodynemapnode.InnerText;
                        bool bStore = false;
                        foreach (var keyword in keywords)
                            if (str.Contains(keyword))
                            {
                                bStore = true;
                                break;
                            }

                        if (bStore)
                        {
                            XmlNode newsnode = bodynemapnode.ParentNode;
                            XmlNode languagenode = newsnode.SelectSingleNode("Language");
                            //Test whether it is written in english
                            if (!languages.Contains(languagenode.InnerText))
                                continue;

                            /// Unique Document ///
                            //Extract all useful fields
                            string docid = newsnode.Attributes[0].Value;
                            Document document = new Document();
                            document.Add(new Field("DocId", docid, Field.Store.YES, Field.Index.ANALYZED));
                            foreach (string fieldname in selectedFields)
                            {
                                XmlNode node = newsnode.SelectSingleNode(fieldname);
                                if (node != null)
                                {
                                    string luceneFieldName = fieldname;
                                    if (luceneFieldName == "DocumentUrl")
                                        luceneFieldName = "DocumentURL";
                                    document.Add(new Field(luceneFieldName, node.InnerText, Field.Store.YES, Field.Index.ANALYZED));
                                }
                            }

                            indexwriter.AddDocument(document);
                            Interlocked.Increment(ref newsfoundcnts[ikeyword]);
                            deltanewsfoundcnt++;
                        }
                    }
                }

                /// Delete temp file 
                //File.Delete(extractpath + entry.FileName);
            }

            for (int ikeyword = 0; ikeyword < keywordLists.Count; ikeyword++)
            {
                infofiles[ikeyword].WriteLine(filename + "\t\t" + deltanewsfoundcnt + "\t\t" + newsfoundcnts[ikeyword]);
                infofiles[ikeyword].Flush();
            };

            progress.PrintIncrementExperiment();
        }

        private static string[] FilterDates(IEnumerable<string> dates, string startDate, string endDate)
        {
            DateTime startDateTime = StringOperations.ParseDateTimeString(startDate, "yy-MM-dd");
            DateTime endDateTime = StringOperations.ParseDateTimeString(endDate, "yy-MM-dd");

            List<string> selectedDates = new List<string>();
            List<string> pureDates = new List<string>();
            Dictionary<string, string> puredateToDate = new Dictionary<string, string>();
            foreach (string date in dates)
            {
                try
                {
                    var filename = StringOperations.GetFileName(date);
                    string date_pure = filename.Substring(0, 10);//date.Substring(date.Length - 10, 10);
                    DateTime dateTime = StringOperations.ParseDateTimeString(date_pure, "yy-MM-dd");
                    if (dateTime.Ticks >= startDateTime.Ticks &&
                        dateTime.Ticks <= endDateTime.Ticks)
                    {
                        //selectedDates.Add(date);
                        if (puredateToDate.ContainsKey(date_pure))
                            throw new Exception("Duplicate Dates!");
                        puredateToDate.Add(date_pure, date);
                        pureDates.Add(date_pure);
                    }
                }
                catch
                {
                }
            }

            pureDates.Sort();
            foreach (string puredate in pureDates)
                selectedDates.Add(puredateToDate[puredate]);
            return selectedDates.ToArray();
        }

        public static List<XmlDocument> GetXMLDocList(string filename)
        {
            StreamReader sr = new StreamReader(filename);
            string line;
            List<XmlDocument> xmldoclist = new List<XmlDocument>();
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("<News ID="))
                {
                    string newsBody = line;
                    while (!(line = sr.ReadLine()).Contains("</News>"))
                        newsBody += "\n" + line;
                    newsBody += "\n\t</News>\n";
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(newsBody);
                    xmldoclist.Add(xmldoc);
                }
            }
            return xmldoclist;
        }


    }
}
