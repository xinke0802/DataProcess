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
    class FilterTweets
    {
        /// <summary>
        /// Filter out tweets within a certain time range
        /// Output: *.filter.txt
        /// </summary>
        /// <param name="lucenePath">Lucene index folder path of tweets</param>
        /// <param name="fileName">Input file path and prefix of output file</param>
        /// <param name="minTimeStr">Lower bound of time range</param>
        /// <param name="maxTimeStr">Upper bound of time range</param>
        public static void filterTimeRange(string lucenePath, string fileName, string minTimeStr, string maxTimeStr)
        {
            var indexReader = LuceneOperations.GetIndexReader(lucenePath);
            StreamReader sr = new StreamReader(fileName, Encoding.Default);
            FileStream fs = new FileStream(fileName + ".filter.txt", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                int iDoc = int.Parse(line);
                Document inDoc = indexReader.Document(iDoc);
                string timeStr = inDoc.Get("CreatedAt");
                DateTime time = DateTime.Parse(timeStr);
                DateTime minTime = DateTime.Parse(minTimeStr);
                DateTime maxTime = DateTime.Parse(maxTimeStr);
                if (DateTime.Compare(time, minTime) > 0 && DateTime.Compare(time, maxTime) < 0)
                    sw.WriteLine(iDoc);
            }

            sw.Close();
            fs.Close();
            sr.Close();
        }
    }
}
