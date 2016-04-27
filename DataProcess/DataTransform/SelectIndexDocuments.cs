using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataProcess.Utils;
using Lucene.Net.Index;
using Lucene.Net.Documents;

namespace DataProcess.DataTransform
{
    class SelectIndexDocumentsConfigure : AbstractConfigure
    {
        private readonly static string _configFileName = "configSelectIndexDocuments.txt";

        public SelectIndexDocumentsConfigure()
            : base(_configFileName)
        {

        }

        public string InputPath = null;
        public string OutputPath = null;

        public bool IsSampling = false;
        public double SampleRatio = 0.1;
        public int SampleSeed = 0;

        public bool IsSelectByTime = false;
        public string TimeField = BingNewsFields.DiscoveryStringTime;
        public string ParseTimeFormat = BingNewsFields.TimeFormat;
        public string StartDate = "2010-1-1";
        public string EndDate = DateTime.Now.ToString("yyyy-MM-dd");

        public bool IsSplitByTime = false;
        public int SplitDayCount = 7;

        public bool IsSelectByExactMatch = false;
        public Dictionary<string, string> FieldMatchDict = new Dictionary<string, string>() { { BingNewsFields.DiscoveryStringTime, "2010-1-1 00:00:00" } };
    }

    /// <summary>
    /// Build a lucene index by filtering/sampling the input index
    /// </summary>
    class SelectIndexDocuments
    {
        DateTime StartDateTime;
        DateTime EndDateTime;

        public SelectIndexDocumentsConfigure Configure { get; set; }
        
        Random Random;

        public SelectIndexDocuments(bool isLoadFromFile = false)
        {
            Configure = new SelectIndexDocumentsConfigure();
            if (isLoadFromFile)
                Configure.Read();
        }

        private void Initialize()
        {
            if (Configure.IsSelectByTime)
            {
                StartDateTime = StringOperations.ParseDateTimeString(Configure.StartDate, "yyyy-MM-dd");
                EndDateTime = StringOperations.ParseDateTimeString(Configure.EndDate, "yyyy-MM-dd");
            }

            if (Configure.IsSampling)
                Random = new Random(Configure.SampleSeed >= 0 ? Configure.SampleSeed : (int)(DateTime.Now.Ticks));
        }

        public void Start()
        {
            Initialize();

            var reader = LuceneOperations.GetIndexReader(Configure.InputPath);
            InitializeWriters();

            var docNum = reader.NumDocs();
            var progress = new ProgramProgress(docNum);
            for (int iDoc = 0; iDoc < docNum; iDoc++)
            {
                var doc = reader.Document(iDoc);
                bool isSkip = false;
                
                //random sample
                if (!isSkip && Configure.IsSampling)
                {
                    if (Random.NextDouble() > Configure.SampleRatio)
                        isSkip = true;
                }

                //filter by time
                if (!isSkip && Configure.IsSelectByTime)
                {
                    var dateTime = StringOperations.ParseDateTimeString(
                        doc.Get(Configure.TimeField), Configure.ParseTimeFormat);
                    if (dateTime.Subtract(StartDateTime).Ticks < 0 ||
                        dateTime.Subtract(EndDateTime).Ticks > 0)
                        isSkip = true;
                }

                //filter by exact match
                if (!isSkip && Configure.IsSelectByExactMatch)
                {
                    foreach (var kvp in Configure.FieldMatchDict)
                    {
                        if (doc.Get(kvp.Key) != kvp.Value)
                        {
                            isSkip = true;
                            break;
                        }
                    }
                }

                if (!isSkip)
                {
                    GetWriter(doc).AddDocument(doc);
                }

                progress.PrintIncrementExperiment();
            }

            CloseWriters();

            reader.Close();
        }

        Dictionary<string, IndexWriter> _writers = new Dictionary<string, IndexWriter>();
        Func<string, string> _dateTransferFunc = null;
        static readonly string _dateFormatString = "yyyy-MM-dd";
        static readonly DateTime _minDateTime = new DateTime(1, 1, 1);
        private void InitializeWriters()
        {
            if (Configure.IsSplitByTime)
            {
                _dateTransferFunc = str =>
                {
                    var dateTime = StringOperations.ParseDateTimeString(str, _dateFormatString);
                    if (Configure.SplitDayCount == 7)
                        dateTime = dateTime.Subtract(TimeSpan.FromDays((int)dateTime.DayOfWeek));
                    else
                    {
                        var days = dateTime.Subtract(_minDateTime).TotalDays;
                        var residueDays = days % Configure.SplitDayCount;
                        dateTime = dateTime.Subtract(TimeSpan.FromDays(residueDays));
                    }
                    return dateTime.ToString("yyyy-MM-dd");
                };
            }
            else
            {
                IndexWriter writer = LuceneOperations.GetIndexWriter(Configure.OutputPath);
                _writers.Add("", writer);
            }
        }

        private IndexWriter GetWriter(Document doc)
        {
            if (!Configure.IsSplitByTime)
                return _writers.Values.First();
            else
            {
                var dateTime = StringOperations.ParseDateTimeString(doc.Get(Configure.TimeField), Configure.ParseTimeFormat);
                string projDate = _dateTransferFunc(dateTime.ToString(_dateFormatString));
                IndexWriter writer;
                if(!_writers.TryGetValue(projDate, out writer))
                {
                    string path = StringOperations.EnsureFolderEnd(Configure.OutputPath) + projDate;
                    writer = LuceneOperations.GetIndexWriter(path);
                    _writers[projDate] = writer;
                }
                return writer;
            }
        }

        private void CloseWriters()
        {
            foreach(var writer in _writers.Values)
            {
                writer.Optimize();
                writer.Close();
            }
        }
    }
}
