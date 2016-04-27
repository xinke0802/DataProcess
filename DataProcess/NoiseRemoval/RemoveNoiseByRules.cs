using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = System.IO.Directory;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;
using System.Diagnostics;
using DataProcess.Utils;


namespace DataProcess.NoiseRemoval
{
    /// <summary>
    /// Remove noisy documents using certain rules
    /// </summary>
    class RemoveNoiseByRules
    {
        public string InputPath;
        public string OutputPath;

        //remove language error
        public bool IsRemoveLanguageError = true;
        public bool IsEnglish = true;
        public double MinLanguageCorrectRatio = 0.22;

        //remove short documents
        public bool IsRemoveShortDocuments = true;
        public TokenizeConfig TokenizeConfig = null;
        public int MinLongDocumentsWordCount = 5;

        //remove documents with too many or too few hashtags
        public bool IsRemoveByHashtagNumber = false;
        public int MinHashtagNumber = 1;
        public int MaxHashtagNumber = 5;

        //remove documents with certain words
        public bool IsRemoveDocumentsWithCertainWords = false;
        public string[] NoisyWords;
        public int NoisyWordFilterCount = 1;

        //remove leading paragraph no keywords
        public bool IsRemoveLeadingParagraphNoKeywords = false;
        public bool IsSearchKeywordBruteForce = true;
        public string[] Keywords;
        public int LeadingParaSentenseNum = 15;
        public int TitlePassNumber = 1;
        public int LeadingPassNumber = 1;
        public int BodyPassNumber = 2;

        //remove yahoonews
        public bool IsRemoveYahooNews = false;

        //For all
        public bool IsCaseSensitive = false;
        public bool IsPrintTextFiles = true;
        public string TitleField = "NewsArticleHeadline";
        public string BodyField = "NewsArticleDescription";
        public string DateField = "DiscoveryStringTime";
        public string SourceField = "NewsSource";
        public string URLField = "DocumentURL";
        
        public RemoveNoiseByRules()
        {
            var config = FileOperations.LoadConfigure("configRemoveNoiseByRules.txt");
            InputPath = config["InputPath"][0];
            OutputPath = config["OutputPath"][0];

            IsRemoveShortDocuments = bool.Parse(config["IsRemoveShortDocuments"][0]);
            TokenizeConfig = new TokenizeConfig(config["TokenizeConfig"][0]);
            MinLongDocumentsWordCount = int.Parse(config["MinLongDocumentsWordCount"][0]);

            IsRemoveByHashtagNumber = bool.Parse(config["IsRemoveByHashtagNumber"][0]);
            MinHashtagNumber = int.Parse(config["MinHashtagNumber"][0]);
            MaxHashtagNumber = int.Parse(config["MaxHashtagNumber"][0]);

            IsRemoveLanguageError = bool.Parse(config["IsRemoveLanguageError"][0]);
            IsEnglish = bool.Parse(config["IsEnglish"][0]);
            MinLanguageCorrectRatio = double.Parse(config["MinLanguageCorrectRatio"][0]);

            IsRemoveDocumentsWithCertainWords = bool.Parse(config["IsRemoveDocumentsWithCertainWords"][0]);
            NoisyWords = config["NoisyWords"][0].Split(new char[]{'\t'},StringSplitOptions.RemoveEmptyEntries);
            NoisyWordFilterCount = int.Parse(config["NoisyWordFilterCount"][0]);

            IsRemoveLeadingParagraphNoKeywords = bool.Parse(config["IsRemoveLeadingParagraphNoKeywords"][0]);
            IsSearchKeywordBruteForce = bool.Parse(config["IsSearchKeywordBruteForce"][0]);
            Keywords = config["Keywords"][0].Split(new char[]{'\t'},StringSplitOptions.RemoveEmptyEntries);
            LeadingParaSentenseNum = int.Parse(config["LeadingParaSentenseNum"][0]);
            TitlePassNumber = int.Parse(config["TitlePassNumber"][0]);
            LeadingPassNumber = int.Parse(config["LeadingPassNumber"][0]);
            BodyPassNumber = int.Parse(config["BodyPassNumber"][0]);

            //remove yahoonews
            IsRemoveYahooNews = bool.Parse(config["IsRemoveYahooNews"][0]);

            //For all
            IsCaseSensitive = bool.Parse(config["IsCaseSensitive"][0]);
            IsPrintTextFiles = bool.Parse(config["IsPrintTextFiles"][0]);
            TitleField = config["TitleField"][0];
            BodyField = config["BodyField"][0];
            DateField = config["DateField"][0];
            SourceField = config["SourceField"][0];
            URLField = config["URLField"][0];
        }

