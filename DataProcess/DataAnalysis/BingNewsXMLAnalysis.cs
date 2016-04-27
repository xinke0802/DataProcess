using DataProcess.Utils;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DataProcess.DataAnalysis
{
    class BingNewsXMLAnalysis
    {
        public static void AnalyzeLanguageDistribution(string inputPath)
        {
            Counter<string> counter = new Counter<string>();
            var filenames = Directory.GetFiles(inputPath, "*.*", System.IO.SearchOption.AllDirectories);
            ProgramProgress progress = new ProgramProgress(filenames.Length, PrintType.Console);
            foreach(var filename in filenames)
            {
                ZipFile zipfile = null;
                List<XmlDocument> xmldocs = new List<XmlDocument>();
                if (filename.EndsWith(".zip"))
                {
                    zipfile = new ZipFile(filename);
                    MemoryStream ms = new MemoryStream();
                    foreach (ZipEntry entry in zipfile.Entries)
                    {
                        entry.Extract(ms);
                        ms.Position = 0;
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(ms);
                        xmldocs.Add(xmldoc);
                        ms.Dispose();
                    }
                }
                else
                {
                    try
                    {
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(filename);
                        xmldocs.Add(xmldoc);
                    }
                    catch
                    {
                        var xmldoclist = DataProcess.DataTransform.BuildLuceneIndex.GetXMLDocList(filename);
                        xmldocs.AddRange(xmldoclist);
                    }
                }
                foreach (XmlDocument xmldoc in xmldocs)
                {
                    XmlNodeList list = xmldoc.GetElementsByTagName("NewsArticleDescription");
                    foreach (XmlNode bodynemapnode in list)
                    {
                        XmlNode newsnode = bodynemapnode.ParentNode;
                        XmlNode languagenode = newsnode.SelectSingleNode("Language");
                        counter.Add(languagenode.InnerText);
                    }
                    /// Delete temp file 
                    //File.Delete(extractpath + entry.FileName);
                }
                progress.PrintIncrementExperiment();
            }

            foreach(var kvp in counter.GetCountDictionary())
            {
                Console.WriteLine(kvp.Key + "\t" + kvp.Value);
            }
        }

    }
}
