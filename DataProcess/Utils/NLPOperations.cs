using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataProcess.NoiseRemoval;
using DataProcess.Utils;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using MSRA.NLC.Common.MLT;
using MSRA.NLC.Common.NLP;
using MSRA.NLC.Common.NLP.Twitter;
using MSRA.NLC.Sentiment.Common;
using MSRA.NLC.Sentiment.Core;
using MSRA.NLC.Sentiment.Utils;


namespace DataProcess.Utils
{
    public enum SentimentType
    {
        Positive,
        Negative,
        Neutral
    };

    public enum NrcSentimentType
    {
        anger,
        anticipation,
        disgust,
        fear,
        joy,
        negative,
        positive,
        sadness,
        surprise,
        trust,
    };
    //public enum NrcSentimentType
    //{
    //    Anger,
    //    Anticipation,
    //    Disgust,
    //    Fear,
    //    Joy,
    //    Negative,
    //    Positive,
    //    Sadness,
    //    Surprise,
    //    Trust,
    //};

    public class SentimentAnalyzer
    {
        private SentimentEngine _engine;
        public SentimentAnalyzer()
        {
            Console.WriteLine("[LOG] /BEGIN AT {0}\n", DateTime.Now.ToString());

            // Please copy the models from <code>\\msranlcqa02\social\root\model</code> and set
            // the root path to your local copy.
            //Constants.SPP_RootPath = @"../../../../SentimentAnalyzeRoot";
            Constants.SPP_RootPath = @"Utils\\SentimentAnalysis";

            // train a model and test it
            // NOTE: SET THIS FLAG AS TRUE IF YOU WANT TO TRAIN YOUR OWN MODEL!
            bool train_test_flag = false;

            if (train_test_flag)
            {
                // Train and test data from SemEval 2013 <code>\\msranlcqa02\social\root\data</code>
                string train = Path.Combine(Constants.SPP_RootPath, @"data\train");
                string test = Path.Combine(Constants.SPP_RootPath, @"data\test");
                SentimentLearner.Train(train, test, Constants.SPP_RootPath);
                return;
            }

            ITokenizer tokenizer = new TwitterTokenizer();
            IWordNormalizer wordNormalizer = new TweetWordNormalizer();
            ITextConverter textConverter = new BaseTextConverter(tokenizer, null, null, wordNormalizer);

            classifier = "LIBLINEAER";
            //classifier = "svm_light";

            ISemanticTagger lexiconTagger = new LexiconSentimentTagger(tokenizer);

            SECoreImpl coreImpl = CreateLearningCoreImpl(Constants.SPP_RootPath);
            _engine = new SentimentEngine(textConverter, lexiconTagger, coreImpl);

            Console.WriteLine("\n[LOG] /END AT {0}\n", DateTime.Now.ToString());

            string testFilePath = Path.Combine(Constants.SPP_RootPath, @"data\test");
            //testFilePath = @"D:\users\fuwei\Sentiment\root\data\carol\full";
            Test(testFilePath, _engine);
        }

        /// <summary>
        /// returns true if success and false if failed. For every sentimentType = neutral, the output score is zero
        /// </summary>
        public bool GetSentiment(string sentence, out SentimentType sentimentType, out double score, bool isTransformNeutralScore = true)
        {
            sentimentType = SentimentType.Neutral;
            score = 0;
            if (string.IsNullOrWhiteSpace(sentence))
                return false;

            sentence = sentence.Trim();

            IList<Sentiment> sentiments = _engine.Analyze(sentence);
            if (sentiments.Count == 0)
                return false;
            var sentiment = sentiments.First();
            score = sentiment.Score;
            switch (sentiment.Polarity)
            {
                case Polarity.Negative:
                    sentimentType = SentimentType.Negative;
                    break;
                case Polarity.Positive:
                    sentimentType = SentimentType.Positive;
                    break;
                case Polarity.Neutral:
                    if (isTransformNeutralScore)
                    {
                        score = 0;
                    }
                    sentimentType = SentimentType.Neutral;
                    break;
                default:
                    throw new ArgumentException();
            }
            return true;
        }


