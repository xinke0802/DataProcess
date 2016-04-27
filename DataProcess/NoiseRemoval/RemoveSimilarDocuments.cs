using DataProcess.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DataProcess.NoiseRemoval
{
    class RemoveSimilarDocumentsConfigure : AbstractConfigure
    {
        private static readonly string _configFileName = "configRemoveSimilarDocument.txt";

        public RemoveSimilarDocumentsConfigure()
            : base(_configFileName)
        {

        }

        public string InputPath = null;
        public string OutputPath = null;

        public bool IsRemoveSameURL = true;
        public string URLField = BingNewsFields.DocumentURL;

        public bool IsRemoveSimilarContent = true;
        public Dictionary<string, int> FieldWeightDict = new Dictionary<string, int>() { { BingNewsFields.NewsArticleHeadline, 3 }, { BingNewsFields.NewsArticleDescription, 1 } };
        public Dictionary<string, int> LeadingSentenceCntDict = new Dictionary<string, int> { { BingNewsFields.NewsArticleDescription, 6 } };
        public TokenizeConfig TokenizeConfig = new TokenizeConfig();
        public string TimeField = BingNewsFields.DiscoveryStringTime;
        public string ParseTimeFormat = "yy-MM-dd hh:mm:ss";
        public double MinDistinctiveDocumentCosine = 0.75;

        public int[] RemoveDateGranularity = new int[] { 1, 7, 30 };
        public int[] RemoveWordGranularity = new int[] { 30, 20, 10 };

        public bool IsDebug = true;
        public double MaxShowDebugCosine = 1;
    }

    class RemoveSimilarDocuments
    {
        public RemoveSimilarDocumentsConfigure Configure { get; protected set; }

        public RemoveSimilarDocuments(bool isLoadFromFile = false)
        {
            Configure = new RemoveSimilarDocumentsConfigure();
            if (isLoadFromFile)
                Configure.Read();


            //InputPath = config["InputPath"][0];
            //OutputPath = config["OutputPath"][0];

            //IsRemoveSameURL = bool.Parse(config["IsRemoveSameURL"][0]);
            //URLField = config["URLField"][0];

            //IsRemoveSimilarContent = bool.Parse(config["IsRemoveSimilarContent"][0]);
            //FieldWeightDict = StringOperations.ParseStringIntDictionary(config["FieldWeightDict"][0]);
            //IsRemoveSameURL = bool.Parse(config["IsRemoveSameURL"][0]);
            //TokenizeConfig = new TokenizeConfig(config["TokenizeConfig"][0]);
            //TimeField = config["TimeField"][0];
            //ParseTimeFormat = config["ParseTimeFormat"][0];

            //MinDistinctiveDocumentCosine = double.Parse(config["MinDistinctiveDocumentCosine"][0]);

            //RemoveDateGranularity = StringOperations.ParseIntArray(config["RemoveDateGranularity"][0]);
            //RemoveWordGranularity = StringOperations.ParseIntArray(config["RemoveWordGranularity"][0]);
        }

        static readonly string _debugFileName = "debug.txt";


        public void Start()
        {
            string debugFileName = Configure.OutputPath + _debugFileName;
            if (File.Exists(debugFileName))
                File.Delete(debugFileName);

            var reader = LuceneOperations.GetIndexReader(Configure.InputPath);
            List<int> docIDs = new List<int>();
            for (int iDoc = 0; iDoc < reader.NumDocs(); iDoc++)
            {
                docIDs.Add(iDoc);
            }

            if (Configure.IsRemoveSameURL)
            {
                Console.WriteLine("=====================RemoveSameURL=====================");
                docIDs = RemoveSameURLDocument(reader, docIDs);
            }

            if (Configure.IsRemoveSimilarContent)
            {
                for (int iGranu = 0; iGranu < Configure.RemoveDateGranularity.Length; iGranu++)
                {
                    int timeGranu = Configure.RemoveDateGranularity[iGranu];
                    int wordGranu = Configure.RemoveWordGranularity[iGranu];

                    Console.WriteLine("========Remove Similar Document: {0} out of {1}, Granu: {2} {3}========",
                            iGranu, Configure.RemoveDateGranularity.Length, timeGranu, wordGranu);

                    docIDs = RemoveSimilarDocumentsGranu(reader, docIDs, timeGranu, wordGranu);
                }

            }

            var writer = LuceneOperations.GetIndexWriter(Configure.OutputPath);
            foreach (var docID in docIDs)
                writer.AddDocument(reader.Document(docID));

            writer.Optimize();
            writer.Close();
            reader.Close();

            Console.WriteLine("All done");
            //Console.ReadKey();
        }

        private List<int> RemoveSameURLDocument(IndexReader reader, List<int> orgDocIDs)
        {
            var newDocIDs = new List<int>();

            var docNum = orgDocIDs.Count;
            HashSet<string> urlHash = new HashSet<string>();
            Console.WriteLine("Total {0} docs", docNum);

            int removeDocNum = 0;
            string urlfield = Configure.URLField;
            var progress = new ProgramProgress(docNum);
            foreach (var iDoc in orgDocIDs)
            {
                var document = reader.Document(iDoc);
                string url = document.Get(urlfield);
                if (url != null)
                {
                    url = url.ToLower();
                    if (!urlHash.Contains(url))
                    {
                        newDocIDs.Add(iDoc);
                        urlHash.Add(url);
                    }
                    else
                        removeDocNum++;
                }

                progress.PrintIncrementExperiment();
            }
            Console.WriteLine("Finished remove same URL. Removed {0} out of {1}", removeDocNum, docNum);

            return newDocIDs;
        }

        private List<int> RemoveSimilarDocumentsGranu(IndexReader reader, List<int> orgDocIDs,
            int timeWindowSize, int wordWindowSize)
        {
            var newDocIDs = new List<int>();
            var removeSimilarity = Configure.MinDistinctiveDocumentCosine;

            Dictionary<int, Dictionary<int, List<SparseVectorList>>> uniqueDocHash = new Dictionary<int, Dictionary<int, List<SparseVectorList>>>();
            int docNum = orgDocIDs.Count;

            int removeDocNum = 0;
            Dictionary<string, int> lexicon = new Dictionary<string, int>();

            int timeslicesize = 1;
            if (timeWindowSize >= 15)
            {
                int[] dividePieceNumbers = new int[] { 3, 4, 5, 7 };
                foreach (int dividePieceNumber in dividePieceNumbers)
                    if (timeWindowSize % dividePieceNumber == 0)
                    {
                        timeslicesize = timeWindowSize / dividePieceNumber;
                        break;
                    }
                if (timeslicesize == 1)
                {
                    timeslicesize = (timeWindowSize + 2) / 3;
                    timeWindowSize = 3;
                }
                else
                    timeWindowSize /= timeslicesize;
                Console.WriteLine("Reset window size! TimeSliceSize: {0}, WindowSize: {1}", timeslicesize, timeWindowSize);
            }
            int begintimedelta = -(timeWindowSize - 1) / 2;
            int endtimedelta = timeWindowSize / 2;
            var progress = new ProgramProgress(docNum);

            StreamWriter debugSw = null;
            if (Configure.IsDebug)
            {
                string fileName = Configure.OutputPath + "debug.txt";
                FileOperations.EnsureFileFolderExist(fileName);
                debugSw = new StreamWriter(fileName, true, Encoding.UTF8);
            }

            foreach (var iDoc in orgDocIDs)
            {
                var doc = reader.Document(iDoc);
                SparseVectorList vector = GetFeatureVector(doc, lexicon);
                if (vector == null)
                {
                    removeDocNum++;
                    continue;
                }

                vector.documentid = iDoc;
                int time = getDateTimeBingNews(doc) / timeslicesize;
                int[] words = getMostFreqWordIndex(vector, wordWindowSize);
                bool bunqiue = true;
                for (int stime = time + begintimedelta; stime <= time + endtimedelta; stime++)
                {
                    if (uniqueDocHash.ContainsKey(stime))
                    {
                        Dictionary<int, List<SparseVectorList>> wordHash = uniqueDocHash[stime];
                        foreach (int sword in words)
                        {
                            if (wordHash.ContainsKey(sword))
                            {
                                List<SparseVectorList> vectorList = wordHash[sword];
                                foreach (SparseVectorList svector in vectorList)
                                    if (SparseVectorList.Cosine(svector, vector) >= removeSimilarity)
                                    {
                                        if (Configure.IsDebug && removeDocNum <= 10000)
                                        {
                                            double simi = SparseVectorList.Cosine(svector, vector);
                                            if (simi <= Configure.MaxShowDebugCosine)
                                            {
                                                debugSw.WriteLine("---------------------------------------------------");
                                                debugSw.WriteLine(reader.Document(svector.documentid).Get(BingNewsFields.NewsArticleHeadline));//Get("NewsArticleDescription"));
                                                debugSw.WriteLine(reader.Document(vector.documentid).Get(BingNewsFields.NewsArticleHeadline));//Get("NewsArticleDescription"));
                                                debugSw.WriteLine("");
                                                string body1 = reader.Document(svector.documentid).Get(BingNewsFields.NewsArticleDescription);
                                                string body2 = reader.Document(vector.documentid).Get(BingNewsFields.NewsArticleDescription);
                                                if (body1.Length > 100)
                                                    body1 = body1.Substring(0, 100);
                                                if (body2.Length > 100)
                                                    body2 = body2.Substring(0, 100);
                                                debugSw.WriteLine(body1);
                                                debugSw.WriteLine(body2);
                                                debugSw.WriteLine(simi);
                                            }
                                            debugSw.Flush();
                                        }
                                        bunqiue = false;
                                        break;
                                    }
                            }
                            if (!bunqiue)
                                break;
                        }
                    }
                    if (!bunqiue)
                        break;

                }

                if (bunqiue)
                {
                    int keytime = time;
                    int keyword = words[0];
                    if (!uniqueDocHash.ContainsKey(keytime))
                        uniqueDocHash.Add(keytime, new Dictionary<int, List<SparseVectorList>>());
                    Dictionary<int, List<SparseVectorList>> wordHash = uniqueDocHash[keytime];
                    if (!wordHash.ContainsKey(keyword))
                        wordHash.Add(keyword, new List<SparseVectorList>());
                    List<SparseVectorList> list = wordHash[keyword];
                    list.Add(vector);

                    newDocIDs.Add(iDoc);
                }
                else
                    removeDocNum++;

                progress.PrintIncrementExperiment();
            }

            Console.WriteLine("Finished remove similar documents. Removed {0} out of {1}", removeDocNum, docNum);

            int listLengthSum = 0, listCnt = 0;
            foreach (Dictionary<int, List<SparseVectorList>> hash0 in uniqueDocHash.Values)
                foreach (List<SparseVectorList> list in hash0.Values)
                {
                    listLengthSum += list.Count;
                    listCnt++;
                }
            Console.WriteLine("AvgListLength: {0}, ListCnt: {1}", listLengthSum / listCnt, listCnt);

            if (Configure.IsDebug)
            {
                debugSw.Flush();
                debugSw.Close();
            }

            return newDocIDs;
        }

        private static int[] getMostFreqWordIndex(SparseVectorList featurevector, int k)
        {
            var sort = new HeapSortDouble(k);
            for (int iword = 0; iword < featurevector.keyarray.Length; iword++)
            {
                sort.Insert(featurevector.keyarray[iword], featurevector.valuearray[iword]);
            }

            return sort.GetTopIndices().ToArray<int>();
        }

        private int getDateTimeBingNews(Document document)
        {
            string timeStr = document.Get(Configure.TimeField);
            var dateTime = StringOperations.ParseDateTimeString(timeStr, Configure.ParseTimeFormat);
            return 366 * dateTime.Year + dateTime.DayOfYear;
        }



        private SparseVectorList GetFeatureVector(Document doc, Dictionary<string, int> lexicon)
        {
            SparseVectorList featurevector = new SparseVectorList();

            int lexiconindexcount = lexicon.Count;

            var content = LuceneOperations.GetDocumentContent(doc, Configure.FieldWeightDict, Configure.LeadingSentenceCntDict);
            var words = NLPOperations.Tokenize(content, Configure.TokenizeConfig);

            foreach (var word in words)
            {
                int value = 0;
                if (lexicon == null || lexicon.TryGetValue(word, out value) == false)
                {
                    lexicon.Add(word, lexiconindexcount);
                    value = lexiconindexcount;
                    lexiconindexcount++;
                }
                if (!featurevector.Increase(value, 1))
                {
                    featurevector.Insert(value, 1);
                }
            }

            featurevector.ListToArray();
            featurevector.count = featurevector.keyarray.Length;
            //featurevector.SumUpValueArray();
            if (featurevector.count < 1)
                return null;
            featurevector.InvalidateList();
            featurevector.GetNorm();
            return featurevector;
        }

    }
}