        public RemoveNoiseByRules(string inputpath, string outputpath)
        {
            this.InputPath = inputpath;
            this.OutputPath = outputpath;
        }

        public void Start()
        {
            if (!InputPath.EndsWith("\\"))
                InputPath += "\\";
            if (!OutputPath.EndsWith("\\"))
                OutputPath += "\\";

            var indexpath = this.InputPath;
            var outputindexpath = this.OutputPath;
            IndexReader indexreader = (new IndexSearcher(FSDirectory.Open(new DirectoryInfo(indexpath)), true)).GetIndexReader();

            List<List<int>> removeDocumentsList = new List<List<int>>();

            if (IsRemoveLanguageError)
                removeDocumentsList.Add(GetLanguageErrorDocuments(indexreader, indexpath + "languageErrorDocs.dat"));

            if(IsRemoveShortDocuments)
                removeDocumentsList.Add(GetShortDocuments(indexreader, indexpath + "shortDocs.dat"));

            if (IsRemoveByHashtagNumber)
                removeDocumentsList.Add(GetHashtagNumberInappropriateDocuments(indexreader, indexpath + "hashtagNumberInappropriate.dat"));

            if (IsRemoveLeadingParagraphNoKeywords)
            {
                if(IsSearchKeywordBruteForce)
                    removeDocumentsList.Add(GetLeadingParagraphNoKeyWordsBruteForce(indexreader, indexpath + "leadingNoKeyWords.dat"));
                else
                    removeDocumentsList.Add(GetLeadingParagraphNoKeyWords(indexreader, indexpath + "leadingNoKeyWords.dat"));
            }

            if (IsRemoveDocumentsWithCertainWords)
                removeDocumentsList.Add(GetDocumentsWithCertainNoisyWords(indexreader, indexpath + "certainkeywords.dat"));

            if (IsRemoveYahooNews)
                removeDocumentsList.Add(GetYahooNews(indexreader, indexpath + "yahooNews.dat"));

            RemoveNoisyDocuments(indexreader, outputindexpath, removeDocumentsList, outputindexpath + "RemainedDocuments.dat");

            Console.WriteLine("All done");
            //Console.ReadKey();
        }



        void RemoveNoisyDocuments(IndexReader indexreader, string outputindexpath,
     List<List<int>> removedDocuments, string outputfile)
        {
            if (Directory.Exists(outputindexpath))
                Directory.Delete(outputindexpath, true);
            Analyzer standardanalyzer = new StandardAnalyzer(Version.LUCENE_24);
            LuceneDirectory outputdir = FSDirectory.Open(new DirectoryInfo(outputindexpath));
            IndexWriter indexwriter = new IndexWriter(outputdir, standardanalyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);

            StreamWriter sw = new StreamWriter(outputfile);
            int removedDocNum = 0;

            List<IEnumerator<int>> removedDocumentsEnums = new List<IEnumerator<int>>();
            foreach (var removedDocumentList in removedDocuments)
            {
                var docEum = removedDocumentList.GetEnumerator();
                docEum.MoveNext();
                removedDocumentsEnums.Add(docEum);
            }

            int docNum = indexreader.NumDocs();
            int remainedDocIndex = 0;
            Console.WriteLine("Total documents: {0}", docNum);
            for (int iDoc = 0; iDoc < docNum; iDoc++)
            {
                if (iDoc % 10000 == 0)
                {
                    if (iDoc == 0)
                        continue;
                    Console.WriteLine("Process " + iDoc + "th document!");
                    Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, iDoc, 100 * removedDocNum / iDoc);
                    sw.Flush();
                }

                bool bDeleted = false;
                foreach (var delDocEnum in removedDocumentsEnums)
                {
                    if (delDocEnum.Current == iDoc)
                    {
                        delDocEnum.MoveNext();
                        bDeleted = true;
                    }
                }

                if (!bDeleted)
                {
                    Document document = indexreader.Document(iDoc);
                    if (document != null)
                    {
                        indexwriter.AddDocument(document);
                        sw.WriteLine(DocumentToString(document));
                    }
                    remainedDocIndex++;
                }
                else
                    removedDocNum++;
            }

            sw.Flush();
            sw.Close();

            Console.WriteLine("Remained Documents: {0}", remainedDocIndex);