        static void Test(string file, SentimentEngine engine)
        {
            SentimentEvaluator evaluator = new SentimentEvaluator(engine);
            evaluator.Evaluate(file);
        }

        static SECoreImpl CreateLearningCoreImpl(string root)
        {
            string path = Path.Combine(root, @"model\sentiment\learning\");

            string subFeatureSetFilePath = Path.Combine(path, @"sub.fv");
            string subjectivityModelFilePath = Path.Combine(path, @"sub.model");
            BaseFeatureExtractor subFeatureExtractor = null;
            BaseDecoder sub_decoder = null;
            if (File.Exists(subFeatureSetFilePath) && File.Exists(subjectivityModelFilePath))
            {
                subFeatureExtractor = new SubjectivityFeatureExtractor(subFeatureSetFilePath);
                sub_decoder = CreateDecoder(subjectivityModelFilePath, classifier);
            }

            string polFeatureSetFilePath = Path.Combine(path, @"pol.fv");
            BaseFeatureExtractor polFeatureExtractor = new PolarityFeatureExtractor(polFeatureSetFilePath);

            string polarityModelFilePath = Path.Combine(path, @"pol.model");
            BaseDecoder pol_decoder = CreateDecoder(polarityModelFilePath, classifier);

            SELearningCoreImpl coreImpl = new SELearningCoreImpl(subFeatureExtractor, polFeatureExtractor,
                sub_decoder, pol_decoder);

            return coreImpl;
        }

        static BaseDecoder CreateDecoder(string model, string classifier)
        {
            BaseDecoder decoder = null;
            if (classifier == "LIBLINEAER")
            {
                decoder = new LibLinearDecoder(model);
            }
            else
            {
                decoder = new SVMDecoder(model);
            }

            return decoder;
        }

        private static string classifier = "LIBLINEAER"; //"svm_light"
    }

    public class StopWordsFile
    {
        public static readonly string EN = "Utils\\Stopwords\\General\\EN.txt";
        public static readonly string ENTwitter = "Utils\\Stopwords\\General\\ENTwitter.txt";
        public static readonly string CH = "Utils\\Stopwords\\General\\CH.txt";
        public static readonly string FR = "Utils\\Stopwords\\General\\FR.txt";
        public static readonly string NO = "Utils\\Stopwords\\General\\NO.txt";

        //Spec
        public static readonly string SpecCHBAT360 = "Utils\\Stopwords\\Specific\\SpecCHBAT360.txt";
        public static readonly string SpecCHTechNews = "Utils\\Stopwords\\Specific\\SpecCHTechNews.txt";
        public static readonly string SpecENDebtCrisis = "Utils\\Stopwords\\Specific\\SpecENDebtCrisis.txt";
        public static readonly string SpecENMicrosoft = "Utils\\Stopwords\\Specific\\SpecENMicrosoft.txt";
        public static readonly string SpecENObamaElection = "Utils\\Stopwords\\Specific\\SpecENObamaElection.txt";
        public static readonly string SpecENSyria = "Utils\\Stopwords\\Specific\\SpecENSyria.txt";
        public static readonly string SpecENTechCompanies = "Utils\\Stopwords\\Specific\\SpecENTechCompanies.txt";
        public static readonly string SpecENTechNews = "Utils\\Stopwords\\Specific\\SpecENTechNews.txt";
        public static readonly string SpecENEbolaNews = "Utils\\Stopwords\\Specific\\SpecENEbolaNews.txt";
        public static readonly string SpecENEbolaNewsExtend = "Utils\\Stopwords\\Specific\\SpecENEbolaNewsExtend.txt";
        public static readonly string SpecENEbolaTwitter = "Utils\\Stopwords\\Specific\\SpecENEbolaTwitter.txt";
    }

    public class UserDictionaryFile
    {
        public static readonly string CHMH370 = "Utils\\UserDictionaries\\CHMH370.txt";
        public static readonly string CHBAT360 = "Utils\\UserDictionaries\\CHBAT360.txt";
    }

