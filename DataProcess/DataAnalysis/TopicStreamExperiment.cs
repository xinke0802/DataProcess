using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataProcess.Utils;
using Lava;

namespace DataProcess.DataAnalysis
{
    public enum TopicStreamDataType
    {
        nsa, kdd, ebola, obama
    };

    public class TopicStreamConfigure : AbstractConfigure
    {
        public static readonly string ConfigureFileName = "configTopicStream.txt";

        public int Index;
        public string DataType = "ebola";
        public int CopyFactor;
        public int FocusCount;
        public string DefaultTreeCut = "0_2-1_3";
        public int DefaultTreeCutRandomSeed;
        public int TreeCount;

        public TopicStreamConfigure()
            : base(ConfigureFileName)
        {
        }
    }

    public class TopicStreamResult : AbstractConfigure
    {
        public int Index;
        public string DataType = "ebola";
        public int CopyFactor;
        public int FocusCount;
        public string DefaultTreeCut = "0_2-1_3";
        public int DefaultTreeCutRandomSeed;
        public int TreeCount;

        public bool IsSucceed;
        public double TreeCutTime;
        public double DAGTime;
        public double SedimentationTime;
        public double VisTime;
        public double OverallTime;

        private static readonly string _folder = "RunTimeExperiment";

        public TopicStreamResult(string fileName):base(fileName)
        {
            this.Read();
        }

