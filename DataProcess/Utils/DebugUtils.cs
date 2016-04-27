using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataProcess.Utils
{
    #region Data structures
    public enum PrintType { Console, Trace, File };

    public class ProgramProgress
    {
        Stopwatch _stopwatch;
        Stopwatch _stopwatch2;
        double _printThreshold = 1;
        long _finishedExperimentCount = 0;
        long _experimentNumber;
        PrintType[] _printTypes;
        StreamWriter _sw;
        //bool _isNowPrinting = false;
        private string _programDescription;

        public ProgramProgress(long experimentNumber, PrintType printType = PrintType.Console, StreamWriter sw = null)
            :this(experimentNumber, null, printType, sw)
        {
        }


        public ProgramProgress(long experimentNumber, string programDescription, PrintType printType = PrintType.Console,
            StreamWriter sw = null)
        {
            _programDescription = programDescription;
            _experimentNumber = experimentNumber;
            _printTypes = new PrintType[] {printType};
            _sw = sw;

            if (printType == PrintType.File && sw == null)
                throw new Exception("No stream writer input!");

            _stopwatch = new Stopwatch();
            _stopwatch2 = new Stopwatch();
            _stopwatch.Start();
            _stopwatch2.Start();


            if (_printTypes.Contains(PrintType.Console))
            {
                //Trace.Write("ProgramProgress intialized");
                if (_programDescription != null)
                {
                    DebugUtils.PrintString("Start " + _programDescription + " ...", _printTypes, _sw);
                }
                DebugUtils.PrintString("ProgramProgress intialized", _printTypes, _sw);
            }
        }

        public ProgramProgress(int experimentNumber, PrintType[] printTypes, StreamWriter sw = null)
        {
            _experimentNumber = experimentNumber;
            _printTypes = printTypes;
            _sw = sw;

            if (printTypes.Contains(PrintType.File) && sw == null)
                throw new Exception("No stream writer input!");

            _stopwatch = new Stopwatch();
            _stopwatch2 = new Stopwatch();
            _stopwatch.Start();
            _stopwatch2.Start();

            if (_printTypes.Contains(PrintType.Console))
            {
                DebugUtils.PrintString("ProgramProgress intialized", _printTypes, _sw);
            }
        }

        public void PrintSkipExperiment(string addInfo = null)
        {
            _experimentNumber--;
            Print(addInfo);
        }

        public void PrintIncrementExperiment(string addInfo = null)
        {
            Interlocked.Increment(ref _finishedExperimentCount);
            Print(addInfo);
        }

        private void Print(string addInfo = null)
        {
            //if (_isNowPrinting)
            //    return;

            //_isNowPrinting = true;

            var experimentIndex = _finishedExperimentCount;
            var experimentNumber = _experimentNumber;
            PrintType[] printTypes = _printTypes;
            StreamWriter sw = _sw;

            _stopwatch.Stop();
            var part = Math.Max((double)experimentNumber / 100, 1);
            //if (experimentIndex % part == 0 || _printThreshold < _stopwatch2.Elapsed.TotalSeconds)
            if (_printThreshold < _stopwatch2.Elapsed.TotalSeconds)
            {
                ClearConsoleLine();

                DebugUtils.PrintString(string.Format(">>>Finish {0} out of {1} ({2}%)\t", experimentIndex, experimentNumber, (int)(experimentIndex / part)), printTypes, sw);
                //if (experimentIndex == 0)
                //{
                //    Trace.WriteLine("");
                //    return;
                //}

                double avgTime = (double)_stopwatch.Elapsed.Ticks / 1e7 / experimentIndex;
                double remainingTime = avgTime * (experimentNumber - experimentIndex);
                double hours, minutes, seconds;
                GetHourMinuteSecond(remainingTime, out hours, out minutes, out seconds);

                seconds = Math.Floor(seconds);
                if (hours > 0)
                    DebugUtils.PrintString(string.Format("Remaining: {0} hours, {1} minutes, {2} seconds\t{3}\r", hours, minutes, seconds, addInfo), printTypes, sw);
                else if (minutes > 0)
                    DebugUtils.PrintString(string.Format("Remaining: {0} minutes, {1} seconds\t{2}\r", minutes, seconds, addInfo), printTypes, sw);
                else
                    DebugUtils.PrintString(string.Format("Remaining: {0} seconds\t{1}\r", seconds, addInfo), printTypes, sw);

                _stopwatch2.Restart();
            }
            _finishedExperimentCount = experimentIndex;

            _stopwatch.Start();


            if (sw != null)
                sw.Flush();

            //_isNowPrinting = false;
        }

        

        public void PrintTotalTime()
        {
            double totalTime = _stopwatch.ElapsedMilliseconds / 1e3;
            double hours, minutes, seconds;
            GetHourMinuteSecond(totalTime, out hours, out minutes, out seconds);

            ClearConsoleLine();

            seconds = Math.Floor(seconds);
            var endDescription = _programDescription == null ? null : ( "(Finish " + _programDescription + ")");
            if (hours > 0)
                DebugUtils.PrintString(string.Format(">>>>>>Total: {0} hours, {1} minutes, {2} seconds {3}\n", hours, minutes, seconds, endDescription), _printTypes, _sw);
            else if (minutes > 0)
                DebugUtils.PrintString(string.Format(">>>>>>Total: {0} minutes, {1} seconds {2}\n", minutes, seconds, endDescription), _printTypes, _sw);
            else
                DebugUtils.PrintString(string.Format(">>>>>>Total: {0} seconds {1}\n", seconds, endDescription), _printTypes, _sw);
        }

        public static void Test(PrintType printType = PrintType.Trace)
        {
            ProgramProgress progress = new ProgramProgress(600, printType);

            for (int i = 0; i < 600; i++)
            {
                Thread.Sleep(100);
                progress.PrintIncrementExperiment();
            }
        }

        static void GetHourMinuteSecond(double deltatime, out double hours, out double minutes, out double seconds)
        {
            hours = minutes = 0;
            seconds = deltatime;
            if (seconds > 60)
            {
                minutes = Math.Floor(seconds / 60);
                seconds -= minutes * 60;
            }
            if (minutes > 60)
            {
                hours = Math.Floor(minutes / 60);
                minutes -= hours * 60;
            }
        }

        private void ClearConsoleLine()
        {
            if (_printTypes.Contains(PrintType.Console))
            {
                Util.ClearCurrentConsoleLine();
            }
        }
    }
    #endregion

    public enum PrintVectorType { Normal, SortedByKey, SortedByValue };

    public class DebugUtils
    {
        public static void PrintString(string str, PrintType printType = PrintType.Console, StreamWriter sw = null)
        {
            switch (printType)
            {
                case PrintType.Console:
                    Console.Write(str);
                    break;
                case PrintType.File:
                    sw.Write(str);
                    sw.Flush();
                    break;
                case PrintType.Trace:
                    Trace.Write(str);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void PrintString(string str, PrintType[] printTypes, StreamWriter sw = null)
        {
            foreach (var printType in printTypes)
                PrintString(str, printType, sw);
        }

        public static void PrintMatrix<T1, T2, T3>(Dictionary<T1, Dictionary<T2, T3>> mat)
        {
            foreach (var kvp in mat)
            {
                foreach (var kvp2 in kvp.Value)
                {
                    Trace.Write(string.Format("{0},{1},{2}\t", kvp.Key, kvp2.Key, kvp2.Value));
                }
            }
            Trace.WriteLine("");
        }

        public static void PrintMatrix(double[,] mat)
        {
            Trace.Write("    \t");
            for (int j = 0; j < mat.GetLength(1); j++)
            {
                Trace.Write(string.Format("[{0}]\t", j.ToString("#000")));
            }
            Trace.WriteLine("");

            for (int i = 0; i < mat.GetLength(0); i++)
            {
                Trace.Write(string.Format("[{0}]\t", i.ToString("#000")));
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    if (mat[i, j] == double.MaxValue)
                        Trace.Write("Max  \t");
                    else
                        Trace.Write(string.Format("{0}\t", mat[i, j].ToString("#0.00")));
                }
                Trace.WriteLine(null);
            }
        }

        public static void PrintAllProperties<T>(T obj)
        {
            Trace.WriteLine("---------All Properties of type " + obj.GetType().Name + "---------");
            List<PropertyInfo> propertyInfos = new List<PropertyInfo>(obj.GetType().GetProperties());
            propertyInfos.Sort(new Comparison<PropertyInfo>(
                (info1, info2) =>
                {
                    return info1.Name.CompareTo(info2.Name);
                }));

            foreach (var propertyInfo in propertyInfos)
            {
                Trace.WriteLine(propertyInfo.Name + ":\t" + propertyInfo.GetValue(obj));
            }
            Trace.WriteLine("---------------------------------------------");
        }

        public static void PrintWordDictionary(Dictionary<int, double> wordDict, Func<int, string> index2WordFunc, PrintVectorType type = PrintVectorType.Normal)
        {
            if(type != PrintVectorType.Normal)
            {
                wordDict = new Dictionary<int, double>(wordDict);
                if (type == PrintVectorType.SortedByKey)
                    wordDict = SortUtils.EnsureSortedByKey(wordDict);
                else if (type == PrintVectorType.SortedByValue)
                    wordDict = SortUtils.EnsureSortedByValue(wordDict);
                else
                    throw new NotImplementedException();
            }

            foreach (var kvp in wordDict)
            {
                Trace.Write(string.Format("{0},{1}\t", index2WordFunc(kvp.Key), kvp.Value));
            }
            Trace.WriteLine("");
        }
    }
}
