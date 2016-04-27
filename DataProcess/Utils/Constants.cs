using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;

namespace DataProcess.Utils
{
    public static class TwentyNewsGroupFields
    {
        public static readonly string Title = "Subject";
        public static readonly string Body = "plain";
        /// <summary>
        /// e.g. comp.graphics
        /// </summary>
        public static readonly string NewsGroup = "newsgroup";
        /// <summary>
        /// e.g. 00000000732543023500
        /// </summary>
        public static readonly string Date = "cdate";

        /// <summary>
        /// e.g. comp.graphics
        /// </summary>
        public static readonly string Receiver = "Receiver";
        /// <summary>
        /// e.g. lipman@oasys.dt.navy.mil (Robert Lipman)
        /// </summary>
        public static readonly string Sender = "Sender";
        /// <summary>
        /// e.g. D:\data_test\20newsgroup\20NG_Source\comp.graphics\37261
        /// </summary>
        public static readonly string URI = "uri"; 
    }

    //public static class RawDataFields
    //{
    //    public static readonly string RawTimeFormat = "M/d/yyyy H:m:s am";
    //}

    public static class TweetFields
    {
        public static readonly string TweetId = "TweetId";
        public static readonly string CreatedAt = "CreatedAt";
        public static readonly string Text = "Text";
        public static readonly string IsRetweet = "IsRetweet";
        public static readonly string Retweeted = "Retweeted";
        public static readonly string RetweetCount = "RetweetCount";
        public static readonly string UserScreenName = "UserScreenName";
        public static readonly string UserName = "UserName";
        public static readonly string UserDescription = "UserDescription";
        public static readonly string Tags = "Tags";
        public static readonly string UserId = "UserId";
        public static readonly string UserFollowersCount = "UserFollowersCount";
        public static readonly string UserFavoritesCount = "UserFavoritesCount";
        public static readonly string UserFriendsCount = "UserFriendsCount";
        public static readonly string UserLikesCount = "UserLikesCount";
        public static readonly string UserStatusesCount = "UserStatusesCount";
        public static readonly string Location = "Location";
        public static readonly string UtcOffset = "UtcOffset";
        public static readonly string Language = "Language";

        public static readonly string InReplyToStatusId = "InReplyToStatusId";
        public static readonly string InReplyToUserId = "InReplyToUserId";
        public static readonly string InReplyToScreenName = "InReplyToScreenName";
        public static readonly string HasAdult = "HasAdult";

        public static readonly string Sentiment = "Sentiment";
        public static readonly string SentimentScore = "SentimentScore";
        

        private static readonly string TimeFormat = "M/d/yyyy h:mm:ss tt";

        public static DateTime GetDateTimeByDocument(Document document)
        {
            return GetDateTimeByString(document.Get(CreatedAt));
        }

        public static DateTime GetDateTimeByString(string dateTimeString)
        {
            return StringOperations.ParseDateTimeStringSystem(dateTimeString, TimeFormat);
        }


