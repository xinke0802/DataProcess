using DataProcess.Utils;
using Lucene.Net.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace DataProcess.RumorDetection
{
    public class MatchSignal
    {
        /// <summary>
        /// Match rumor patterns to find signal tweets
        /// Preparing step for method ClusterSignal.preCluster_ori()
        /// Output: signal.txt
        /// </summary>
        /// <param name="fileName">Lucene index folder path of tweets</param>
        public static void match_ori(string fileName)
        {
            var indexReader = LuceneOperations.GetIndexReader(fileName);
            FileStream fs = new FileStream("signal.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            
            for (int iDoc = 0; iDoc < indexReader.NumDocs(); iDoc++)
            {
                Document inDoc = indexReader.Document(iDoc);
                string text = inDoc.Get("Text").ToLower();
                if (Regex.IsMatch(text, @"is (this|that|it) true"))
                {
                    sw.WriteLine(iDoc);
                    continue;
                }
                if (Regex.IsMatch(text, @"(^|[^A-Za-z] )wh(a*)t([\?!]+)"))
                {
                    sw.WriteLine(iDoc);
                    continue;
                }
                if (Regex.IsMatch(text, @"(real\?|really\?|unconfirmed)"))
                {
                    sw.WriteLine(iDoc);
                    continue;
                }
                if (Regex.IsMatch(text, @"(rumor|debunk)"))
                {
                    sw.WriteLine(iDoc);
                    continue;
                }
                if (Regex.IsMatch(text, @"(that|this|it) is not true"))
                {
                    sw.WriteLine(iDoc);
                    continue;
                }
                if (iDoc % 100000 == 0)
                    Console.WriteLine(iDoc);
            }

            sw.Close();
            fs.Close();
        }
    }
}
