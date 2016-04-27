using System;
using System.Collections.Generic;
using System.IO;
using DataProcess.Utils;
using Lucene.Net.Documents;

namespace DataProcess.NoiseRemoval
{
    /// <summary>
    /// Remove noise by iteratively ranking documents and select keywords
    /// </summary>
    class RemoveNoiseByRanking
    {
        public string inputpath = null;
        public string outputpath = null;
        public string searchfield = null;
        public List<string> keywords = null;
        public string tokenizeConfigStr = null;
        public int keywordNum = 20;
        public double threshold = 0.8;
        public double searchDocRatio = 0.1; 
        public double saveDocRatio = 0.1;
        public bool isPrintRemovedDocuments;

        public RemoveNoiseByRanking()
        {
            var config = FileOperations.LoadConfigure("configRemoveNoiseByRanking.txt");
            inputpath = config["InputPath"][0];
            outputpath = config["OutputPath"][0];
            searchfield = config["SearchField"][0];
            tokenizeConfigStr = config["TokenizeConfig"][0];
            keywords = config["Keywords"];

            keywordNum = int.Parse(config["KeywordNum"][0]);
            threshold = double.Parse(config["Threshold"][0]);
            searchDocRatio = double.Parse(config["SearchDocRatio"][0]);
            saveDocRatio = double.Parse(config["SaveDocRatio"][0]);
            isPrintRemovedDocuments = bool.Parse(config["IsPrintRemovedDocuments"][0]);
        }

        public RemoveNoiseByRanking(string inputpath, string outputpath,
            string searchfield, List<string> keywords, string tokenizeConfigStr,
            int keywordNum = -1, int threshold = -1, int searchDocRatio = -1, int saveDocRatio = -1)
        {
            this.inputpath = inputpath;
            this.outputpath = outputpath;
            this.searchfield = searchfield;
            this.keywords = keywords;
            this.tokenizeConfigStr = tokenizeConfigStr;
            if (keywordNum >= 0)
                this.keywordNum = keywordNum;
            if (threshold >= 0)
                this.threshold = threshold;
            if (searchDocRatio >= 0)
                this.searchDocRatio = searchDocRatio;
            if (saveDocRatio >= 0)
                this.saveDocRatio = saveDocRatio;
        }

        public void Start()
        {
            if (!outputpath.EndsWith("\\"))
                outputpath += "\\";

            var tokenizerConfig = new TokenizeConfig(tokenizeConfigStr);

            var searcher = LuceneOperations.GetIndexSearcher(inputpath);
            var max_doc_num = (int)(searchDocRatio * searcher.GetIndexReader().NumDocs());
            var scoredDocs = LuceneOperations.Search(searcher, searchfield, keywords, max_doc_num);

            int iter = 0;
            bool bContinue = threshold == 0 ? false : true;
            while (bContinue && iter < 5)
            {
                iter++;
                Console.WriteLine("iteration------------------" + iter);
                List<string> keywordsNew;
                #region Calculate Keywords
                var counter = new Counter<string>();
                foreach (var scoredDoc in scoredDocs)
                {
                    var doc = searcher.Doc(scoredDoc.doc);
                    var content = doc.Get(searchfield);
                    foreach (var word in NLPOperations.Tokenize(content, tokenizerConfig))
                        counter.Add(word);
                }
                keywordsNew = counter.GetMostFreqObjs(keywordNum);
                #endregion

                var scoredDocsNew = LuceneOperations.Search(searcher, searchfield, keywordsNew, max_doc_num);
                #region Test whether exit
                int repeatNum = 0;
                var docIDs = new HashSet<int>();
                foreach (var scoredDoc in scoredDocs)
                    docIDs.Add(scoredDoc.doc);

                foreach(var scoredDocNew in scoredDocsNew)
                    if(docIDs.Contains(scoredDocNew.doc))
                        repeatNum++;

                bContinue = (double)repeatNum / scoredDocs.Length < threshold;
                #endregion

                Console.WriteLine(repeatNum + "  " + scoredDocsNew.Length);

                keywords = keywordsNew;
                scoredDocs = scoredDocsNew;

                Console.WriteLine(StringOperations.GetMergedString(keywords));
            }

            max_doc_num = (int)(saveDocRatio * searcher.GetIndexReader().NumDocs());
            scoredDocs = LuceneOperations.Search(searcher, searchfield, keywords, max_doc_num);
            var writer = LuceneOperations.GetIndexWriter(outputpath);
            foreach (var scoredDoc in scoredDocs)
            {
                Document doc = searcher.Doc(scoredDoc.doc);
                writer.AddDocument(doc);
            }
            writer.Optimize();
            writer.Close();

            if (isPrintRemovedDocuments)
            {
                var sw = new StreamWriter(outputpath + "removeDocuments.txt");
                var selectedDocIDs = new HashSet<int>();
                foreach (var scoredDoc in scoredDocs)
                {
                    selectedDocIDs.Add(scoredDoc.doc);
                }

                var reader = searcher.GetIndexReader();
                for (int iDoc = 0; iDoc < reader.NumDocs(); iDoc++)
                {
                    if (!selectedDocIDs.Contains(iDoc))
                    {
                        sw.WriteLine(LuceneOperations.GetDocumentString(reader.Document(iDoc)));
                    }
                }
                reader.Close();
                sw.Flush();
                sw.Close();
            }

            searcher.Close();

            Console.WriteLine("Done");
            Console.ReadKey();
        }

    }
}
