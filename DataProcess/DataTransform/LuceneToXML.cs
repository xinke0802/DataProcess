using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Xml.Serialization;
using DataProcess.Utils;
using Lava.Util.Collections;
using Lucene.Net.Documents;

namespace DataProcess.DataTransform
{
    [XmlRoot("root")]
    public class XmlDocCollection
    {
        [XmlArray("docs"), XmlArrayItem("doc")]
        public XmlDoc[] XmlDocs { get; set; }
    }

    [XmlRoot("doc")]
    public class XmlDoc
    {
        public XmlDoc(Document document)
        {
            DocId = document.Get(BingNewsFields.DocId);
            Country = document.Get(BingNewsFields.Country);
            DiscoveryStringTime = document.Get(BingNewsFields.DiscoveryStringTime);
            DocumentURL = document.Get(BingNewsFields.DocumentURL);
            DownloadStringTime = document.Get(BingNewsFields.DownloadStringTime);
            NewsArticleCategoryData = document.Get(BingNewsFields.NewsArticleCategoryData);
            NewsArticleDescription = document.Get(BingNewsFields.NewsArticleDescription);
            NewsArticleHeadline = document.Get(BingNewsFields.NewsArticleHeadline);
            NewsSource = document.Get(BingNewsFields.NewsSource);
        }

        public XmlDoc()
        {
            
        }

        //定义Color属性的序列化为cat节点的属性
        //[XmlAttribute("color")]
        //public string Color { get; set; }

        //要求不序列化Speed属性
        //[XmlIgnore]
        //public int Speed { get; set; }

        //设置Saying属性序列化为Xml子元素
        [XmlElement("DocId")]
        public string DocId { get; set; }
        
        [XmlElement("NewsArticleHeadline")]
        public string NewsArticleHeadline { get; set; }

        [XmlElement("NewsArticleDescription")]
        public string NewsArticleDescription { get; set; }

        [XmlElement("DiscoveryStringTime")]
        public string DiscoveryStringTime { get; set; }

        [XmlElement("DownloadStringTime")]
        public string DownloadStringTime { get; set; }

        [XmlElement("Country")]
        public string Country { get; set; }

        [XmlElement("DocumentURL")]
        public string DocumentURL { get; set; }

        [XmlElement("NewsArticleCategoryData")]
        public string NewsArticleCategoryData { get; set; }
        
        [XmlElement("NewsSource")]
        public string NewsSource { get; set; }
    }


    public class LuceneToXmlConfigure : AbstractConfigure
    {
        private static readonly string _configureFileName = "configLuceneToXML.txt";

        public LuceneToXmlConfigure() : base(_configureFileName)
        {
            
        }

        public string InputPath;
        public string OutputPath;
    }

    public class LuceneToXml
    {
        public LuceneToXmlConfigure Configure = null;

        public LuceneToXml(bool isLoadFromFile = true)
        {
            Configure = new LuceneToXmlConfigure();
            if(isLoadFromFile)
                Configure.TestReadWrite();
        }

        public void Start()
        {
            var reader = LuceneOperations.GetIndexReader(Configure.InputPath);
            var docNum = reader.NumDocs();
            ProgramProgress progress = new ProgramProgress(docNum);
            XmlDoc[] xmlDocs = new XmlDoc[docNum];
            for (int iDoc = 0; iDoc < docNum; iDoc++)
            {
                var doc = reader.Document(iDoc);
                xmlDocs[iDoc] = new XmlDoc(doc);
                progress.PrintIncrementExperiment();
            }
            progress.PrintTotalTime();

            //序列化这个对象
            XmlSerializer serializer = new XmlSerializer(typeof(XmlDocCollection));

            ////将对象序列化输出到控制台
            serializer.Serialize(new StreamWriter(Configure.OutputPath), new XmlDocCollection() {XmlDocs = xmlDocs});
        }
    }
}