    public enum TokenizerType {Standard, ICTCLAS, Twitter, TweetUser, Hashtag, Mention, Retweet, ChineseWordBreaker, SimpleSplit, FeatureVector};
    public class TokenizeConfig : IConfigParameter
    {
        #region Interfaces
        public void Get(List<string> configStrs, string seperator4 = "\t")
        {
            foreach(var configStr in configStrs)
            {
                var tokens = configStr.Split(new string[] { seperator4 }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 1)
                    continue;
                var paraName = tokens[0];
                var paraVal = tokens.Length >= 2 ? tokens[1] : null;
                switch(paraName)
                {
                    case "TokenizerType":
                        TokenizerType = (TokenizerType)StringOperations.ParseEnum(typeof(TokenizerType), paraVal);
                        break;
                    case "StopWordFile":
                        StopWordFile = paraVal;
                        break;
                    case "AddStopWordsFile":
                        AddStopWordsFile = paraVal;
                        break;
                    case "UserDictFile":
                        UserDictFile = paraVal;
                        break;
                    default:
                        throw new ArgumentException();
                }
            }

            Initialize();
        }

        public List<string> GetText(string seperator4 = "\t")
        {
            List<string> strs = new List<string>();
            strs.Add("TokenizerType" + seperator4 + TokenizerType);
            strs.Add("StopWordFile" + seperator4 + StopWordFile);
            strs.Add("AddStopWordsFile" + seperator4 + AddStopWordsFile);
            strs.Add("UserDictFile" + seperator4 + UserDictFile);
            return strs;
        }
        #endregion 

        public string StopWordFile = StopWordsFile.EN;
        public string AddStopWordsFile = StopWordsFile.SpecENTechNews;
        public string UserDictFile = UserDictionaryFile.CHMH370;

        public TokenizerType TokenizerType = TokenizerType.Standard;

        public string[] StopWords;
        public Dictionary<string, string> UserDict;

        static Dictionary<string, string[]> _stopwordsCache = new Dictionary<string, string[]>();

        public TokenizeConfig()
        {

        }

        public TokenizeConfig(string configStr)
        {
            foreach (var kvp in StringOperations.ParseStringStringDictionary(configStr))
            {
                switch (kvp.Key)
                {
                    case "TokenizerType":
                        TokenizerType = (TokenizerType)StringOperations.ParseEnum(typeof(TokenizerType), kvp.Value);
                        break;
                    case "StopWordFile":
                        StopWordFile = kvp.Value;
                        break;
                    case "AddStopWordsFile":
                        AddStopWordsFile = kvp.Value.Length > 0 ? kvp.Value : null;
                        break;
                    case "UserDictFile":
                        UserDictFile = kvp.Value.Length > 0 ? kvp.Value : null;
                        break;
                }
            }

            Initialize();
        }

        public TokenizeConfig(TokenizerType TokenizerType,
            string StopWordFile = null, string AddStopWordsFile = null,
            string UserDictFile = null)
        {
            this.TokenizerType = TokenizerType;
            this.StopWordFile = StopWordFile;
            this.AddStopWordsFile = AddStopWordsFile;
            this.UserDictFile = UserDictFile;

            Initialize();
        }

        private void Initialize()
        {
            if (StopWordFile == null)
                StopWordFile = StopWordsFile.NO;

            if (UserDictFile != null)
                this.UserDict = FileOperations.LoadDictionaryFile(UserDictFile);

            string[] stopwords = null;
            if (!_stopwordsCache.TryGetValue(StopWordFile + "\t" + AddStopWordsFile, out stopwords))
            {
                var stopwordlist = new List<string>();
                stopwordlist.AddRange(FileOperations.LoadKeyWordFile(this.StopWordFile));
                if (AddStopWordsFile != null)
                    stopwordlist.AddRange(FileOperations.LoadKeyWordFile(this.AddStopWordsFile));
                stopwords = stopwordlist.ToArray<string>();

                _stopwordsCache.Add(StopWordFile + "\t" + AddStopWordsFile, stopwords);
            }

            StopWords = stopwords;
        }
    }

    public class VectorGenerator
    {
        public Dictionary<string, int> Lexicon { get; set; }

