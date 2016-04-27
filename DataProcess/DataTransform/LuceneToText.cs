using DataProcess.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;

namespace DataProcess.DataTransform
{
    public class LuceneToTextConfigure : AbstractConfigure
    {
        private static readonly string _configureFileName = "configLuceneToText.txt";

        public LuceneToTextConfigure() 
            : base(_configureFileName)
        {
        }

        public string InputPath;
        public string OutputPath;
        public TokenizeConfig TokenizeConfig = new TokenizeConfig();
        public Dictionary<string, int> FieldWeightDict = new Dictionary<string, int>() { { BingNewsFields.NewsArticleHeadline, 3 }, { BingNewsFields.NewsArticleDescription, 1 } };
        public Dictionary<string, int> LeadingSentenceCntDict = new Dictionary<string, int>{ {BingNewsFields.NewsArticleDescription, 6} };
        public bool IsLoadFromFeatureVector = false;
        public string FeatureVectorField = BingNewsFields.FeatureVector;

        public bool IsFilterByWordCount = false;
        public int MinWordCount = 5;
        public string FilterWordCountIndexPath;
        //public string[] FeatureVectorStopWords = new string[] {"are", "is"};
    }


    /// <summary>
    /// Generate the input for CTM using the lucene index as input
    /// </summary>
    class LuceneToText
    {
        public LuceneToTextConfigure Configure;

        public LuceneToText(bool isLoadFromFile = false)
        {
            Configure = new LuceneToTextConfigure();
            if(isLoadFromFile)
                Configure.Read();
        }

        public void Start()
        {
            var reader = LuceneOperations.GetIndexReader(Configure.InputPath);
            var sw = new StreamWriter(Configure.OutputPath);
            IndexWriter writer = null;
            if (Configure.IsFilterByWordCount)
            {
                writer = LuceneOperations.GetIndexWriter(Configure.FilterWordCountIndexPath);
            }
            if(Configure.IsLoadFromFeatureVector)
                Configure.TokenizeConfig.TokenizerType = TokenizerType.FeatureVector;

            Console.WriteLine("Total: " + reader.NumDocs());
            int docIndex = 0;
            for (int iDoc = 0; iDoc < reader.NumDocs(); iDoc++)
            {
                if (iDoc % 10000 == 0)
                {
                    Console.WriteLine(iDoc);
                    sw.Flush();
                }

                string content = Configure.IsLoadFromFeatureVector ? reader.Document(iDoc).Get(BingNewsFields.FeatureVector) :
                    LuceneOperations.GetDocumentContent(reader.Document(iDoc), Configure.FieldWeightDict, Configure.LeadingSentenceCntDict);
                
                List<string> words = NLPOperations.Tokenize(content, Configure.TokenizeConfig);;
                bool isPrintDoc = !Configure.IsFilterByWordCount || words.Count >= Configure.MinWordCount;
                if (isPrintDoc)
                {
                    if (Configure.IsFilterByWordCount)
                    {
                        writer.AddDocument(reader.Document(iDoc));
                    }

                    sw.Write(docIndex + " " + docIndex + " ");

                    foreach (var word in words)
                        sw.Write(word + " ");
                    sw.Write("\n");

                    docIndex++;
                }
                
            }

            if (Configure.IsFilterByWordCount)
            {
                writer.Optimize();
                writer.Close();
            }

            sw.Flush();
            sw.Close();
            reader.Close();
        }
    }
}