        public static string DateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString(TimeFormat, System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public static class ProfileFields
    {
        public static readonly string UserScreenName = TweetFields.UserScreenName;
        public static readonly string UserName = TweetFields.UserName;
        public static readonly string UserDescription = TweetFields.UserDescription;
        public static readonly string CreatedAt = TweetFields.CreatedAt;
        public static readonly string Language = TweetFields.Language;
        public static readonly string UserId = TweetFields.UserId;
        public static readonly string Url = "Url";
        public static readonly string Location = TweetFields.Location;
        public static readonly string UtcOffset = "UtcOffset";
        public static readonly string FavouritesCount = "FavouritesCount";
        public static readonly string FollowersCount = "FollowersCount";
        public static readonly string FriendsCount = "FriendsCount";
        public static readonly string TwitterListCount = "TwitterListCount";
        public static readonly string TotalTweetCount = "TotalTweetCount";

        public static readonly string TwitterLists = "TwitterLists";
        public static readonly string Friendship = "Friendship";
        public static readonly string CleanLocationHierarchy = "CleanLocationHierarchy";

        public static Func<Document, DateTime> GetDateTimeByDocument = TweetFields.GetDateTimeByDocument;
        public static Func<string, DateTime> GetDateTimeByString = TweetFields.GetDateTimeByString;
        public static Func<DateTime, string> DateTimeToString = TweetFields.DateTimeToString;
    }
    
    public static class BingNewsFields
    {
        public static readonly string DocumentURL = "DocumentURL";
        public static readonly string Country = "Country";
        public static readonly string NewsArticleCategoryData = "NewsArticleCategoryData";
        public static readonly string NewsArticleHeadline = "NewsArticleHeadline";
        public static readonly string NewsArticleDescription = "NewsArticleDescription";
        public static readonly string DiscoveryStringTime = "DiscoveryStringTime";
        public static readonly string PublishedDateTime = "PublishedDateTime";
        public static readonly string DownloadStringTime = "DownloadStringTime";
        public static readonly string NewsSource = "NewsSource";
        public static readonly string NewsArticleBodyNEMap = "NewsArticleBodyNEMap";
        public static readonly string RealTimeType = "RealTimeType";
        public static readonly string Language = "Language";

        public static readonly string DocumentUrl = "DocumentUrl";
        public static readonly string FeatureVector = "FeatureVector";
        public static readonly string DocId = "DocId";

        public static readonly string TimeFormat = "yyyy-MM-dd hh:mm:ss";

        public static readonly string User_ScreenName = "User_ScreenName";
        public static readonly string User_Name = "User_Name";
        public static readonly string User_FollowersCount = "User_FollowersCount";

        public static readonly Dictionary<string, int> NewsFieldWeights = new Dictionary<string, int>()
        {
            {NewsArticleHeadline, 3},
            {NewsArticleDescription, 1}
        };

        public static readonly  Dictionary<string, int> FeatureVectorFieldWeights = new Dictionary<string, int>()
        {
            {FeatureVector, 1}
        };
    }

    /// <summary>
    /// 尽量别用了，统一成TwitterFields
    /// </summary>
    public static class NewCongressTwitterFields
    {
        public static readonly string CreatedAt = "CreatedAt";
        public static readonly string FriendsCount = "FriendsCount";
        public static readonly string TweetId = "ID";
        public static readonly string IsRetweet = "IsRetweet";
        public static readonly string Language = "Language";
        public static readonly string RetweetCount = "RetweetCount";
        public static readonly string Text = "Text";
        public static readonly string UserDescription = "User_Description";
        public static readonly string UserFollowersCount = "User_FollowersCount";
        public static readonly string UserId = "User_ID";
        public static readonly string UserName = "User_Name";
        public static readonly string UserScreenName = "User_ScreenName";
        //public static readonly string Retweeted = "Retweeted";
        //public static readonly string Tags = "Tags";
        //public static readonly string UserFriendsCount = "UserFriendsCount";
        //public static readonly string Location = "Location";
        //public static readonly string UtcOffset = "UtcOffset";
        public static readonly string TimeFormat = "M/d/yyyy H:m:s am";
        public static readonly string TwitterLists = ProfileFields.TwitterLists;
    }

    /// <summary>
    /// 尽量别用了，统一成TwitterFields
    /// </summary>
    public static class Ebola2014PreviousFields
    {
        public static readonly string DocumentURL = "DocumentURL";
        public static readonly string Country = "Country";
        public static readonly string NewsArticleCategoryData = "NewsArticleCategoryData";
        public static readonly string NewsArticleHeadline = "NewsArticleHeadline";
        public static readonly string NewsArticleDescription = "NewsArticleDescription";
        public static readonly string DiscoveryStringTime = "DiscoveryStringTime";
        public static readonly string PublishedDateTime = "PublishedDateTime";
        public static readonly string DownloadStringTime = "DownloadStringTime";
        public static readonly string NewsSource = "NewsSource";
        public static readonly string NewsArticleBodyNEMap = "NewsArticleBodyNEMap";
        public static readonly string RealTimeType = "RealTimeType";
        public static readonly string Language = "Language";
        public static readonly string Retweet = "Retweet";

        public static readonly string DocumentUrl = "DocumentUrl";
        public static readonly string FeatureVector = "FeatureVector";
        public static readonly string DocId = "DocId";

        public static readonly string TimeFormat = "yyyy-MM-dd hh:mm:ss";

        public static readonly string User_ScreenName = "User_ScreenName";
        public static readonly string User_Name = "User_Name";
        public static readonly string User_FollowersCount = "User_FollowersCount";
    }

    public class OldCongressUserFields
    {
        public static readonly string ScreenNameField = "ScreenName";
        public static readonly string UserGroupField = "Party";

        public static readonly Dictionary<string, int> UserGroupToCorpusIDDict = new Dictionary<string, int>()
            {
                {"Democratic", 0},
                {"Republican", 1},
            };
    }

    public class OldCongressTweetFields
    {
        public static readonly string UserField = "RepreUser";
        public static readonly string TweetField = "Text";
        public static readonly string TimeField = "Time";
        public static readonly string WordField = "Words";

        public static DateTime ParseTimeFunction(string timeString)
        {
            var year = int.Parse(timeString.Substring(0, 4));
            var month = int.Parse(timeString.Substring(4, 2));
            var day = int.Parse(timeString.Substring(6, 2));
            var hour = int.Parse(timeString.Substring(8, 2));
            var minute = int.Parse(timeString.Substring(10, 2));
            return new DateTime(year, month, day, hour, minute, 0);
        }
    }
}