            indexwriter.Optimize();
            indexwriter.Close();
        }

        List<int> GetLanguageErrorDocuments(IndexReader indexreader, string outputfile)
        {
            Console.WriteLine("==========Remove language error documents!==========");

            StreamWriter sw = IsPrintTextFiles ? new StreamWriter(outputfile) : null;
            List<int> removedDocuments = new List<int>();
            var stopWords = IsEnglish ?
                FileOperations.LoadKeyWordFile(StopWordsFile.EN, true) :
                FileOperations.LoadKeyWordFile(StopWordsFile.CH, false);
            var stopHash = Util.GetHashSet(stopWords);

            int docNum = indexreader.NumDocs();
            string titlefield = this.TitleField;
            string bodyfield = this.BodyField;
            int removedDocNum = 0;
            Console.WriteLine("Total documents: {0}", docNum);

            var tokenConfig = new TokenizeConfig(IsEnglish ? TokenizerType.Standard : TokenizerType.ICTCLAS, StopWordsFile.NO);
            DoubleStatistics stat_percent = new DoubleStatistics();
            DoubleStatistics stat_absolute = new DoubleStatistics();

            for (int idoc = 0; idoc < docNum; idoc++)
            {
                if (idoc % 10000 == 0)
                {
                    if (idoc == 0)
                        continue;
                    Console.WriteLine("Process " + idoc + "th document!");
                    Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
                    if (IsPrintTextFiles) sw.Flush();
                }

                Document document = indexreader.Document(idoc);

                string content = document.Get(titlefield) + " " + document.Get(bodyfield);
                if (IsEnglish) content = content.ToLower();
                var words = NLPOperations.Tokenize(content, tokenConfig);
                var termCnt0 = words.Count;
                var termCnt1 = 0;
                foreach (var word in words)
                {
                    if (!stopHash.Contains(word))
                        termCnt1++;
                }

                if (((double)termCnt0 - termCnt1) / termCnt0 < MinLanguageCorrectRatio)
                {
                    if(IsPrintTextFiles) sw.WriteLine(DocumentToString(document));
                    removedDocuments.Add(idoc);
                    removedDocNum++;
                }
                else
                {
                    stat_absolute.AddNumber(termCnt0 - termCnt1);
                    stat_percent.AddNumber((100.0) * (termCnt0 - termCnt1) / termCnt0);

                }
            }

            Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
            if (IsPrintTextFiles)
            {
                sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
                sw.Flush();
                sw.Close();
            }

            Console.WriteLine(stat_percent.ToString("stat_percent"));
            Console.WriteLine(stat_absolute.ToString("stat_absolute"));

            return removedDocuments;
        }


        List<int> GetShortDocuments(IndexReader indexreader, string outputfile)
        {
            Console.WriteLine("==========Remove short documents!==========");

            StreamWriter sw = IsPrintTextFiles ? new StreamWriter(outputfile) : null;
            List<int> removedDocuments = new List<int>();

            int docNum = indexreader.NumDocs();
            string bodyfield = this.BodyField;
            int removedDocNum = 0;
            Console.WriteLine("Total documents: {0}", docNum);

            var tokenConfig = this.TokenizeConfig;

            for (int idoc = 0; idoc < docNum; idoc++)
            {
                if (idoc % 10000 == 0)
                {
                    if (idoc == 0)
                        continue;
                    Console.WriteLine("Process " + idoc + "th document!");
                    Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
                    if (IsPrintTextFiles) sw.Flush();
                }

                Document document = indexreader.Document(idoc);

                string content = document.Get(bodyfield);
                var words = NLPOperations.Tokenize(content, tokenConfig);
                
                if (words.Count < MinLongDocumentsWordCount)
                {
                    if (IsPrintTextFiles) sw.WriteLine(DocumentToString(document));
                    removedDocuments.Add(idoc);
                    removedDocNum++;
                }
            }

            Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
            if (IsPrintTextFiles)
            {
                sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

                sw.Flush();
                sw.Close();
            }

            return removedDocuments;
        }


