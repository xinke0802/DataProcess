using DataProcess.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.DataAnalysis
{
    class WordOccurrenceConfigure : AbstractConfigure
    {
        private static readonly string _configFileName = "configWordOccurrence.txt";

        public WordOccurrenceConfigure():base(_configFileName)
        {

        }

        public string InputPath = null;
        public int TopWordCount = 1000;
        public int TopOccurrenceCount = 1000;
        public TokenizeConfig TokenizeConfig = new TokenizeConfig(TokenizerType.Standard, StopWordsFile.EN);
        public Dictionary<string, int> FieldWeightDict = new Dictionary<string, int>() { { BingNewsFields.NewsArticleHeadline, 3 }, { BingNewsFields.NewsArticleDescription, 1 } };
        public Dictionary<string, int> LeadingSentenceCntDict = new Dictionary<string, int> { { BingNewsFields.NewsArticleDescription, 6 } };
        public double SampleRatio = 1;
        public int SampleSeed = -1;
        public bool IsPrintCooccurrence = false;
    }

    /// <summary>
    /// Find the top words in the index, print these words and
    /// their co-occurrence relationships
    /// </summary>
    class WordOccurrence
    {
        public WordOccurrenceConfigure Configure { get; set; }

        public WordOccurrence(bool isLoadFromFile = false)
        {
            Configure = new WordOccurrenceConfigure();
            if (isLoadFromFile)
                Configure.Read();
        }

        public WordOccurrence(string inputPath, TokenizeConfig tokenizeConfig, Dictionary<string,int> FieldWeightDict,
            int TopWordCount = 100, int TopOccurrenceCount = 1000)
        {
            Configure = new WordOccurrenceConfigure();
            Configure.InputPath = inputPath;
            Configure.TokenizeConfig = tokenizeConfig;
            Configure.TopWordCount = TopWordCount;
            Configure.TopOccurrenceCount = TopOccurrenceCount;
            Configure.FieldWeightDict = FieldWeightDict;
        }

        public void Start()
        {
            if (!Configure.InputPath.EndsWith("\\"))
                Configure.InputPath += "\\";
            var reader = LuceneOperations.GetIndexReader(Configure.InputPath);
            var docNum = reader.NumDocs();
            var docNumPart = docNum / 100;

            Console.WriteLine("Total: " + docNum);

            Random random = new Random(Configure.SampleSeed == -1 ? (int)DateTime.Now.Ticks : Configure.SampleSeed);

            //Topwords
            var counter = new Counter<string>();
            for (int iDoc = 0; iDoc < docNum; iDoc++)
            {
                if (iDoc % docNumPart == 0)
                    Console.WriteLine(iDoc + "\t" + (iDoc / docNumPart) + "%");
                if (random.NextDouble() > Configure.SampleRatio)
                    continue;

                var doc = reader.Document(iDoc);
                var content = LuceneOperations.GetDocumentContent(doc, Configure.FieldWeightDict, Configure.LeadingSentenceCntDict);
                var words = NLPOperations.Tokenize(content, Configure.TokenizeConfig);
                foreach (var word in words)
                    counter.Add(word);
            }
            var topwords = counter.GetMostFreqObjs(Configure.TopWordCount);
            var wordCounterDict = counter.GetCountDictionary();

            var swTopWords = new StreamWriter(Configure.InputPath + "TopWords.txt");
            foreach (var topword in topwords)
                swTopWords.WriteLine(topword);
            swTopWords.Flush();
            swTopWords.Close();

            //CoOccurrence
            if (Configure.IsPrintCooccurrence)
            {
                var k = topwords.Count;
                var occurCounterDict = new Dictionary<string, Counter<string>>();
                foreach (var topword in topwords)
                    occurCounterDict.Add(topword, new Counter<string>());
                for (int iDoc = 0; iDoc < docNum; iDoc++)
                {
                    if (iDoc % docNumPart == 0)
                        Console.WriteLine(iDoc + "\t" + (iDoc / docNumPart) + "%");
                    if (random.NextDouble() > Configure.SampleRatio)
                        continue;

                    var doc = reader.Document(iDoc);
                    var content = LuceneOperations.GetDocumentContent(doc, Configure.FieldWeightDict, Configure.LeadingSentenceCntDict);
                    var words = Util.GetHashSet(NLPOperations.Tokenize(content, Configure.TokenizeConfig));
                    foreach (var word in words)
                    {
                        if (occurCounterDict.ContainsKey(word))
                        {
                            var occurCounter = occurCounterDict[word];
                            foreach (var word2 in words)
                            {
                                if (word2 == word)
                                    continue;
                                if (occurCounterDict.ContainsKey(word2))
                                    occurCounter.Add(word2);
                            }
                        }
                    }
                }
                var heapSort = new HeapSortDouble(Configure.TopOccurrenceCount);
                var pairDict = new Dictionary<int, Tuple<string, string>>();
                var iPair = 0;
                foreach (var kvp in occurCounterDict)
                {
                    var word = kvp.Key;
                    var occurCounter = kvp.Value;
                    foreach (var kvp2 in occurCounter.GetCountDictionary())
                    {
                        heapSort.Insert(iPair, kvp2.Value);
                        pairDict.Add(iPair, new Tuple<string, string>(word, kvp2.Key));
                        iPair++;
                    }
                }

                var swCoOccurrence = new StreamWriter(Configure.InputPath + "CoOccurrence.txt");
                foreach (var kvp in heapSort.GetSortedDictionary())
                {
                    var pair = pairDict[kvp.Key];
                    swCoOccurrence.WriteLine("{0} - {1}\t{2}",
                        pair.Item1, pair.Item2, kvp.Value);
                }

                swCoOccurrence.Flush();
                swCoOccurrence.Close();
            }

            reader.Close();
        }
    }
}
