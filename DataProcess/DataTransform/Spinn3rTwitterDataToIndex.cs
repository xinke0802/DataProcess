using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using DataProcess.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace DataProcess.DataTransform
{
    #region Data structures

    public class Spinn3rTwitterData
    {
        public int count { get; set; }
        public List<Spinn3rTweet> items { get; set; }
    }

    public class Spinn3rTweet
    {
        public string lang { get; set; }
        public string permalink { get; set; }
        public string main { get; set; }
        public List<string> links { get; set; }
        public string author_link { get; set; }
        public string author_name { get; set; }
        public List<string> tags { get; set; }
        public string published { get; set; }
        public string source_location { get; set; }
        public string source_description { get; set; }
        public int source_followers { get; set; }
        public int source_following { get; set; }

        public Spinn3rTweet()
        {
            lang = "";
            permalink = "";
            main = "";
            author_link = "";
            author_name = "";
            published = "";
            source_location = "";
            source_description = "";
        }
    }

    public enum SearchSpinn3rType
    {
        Main,
        User
    };
    #endregion

    public class Spinn3rTwitterDataToIndex
    {
        public void TransformUsers(string[] inputFolders, string indexPath, HashSet<string> users)
        {
            List<string> files = new List<string>();

            foreach (var inputFolder in inputFolders)
            {
                files.AddRange(Directory.GetFiles(inputFolder));
            }

            HashSet<string> authorLinks = new HashSet<string>();
            foreach (var user in users)
            {
                //authorLinks.Add("https://twitter.com/" + user);
                authorLinks.Add(user.ToLower());
            }

            TransformWithFileNames(files.ToArray(), indexPath, authorLinks, SearchSpinn3rType.User);
        }

        public void Transform(string[] inputFolders, string indexPath, HashSet<string> keywords)
        {
            List<string> files = new List<string>();

            foreach (var inputFolder in inputFolders)
            {
                files.AddRange(Directory.GetFiles(inputFolder));
            }

            TransformWithFileNames(files.ToArray(), indexPath, keywords, SearchSpinn3rType.Main);
        }

        public void Transform(string inputFolder, string indexPath, HashSet<string> keywords)
        {
            var files = Directory.GetFiles(inputFolder);

            TransformWithFileNames(files, indexPath, keywords, SearchSpinn3rType.Main);
        }

        public void TransformWithFileNameContentSearch(string[] files, string indexPath, string searchStr, string progressEndStr = null)
        {
            double tweetCnt = 0;
            var indexWriter = LuceneOperations.GetIndexWriter(indexPath);
            searchStr = searchStr.ToLower();

            var progress = new ProgramProgress(files.Length);
            int docFoundCount = 0;
            int totalDocCount = 0;
            foreach (var file in files)
            {
                FileOperations.ReadJsonFile<Spinn3rTwitterData>(file, (data) =>
                {
                    tweetCnt += data.count;
                    //Console.WriteLine(data.count);
                    //Console.WriteLine(data.items[0].main);
                    foreach (var tweet in data.items)
                    {
                        if (tweet.lang != "en")
                        {
                            continue;
                        }

                        if (tweet.main.ToLower().Contains(searchStr))
                        {
                            var document = new Document();
                            document.Add(new Field(TweetFields.TweetId, tweet.permalink, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.Text, tweet.main, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserScreenName, tweet.author_link, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserName, tweet.author_name, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.Tags, StringOperations.ConvertNullStringToEmpty(StringOperations.GetMergedString(tweet.tags)), Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.CreatedAt, tweet.published, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.Location, tweet.source_location, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserDescription, tweet.source_description, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserFollowersCount, tweet.source_followers.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserFriendsCount, tweet.source_following.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                            indexWriter.AddDocument(document);
                            docFoundCount++;
                        }
                        totalDocCount++;
                    }
                });
                progress.PrintIncrementExperiment(string.Format("docFound: {0} out of {1} ({2}%) -- {3}", docFoundCount, totalDocCount, 100 * docFoundCount / totalDocCount, progressEndStr));
            }
            progress.PrintTotalTime();

            Console.WriteLine("Final docFound: {0} out of {1} ({2}%)", docFoundCount, totalDocCount, 100 * docFoundCount / totalDocCount);

            Console.WriteLine("Start writing index...");
            indexWriter.Commit();
            indexWriter.Close();

            //Util.ProgramFinishHalt();
        }


        public void TransformWithFileNames(string[] files, string indexPath, HashSet<string> searchHashSet, SearchSpinn3rType searchType)
        {
            double tweetCnt = 0;
            TokenizeConfig tokenizeConfig = new TokenizeConfig(TokenizerType.Twitter);
            var indexWriter = LuceneOperations.GetIndexWriter(indexPath);

            var progress = new ProgramProgress(files.Length);
            int docFoundCount = 0;
            int totalDocCount = 0;
            foreach (var file in files)
            {
                FileOperations.ReadJsonFile<Spinn3rTwitterData>(file, (data) =>
                {
                    tweetCnt += data.count;
                    //Console.WriteLine(data.count);
                    //Console.WriteLine(data.items[0].main);
                    foreach (var tweet in data.items)
                    {
                        if (tweet.lang != "en")
                        {
                            continue;
                        }

                        bool isContainSearch = false;
                        switch (searchType)
                        {
                            case SearchSpinn3rType.Main:
                                var words = NLPOperations.Tokenize(tweet.main, tokenizeConfig);
                                foreach (var word in words)
                                {
                                    if (searchHashSet.Contains(word))
                                    {
                                        isContainSearch = true;
                                        break;
                                    }
                                }
                                break;
                            case SearchSpinn3rType.User:
                                isContainSearch = searchHashSet.Contains(tweet.author_link.ToLower());
                                break;
                            default:
                                throw new ArgumentException();
                        }
                        
                        if (isContainSearch)
                        {
                            var document = new Document();
                            document.Add(new Field(TweetFields.TweetId, tweet.permalink, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.Text, tweet.main, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserScreenName, tweet.author_link, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserName, tweet.author_name, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.Tags, StringOperations.ConvertNullStringToEmpty(StringOperations.GetMergedString(tweet.tags)), Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.CreatedAt, tweet.published, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.Location, tweet.source_location, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserDescription, tweet.source_description, Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserFollowersCount, tweet.source_followers.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                            document.Add(new Field(TweetFields.UserFriendsCount, tweet.source_following.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                            indexWriter.AddDocument(document);
                            docFoundCount++;
                        }
                        totalDocCount++;
                    }
                });
                progress.PrintIncrementExperiment(string.Format("docFound: {0} out of {1} ({2}%)", docFoundCount, totalDocCount, 100 * docFoundCount / totalDocCount));
            }
            progress.PrintTotalTime();

            Console.WriteLine("Final docFound: {0} out of {1} ({2}%)", docFoundCount, totalDocCount, 100 * docFoundCount / totalDocCount);

            Console.WriteLine("Start writing index...");
            indexWriter.Commit();
            indexWriter.Close();

            Util.ProgramFinishHalt();
        }
    }
}