        private List<int> GetHashtagNumberInappropriateDocuments(IndexReader indexreader, string outputfile)
        {
            Console.WriteLine("==========Remove inappropriate hashtag number documents!==========");

            StreamWriter sw = IsPrintTextFiles ? new StreamWriter(outputfile) : null;
            List<int> removedDocuments = new List<int>();

            int docNum = indexreader.NumDocs();
            string bodyfield = this.BodyField;
            int removedDocNum = 0;
            Console.WriteLine("Total documents: {0}", docNum);

            var tokenConfig = new TokenizeConfig(TokenizerType.Hashtag, StopWordsFile.NO);

            for (int idoc = 0; idoc < docNum; idoc++)
            {
                if (idoc % 10000 == 0)
                {
                    if (idoc == 0)
                        continue;
                    Console.WriteLine("Process " + idoc + "th document!");
                    Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
                    if (IsPrintTextFiles) sw.Flush();
                }

                Document document = indexreader.Document(idoc);

                string content = document.Get(bodyfield);
                var words = NLPOperations.Tokenize(content, tokenConfig);

                if (words.Count < MinHashtagNumber || words.Count > MaxHashtagNumber)
                {
                    if (IsPrintTextFiles) sw.WriteLine(DocumentToString(document));
                    removedDocuments.Add(idoc);
                    removedDocNum++;
                }
            }

            Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
            if (IsPrintTextFiles)
            {
                sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

                sw.Flush();
                sw.Close();
            }

            return removedDocuments;
        }

        List<int> GetDocumentsWithCertainNoisyWords(IndexReader indexreader, string outputfile)
        {
            Console.WriteLine("==========Remove documents with certain keywords!==========");

            StreamWriter sw = IsPrintTextFiles ? new StreamWriter(outputfile) : null;
            List<int> removedDocuments = new List<int>();
            IStringSearchAlgorithm stringSearchAlg;// = new StringSearch();

            if (!IsCaseSensitive)
            {
                for (int i = 0; i < NoisyWords.Length; i++)
                    NoisyWords[i] = NoisyWords[i].ToLower();
            }
            //bool isContainChinese = false;
            //foreach(var word in NoisyWords)
            //    if (NLPOperations.IsChineseWord(word))
            //    {
            //        isContainChinese = true;
            //        break;
            //    }
            //if (isContainChinese)
                stringSearchAlg = new IndexOfSearch();
            //else
            //    stringSearchAlg = new StringSearch();
            stringSearchAlg.Keywords = NoisyWords;

            int docNum = indexreader.NumDocs();
            string titlefield = this.TitleField;
            string plainfield = this.BodyField;
            int removedDocNum = 0;
            Console.WriteLine("Total documents: {0}", docNum);
            for (int idoc = 0; idoc < docNum; idoc++)
            {
                if (idoc % 10000 == 0)
                {
                    if (idoc == 0)
                        continue;
                    Console.WriteLine("Process " + idoc + "th document!");
                    Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
                    if (IsPrintTextFiles) sw.Flush();
                }

                Document document = indexreader.Document(idoc);
                //PreProcess
                string content = document.Get(titlefield) + " " + document.Get(plainfield);
                if(!IsCaseSensitive)
                    content = content.ToLower();

                if (stringSearchAlg.FindAll(content).Length >= NoisyWordFilterCount)
                {
                    if (IsPrintTextFiles) sw.WriteLine(DocumentToString(document));
                    removedDocuments.Add(idoc);
                    removedDocNum++;
                }
            }

            Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
            if (IsPrintTextFiles)
            {
                sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

                sw.Flush();
                sw.Close();
            }

            return removedDocuments;
        }

        List<int> GetYahooNews(IndexReader indexreader,
            string outputfile)
        {
            Console.WriteLine("==========Remove news from Yahoo==========");

            StreamWriter sw = IsPrintTextFiles? new StreamWriter(outputfile) : null;
            List<int> removedDocuments = new List<int>();
            Regex regex = new Regex("yahoo", RegexOptions.IgnoreCase);

            int docNum = indexreader.NumDocs();
            string sourcefield = this.SourceField;
            int removedDocNum = 0;
            Console.WriteLine("Total documents: {0}", docNum);
            for (int idoc = 0; idoc < docNum; idoc++)
            {
                if (idoc % 10000 == 0)
                {
                    if (idoc == 0)
                        continue;
                    Console.WriteLine("Process " + idoc + "th document!");
                    Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
                    if (IsPrintTextFiles) sw.Flush();
                }

                Document document = indexreader.Document(idoc);
                //PreProcess
                var source = document.Get(sourcefield);
                if (source != null)
                {
                    string newssource = source.ToLower();

                    if (regex.Match(newssource).Success)
                    {
                        if (IsPrintTextFiles) sw.WriteLine(DocumentToString(document));
                        removedDocuments.Add(idoc);
                        removedDocNum++;
                    }
                }
            }

            Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
            if (IsPrintTextFiles)
            {
                sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

                sw.Flush();
                sw.Close();
            }

            return removedDocuments;
        }

