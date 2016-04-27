using DataProcess.Utils;
using Lucene.Net.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.DataTransform
{
    public class LuceneIndexTransform
    {
        //public LuceneIndexTransform()
        //{

        //}

        public void Transform(string inputPath, string outputPath, Dictionary<string, string> fieldNameTransformDictionary, 
            Dictionary<string, Func<string, string>> fieldValueTransformDictionary = null, Func<Document, bool> documentPredicate = null)
        {
            if (fieldValueTransformDictionary == null)
            {
                fieldValueTransformDictionary = new Dictionary<string, Func<string, string>>();
            }
            if (documentPredicate == null)
            {
                documentPredicate = document => true;
            }

            Func<string, string> defaultValueTransformFunc = str => str;
            LuceneOperations.EnumerateIndexReaderWriter(inputPath, outputPath, (inDoc, indexWriter) =>
            {
                if (documentPredicate(inDoc))
                {
                    var outDoc = new Document();
                    foreach (var kvp in fieldNameTransformDictionary)
                    {
                        var inFieldName = kvp.Key;
                        var inValue = inDoc.Get(inFieldName);
                        if (inValue != null)
                        {
                            var outFieldName = kvp.Value;
                            Func<string, string> valueTransformFunc;
                            if (!fieldValueTransformDictionary.TryGetValue(inFieldName, out valueTransformFunc))
                            {
                                valueTransformFunc = defaultValueTransformFunc;
                            }
                            LuceneOperations.AddField(outDoc, outFieldName, valueTransformFunc(inValue));
                        }
                    }
                    indexWriter.AddDocument(outDoc);
                }
            });
        }

        public void StartTransformTweetIndexForStreamingRoseRiver()
        {
            string inputPath = @"D:\DataProcess\TweetIndex\EbolaTwitter3_Sample0.01\";
            string outputPath = @"D:\DataProcess\TweetIndex\EbolaTwitter3_Sample0.01_MOD\";

            var indexReader = LuceneOperations.GetIndexReader(inputPath);
            var indexWriter = LuceneOperations.GetIndexWriter(outputPath);

            string docIDField = BingNewsFields.DocId;
            string urlField = BingNewsFields.DocumentURL;
            ProgramProgress progress = new ProgramProgress(indexReader.NumDocs());
            for (int iDoc = 0; iDoc < indexReader.NumDocs(); iDoc++)
            {
                Document inDoc = indexReader.Document(iDoc);
                Document outDoc = inDoc;

                outDoc.RemoveField(docIDField);
                outDoc.Add(new Field(docIDField, iDoc.ToString(), Field.Store.YES, Field.Index.ANALYZED));

                outDoc.RemoveField(urlField);
                outDoc.Add(new Field(urlField, "http://" + iDoc.ToString(), Field.Store.YES, Field.Index.ANALYZED));

                indexWriter.AddDocument(inDoc);
                progress.PrintIncrementExperiment();
            }


            indexWriter.Optimize();
            indexWriter.Close();

            indexReader.Close();
        }


        public void Start()
        {
            string inputPath = @"D:\DataProcess\TweetIndex\tweets-Ebola-20150101-20150228_dedup\";
            string outputPath = @"D:\DataProcess\TweetIndex\EbolaTwitter2\";

            var indexReader = LuceneOperations.GetIndexReader(inputPath);
            var indexWriter = LuceneOperations.GetIndexWriter(outputPath);

            char[] seperator = new char[] { ' ' };
            string[] aidFields = new string[] { "User_FollowersCount", "User_Name", "User_ScreenName", 
                "Retweet", "Mention" };
            ProgramProgress progress = new ProgramProgress(indexReader.NumDocs());
            //for (int iDoc = 0; iDoc < 1000; iDoc++)
            for (int iDoc = 0; iDoc < indexReader.NumDocs(); iDoc++)
            {
                Document inDoc = indexReader.Document(iDoc);
                Document outDoc = new Document();

                string inTime = inDoc.Get("CreateAt");
                DateTime dateTime = DateTime.Parse(inTime);
                outDoc.Add(new Field(BingNewsFields.DiscoveryStringTime, dateTime.ToString(BingNewsFields.TimeFormat), Field.Store.YES, Field.Index.ANALYZED));

                string hashtag = inDoc.Get("Hashtag");
                string word = inDoc.Get("Word");
                if (hashtag == null) hashtag = "";
                var hashtagTokens = hashtag.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                var wordTokens = word.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

                string title = hashtagTokens.Length > 0 ? hashtagTokens[0] : wordTokens.Length > 0 ? wordTokens[0] : "";
                outDoc.Add(new Field(BingNewsFields.NewsArticleHeadline, title, Field.Store.YES, Field.Index.ANALYZED));

                outDoc.Add(new Field(BingNewsFields.NewsArticleDescription, inDoc.Get("Text"), Field.Store.YES, Field.Index.ANALYZED));

                string featureVector = "";
                Counter<string> counter = new Counter<string>();
                foreach (var tag in hashtagTokens)
                {
                    counter.Add(tag);
                    counter.Add(tag);
                }
                foreach(var w in wordTokens)
                {
                    counter.Add(w);
                }
                foreach(var kvp in counter.GetSortedCountDictioanry())
                {
                    featureVector += string.Format("{0}({1})\\n", kvp.Key, kvp.Value);
                }
                outDoc.Add(new Field(BingNewsFields.FeatureVector, featureVector, Field.Store.YES, Field.Index.ANALYZED));

                outDoc.Add(new Field(BingNewsFields.DocId, iDoc.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                outDoc.Add(new Field(BingNewsFields.DocumentURL, "http://" + iDoc.ToString(), Field.Store.YES, Field.Index.ANALYZED));

                foreach(var aidField in aidFields)
                {
                    var value = inDoc.Get(aidField);
                    outDoc.Add(new Field(aidField, value == null ? "" : value, Field.Store.YES, Field.Index.ANALYZED));
                }

                indexWriter.AddDocument(outDoc);

                progress.PrintIncrementExperiment();
            }

            indexWriter.Optimize();
            indexWriter.Close();

            indexReader.Close();
        }
    }
}