public class SparseVectorList
{
    public List<int> keylist = new List<int>();
    public List<int> valuelist = new List<int>();

    public int[] keyarray;
    public int[] valuearray;

    public double normvalue = 0;

    public int count = 0;

    public int documentid = -1;

    public SparseVectorList()
    {
    }

    public void GetNorm()
    {
        var array = this.valuearray;

        double sum = 0;
        double scale = 1000;

        if (array.Max() < scale)
        {
            for (int i = 0; i < array.Length; i++)
                sum += array[i] * array[i];
            this.normvalue = Math.Sqrt(sum);
        }
        else
        {
            double[] scaledarray = new double[array.Length];

            for (int i = 0; i < array.Length; i++)
                scaledarray[i] = array[i] / scale;

            for (int i = 0; i < array.Length; i++)
                sum += scaledarray[i] * scaledarray[i];
            this.normvalue = Math.Sqrt(sum) * scale;
        }
    }

    public void InvalidateList()
    {
        this.keylist = null;
        this.valuelist = null;
    }

    public bool Increase(int key, int incamount)
    {
        bool contains;
        int idx = Search(key, out contains);
        if (!contains)
            return false;

        valuelist[idx] += incamount;
        return true;
    }

    public void ListToArray()
    {
        this.keyarray = keylist.ToArray();
        this.valuearray = valuelist.ToArray();
    }

