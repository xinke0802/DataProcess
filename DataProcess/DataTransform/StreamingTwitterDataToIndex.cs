using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup.Localizer;
using DataProcess.Utils;
using Lucene.Net.Documents;

namespace DataProcess.DataTransform
{
    public class StreamingTwitterDataToIndex
    {
        public void Transform(string inputFolder, string indexPath, HashSet<string> keywords)
        {
            Console.WriteLine("Start to search words: " + StringOperations.GetMergedString(keywords));
            Console.WriteLine("InputFolder: " + inputFolder + "\n");

            string notParseSpecString = "Temp-DoNotParse";
            inputFolder = StringOperations.EnsureFolderEnd(inputFolder);

            string[] schema = new[]
            {
                "CreatedAt", "Text", "IsRetweet", "Retweeted", "RetweetCount",
                "UserScreenName", "UserId", "UserFollowersCount", "UserFriendsCount"
            };
            var schemeDict = Util.GetInvertedDictionary(schema);
            var textFieldIndex = schemeDict["Text"];
            var createdTimeFieldIndex = schemeDict["CreatedAt"];
            var userIdFieldIndex = schemeDict["UserId"];

            //string outputPath = inputFolder + notParseSpecString + "\\";
            //if (Directory.Exists(outputPath))
            //{
            //    Directory.Delete(outputPath, true);
            //}
            //Directory.CreateDirectory(outputPath);
            //var indexPath = outputPath + "Index\\";
            if (Directory.Exists(indexPath))
            {
                Directory.Delete(indexPath, true);
            }

            var files = Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories);

            //Preprocess
            Console.WriteLine("Start preprocesing...");
            ProgramProgress progress = new ProgramProgress(files.Length);
            int estiDocCnt = 0;
            foreach (var file in files)
            {
                estiDocCnt += FileOperations.GetLineCount(file);
                progress.PrintIncrementExperiment();
            }
            progress.PrintTotalTime();
            Console.WriteLine("Estimate tweet count: " + estiDocCnt + "\n");

            //Parse
            Console.WriteLine("Start parsing...");
            
            var indexWriter = LuceneOperations.GetIndexWriter(indexPath);
            TokenizeConfig tokenizeConfig = new TokenizeConfig(TokenizerType.Twitter);

            progress = new ProgramProgress(estiDocCnt);
            var sep = new char[] {'\t'};
            int uniqDocFoundCnt = 0;
            int docFoundCnt = 0;
            int docCnt = 0;

            ThreeLayerHashSet<string, long, string> hash3Layer = new ThreeLayerHashSet<string, long, string>();
            int notUsedDocCnt = 0;
            foreach (var file in files)
            {
                if (file.Contains(notParseSpecString))
                {
                    continue;
                }

                if (file.EndsWith(".txt"))
                {
                    var sr = new StreamReader(file);
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        var tokens = line.Split(sep, StringSplitOptions.None);
                        if (tokens.Length != schema.Length)
                        {
                            notUsedDocCnt++;
                            continue;
                            //throw new ArgumentException();
                        }

                        var words = NLPOperations.Tokenize(tokens[textFieldIndex], tokenizeConfig);
                        bool isContainSearch = false;
                        foreach (var word in words)
                        {
                            if (keywords.Contains(word))
                            {
                                isContainSearch = true;
                                break;
                            }
                        }
                        if (isContainSearch)
                        {
                            string createdAt = tokens[createdTimeFieldIndex];
                            long userId = long.Parse(tokens[userIdFieldIndex]);
                            string text = tokens[textFieldIndex];

                            if (!hash3Layer.Contains(createdAt, userId, text))
                            {
                                var document = new Document();
                                for (int i = 0; i < schema.Length; i++)
                                {
                                    document.Add(new Field(schema[i], tokens[i], Field.Store.YES, Field.Index.ANALYZED));
                                }
                                indexWriter.AddDocument(document);

                                hash3Layer.Add(createdAt, userId, text);
                                
                                uniqDocFoundCnt++;
                            }
                            docFoundCnt++;
                        }
                        docCnt++;
                        progress.PrintIncrementExperiment(string.Format("uniqDocFound: {0} out of {1} ({2}%), docFoundUnqiueRatio: {3}%",
                            uniqDocFoundCnt, docCnt, 100 * uniqDocFoundCnt / docCnt, (docFoundCnt == 0 ? 0 : (100 * uniqDocFoundCnt / docFoundCnt))));
                    }

                    sr.Close();
                }
            }
            progress.PrintTotalTime();

            Console.WriteLine(string.Format("uniqDocFound: {0} out of {1} ({2}%), docFoundUnqiueRatio: {3}%",
                    uniqDocFoundCnt, docCnt, 100 * uniqDocFoundCnt / docCnt, 100 * uniqDocFoundCnt / docFoundCnt));
            Console.WriteLine("Not used doc count: " + notUsedDocCnt);

            Console.WriteLine("Start writing index...");
            indexWriter.Commit();
            indexWriter.Close();

            Console.WriteLine("Finish");
            Console.ReadKey();
        }
    }
}