        TokenizeConfig _tokenizeConfig;
        Dictionary<string, int> _fieldWeightDict;
        Dictionary<string, int> _leadingSentencesCnt;
        public VectorGenerator(TokenizeConfig tokenizeConfig, Dictionary<string, int> fieldWeightDict = null, Dictionary<string, int> leadingSentencesCnt = null)
        {
            if (fieldWeightDict == null)
            {
                fieldWeightDict = new Dictionary<string, int>()
                {
                    {BingNewsFields.NewsArticleHeadline , 3},
                    {BingNewsFields.NewsArticleDescription, 1}
                };
            }

            if (leadingSentencesCnt == null)
            {
                leadingSentencesCnt = new Dictionary<string, int> { { BingNewsFields.NewsArticleDescription, 6 } };
            }

            _tokenizeConfig = tokenizeConfig;
            _fieldWeightDict = fieldWeightDict;
            _leadingSentencesCnt = leadingSentencesCnt;

            Lexicon = new Dictionary<string, int>();
        }

        public SparseVectorList GetFeatureVector(Document doc)
        {
            SparseVectorList featurevector = new SparseVectorList();

            int lexiconindexcount = Lexicon.Count;

            var content = LuceneOperations.GetDocumentContent(doc, _fieldWeightDict, _leadingSentencesCnt);
            var words = NLPOperations.Tokenize(content, _tokenizeConfig);

            foreach (var word in words)
            {
                int value = 0;
                if (Lexicon == null || Lexicon.TryGetValue(word, out value) == false)
                {
                    Lexicon.Add(word, lexiconindexcount);
                    value = lexiconindexcount;
                    lexiconindexcount++;
                }
                if (!featurevector.Increase(value, 1))
                {
                    featurevector.Insert(value, 1);
                }
            }

            featurevector.ListToArray();
            featurevector.count = featurevector.keyarray.Length;
            //featurevector.SumUpValueArray();
            if (featurevector.count < 1)
                return null;
            featurevector.InvalidateList();
            featurevector.GetNorm();
            return featurevector;
        }

        public SparseVectorList GetFeatureVector(Dictionary<string, int> vec)
        {
            SparseVectorList featurevector = new SparseVectorList();

            int lexiconindexcount = Lexicon.Count;

            foreach (var kvp in vec)
            {
                var word = kvp.Key;
                int value = 0;
                if (Lexicon == null || Lexicon.TryGetValue(word, out value) == false)
                {
                    Lexicon.Add(word, lexiconindexcount);
                    value = lexiconindexcount;
                    lexiconindexcount++;
                }
                if (!featurevector.Increase(value, kvp.Value))
                {
                    featurevector.Insert(value, kvp.Value);
                }
            }

            featurevector.ListToArray();
            featurevector.count = featurevector.keyarray.Length;
            if (featurevector.count < 1)
                return null;
            featurevector.InvalidateList();
            featurevector.GetNorm();
            return featurevector;
        }
    }


    public class NLPOperations
    {
        #region Judge Chinese
        protected static bool IsChineseLetter(string input, int index)
        {

            int code = 0;
            int chfrom = Convert.ToInt32("4e00", 16);    //范围（0x4e00～0x9fff）转换成int（chfrom～chend）
            int chend = Convert.ToInt32("9fff", 16);
            if (input != "")
            {
                code = Char.ConvertToUtf32(input, index);    //获得字符串input中指定索引index处字符unicode编码

                if (code >= chfrom && code <= chend)
                {
                    return true;     //当code在中文范围内返回true

                }
                else
                {
                    return false;    //当code不在中文范围内返回false
                }
            }

            return false;
        }

        public static bool IsChineseWord(string word)
        {
            for (int i = 0; i < word.Length; i++)
            {
                if (IsChineseLetter(word, i))
                    return true;
            }
            return false;
        }


        //static Regex EnglishCharacterRegex = new Regex(@"^[a-zA-Z0-9\p{S}\p{P}]*$", RegexOptions.Compiled);
        static Regex EnglishLetterNumberRegex = new Regex("^[a-zA-Z0-9\\s]*$", RegexOptions.Compiled);
        //static Regex EnglishPunctuationRegex = new Regex(@"^[\p{P}]*", RegexOptions.Compiled);