        List<int> GetLeadingParagraphNoKeyWords(IndexReader indexreader, string outputfile)            
        {
            Console.WriteLine("==========Remove documents with leading no keywords!==========");

            var titlePassNumber = TitlePassNumber;
            var leadingPassNumber = LeadingPassNumber;
            var bodyPassNumber = BodyPassNumber;

            if (!IsCaseSensitive)
            {
                for (int i = 0; i < Keywords.Length; i++)
                {
                    Keywords[i] = Keywords[i].ToLower();
                }
            }

            StringSearch stringSearchAlg = new StringSearch();
            stringSearchAlg.Keywords = Keywords;

            StreamWriter sw = IsPrintTextFiles ? new StreamWriter(outputfile) : null;
            List<int> removedDocuments = new List<int>();

            int docNum = indexreader.NumDocs();
            string titlefield = this.TitleField;
            string bodyfield = this.BodyField;
            int removedDocNum = 0;
            Console.WriteLine("Total documents: {0}", docNum);
            for (int idoc = 0; idoc < docNum; idoc++)
            {
                if (idoc % 10000 == 0)
                {
                    if (idoc == 0)
                        continue;
                    Console.WriteLine("Process " + idoc + "th document!");
                    Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
                    if (IsPrintTextFiles) sw.Flush();
                }

                Document document = indexreader.Document(idoc);
                string title = document.Get(titlefield);
                string plain = document.Get(bodyfield);
                string leadingPara = GetLeadingParagraph(plain);
                if (!IsCaseSensitive)
                {
                    title = title.ToLower();
                    plain = plain.ToLower();
                    leadingPara = leadingPara.ToLower();
                }

                bool bRemove = true;
                if (stringSearchAlg.FindAll(title).Length >= titlePassNumber)
                    bRemove = false;
                else if (stringSearchAlg.FindAll(plain).Length >= bodyPassNumber)
                    bRemove = false;

                if (stringSearchAlg.FindAll(leadingPara).Length < leadingPassNumber)
                    bRemove = true;

                if (bRemove)
                {
                    if (IsPrintTextFiles) sw.WriteLine(DocumentToString(document));
                    removedDocuments.Add(idoc);
                    removedDocNum++;
                }
            }

            Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
            if (IsPrintTextFiles)
            {
                sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

                sw.Flush();
                sw.Close();
            }

            return removedDocuments;
        }

        List<int> GetLeadingParagraphNoKeyWordsBruteForce(IndexReader indexreader, string outputfile)
        {
            Console.WriteLine("==========Remove documents with leading no keywords!==========");

            var titlePassNumber = TitlePassNumber;
            var leadingPassNumber = LeadingPassNumber;
            var bodyPassNumber = BodyPassNumber;

            if (!IsCaseSensitive)
            {
                for (int i = 0; i < Keywords.Length; i++)
                {
                    Keywords[i] = Keywords[i].ToLower();
                }
            }

            StreamWriter sw = IsPrintTextFiles ? new StreamWriter(outputfile) : null;
            List<int> removedDocuments = new List<int>();

            int docNum = indexreader.NumDocs();
            string titlefield = this.TitleField;
            string bodyfield = this.BodyField;
            int removedDocNum = 0;
            Console.WriteLine("Total documents: {0}", docNum);
            for (int idoc = 0; idoc < docNum; idoc++)
            {
                if (idoc % 10000 == 0)
                {
                    if (idoc == 0)
                        continue;
                    Console.WriteLine("Process " + idoc + "th document!");
                    Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
                    if (IsPrintTextFiles) sw.Flush();
                }

                Document document = indexreader.Document(idoc);
                //PreProcess
                string title = document.Get(titlefield);
                string plain = document.Get(bodyfield);
                string leadingPara = GetLeadingParagraph(plain);
                if (!IsCaseSensitive)
                {
                    title = title.ToLower();
                    plain = plain.ToLower();
                    leadingPara = leadingPara.ToLower();
                }

                bool bRemove = true;
                if (GetKeywordCountBruteForce(title, Keywords) >= titlePassNumber)
                    bRemove = false;
                else if (GetKeywordCountBruteForce(plain, Keywords) >= bodyPassNumber)
                    bRemove = false;

                if (GetKeywordCountBruteForce(leadingPara, Keywords) < leadingPassNumber)
                    bRemove = true;

                if (bRemove)
                {
                    if (IsPrintTextFiles) sw.WriteLine(DocumentToString(document));
                    removedDocuments.Add(idoc);
                    removedDocNum++;
                }
            }

            Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
            if (IsPrintTextFiles)
            {
                sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

                sw.Flush();
                sw.Close();
            }

            return removedDocuments;
        }

