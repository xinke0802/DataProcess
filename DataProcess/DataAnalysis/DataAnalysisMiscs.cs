using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.DataAnalysis
{
    public class DataAnalysisMiscs
    {
        public static void TestReadLargeFiles(string fileName)
        {
            //string filename = File.ReadAllText("configReadLargeFile.txt");

            StreamReader sr = new StreamReader(fileName);
            int buffersize = 1000;
            char[] buffer = new char[buffersize];
            while (sr.ReadBlock(buffer, 0, buffersize) > 0)
            {
                Console.WriteLine(new String(buffer));
                Console.ReadLine();
            }

            sr.Close();
        }
         
        public static void SplitLargeFiles()
        {
            string[] lines = File.ReadAllLines("configSplitLargeFile.txt");
            string filename = lines[0];
            int buffersize = int.Parse(lines[1]);

            StreamReader sr = new StreamReader(filename);
            char[] buffer = new char[buffersize];
            int splitIndex = 0;
            while (sr.ReadBlock(buffer, 0, buffersize) > 0)
            {
                StreamWriter sw = new StreamWriter(filename + splitIndex + ".split");
                sw.Write(buffer);
                sw.Flush();
                sw.Close();
                splitIndex++;
            }

            sr.Close();
        }
    }
}