        static HashSet<char> EnglishPunctuationHash = new HashSet<char>(new char[] { ' ', ',', '.', '?',':','!',';','(',')','\'','\"','[',']',
            '{','}','–','—','-','+','=','/','“','”','’','<','>','_'});
        public static bool IsEnglish(string str)
        {
            //var match = EnglishCharacterRegex.Match(word);
            //Trace.WriteLine(match);
            //return match.Success;
            bool isEnglish = true;
            foreach (var fo in str)
            {
                //UnicodeCategory cat = char.GetUnicodeCategory(fo);
                var fostr = fo.ToString();
                //if (str == "我")
                //    Trace.WriteLine("");
                if (!EnglishLetterNumberRegex.IsMatch(fostr) && !EnglishPunctuationHash.Contains(fo))
                {
                    Trace.WriteLine("Non english char: " + fostr);
                    isEnglish = false;
                    break;
                }
            }
            return isEnglish;
        }
        #endregion

        public static List<string> Tokenize(string content, TokenizeConfig config)
        {

            switch (config.TokenizerType)
            {
                case TokenizerType.Standard:
                    return TokenizeStandard(content, config);
                case TokenizerType.ICTCLAS:
                    return TokenizeICTCLAS(content, config);
                case TokenizerType.Twitter:
                    return TokenizeTwitter(content, config);
                case TokenizerType.TweetUser:
                    return TokenizeTweetUser(content, config);
                case TokenizerType.Hashtag:
                    return TokenizeHashtag(content, config);
                case TokenizerType.Mention:
                    return TokenizeMention(content, config);
                case TokenizerType.Retweet:
                    return TokenizeRetweet(content, config);
                case TokenizerType.ChineseWordBreaker:
                    return TokenizeCWB(content, config);
                case TokenizerType.SimpleSplit:
                    return TokenizeSimpleSplit(content, config);
                case TokenizerType.FeatureVector:
                    return TokenizeFeatureVector(content, config);
                default:
                    throw new NotImplementedException();
            }
        }

        static List<string> TokenizeStandard(string content, TokenizeConfig config)
        {
            StringReader reader = new StringReader(content);
            TokenStream result = new StandardTokenizer(Lucene.Net.Util.Version.LUCENE_24, reader);

            var stophash = StopFilter.MakeStopSet(config.StopWords);
            result = new StandardFilter(result);
            result = new LowerCaseFilter(result);
            result = new StopFilter(true, result, stophash, true);

            /// Set up lexicon/invertlexicon, featurevectors, wordappearancecount ///
            result.Reset();
            TermAttribute termattr = (TermAttribute)result.GetAttribute(typeof(TermAttribute));
            List<string> words = new List<string>();
            while (result.IncrementToken())
            {
                words.Add(termattr.Term());
            }
            return words;
        }

        #region import dlls
        [StructLayout(LayoutKind.Explicit)]
        struct result_t
        {
            [FieldOffset(0)]
            public int start;
            [FieldOffset(4)]
            public int length;
            [FieldOffset(8)]
            public int sPos1;
            [FieldOffset(12)]
            public int sPos2;
            [FieldOffset(16)]
            public int sPos3;
            [FieldOffset(20)]
            public int sPos4;
            [FieldOffset(24)]
            public int sPos5;
            [FieldOffset(28)]
            public int sPos6;
            [FieldOffset(32)]
            public int sPos7;
            [FieldOffset(36)]
            public int sPos8;
            [FieldOffset(40)]
            public int sPos9;
            [FieldOffset(44)]
            public int sPos10;
            //[FieldOffset(12)] public int sPosLow;
            [FieldOffset(48)]
            public int POS_id;
            [FieldOffset(52)]
            public int word_ID;
            [FieldOffset(56)]
            public int word_type;
            [FieldOffset(60)]
            public int weight;
        }

        const string path = @"Utils\Lib\NLPIR\NLPIR.dll";//设定dll的路径
        const string datapath = @"Utils\Lib\NLPIR\";
        //对函数进行申明
        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_Init")]
        static extern bool NLPIR_Init(String sInitDirPath, int encoding, String sLicenseCode);

