using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;

namespace DataProcess.Utils
{
    public class LuceneOperations
    {
        static readonly Version version = Version.LUCENE_24;
        static readonly Analyzer standardAnalyzer = new StandardAnalyzer(version);

        public static Document CopyDocument(Document inDoc)
        {
            Document outDoc = new Document();
            foreach (var rawField in inDoc.GetFields())
            {
                var field = (Field) rawField;
                AddField(outDoc, field.Name(), field.StringValue());
            }
            return outDoc;
        }

        public static void AddField<T>(Document document, string fieldName, T? value) where T: struct 
        {
            document.Add(new Field(fieldName, value == null ? "" : value.Value.ToString(), Field.Store.YES, Field.Index.ANALYZED));
        }

        public static void AddField(Document document, string fieldName, string value)
        {
            document.Add(new Field(fieldName, value ?? "", Field.Store.YES, Field.Index.ANALYZED));
        }

        public static int EnumerateIndexReader(string inputPath, Action<Document> action)
        {
            var indexReader = LuceneOperations.GetIndexReader(inputPath);

            var docNum = indexReader.NumDocs();
            ProgramProgress progress = new ProgramProgress(docNum);
            for (int iDoc = 0; iDoc < docNum; iDoc++)
            {
                action(indexReader.Document(iDoc));
                progress.PrintIncrementExperiment();
            }
            progress.PrintTotalTime();

            indexReader.Close();

            return docNum;
        }

        public static int EnumerateIndexReader(string inputPath, Action<int, Document> action, bool isPrintProgress = true)
        {
            var indexReader = LuceneOperations.GetIndexReader(inputPath);

            var docNum = indexReader.NumDocs();
            ProgramProgress progress = new ProgramProgress(docNum);
            for (int iDoc = 0; iDoc < docNum; iDoc++)
            {
                action(iDoc, indexReader.Document(iDoc));
                if (isPrintProgress)
                {
                    progress.PrintIncrementExperiment();
                }
            }
            if (isPrintProgress)
            {
                progress.PrintTotalTime();
            }

            indexReader.Close();

            return docNum;
        }

        public static int EnumerateIndexReaderWriter(string inputPath, string outputPath, Action<Document, IndexWriter> action)
        {
            var indexReader = LuceneOperations.GetIndexReader(inputPath);

            var indexWriter = LuceneOperations.GetIndexWriter(outputPath);

            var docNum = indexReader.NumDocs();
            ProgramProgress progress = new ProgramProgress(docNum);
            for (int iDoc = 0; iDoc < docNum; iDoc++)
            {
                action(indexReader.Document(iDoc), indexWriter);
                progress.PrintIncrementExperiment();
            }
            progress.PrintTotalTime();

            indexWriter.Commit();
            indexWriter.Close();

            indexReader.Close();

            return docNum;
        }

        public static string GetContent(Document doc, Dictionary<string, int> fieldWeights)
        {
            string content = "";
            foreach (var kvp in fieldWeights)
            {
                var value = doc.Get(kvp.Key);
                for (int i = 0; i < kvp.Value; i++)
                {
                    content += value + " ";
                }
            }
            return content;
        }

        public static List<int> Search(IndexSearcher searcher, string queryStr, string queryField, int docCnt = -1)
        {
            if (docCnt == -1)
            {
                docCnt = searcher.MaxDoc();
            }
            QueryParser queryparser = new QueryParser(version, queryField, standardAnalyzer);
            queryStr = queryStr.Replace("-", "");
            if (String.IsNullOrEmpty(queryStr))
            {
                return new List<int>();
            }
            Query query = queryparser.Parse(queryStr);
            var docs = searcher.Search(query, null, docCnt).scoreDocs;

            List<int> docIDs = new List<int>();
            foreach (var scoreDoc in docs)
            {
                docIDs.Add(scoreDoc.doc);
            }
            return docIDs;
        }

        public static Document ExactSearch(IndexSearcher searcher, string queryStr, string queryField)
        {
            var docIds = Search(searcher, queryStr, queryField);
            var reader = searcher.GetIndexReader();
            foreach (var docId in docIds)
            {
                var document = reader.Document(docId);
                if (document.Get(queryField) == queryStr)
                {
                    //reader.Close();
                    return document;
                }
            }
            //reader.Close();
            return null;
        }

        public static IndexSearcher GetIndexSearcher(string indexpath)
        {
            return new IndexSearcher(FSDirectory.Open(new DirectoryInfo(indexpath)), true);
        }

        public static IndexReader GetIndexReader(string indexpath)
        {
            return
                (new IndexSearcher(FSDirectory.Open(new DirectoryInfo(indexpath)), true)).GetIndexReader();
        }