        private string DocumentToString(Document document)
        {
            return LuceneOperations.GetDocumentString(document);
        }

        void GetOccurrenceBruteForce(string content, string[] keywords, ref int[] occurrences)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                occurrences[i] = GetKeywordCountBruteForce(content, keywords[i]);
            }
        }

        int GetKeywordCountBruteForce(string content, string keyword)
        {
            if (content.Length == 0)
                return 0;

            int keywordCnt = 0;
            //foreach (var keyword in keywords)
            //{
            int startIndex = 0;
            while ((startIndex = content.IndexOf(keyword, startIndex + 1)) >= 0)
                keywordCnt++;
            //}
            return keywordCnt;
        }

        int GetKeywordCountBruteForce(string content, string[] keywords)
        {
            if (content.Length == 0)
                return 0;

            int keywordCnt = 0;
            foreach (var keyword in keywords)
            {
                int startIndex = 0;
                while ((startIndex = content.IndexOf(keyword, startIndex + 1)) >= 0)
                    keywordCnt++;
            }
            return keywordCnt;
        }


        string GetLeadingParagraph(string plain, int leadingSentenceNum = -1)
        {
            if (leadingSentenceNum < 0)
                leadingSentenceNum = LeadingParaSentenseNum;

            var contents = plain.Split('.', '?', '!');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < leadingSentenceNum && i < contents.Length; i++)
            {
                sb.Append(contents[i]);
                sb.Append('.');
            }
            return sb.ToString();
        }

        #region Previous 

   //     internal List<int> GetLeadingParagraphFilteredRules(IndexReader indexreader,
   //         ILeadingParagraphFilterRule leadingParagraphFilterRule, string outputfile)
   //     {
   //         GetStreamWriter sw = new GetStreamWriter(outputfile);
   //         List<int> removedDocuments = new List<int>();
   //         string[] keywords = leadingParagraphFilterRule.keywords;
   //         int[] occurrences = new int[keywords.Length];

   //         int docNum = indexreader.NumDocs();
   //         string titlefield = Constant.IndexedBingNewsDataFields.NewsArticleHeadline;
   //         string bodyfield = Constant.IndexedBingNewsDataFields.NewsArticleDescription;
   //         string docidfield = Constant.IndexedBingNewsDataFields.DocumentId;
   //         int removedDocNum = 0;
   //         Console.WriteLine("Total documents: {0}", docNum);
   //         for (int idoc = 0; idoc < docNum; idoc++)
   //         {
   //             if (idoc % 10000 == 0)
   //             {
   //                 if (idoc == 0)
   //                     continue;
   //                 Console.WriteLine("Process " + idoc + "th document!");
   //                 Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
   //                 sw.Flush();
   //             }

   //             Document document = indexreader.Document(idoc);
   //             //PreProcess
   //             string title = document.Get(titlefield).ToLower();
   //             string plain = document.Get(bodyfield).ToLower();
   //             string leadingPara = GetLeadingParagraph(plain, 15);

   //             GetOccurrenceBruteForce(title + " " + leadingPara, keywords, ref occurrences);

   //             if (!leadingParagraphFilterRule.Passed(occurrences))
   //             {
   //                 sw.WriteLine(DocumentToString(document));
   //                 removedDocuments.Add(idoc);
   //                 removedDocNum++;
   //             }
   //         }

   //         Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
   //         sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

   //         sw.Flush();
   //         sw.Close();

   //         return removedDocuments;
   //     }

   //     #region Remove 2012 Yahoo News
   //     List<int> GetRemoveYahooNews2012_FirstPerson_Contributor(
   //         IndexReader indexreader, string outputfile)
   //     {

   //         GetStreamWriter sw = new GetStreamWriter(outputfile);
   //         List<int> removedDocuments = new List<int>();
   //         StringSearch stringSearchAlg = new StringSearch();
   //         stringSearchAlg.Keywords = new string[] { "yahoo contributor", "yahoo! contributor" };

   //         int docNum = indexreader.NumDocs();
   //         string titlefield = this.TitleField;
   //         string bodyfield = this.BodyField;
   //         int removedDocNum = 0;
   //         Console.WriteLine("Total documents: {0}", docNum);

   //         for (int idoc = 0; idoc < docNum; idoc++)
   //         {
   //             if (idoc % 10000 == 0)
   //             {
   //                 if (idoc == 0)
   //                     continue;
   //                 Console.WriteLine("Process " + idoc + "th document!");
   //                 Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
   //                 sw.Flush();
   //             }

   //             Document document = indexreader.Document(idoc);

   //             bool bRemove = false;

   //             string title = document.Get(titlefield).ToLower();

   //             if (!bRemove)
   //             {
   //                 if (title.StartsWith("first person"))
   //                     bRemove = true;
   //             }

   //             if (!bRemove)
   //             {
   //                 string body = document.Get(bodyfield).ToLower();
   //                 bRemove = stringSearchAlg.ContainsAny(body);
   //             }

   //             if (bRemove)
   //             {
   //                 sw.WriteLine(DocumentToString(document));
   //                 removedDocuments.Add(idoc);
   //                 removedDocNum++;
   //             }
   //         }

   //         Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
   //         sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

   //         sw.Flush();
   //         sw.Close();

   //         return removedDocuments;
   //     }

   //     List<int> GetRemoveYahooNews2012_YahooStoreFinanceNews(
   // IndexReader indexreader, string outputfile)
   //     {

   //         GetStreamWriter sw = new GetStreamWriter(outputfile);
   //         List<int> removedDocuments = new List<int>();
   //         StringSearch stringSearchAlg0 = new StringSearch();
   //         stringSearchAlg0.Keywords = new string[] { "yahoo" };
   //         StringSearch stringSearchAlg1 = new StringSearch();
   //         stringSearchAlg1.Keywords = new string[] { "yahoo store", "yahoo! store",
   //             "yahoo finance", "yahoo! finance", "yahoo news", "yahoo! news"};

   //         int docNum = indexreader.NumDocs();
   //         string titlefield = this.TitleField;
   //         string bodyfield = this.BodyField;
   //         int removedDocNum = 0;
   //         Console.WriteLine("Total documents: {0}", docNum);

   //         for (int idoc = 0; idoc < docNum; idoc++)
   //         {
   //             if (idoc % 10000 == 0)
   //             {
   //                 if (idoc == 0)
   //                     continue;
   //                 Console.WriteLine("Process " + idoc + "th document!");
   //                 Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
   //                 sw.Flush();
   //             }

   //             Document document = indexreader.Document(idoc);

   //             bool bRemove = false;

   //             string content = document.Get(titlefield).ToLower() + " " + document.Get(bodyfield).ToLower();

   //             if (!bRemove)
   //             {
   //                 if (stringSearchAlg0.FindAll(content).Length ==
   //                     stringSearchAlg1.FindAll(content).Length)
   //                     bRemove = true;
   //             }


   //             if (bRemove)
   //             {
   //                 sw.WriteLine(DocumentToString(document));
   //                 removedDocuments.Add(idoc);
   //                 removedDocNum++;
   //             }
   //         }

   //         Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
   //         sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

   //         sw.Flush();
   //         sw.Close();

   //         return removedDocuments;
   //     }

   //     List<int> GetRemoveYahooNews2012_YahooNewsStory(
   // IndexReader indexreader, string outputfile)
   //     {

   //         GetStreamWriter sw = new GetStreamWriter(outputfile);
   //         List<int> removedDocuments = new List<int>();
   //         StringSearch stringSearchAlg1 = new StringSearch();
   //         stringSearchAlg1.Keywords = new string[] { 
   //             "according to yahoo", 
   //             "go to yahoo news", "go to yahoo! news",
   //             "- yahoo news", "- yahoo! news", "told yahoo", 
   //             "read story", "full story", "click here"
   //         };
   //         StringSearch stringSearchAlg2 = new StringSearch();
   //         stringSearchAlg2.Keywords = new string[] { "story" };


   //         int docNum = indexreader.NumDocs();
   //         string bodyfield = this.BodyField;
   //         int removedDocNum = 0;
   //         Console.WriteLine("Total documents: {0}", docNum);

   //         for (int idoc = 0; idoc < docNum; idoc++)
   //         {
   //             if (idoc % 10000 == 0)
   //             {
   //                 if (idoc == 0)
   //                     continue;
   //                 Console.WriteLine("Process " + idoc + "th document!");
   //                 Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
   //                 sw.Flush();
   //             }

   //             Document document = indexreader.Document(idoc);

   //             bool bRemove = false;

   //             string body = document.Get(bodyfield).ToLower();

   //             if (!bRemove)
   //             {
   //                 bRemove = stringSearchAlg1.ContainsAny(body);
   //             }

   //             if (!bRemove)
   //             {
   //                 bRemove = stringSearchAlg2.FindAll(body).Length >= 3;
   //                 if (bRemove)
   //                     Trace.WriteLine("");
   //             }

   //             if (bRemove)
   //             {
   //                 sw.WriteLine(DocumentToString(document));
   //                 removedDocuments.Add(idoc);
   //                 removedDocNum++;
   //             }
   //         }

   //         Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
   //         sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

   //         sw.Flush();
   //         sw.Close();

   //         return removedDocuments;
   //     }

   //     List<int> GetRemoveYahooNews2012_URL(
   // IndexReader indexreader, string outputfile)
   //     {

   //         GetStreamWriter sw = new GetStreamWriter(outputfile);
   //         List<int> removedDocuments = new List<int>();
   //         StringSearch stringSearchAlg = new StringSearch();
   //         stringSearchAlg.Keywords = new string[] { "shopping.yahoo.com", "homes.yahoo.com", "sports.yahoo.com" };

   //         int docNum = indexreader.NumDocs();
   //         string urlfield = URLField;
   //         int removedDocNum = 0;
   //         Console.WriteLine("Total documents: {0}", docNum);

   //         for (int idoc = 0; idoc < docNum; idoc++)
   //         {
   //             if (idoc % 10000 == 0)
   //             {
   //                 if (idoc == 0)
   //                     continue;
   //                 Console.WriteLine("Process " + idoc + "th document!");
   //                 Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
   //                 sw.Flush();
   //             }

   //             Document document = indexreader.Document(idoc);

   //             bool bRemove = false;

   //             string url = document.Get(urlfield).ToLower();

   //             if (!bRemove)
   //             {
   //                 bRemove = stringSearchAlg.ContainsAny(url);
   //             }

   //             if (bRemove)
   //             {
   //                 sw.WriteLine(DocumentToString(document));
   //                 removedDocuments.Add(idoc);
   //                 removedDocNum++;
   //             }
   //         }

   //         Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
   //         sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

   //         sw.Flush();
   //         sw.Close();

   //         return removedDocuments;
   //     }

   //     List<int> GetRemoveGoogleNews2012_GoogleMapStore(
   //IndexReader indexreader, string outputfile)
   //     {

   //         GetStreamWriter sw = new GetStreamWriter(outputfile);
   //         List<int> removedDocuments = new List<int>();
   //         StringSearch stringSearchAlg0 = new StringSearch();
   //         stringSearchAlg0.Keywords = new string[] { "google" };
   //         StringSearch stringSearchAlg1 = new StringSearch();
   //         stringSearchAlg1.Keywords = new string[] { "google map", "google search" };

   //         int docNum = indexreader.NumDocs();
   //         string titlefield = this.TitleField;
   //         string bodyfield = this.BodyField;
   //         int removedDocNum = 0;
   //         Console.WriteLine("Total documents: {0}", docNum);

   //         for (int idoc = 0; idoc < docNum; idoc++)
   //         {
   //             if (idoc % 10000 == 0)
   //             {
   //                 if (idoc == 0)
   //                     continue;
   //                 Console.WriteLine("Process " + idoc + "th document!");
   //                 Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, idoc, 100 * removedDocNum / idoc);
   //                 sw.Flush();
   //             }

   //             Document document = indexreader.Document(idoc);

   //             bool bRemove = false;

   //             string content = document.Get(titlefield).ToLower() + " " + document.Get(bodyfield).ToLower();

   //             if (!bRemove)
   //             {
   //                 if (stringSearchAlg0.FindAll(content).Length ==
   //                     stringSearchAlg1.FindAll(content).Length)
   //                     bRemove = true;
   //             }


   //             if (bRemove)
   //             {
   //                 sw.WriteLine(DocumentToString(document));
   //                 removedDocuments.Add(idoc);
   //                 removedDocNum++;
   //             }
   //         }

   //         Console.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);
   //         sw.WriteLine("Remove {0} out of {1}: {2}%", removedDocNum, docNum, 100 * removedDocNum / docNum);

   //         sw.Flush();
   //         sw.Close();

   //         return removedDocuments;
   //     }
   //     #endregion

        #endregion
    }

}