        //特别注意，C语言的函数NLPIR_API const char * NLPIR_ParagraphProcess(const char *sParagraph,int bPOStagged=1);必须对应下面的申明
        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_ParagraphProcess")]
        static extern IntPtr NLPIR_ParagraphProcess(String sParagraph, int bPOStagged = 1);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_Exit")]
        static extern bool NLPIR_Exit();

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_ImportUserDict")]
        static extern int NLPIR_ImportUserDict(String sFilename);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_FileProcess")]
        static extern bool NLPIR_FileProcess(String sSrcFilename, String sDestFilename, int bPOStagged = 1);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_FileProcessEx")]
        static extern bool NLPIR_FileProcessEx(String sSrcFilename, String sDestFilename);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_GetParagraphProcessAWordCount")]
        static extern int NLPIR_GetParagraphProcessAWordCount(String sParagraph);
        //NLPIR_GetParagraphProcessAWordCount
        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_ParagraphProcessAW")]
        static extern void NLPIR_ParagraphProcessAW(int nCount, [Out, MarshalAs(UnmanagedType.LPArray)] result_t[] result);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_AddUserWord")]
        static extern int NLPIR_AddUserWord(String sWord);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_SaveTheUsrDic")]
        static extern int NLPIR_SaveTheUsrDic();

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_DelUsrWord")]
        static extern int NLPIR_DelUsrWord(String sWord);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_Start")]
        static extern bool NLPIR_NWI_Start();

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_Complete")]
        static extern bool NLPIR_NWI_Complete();

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_AddFile")]
        static extern bool NLPIR_NWI_AddFile(String sText);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_AddMem")]
        static extern bool NLPIR_NWI_AddMem(String sText);

        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_NWI_GetResult")]
        static extern IntPtr NLPIR_NWI_GetResult(bool bWeightOut = false);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_Result2UserDict")]
        static extern uint NLPIR_NWI_Result2UserDict();

        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_GetKeyWords")]
        static extern IntPtr NLPIR_GetKeyWords(String sText, int nMaxKeyLimit = 50, bool bWeightOut = false);

        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_GetFileKeyWords")]
        static extern IntPtr NLPIR_GetFileKeyWords(String sFilename, int nMaxKeyLimit = 50, bool bWeightOut = false);
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        #endregion

        static bool IsICTCLASInitialized = false;
        static Regex Regex = new Regex("^[0-9a-z\u4e00-\u9fa5]*$");
        [STAThread]
        static List<string> TokenizeICTCLAS(string content, TokenizeConfig config)
        {
            if (!IsICTCLASInitialized)
            {
                if (!NLPIR_Init(datapath, 0, ""))//给出Data文件所在的路径，注意根据实际情况修改。
                {
                    throw new Exception("Init ICTCLAS failed!");
                }
                //System.Console.WriteLine("Init ICTCLAS success!");

                IsICTCLASInitialized = true;
            }

            //Add user dictionary
            if (config.UserDict != null && config.UserDict.Count != 0)
            {
                foreach(var kvp in config.UserDict)
                    NLPIR_AddUserWord(kvp.Key + " " + kvp.Value);//词 词性 example:点击下载 vyou
            }   

            //Tokenize
            var intPtr = NLPIR_ParagraphProcess(content.ToLower(), 1);
            var str = Marshal.PtrToStringAnsi(intPtr);
            var tokens = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> words = new List<string>();
            foreach (var token in tokens)
            {
                var index = token.IndexOf('/');
                if (index > 0)
                    words.Add(token.Substring(0, index));
            }

            //Filter Stopwords
            var words2 = new List<string>();
            var stophash = StopFilter.MakeStopSet(config.StopWords);
            foreach (var word in words)
            {
                if (!stophash.Contains(word) && Regex.Match(word).Success)
                    words2.Add(word);
            }

            return words2;
        }