        public static IndexWriter GetIndexWriter(string indexpath, Dictionary<string, Analyzer> customAnalyzerDict = null)
        {
            #region initialize analyzer
            Analyzer analyzer;
            if (customAnalyzerDict == null)
                analyzer = standardAnalyzer;
            else
            {
                var perFieldAnalyzer = new PerFieldAnalyzerWrapper(standardAnalyzer);
                foreach (var kvp in customAnalyzerDict)
                    perFieldAnalyzer.AddAnalyzer(kvp.Key, kvp.Value);
                analyzer = perFieldAnalyzer;
            }
            #endregion

            FileOperations.EnsureFileFolderExist(indexpath);

            IndexWriter indexwriter = new IndexWriter(FSDirectory.Open(new DirectoryInfo(indexpath)),
                analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            return indexwriter;
        }

        internal static ScoreDoc[] Search(IndexSearcher searcher, string searchfield, List<string> keywords, int max_doc_num)
        {
            QueryParser queryparser = new QueryParser(version, searchfield, standardAnalyzer);
            string queryStr = "";
            for (int i = 0; i < keywords.Count - 1; i++)
            {
                queryStr += keywords[i] + " OR ";
            }
            queryStr += keywords[keywords.Count - 1];

            Query query = queryparser.Parse(queryStr);
            TopDocs hits = searcher.Search(query, null, (int)Math.Min(searcher.MaxDoc(), max_doc_num));
            ScoreDoc[] scoredocs = hits.scoreDocs;

            return scoredocs;

            //List<Document> docs = new List<Document>();
            //foreach (var scoredoc in scoredocs)
            //{
            //    docs.Add(searcher.XmlDoc(scoredoc.doc));
            //}
            //return docs;
        }

        

        public static string GetDocumentString(Document doc)
        {
            string str = "";
            foreach (var rawfield in doc.GetFields())
            {
                var field = (Field)rawfield;
                str += string.Format("[{0}]\t{1}\n", field.Name(), field.StringValue());
            }
            return str;
        }

        public static string GetDocumentContent(Document doc, Dictionary<string,int> fieldWeightDict, Dictionary<string, int> leadingSentencesCnt = null)
        {
            string content = "";
            foreach (var kvp in fieldWeightDict)
            {
                var val = doc.Get(kvp.Key);
                if (leadingSentencesCnt != null && leadingSentencesCnt.ContainsKey(kvp.Key))
                    val = StringOperations.GetLeadingSentences(val, leadingSentencesCnt[kvp.Key]);

                for (int i = 0; i < kvp.Value; i++)
                {
                    content += val + " ";
                }
            }
            return content.Substring(0, content.Length - 1);
        }
    }

    //class CustomAnalyzer : Analyzer
    //{
    //    TokenizeConfig tokenizeConfig;
    //    public CustomAnalyzer(TokenizeConfig tokenizeConfig)
    //    {
    //        this.tokenizeConfig = tokenizeConfig;
    //    }

    //    public override TokenStream TokenStream(string fieldName, TextReader reader)
    //    {
    //        var result = new CustomTokenizer(reader, tokenizeConfig);

    //        return result;
    //    }
    //}

    //class CustomTokenizer : StandardTokenizer
    //{
    //    List<string> words;
    //    List<string>.Enumerator enumerator;

    //    public CustomTokenizer(TextReader input, TokenizeConfig tokenizeConfig)
    //        : base(input)
    //    {
    //        words = NLPOperations.Tokenize(input.ReadToEnd(), tokenizeConfig);
    //        enumerator = words.GetEnumerator();
    //    }

    //    public override Lucene.Net.Util.Attribute AddAttribute(Type attClass)
    //    {
    //        return base.AddAttribute(attClass);
    //    }

    //    public override void AddAttributeImpl(Lucene.Net.Util.AttributeImpl att)
    //    {
    //        base.AddAttributeImpl(att);
    //    }

    //    public override Lucene.Net.Util.AttributeSource.State CaptureState()
    //    {
    //        return base.CaptureState();
    //    }

    //    public override void ClearAttributes()
    //    {
    //        base.ClearAttributes();
    //    }

    //    public override Lucene.Net.Util.AttributeSource CloneAttributes()
    //    {
    //        return base.CloneAttributes();
    //    }

    //    public override void Close()
    //    {
    //        base.Close();
    //    }

    //    public override void End()
    //    {
    //        base.End();
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        return base.Equals(obj);
    //    }

    //    public override Lucene.Net.Util.Attribute GetAttribute(Type attClass)
    //    {
    //        return base.GetAttribute(attClass);
    //    }

    //    public override IEnumerable<Type> GetAttributeClassesIterator()
    //    {
    //        return base.GetAttributeClassesIterator();
    //    }

    //    public override Lucene.Net.Util.AttributeSource.AttributeFactory GetAttributeFactory()
    //    {
    //        return base.GetAttributeFactory();
    //    }

    //    public override IEnumerable<Lucene.Net.Util.AttributeImpl> GetAttributeImplsIterator()
    //    {
    //        return base.GetAttributeImplsIterator();
    //    }

    //    public override int GetHashCode()
    //    {
    //        return base.GetHashCode();
    //    }

    //    public override bool HasAttribute(Type attClass)
    //    {
    //        return base.HasAttribute(attClass);
    //    }

    //    public override bool HasAttributes()
    //    {
    //        return base.HasAttributes();
    //    }

    //    public override bool IncrementToken()
    //    {
    //        return base.IncrementToken();
    //    }

    //    public override Lucene.Net.Analysis.Token Next()
    //    {
    //        return base.Next();
    //    }

    //    public override Lucene.Net.Analysis.Token Next(Lucene.Net.Analysis.Token reusableToken)
    //    {
    //        return base.Next(reusableToken);
    //    }

    //    public override void Reset()
    //    {
    //        enumerator = words.GetEnumerator();
    //        base.Reset();
    //    }

    //    public override void Reset(TextReader input)
    //    {
    //        base.Reset(input);
    //    }

    //    public override void RestoreState(Lucene.Net.Util.AttributeSource.State state)
    //    {
    //        base.RestoreState(state);
    //    }

    //    public override string ToString()
    //    {
    //        return base.ToString();
    //    }


        


        //public override Lucene.Net.Analysis.Token Next()
        //{
        //    if (enumerator.MoveNext())
        //    {
        //        var text = enumerator.Current;
        //        return new Lucene.Net.Analysis.Token(text, 0, text.Length);
        //    }
        //    else
        //        return new Lucene.Net.Analysis.Token(null, 0, 0);
        //}
    //}
}