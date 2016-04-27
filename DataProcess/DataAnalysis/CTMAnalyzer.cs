using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataProcess.Utils;
using Newtonsoft.Json;

namespace DataProcess.DataAnalysis
{
    #region Data strcutures
    public class DummyConverter<T> : JsonConverter
    {
        Action<T> _action = null;
        public DummyConverter(Action<T> action)
        {
            _action = action;
        }
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TempClass);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            serializer.Converters.Remove(this);
            T item = serializer.Deserialize<T>(reader);
            _action(item);
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class TempClass
    {
        public string Name;
        public int Age;
        public string Job;
    }
    #endregion

    class CTMAnalyzer
    {
        public void Start()
        {
            AnalyzeDocuments();
        }

        public void AnalyzeDocuments()
        {
            string fileName = @"D:\Project\TopicPanorama\data\TopicGraphs\NewCode-Ebola-Test2\Raw\news\result\lda.top.json";
            string indexPath = @"D:\DataProcess\Index\Raw_EbolaEnBingNews_Ebola_0_1_RS_R-1";

            int topDocCnt = 20;

            var indexReader = LuceneOperations.GetIndexReader(indexPath);

            //Read from json and sort
            SimpleJsonReader reader = new SimpleJsonReader(new StreamReader(File.Open(fileName, FileMode.Open)));
            HeapSortDouble[] hsd = null;
            int topicNumber = -1;
            ProgramProgress progress = new ProgramProgress(indexReader.NumDocs());
            while (reader.IsReadable)
            {
                int docID = int.Parse(reader.ReadPropertyName());
                double[] topicArray = reader.ReadDoubleArray();

                if (topicNumber < 0)
                {
                    topicNumber = topicArray.Length;
                    hsd = new HeapSortDouble[topicNumber];
                    for (int i = 0; i < topicNumber; i++)
                    {
                        hsd[i] = new HeapSortDouble(topDocCnt);
                    }
                }

                for (int i = 0; i < topicNumber; i++)
                {
                    hsd[i].Insert(docID, topicArray[i]);
                }
                progress.PrintIncrementExperiment();
            }
            progress.PrintTotalTime();

            //Statistics
            

            Console.ReadLine();
        }
    }
}