        static Regex ENRegex = new Regex("^[0-9a-z-_]*$");
        static Regex ENNumRegex = new Regex("^[0-9]*$");
        static char[] TweetSeperator = new char[] { ' ', '\n', '\t', '\r', ',', '.', '?',':','!',';','(',')','\'','\"','[',']',
            '{','}','–','—'};
        private static List<string> TokenizeTwitter(string content, TokenizeConfig config)
        {
            content = content.ToLower();
            content = RemoveContentNoise.RemoveTweetTokenizeNoise(content);

            var sep = TweetSeperator;
            var tokens = content.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            var stophash = Util.GetHashSet(config.StopWords);

            List<string> words = new List<string>();
            foreach (var token in tokens)
            {
                if (token.StartsWith("@"))
                    continue;
                var word = token;
                if (word.StartsWith("#"))
                {
                    word = word.Substring(1);
                }
                if (ENRegex.Match(word).Success && !ENNumRegex.Match(word).Success)
                {
                    if (!stophash.Contains(word))
                        words.Add(word);
                }
            }

            //Trace.WriteLine(content);
            //DiagnosticsOperations.Print(words);

            return words;
        }


        private static List<string> TokenizeTweetUser(string content, TokenizeConfig config)
        {
            content = content.ToLower();
            content = RemoveContentNoise.RemoveTweetTokenizeNoise(content);

            var sep = TweetSeperator;
            var tokens = content.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            var stophash = Util.GetHashSet(config.StopWords);

            List<string> words = new List<string>();
            foreach (var token in tokens)
            {
                if (token.StartsWith("@"))
                {
                    var word = token;
                    word = word.Substring(1);

                    if (!stophash.Contains(word))
                        words.Add(word);
                }
            }

            return words;
        }


        static char[] SimpleSplitSeperator = new char[] { ' ', ',', '.', '?',':','!',';','(',')','\'','\"','[',']',
            '{','}','–','—', '@', '#'};

        private static List<string> TokenizeSimpleSplit(string content, TokenizeConfig config)
        {
            content = content.ToLower();

            var tokens = content.Split(SimpleSplitSeperator, StringSplitOptions.RemoveEmptyEntries);

            List<string> words = new List<string>();
            foreach (var word in tokens)
            {
                //if (ENRegex.Match(word).Success)
                if (word.Length < 20)
                    words.Add(word);
            }

            return words;
        }