    public int Search(int key, out bool contains)
    {
        if (keylist.Count == 0)
        {
            contains = false;
            return -1;
        }
        if (key > keylist[keylist.Count - 1])
        {
            contains = false;
            return -2;
        }
        if (key < keylist[0])
        {
            contains = false;
            return -3;
        }

        int leftindex = 0;
        int rightindex = keylist.Count - 1;
        int midindex = (leftindex + rightindex) / 2;

        while (leftindex + 1 < rightindex)
        {
            if (keylist[midindex] < key)
                leftindex = midindex;
            else if (keylist[midindex] > key)
                rightindex = midindex;
            else
            {
                leftindex = midindex;
                rightindex = midindex + 1;
            }
            midindex = (leftindex + rightindex) / 2;
        }
        if (keylist[leftindex] == key)
        {
            contains = true;
            return leftindex;
        }
        if (keylist[rightindex] == key)
        {
            contains = true;
            return rightindex;
        }
        contains = false;
        return rightindex;
    }

    public void Add(int key, int value)
    {
        keylist.Add(key);
        valuelist.Add(value);
    }

    public void Insert(int key, int value)
    {
        if (this.keylist.Count == 0)
        {
            this.Add(key, value);
            return;
        }
        bool contains;
        int idx = Search(key, out contains);

        if (idx == -2)
        {
            keylist.Add(key);
            valuelist.Add(value);
            return;
        }
        else if (idx == -3)
        {
            keylist.Insert(0, key);
            valuelist.Insert(0, value);
            return;
        }
        if (contains == true)
        {
            Console.WriteLine("contains the key");
            //                int stop;
        }
        keylist.Insert(idx, key);
        valuelist.Insert(idx, value);
    }

