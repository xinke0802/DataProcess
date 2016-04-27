using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataProcess.Utils;

namespace DataProcess.DataTransform
{
    /// <summary>
    /// Merge lucene indices
    /// </summary>
    public class MergeLuceneIndex
    {
        public List<string> InputPaths;
        public string OutputPath;

        public MergeLuceneIndex(List<string> inputPaths, string outputPath)
        {
            InputPaths = inputPaths;
            OutputPath = outputPath;
        }

        public MergeLuceneIndex(bool isLoadFromFile = false)
        {
            if (isLoadFromFile)
            {
                var config = FileOperations.LoadConfigure("configMergeLuceneIndex.txt");
                InputPaths = config["InputPaths"];
                OutputPath = config["OutputPath"][0];
            }
        }

        public void Start()
        {
            var writer = LuceneOperations.GetIndexWriter(OutputPath);

            var totalDocCnt = 0;
            foreach (var inputPath in InputPaths)
            {
                var reader = LuceneOperations.GetIndexReader(inputPath);
                totalDocCnt += reader.NumDocs();
                reader.Close();
            }

            var progress = new ProgramProgress(totalDocCnt);
            foreach (var inputPath in InputPaths)
            {
                var reader = LuceneOperations.GetIndexReader(inputPath);
                for (int iDoc = 0; iDoc < reader.NumDocs(); iDoc++)
                {
                    writer.AddDocument(reader.Document(iDoc));
                    progress.PrintIncrementExperiment();
                }
                reader.Close();
            }

            writer.Optimize();
            writer.Close();
        }
    }
}