        private static List<string> TokenizeFeatureVector(string content, TokenizeConfig config)
        {
            var wordDict = new Dictionary<string, int>();
            var wordDictString = content;
            var wordDictStringSplitList = wordDictString.Split(new string[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in wordDictStringSplitList)
            {
                if (!string.IsNullOrWhiteSpace(str))
                {
                    int pos2 = str.IndexOf('(');
                    wordDict[str.Substring(0, pos2)] = (int)int.Parse(str.Substring(pos2 + 1, str.Length - pos2 - 2));
                }
            }
            List<string> words = new List<string>();
            foreach (var kvp in wordDict)
            {
                if (!config.StopWords.Contains(kvp.Key))
                {
                    for (int i = 0; i < kvp.Value; i++)
                    {
                        words.Add(kvp.Key);
                    }
                }
            }
            //wordDict =
            //    wordDict.Where(kvp => !config.StopWords.Contains(kvp.Key))
            //        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
 
            return words;
        }

        private static List<string> TokenizeHashtag(string content, TokenizeConfig config)
        {
            content = content.ToLower();
            var tokens = content.Split(TweetSeperator, StringSplitOptions.RemoveEmptyEntries);
            var stophash = new HashSet<string>();
            foreach (var stopword in config.StopWords)
                if (stopword.StartsWith("#"))
                    stophash.Add(stopword);

            List<string> words = new List<string>();
            foreach (var token in tokens)
            {
                if (token.StartsWith("#"))
                {
                    if (!stophash.Contains(token))
                    {
                        words.Add(token);
                    }
                }
            }

            //Trace.WriteLine(content);
            //DiagnosticsOperations.Print(words);

            return words;
        } 
        
        private static List<string> TokenizeMention(string content, TokenizeConfig config)
        {
            //content = content.ToLower();
            if (content.StartsWith("RT @"))
            {
                content = content.Substring(content.IndexOf(":") + 1);
            }

            var tokens = content.Split(TweetSeperator, StringSplitOptions.RemoveEmptyEntries);
            List<string> words = new List<string>();
            foreach (var token in tokens)
            {
                if (token.StartsWith("@"))
                {
                    var word = token.Substring(1);
                    if (!config.StopWords.Contains(word))
                    {
                        words.Add(word);
                    }
                }
            }

            return words;
        }

        private static List<string> TokenizeRetweet(string content, TokenizeConfig config)
        {
            //content = content.ToLower();
            if (content.StartsWith("RT @"))
            {
                int index = content.IndexOf(":");
                if (index >= 0)
                {
                    content = content.Substring(0, index);
                }
            }
            else
            {
                return new List<string>();
            }


            var tokens = content.Split(TweetSeperator, StringSplitOptions.RemoveEmptyEntries);

            List<string> words = new List<string>();
            foreach (var token in tokens)
            {
                if (token.StartsWith("@"))
                {
                    var word = token.Substring(1);
                    if (!config.StopWords.Contains(word))
                    {
                        words.Add(word);
                    }
                }
            }

            return words;
        }

        #region CWB helper
        class ChineseWordBreaker
        {
            public ChineseWordBreaker(string splitResourceFile)
            {
                string chiParameters = "-tenglish 0 -numthre 11 -trepneforchilm 0 -twn 0 -dnne 1 -tcs 1 -tmb 1 ";
                chiParameters += @"-datapath " + splitResourceFile;
                engine = new Microsoft.MT.Common.Tokenization.SegRT(chiParameters);
            }

            public string[] Tokenize(string sentence, Dictionary<int, string> replaces)
            {
                string result = engine.preSegSent(sentence);

                int index = result.IndexOf("||||");
                if (index != -1)
                {
                    string first = result.Substring(0, index).Trim();
                    string second = result.Substring(index + 4).Trim();

                    Parse(second, replaces);

                    result = first;
                }

                string[] words = result.Split(splitCharSet, StringSplitOptions.RemoveEmptyEntries);
                if (words != null && words.Length > 0)
                {
                    for (int i = 0; i < words.Length; ++i)
                    {
                        string word = words[i];
                        int pos = word.LastIndexOf('/');
                        if (pos > 0)
                        {
                            words[i] = word.Substring(0, pos);
                        }
                    }
                }

                return words;
            }

            private void Parse(string second, Dictionary<int, string> replaces)
            {
                string[] items = second.Split(splitCharSet2, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in items)
                {
                    int index = item.IndexOf("|||");
                    if (index == -1)
                        continue;

                    try
                    {
                        int id = Int32.Parse(item.Substring(0, index).Trim());

                        index = item.LastIndexOf("|||");
                        if (index == -1)
                            continue;

                        index += 3;
                        string value = item.Substring(index, item.Length - index).Trim();

                        if (!replaces.ContainsKey(id))
                        {
                            replaces.Add(id, value);
                        }
                    }
                    catch //(Exception ex)
                    {
                        replaces = new Dictionary<int, string>();
                        return;
                    }
                }
            }

            public string[] Tokenize(string sentence)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                    return null;

                Dictionary<int, string> replaces = new Dictionary<int, string>();
                string[] words = Tokenize(sentence, replaces);
                if (replaces.Count > 0)
                {
                    foreach (int index in replaces.Keys)
                    {
                        if (index >= 0 && index < words.Length)
                        {
                            words[index] = replaces[index];
                        }
                    }
                }

                return words;
            }

            public static readonly char[] splitCharSet = new char[] { ' ' };
            public static readonly char[] splitCharSet2 = new char[] { '{', '}' };

            private Microsoft.MT.Common.Tokenization.SegRT engine = null;

            public static readonly char[] splitCharset = new char[] { ' ' };
        }
        #endregion

        static ChineseWordBreaker _chineseWordBreaker = null;
        private static List<string> TokenizeCWB(string content, TokenizeConfig config)
        {
            if (_chineseWordBreaker == null)
            {
                _chineseWordBreaker = new ChineseWordBreaker(@"Utils\Lib\WordBreaker\");
            }

            //Tokenize
            var words = _chineseWordBreaker.Tokenize(content);

            //Filter Stopwords
            var words2 = new List<string>();
            var stophash = StopFilter.MakeStopSet(config.StopWords);
            foreach (var word in words)
            {
                if (!stophash.Contains(word) && Regex.Match(word).Success)
                    words2.Add(word);
            }

            return words2;
        }
    }
}