    public static double DotProduct(SparseVectorList featurevector1, SparseVectorList featurevector2)
    {
        int pt1 = 0;
        int pt2 = 0;
        int length1 = featurevector1.count;
        int length2 = featurevector2.count;
        double ret = 0;
        int[] keys1 = featurevector1.keyarray;
        int[] values1 = featurevector1.valuearray;
        int[] keys2 = featurevector2.keyarray;
        int[] values2 = featurevector2.valuearray;

        while (true)
        {
            while (pt1 < length1 && keys1[pt1] < keys2[pt2]) pt1++;
            if (pt1 == length1) break;
            if (keys1[pt1] == keys2[pt2])
            {
                ret += (double)values1[pt1] * values2[pt2];
                pt1++;
                pt2++;
            }
            else
            {
                while (pt2 < length2 && keys2[pt2] < keys1[pt1]) pt2++;
                if (pt2 == length2) break;
                if (keys2[pt2] == keys1[pt1])
                {
                    ret += (double)values1[pt1] * values2[pt2];
                    pt1++;
                    pt2++;
                }
            }
            if (pt1 == length1 || pt2 == length2) break;
        }
        return ret;
    }

    public static double Cosine(SparseVectorList featurevector1, SparseVectorList featurevector2)
    {
        double cosine;
        if (featurevector1.count > featurevector2.count)
        {
            cosine = Cosine(featurevector2, featurevector1);
        }
        else
        {
            long t = DateTime.Now.Ticks;
            cosine = DotProduct(featurevector1, featurevector2) / featurevector1.normvalue / featurevector2.normvalue;
        }
        return cosine;
    }
}