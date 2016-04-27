using MSRA.NLC.Common.MLT;
using MSRA.NLC.Common.NLP;
using MSRA.NLC.Common.NLP.Twitter;
using MSRA.NLC.Sentiment.Common;
using MSRA.NLC.Sentiment.Core;
using MSRA.NLC.Sentiment.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Constants = MSRA.NLC.Sentiment.Common.Constants;

namespace DataProcess.Utils
{
    public class SentimentLearner
    {
        public static void Train(string train, string test, string root)
        {
            ITokenizer tokenizer = new MSRA.NLC.Common.NLP.Twitter.TwitterTokenizer();
            IWordNormalizer wordNormalizer = null;
            wordNormalizer = new TweetWordNormalizer();
            ITextConverter textConverter = new BaseTextConverter(tokenizer, null, null, wordNormalizer);

            Train_Test(train, test, textConverter);
        }

        static string classifier = "LIBLINEAER";
        static double[] c_coll = new double[] { 3.5 };
        static void Train_Test(string train, string test, ITextConverter textConverter)
        {
            string path = Path.Combine(Constants.SPP_RootPath, @"model\Sentiment");
            new LearnerConsole(path, textConverter, classifier).Learn(train, true, true, c_coll);
            string root = Constants.SPP_RootPath;
            Test(root, test, textConverter);
        }

        static void Test(string path, string file, ITextConverter textConverter)
        {
            string root = Path.Combine(Constants.SPP_RootPath, @"model\sentiment\learning\");
            string[] dirs = Directory.GetDirectories(root);
            int max = 0;
            foreach (string dir in dirs)
            {
                string name = Path.GetFileName(dir);
                bool run = true;
                foreach (char ch in name)
                {
                    if (!char.IsDigit(ch))
                    {
                        run = false;
                        break;
                    }
                }

                if (!run)
                    continue;

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("C VALUE AT: {0}", c_coll[Int32.Parse(name)]);
                Console.ResetColor();

                SECoreImpl learnCoreImpl = CreateLearningCoreImpl(path, Int32.Parse(name));
                ISemanticTagger lexiconTagger = new LexiconSentimentTagger(textConverter.Tokenizer);
                SentimentEngine engine = new SentimentEngine(textConverter, lexiconTagger, learnCoreImpl);

                SentimentEvaluator evaluator = new SentimentEvaluator(engine);

                evaluator.Evaluate(file, true, true);

                if (++max >= c_coll.Length)
                    break;
            }
        }

        static SECoreImpl CreateLearningCoreImpl(string root, int folder = -1)
        {
            string path = Path.Combine(root, @"model\sentiment\learning\");

            if (folder >= 0)
            {
                path = Path.Combine(path, folder.ToString());
            }

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
            string polarityModelFilePath = Path.Combine(path, @"pol.model");
            BaseFeatureExtractor polFeatureExtractor = null;
            BaseDecoder pol_decoder = null;
            if (File.Exists(polFeatureSetFilePath) && File.Exists(polarityModelFilePath))
            {
                polFeatureExtractor = new PolarityFeatureExtractor(polFeatureSetFilePath);
                pol_decoder = CreateDecoder(polarityModelFilePath, classifier);
            }

            SECoreImpl coreImpl = new SELearningCoreImpl(subFeatureExtractor, polFeatureExtractor,
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

    }
}

