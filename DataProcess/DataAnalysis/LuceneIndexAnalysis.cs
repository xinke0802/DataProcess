using DataProcess.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.DataAnalysis
{
    class LuceneIndexAnalysis
    {
        public static void AnalyzeSearchWordSentiment(string indexPath, string field, string[] keywords, int printDocumentCnt = 10, string histogramField = null)
        {
            var searcher = LuceneOperations.GetIndexSearcher(indexPath);
            var reader = searcher.GetIndexReader();
            var docIDs = LuceneOperations.Search(searcher, StringOperations.GetMergedString(keywords, " "), field);

            Console.WriteLine("Find {0}% ({1}/{2}) documents containing: {3}", (100.0*docIDs.Count/reader.NumDocs()), docIDs.Count, reader.NumDocs(), StringOperations.GetMergedString(keywords, " "));

            var progress = new ProgramProgress(docIDs.Count);
            var sentiAnalyzer = new SentimentAnalyzer();
            SentimentType sentimentType;
            double sentimentScore;
            HeapSortDouble hsdPos = new HeapSortDouble(printDocumentCnt);
            HeapSortDouble hsdNeg = new HeapSortDouble(printDocumentCnt);
            Counter<string> counterPos = null;
            Counter<string> counterNeg = null;
            Counter<string> counterNeu = null;
            if (histogramField != null)
            {
                counterPos = new Counter<string>();
                counterNeg = new Counter<string>();
                counterNeu = new Counter<string>();
            }
            int posCnt = 0;
            int negCnt = 0;
            int neuCnt = 0;
            foreach (var docID in docIDs)
            {
                var document = reader.Document(docID);
                var content = document.Get(field);
                sentiAnalyzer.GetSentiment(content, out sentimentType, out sentimentScore);

                switch (sentimentType)
                {
                    case SentimentType.Positive:
                        posCnt++;
                        hsdPos.Insert(docID, Math.Abs(sentimentScore));
                        if (histogramField != null)
                            counterPos.Add(document.Get(histogramField));
                        break;
                    case SentimentType.Negative:
                        negCnt++;
                        hsdNeg.Insert(docID, Math.Abs(sentimentScore));
                        if (histogramField != null)
                            counterNeg.Add(document.Get(histogramField));
                        break;
                    case SentimentType.Neutral:
                        neuCnt++;
                        if (histogramField != null)
                            counterNeu.Add(document.Get(histogramField));
                        break;
                    default:
                        throw new NotImplementedException();
                }

                progress.PrintIncrementExperiment();
            }

            Console.WriteLine("Positive document ratio {0}% ({1}/{2})", Math.Round(100.0 * posCnt / docIDs.Count), posCnt, docIDs.Count);
            Console.WriteLine("Negatvie document ratio {0}% ({1}/{2})", Math.Round(100.0 * negCnt / docIDs.Count), negCnt, docIDs.Count);
            Console.WriteLine("Neutral document ratio {0}% ({1}/{2})", Math.Round(100.0 * neuCnt / docIDs.Count), neuCnt, docIDs.Count);

            Console.WriteLine(StringOperations.WrapWithDash("Positive documents"));
            foreach (var kvp in hsdPos.GetSortedDictionary())
            {
                Console.WriteLine(kvp.Value + "\t" + reader.Document(kvp.Key).Get(field));
            }

            Console.WriteLine(StringOperations.WrapWithDash("Negative documents"));
            foreach (var kvp in hsdNeg.GetSortedDictionary())
            {
                Console.WriteLine(kvp.Value + "\t" + reader.Document(kvp.Key).Get(field));
            }

            progress.PrintTotalTime();

            if (histogramField != null)
            {
                string[] featureStrings = new[] {"Pos", "Neg", "Neu"};
                Counter<string>[] counters = new[] {counterPos, counterNeg, counterNeu};
                for (int i = 0; i < featureStrings.Length; i++)
                {
                    Console.WriteLine(StringOperations.WrapWithDash(histogramField + " " + featureStrings[i]));
                    int index = 0;
                    foreach (var kvp in counters[i].GetCountDictionary().OrderByDescending(kvp=>kvp.Value))
                    {
                        Console.WriteLine(kvp.Key + "\t" + kvp.Value);
                        if (++index >= 100)
                            break;
                    }
                }
            }

            Console.ReadKey();
        }


        public static void AnalyzeTwitterWordDistribution(string inputPath, TokenizeConfig tokenConfig)
        {
            var indexReader = LuceneOperations.GetIndexReader(inputPath);
            var docNum = indexReader.NumDocs();
            int[] docWordCnt = new int[docNum];
            int[] docUniqWordCnt = new int[docNum];
            Dictionary<string, int> wordDocCntDict = new Dictionary<string, int>();
            Dictionary<string, int> wordOccCntDict = new Dictionary<string, int>();

            var fieldWeights = tokenConfig.TokenizerType == TokenizerType.FeatureVector
                ? BingNewsFields.FeatureVectorFieldWeights
                : BingNewsFields.NewsFieldWeights;
            
            ProgramProgress progress = new ProgramProgress(docNum);
            for (int iDoc = 0; iDoc < docNum; iDoc++)
            {
                var document = indexReader.Document(iDoc);
                var content = LuceneOperations.GetContent(document, fieldWeights);

                var words = NLPOperations.Tokenize(content, tokenConfig);
                var uniqueWords = new HashSet<string>(words);
                docWordCnt[iDoc] = words.Count;
                docUniqWordCnt[iDoc] = uniqueWords.Count;

                foreach (var word in uniqueWords)
                {
                    if (!wordDocCntDict.ContainsKey(word))
                    {
                        wordDocCntDict.Add(word, 0);
                    }
                    wordDocCntDict[word]++;
                }

                foreach (var word in words)
                {
                    if (!wordOccCntDict.ContainsKey(word))
                    {
                        wordOccCntDict.Add(word, 0);
                    }
                    wordOccCntDict[word]++;
                }

                progress.PrintIncrementExperiment();
            }
            progress.PrintTotalTime();

            indexReader.Close();

            //Statistics
            DoubleStatistics statDocWordCnt = new DoubleStatistics();
            DoubleStatistics statDocUniqWordCnt = new DoubleStatistics();
            DoubleStatistics statWordDocCnt = new DoubleStatistics();
            DoubleStatistics statWordOccCnt = new DoubleStatistics();

            for (int iDoc = 0; iDoc < docNum; iDoc++)
            {
                statDocWordCnt.AddNumber(docWordCnt[iDoc]);
                statDocUniqWordCnt.AddNumber(docUniqWordCnt[iDoc]);
            }

            foreach (var kvp in wordDocCntDict)
            {
                statWordDocCnt.AddNumber(kvp.Value);
            }

            foreach (var kvp in wordOccCntDict)
            {
                statWordOccCnt.AddNumber(kvp.Value);
            }


            Console.WriteLine(statDocWordCnt.ToString("statDocWordCnt"));
            Console.WriteLine(statDocUniqWordCnt.ToString("statDocUniqWordCnt"));
            Console.WriteLine(statWordDocCnt.ToString("statWordDocCnt"));
            Console.WriteLine(statWordOccCnt.ToString("wordOccCnt"));

            //Hist
            var docWordCntHist = new DoubleHistogram(docWordCnt.Select(i => (double) i), (double)1);
            var docUniqueWordCntList = new DoubleHistogram(docUniqWordCnt.Select(i => (double)i), (double)1);
            var wordDocCntHist = new DoubleHistogram(wordDocCntDict.Select(kvp => (double)kvp.Value), 1000);
            var wordDocCntHist2 = new DoubleHistogram(wordDocCntDict.Select(kvp => (double)kvp.Value), (double)1);

            docWordCntHist.PrintToFile(StringOperations.EnsureFolderEnd(inputPath) + "docWordCntHist.csv");
            docUniqueWordCntList.PrintToFile(StringOperations.EnsureFolderEnd(inputPath) + "docUniqueWordCntList.csv");
            wordDocCntHist.PrintToFile(StringOperations.EnsureFolderEnd(inputPath) + "wordDocCntHist.csv");
            wordDocCntHist2.PrintToFile(StringOperations.EnsureFolderEnd(inputPath) + "wordDocCntHist2.csv");

            Console.Read();
        }

        public static void AnalyzeFieldValues(string inputPath, string fieldName, Func<string, string> convertValueFunc = null)
        {
            if (convertValueFunc == null)
                convertValueFunc = str => str;

            string fileName = StringOperations.EnsureFolderEnd(inputPath) + fieldName + ".txt";
            StreamWriter sw = new StreamWriter(fileName);

            Counter<string> counter = new Counter<string>();
            var indexReader = LuceneOperations.GetIndexReader(inputPath);
            for (int iDoc = 0; iDoc < indexReader.NumDocs(); iDoc++)
            {
                var doc = indexReader.Document(iDoc);
                var value = doc.Get(fieldName);
                counter.Add(convertValueFunc(value));
            }
            foreach (var kvp in counter.GetCountDictionary().OrderBy(kvp => kvp.Key))
            {
                sw.WriteLine(kvp.Key + "\t\t" + kvp.Value);
                Console.WriteLine(kvp.Key + "\t\t" + kvp.Value);
            }

            sw.WriteLine("total: " + indexReader.NumDocs());
            sw.Flush();
            sw.Close();

            indexReader.Close();
            Console.ReadKey();
        }
    }
}