        public TopicStreamResult(TopicStreamConfigure configure)
            : base(_folder + "\\" + configure.Index + ".txt")
        {
            Index = configure.Index;
            DataType = configure.DataType;
            CopyFactor = configure.CopyFactor;
            FocusCount = configure.FocusCount;
            DefaultTreeCut = configure.DefaultTreeCut;
            DefaultTreeCutRandomSeed = configure.DefaultTreeCutRandomSeed;
            TreeCount = configure.TreeCount;

            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
            }
        }
    }

    class TopicStreamExperiment
    {
        //public void Start(int[] focusSeeds)
        //{
        //    StartEbola(focusSeeds);
        //}
        public void Start()
        {
            StartEbola();
        }

        #region Start experiment
        //public void StartEbola(int[] focusSeeds)
        public void StartEbola()
        {
            // -- node counts --
            string folder = @"D:\Project\StreamingRoseRiver\EbolaCaseStudyFinal\Trees3\";
            string exeFolder = @"D:\Project\StreamingRoseRiver\EbolaCaseStudyFinal\RoseRiver\RoseRiver\bin\x64\Release\";
            if (!Directory.Exists(folder))
            {
                folder = @"H:\Xiting\StreamingRoseRiver\ScalabilityExperiment\Data\Trees3\";
                exeFolder = @"H:\Xiting\StreamingRoseRiver\ScalabilityExperiment\RoseRiverExe\";
            }
            if (!Directory.Exists(folder))
            {
                folder = @"D:\Documents\roseriver\RoseRiver\RoseRiver\Data\Ebola\Trees3\";
                exeFolder = @"D:\Documents\roseriver\RoseRiver\RoseRiver\Data\Ebola\ScalabilityExperiment\RoseRiver\RoseRiver\bin\x64\Release\";
            }

            List<int> nodeCounts = new List<int>();
            for (int i = 0; i < 30; i++)
            {
                var fileName = folder + i + ".gv";
                var tree = BRTAnalysis.ReadTree(fileName);
                nodeCounts.Add(tree.BFS(tree.Root).Count());
            }

            // -- experiment --
            var copyFactors = new[] { 1 }; //Util.GetIntArray(1, 9, 2); //new[] {1, 2, 5, 10, 20, 50};
            var focusCounts = new[] { 1, 3, 5 }; //DataProcess.Utils.Util.GetIntArray(1, 5);
            //var focusSampleCount = 1;//50;
            var focusSeeds = Util.GetIntArray(51, 100); //Util.GetIntArray(1, 50); //new[] { 1 };//Util.GetIntArray(1, 50);
            //var minMaxTreeCount = 10;
            //var maxMaxTreeCount = 30;
            var treeCounts = Util.GetIntArray(5, 30); //new int[] { 5, 10 };//new[] {10, 20};
            int index = 0;

            ProgramProgress progress =
                new ProgramProgress(copyFactors.Length * focusCounts.Length * focusSeeds.Length * treeCounts.Length);
            var configure = new TopicStreamConfigure();
            foreach (int focusSeed in focusSeeds)
            {
                foreach (var copyFactor in copyFactors)
                {
                    configure.CopyFactor = copyFactor;
                    foreach (var focusCount in focusCounts)
                    {
                        configure.FocusCount = focusCount;
                        configure.DefaultTreeCut = GetRandomManuallyTreeCut(focusCount, treeCounts.Min(), focusSeed,
                            nodeCounts, 1);
                        configure.DefaultTreeCutRandomSeed = focusSeed;
                        foreach (var treeCount in treeCounts)
                        {
                            if (File.Exists("RunTimeExperiment\\" + index + ".txt"))
                            {
                                Console.WriteLine("Skip index = " + index);
                                index++;
                                progress.PrintSkipExperiment();
                                continue;
                            }

                            configure.TreeCount = treeCount;
                            configure.Index = index;
                            configure.Write();

                            File.Copy(TopicStreamConfigure.ConfigureFileName,
                                exeFolder + TopicStreamConfigure.ConfigureFileName, true);

                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.ErrorDialog = false;
                            startInfo.CreateNoWindow = false;
                            startInfo.UseShellExecute = false;
                            startInfo.FileName = exeFolder + @"RoseRiver.exe";
                            startInfo.WindowStyle = ProcessWindowStyle.Normal;

                            using (Process exeProcess = Process.Start(startInfo))
                            {
                                exeProcess.WaitForExit();
                            }

                            progress.PrintIncrementExperiment("\n");
                            index++;
                        }
                    }
                }
            }

            progress.PrintTotalTime();
        }

        public void StartKDD()
        {
            // -- node counts --
            string folder = @"D:\Project\StreamingRoseRiver\EbolaCaseStudyFinal\RoseRiver\Data\KddInfovisGraphicsIndex_Lucene_a=0.003_sm=1\";
            string exeFolder = @"D:\Project\StreamingRoseRiver\EbolaCaseStudyFinal\RoseRiver\RoseRiver\bin\x64\Release\";
            List<int> nodeCounts = new List<int>();
            for (int i = 0; i < 11; i++)
            {
                var fileName = folder + i + ".gv";
                var tree = BRTAnalysis.ReadTree(fileName);
                nodeCounts.Add(tree.BFS(tree.Root).Count());
            }

            // -- experiment --
            var copyFactors = new[] { 2, 1 };
            var focusCounts = DataProcess.Utils.Util.GetIntArray(1, 5);
            var focusSampleCount = 5;
            var minMaxTreeCount = 6;
            var maxMaxTreeCount = 8;
            int index = 0;

            ProgramProgress progress = new ProgramProgress(copyFactors.Length * focusCounts.Length * focusSampleCount * (maxMaxTreeCount - minMaxTreeCount + 1));
            var configure = new TopicStreamConfigure();
            configure.DataType = "kdd";
            foreach (var copyFactor in copyFactors)
            {
                configure.CopyFactor = copyFactor;
                foreach (var focusCount in focusCounts)
                {
                    for (int iFocusSample = 0; iFocusSample < focusSampleCount; iFocusSample++)
                    {
                        configure.FocusCount = focusCount;
                        configure.DefaultTreeCut = GetRandomManuallyTreeCut(focusCount, minMaxTreeCount, iFocusSample, nodeCounts, 1);
                        configure.DefaultTreeCutRandomSeed = iFocusSample;
                        for (int iMaxTreeCount = minMaxTreeCount; iMaxTreeCount <= maxMaxTreeCount; iMaxTreeCount++)
                        {
                            configure.TreeCount = iMaxTreeCount;
                            configure.Index = index;
                            configure.Write();

                            File.Copy(TopicStreamConfigure.ConfigureFileName, exeFolder + TopicStreamConfigure.ConfigureFileName, true);

                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.CreateNoWindow = true;
                            startInfo.UseShellExecute = false;
                            startInfo.FileName = exeFolder + @"RoseRiver.exe";
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                            using (Process exeProcess = Process.Start(startInfo))
                            {
                                exeProcess.WaitForExit();
                            }

                            progress.PrintIncrementExperiment("\n");
                            index++;
                        }
                    }
                }
            }

            progress.PrintTotalTime();

        }


        private string GetRandomManuallyTreeCut(int focusCount, int maxTreeCount, int seed, List<int> nodeCnts, int copyFactor)
        {
            var random = new Random(seed);
            var treeIndices = RandomUtils.GetRandomSamples(0, maxTreeCount - 1, focusCount, random);

            string treecut = "";
            foreach (var treeIndex in treeIndices)
            {
                var internalNodeCnt = nodeCnts[treeIndex] * copyFactor;
                var focusRow = RandomUtils.GetRandomSamples(0, internalNodeCnt - 1, 1, random)[0];
                treecut += treeIndex + "_" + focusRow + "-";
            }
            treecut = treecut.Substring(0, treecut.Length - 1);
            //Console.WriteLine("randome treecut: " + treecut);
            return treecut;
        }
        #endregion

        #region analyze

        public void AnalyzeResultsTreeCounts()
        {
            bool isAddVisTime = true;
            var folders = new[]
            {
                @"D:\Project\StreamingRoseRiver\ScalabilityExperiment\Results\RunTimeExperiment-TreeCountExp2\",
                //@"D:\Project\StreamingRoseRiver\ScalabilityExperiment\Results\RunTimeExperiment-supp\",
                //@"D:\Project\StreamingRoseRiver\ScalabilityExperiment\Results\RunTimeExperiment-TreeCount\",
                //@"D:\Project\StreamingRoseRiver\ScalabilityExperiment\Results\RunTimeExperiment-TreeCountStart19\",
                //@"D:\Project\StreamingRoseRiver\ScalabilityExperiment\Results\RunTimeExperiment-treeCount21End\",
            };
            //Dictionary<int, Dictionary<int, DoubleStatistics[]>> statDictionary = new Dictionary<int, Dictionary<int, DoubleStatistics[]>>();
            Dictionary<Tuple<int, int, int>, Dictionary<int, double>> statDictionary =
                new Dictionary<Tuple<int, int, int>, Dictionary<int, double>>();

            HashSet<int> copyFactors = new HashSet<int>();
            HashSet<int> focusCounts = new HashSet<int>();
            HashSet<int> treeCounts = new HashSet<int>();
            HashSet<int> focusSeeds = new HashSet<int>();
            foreach (var folder in folders)
            {
                foreach (var file in Directory.GetFiles(folder))
                {
                    var result = new TopicStreamResult(file);

                    var key = Tuple.Create(result.CopyFactor, result.FocusCount, result.TreeCount);
                    Dictionary<int, double> value;
                    if (!statDictionary.TryGetValue(key, out value))
                    {
                        value = new Dictionary<int, double>();
                        statDictionary.Add(key, value);
                    }
                    value.Add(result.DefaultTreeCutRandomSeed,
                        (result.TreeCutTime + (isAddVisTime ? result.VisTime : 0.0) + result.DAGTime +
                         result.SedimentationTime));
                    focusSeeds.Add(result.DefaultTreeCutRandomSeed);

                    copyFactors.Add(result.CopyFactor);
                    focusCounts.Add(result.FocusCount);
                    treeCounts.Add(result.TreeCount);
                }
            }

            if (copyFactors.Count != 1)
            {
                throw new ArgumentException();
            }
            var treeCountList = new List<int>(treeCounts);
            treeCountList.Sort();
            var focusCountList = new List<int>(focusCounts);
            focusCountList.Sort();
            var focusSeedList = new List<int>(focusSeeds);
            focusSeedList.Sort();
            var singleCopyFactor = copyFactors.First();
            double[,] matrices = new double[treeCountList.Count - 1, focusCountList.Count];
            int copyFactorIndex = 0;
            int minTreeCnt = treeCountList.First();
            var deltaTreeCount = treeCountList[1] - treeCountList[0];
            foreach (var treeCount in treeCountList)
            {
                if (treeCount == minTreeCnt)
                {
                    continue;
                }
                int focusCountIndex = 0;
                foreach (var focusCount in focusCountList)
                {
                    var tuple1 = Tuple.Create(singleCopyFactor, focusCount, treeCount - deltaTreeCount);
                    var tuple2 = Tuple.Create(singleCopyFactor, focusCount, treeCount);

                    DoubleStatistics stat = new DoubleStatistics();
                    var dict1 = statDictionary[tuple1];
                    var dict2 = statDictionary[tuple2];

                    //if (dict1.Count != 50 || dict2.Count != 50)
                    //{
                    //    throw new ArgumentException();
                    //}

                    foreach (var focusSeed in focusSeedList)
                    {
                        double time1, time2;
                        if (dict1.TryGetValue(focusSeed, out time1) & dict2.TryGetValue(focusSeed, out time2))
                        {
                            stat.AddNumber(Math.Max(time2 - time1, 0));
                        }
                    }
                    matrices[copyFactorIndex, focusCountIndex] = stat.GetAverage();
                    focusCountIndex++;
                }
                copyFactorIndex++;
            }

            Console.Write("[");
            for (int j = 0; j < treeCountList.Count - 1; j++)
            {
                for (int k = 0; k < focusCountList.Count; k++)
                {
                    Console.Write(matrices[j, k] + (k == focusCountList.Count - 1 ? ";" : ","));
                }
                if (j == treeCountList.Count - 1)
                {
                    Console.WriteLine("]/1000;");
                }
                else
                {
                    Console.WriteLine("...");
                }
            }
            Console.WriteLine();
        }


        public void AnalyzeResultsTopicNumber()
        {
            bool isAddVisTime = true;
            var folders = new []
            {
                @"D:\Project\StreamingRoseRiver\ScalabilityExperiment\Results\RunTimeExperiment-13579\",
                @"D:\Project\StreamingRoseRiver\ScalabilityExperiment\Results\RunTimeExperiment-111315\",
                //@"D:\Project\StreamingRoseRiver\ScalabilityExperiment\Results\RunTimeExperiment-17\"
            };
            //Dictionary<int, Dictionary<int, DoubleStatistics[]>> statDictionary = new Dictionary<int, Dictionary<int, DoubleStatistics[]>>();
            Dictionary<Tuple<int, int, int>, DoubleStatistics[]> statDictionary = new Dictionary<Tuple<int, int, int>, DoubleStatistics[]>();

            HashSet<int> copyFactors = new HashSet<int>();
            HashSet<int> focusCounts = new HashSet<int>();
            HashSet<int> treeCounts = new HashSet<int>();
            foreach (var folder in folders)
            {
                foreach (var file in Directory.GetFiles(folder))
                {
                    var result = new TopicStreamResult(file);

                    var key = Tuple.Create(result.CopyFactor, result.FocusCount, result.TreeCount);
                    DoubleStatistics[] value;
                    if (!statDictionary.TryGetValue(key, out value))
                    {
                        value = Util.GetInitializedArray(4, () => new DoubleStatistics()); //new DoubleStatistics[4];
                        statDictionary.Add(key, value);
                    }
                    value[0].AddNumber(result.TreeCutTime + (isAddVisTime ? result.VisTime : 0.0));
                    value[1].AddNumber(result.DAGTime);
                    value[2].AddNumber(result.SedimentationTime);
                    value[3].AddNumber(result.TreeCutTime + (isAddVisTime ? result.VisTime : 0.0) + result.DAGTime + result.SedimentationTime);

                    copyFactors.Add(result.CopyFactor);
                    focusCounts.Add(result.FocusCount);
                    treeCounts.Add(result.TreeCount);
                }
            }

            if (treeCounts.Count != 2)
            {
                throw new ArgumentException();
            }
            var copyFactorList = new List<int>(copyFactors);
            copyFactorList.Sort();
            var focusCountList = new List<int>(focusCounts);
            focusCountList.Sort();
            var minTreeCnt = treeCounts.Min();
            var maxTreeCnt = treeCounts.Max();
            double[][,] matrices = Util.GetInitializedArray(4,
                () => new double[copyFactorList.Count, focusCountList.Count]);
            int copyFactorIndex = 0;
            foreach (var copyFactor in copyFactorList)
            {
                int focusCountIndex = 0;
                foreach (var focusCount in focusCountList)
                {
                    var tuple1 = Tuple.Create(copyFactor, focusCount, minTreeCnt);
                    var tuple2 = Tuple.Create(copyFactor, focusCount, maxTreeCnt);
                    var stats1 = statDictionary[tuple1];
                    var stats2 = statDictionary[tuple2];

                    //var treeCutTime = (stats2[0].GetAverage() - stats1[0].GetAverage())/(maxTreeCnt - minTreeCnt);
                    //var dagTime = (stats2[1].GetAverage() - stats1[1].GetAverage()) / (maxTreeCnt - minTreeCnt);
                    //var sedimentationTime = (stats2[2].GetAverage() - stats1[2].GetAverage()) / (maxTreeCnt - minTreeCnt);
                    //var totalTime = treeCutTime + dagTime + sedimentationTime;
                    for (int i = 0; i < 4; i++)
                    {
                        matrices[i][copyFactorIndex, focusCountIndex] = (stats2[i].GetAverage() - stats1[i].GetAverage()) / (maxTreeCnt - minTreeCnt);
                    }

                    focusCountIndex++;
                }
                copyFactorIndex++;
            }

            for (int i = 0; i < 4; i++)
            {
                Console.Write("[");
                for (int j = 0; j < copyFactorList.Count; j++)
                {
                    for (int k = 0; k < focusCountList.Count; k++)
                    {
                        Console.Write(matrices[i][j,k] + (k == focusCountList.Count - 1 ? ";" : ","));
                    }
                    if (j == copyFactorList.Count - 1)
                    {
                        Console.WriteLine("]/1000;");
                    }
                    else
                    {
                        Console.WriteLine("...");
                    }
                }
                Console.WriteLine();
            }
        }

        public void AnalyzeResults0()
        {
            string folder = @"C:\Users\City-admin\Desktop\experiment\jialun\";
            Dictionary<int, DoubleStatistics> dict = new Dictionary<int, DoubleStatistics>();

            foreach (var file in Directory.GetFiles(folder))
            {
                var res = new TopicStreamResult(file);
                var copyFactor = res.CopyFactor;
                var time = res.OverallTime / 1000;
                if (!dict.ContainsKey(copyFactor))
                {
                    dict.Add(copyFactor, new DoubleStatistics());
                }
                dict[copyFactor].AddNumber(time);
            }

            foreach (var kvp in dict.OrderBy(kvp=>kvp.Key))
            {
                Console.Write(kvp.Key + ", ");
            }
            Console.WriteLine();
            foreach (var kvp in dict.OrderBy(kvp => kvp.Key))
            {
                Console.Write(kvp.Value.GetAverage() + ", ");
            }
            Console.WriteLine();
            foreach (var kvp in dict.OrderBy(kvp => kvp.Key))
            {
                Console.Write(kvp.Value.GetStd() + ", ");
            }
        }

        #endregion
    }
}
