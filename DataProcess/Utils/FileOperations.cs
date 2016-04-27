using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using DataProcess.Utils;
using System.Reflection;
using System.Diagnostics;
using DataProcess.DataAnalysis;
using Ionic.Zip;
using Newtonsoft.Json;
using NUnrar.Archive;
using NUnrar.Common;
using SevenZip;
using System.Windows.Media;
using System.Windows;

namespace DataProcess.Utils
{
    #region Data Structures
    public interface IConfigParameter
    {
        /// <summary>
        /// Get its own parameters from List<string>configStrs
        /// </summary>
        /// <param name="configStrs"></param>
        void Get(List<string> configStrs, string seperator4 = "\t");

        /// <summary>
        /// Return configStrs according to current parameters
        /// </summary>
        List<string> GetText(string seperator4 = "\t");
    }

    public interface IConfigureFile
    {
        string GetString(string paraName);
        string GetFolderName(string paraName);
        List<string> GetRawStrings(string paraName);
        bool GetBool(string paraName);
        int GetInt(string paraName);
        int[] GetIntArray(string paraName);
        double GetDouble(string paraName);
        double[] GetDoubleArray(string paraName);

        T GetEnum<T>(string paraName);
        T[] GetEnumArray<T>(string paraName);

        bool ContainsParameter(string paraName);
    }

    public class ConfigureFile : IConfigureFile
    {
        #region Interfaces
        public string GetString(string paraName)
        {
            CheckParameterExist(paraName);
            return _configDict[paraName][0];
        }

        public string GetFolderName(string paraName)
        {
            return StringOperations.EnsureFolderEnd(GetString(paraName));
        }

        public string[] GetStringArray(string paraName)
        {
            CheckParameterExist(paraName);
            return _configDict[paraName][0].Split(new string[] { _sep4 }, StringSplitOptions.RemoveEmptyEntries);
        }

        public Dictionary<string, int> GetStringIntDictionary(string paraName)
        {
            CheckParameterExist(paraName);
            Dictionary<string, int> dict = new Dictionary<string, int>();
            foreach (var str in _configDict[paraName])
            {
                var tokens = str.Split(new string[] { _sep4 }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 2)
                    return null;
                dict.Add(tokens[0], int.Parse(tokens[1]));
            }
            return dict;
        }

        public Dictionary<string, double> GetStringDoubleDictionary(string paraName)
        {
            CheckParameterExist(paraName);
            Dictionary<string, double> dict = new Dictionary<string, double>();
            foreach (var str in _configDict[paraName])
            {
                var tokens = str.Split(new string[] { _sep4 }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 2)
                    return null;
                dict.Add(tokens[0], double.Parse(tokens[1]));
            }
            return dict;
        }

        public List<string> GetRawStrings(string paraName)
        {
            CheckParameterExist(paraName);
            return _configDict[paraName];
        }

        public List<string[]> GetStringArrayList(string paraName)
        {
            CheckParameterExist(paraName);
            List<string[]> res = new List<string[]>();
            foreach(var str in _configDict[paraName])
            {
                res.Add(str.Split(new string[] { _sep4 }, StringSplitOptions.RemoveEmptyEntries));
            }
            return res;
        }

        public bool GetBool(string paraName)
        {
            CheckParameterExist(paraName);
            return bool.Parse(_configDict[paraName][0]);
        }

        public float GetFloat(string paraName)
        {
            CheckParameterExist(paraName);
            return float.Parse(_configDict[paraName][0]);
        }

        public int GetInt(string paraName)
        {
            CheckParameterExist(paraName);
            return int.Parse(_configDict[paraName][0]);
        }

        public int[] GetIntArray(string paraName)
        {
            CheckParameterExist(paraName);
            var configStrList = _configDict[paraName];
            int temp;
            if (int.TryParse(configStrList[0], out temp))
            {
                var array = new int[configStrList.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = int.Parse(configStrList[i]);
                }
                return array;
            }
            else
            {
                var strArray = configStrList[0].Split(new string[] { _sep4 }, StringSplitOptions.RemoveEmptyEntries);
                var array = new int[strArray.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = int.Parse(strArray[i]);
                }
                return array;
            }
        }

        public double GetDouble(string paraName)
        {
            CheckParameterExist(paraName);
            return double.Parse(_configDict[paraName][0]);
        }

        public double[] GetDoubleArray(string paraName)
        {
            CheckParameterExist(paraName);
            var configStrList = _configDict[paraName];
            double temp;
            if (double.TryParse(configStrList[0], out temp))
            {
                var array = new double[configStrList.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = double.Parse(configStrList[i]);
                }
                return array;
            }
            else
            {
                var strArray = configStrList[0].Split(new string[] { _sep4 }, StringSplitOptions.RemoveEmptyEntries);
                var array = new double[strArray.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = double.Parse(strArray[i]);
                }
                return array;
            }
        }

        public T GetEnum<T>(string paraName)
        {
            CheckParameterExist(paraName);
            return (T)StringOperations.ParseEnum(typeof(T), _configDict[paraName][0]);
        }

        public T[] GetEnumArray<T>(string paraName)
        {
            CheckParameterExist(paraName);
            object temp;
            if (StringOperations.TryParseEnum(typeof(T), _configDict[paraName][0], out temp))
            {
                var cnt = _configDict[paraName].Count;
                var res = new T[cnt];
                for (int i = 0; i < cnt; i++)
                {
                    res[i] = (T)StringOperations.ParseEnum(typeof(T), _configDict[paraName][i]);
                }
                return res;
            }
            else
            {
                var strArray = _configDict[paraName][0].Split(new string[] { _sep4 }, StringSplitOptions.RemoveEmptyEntries);
                var array = new T[strArray.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = (T)StringOperations.ParseEnum(typeof(T), strArray[i]);
                }
                return array;
            }
        }

        public Dictionary<string, string> GetStringStringDictionary(string paraName)
        {
            CheckParameterExist(paraName);
            Dictionary<string, string> res = new Dictionary<string, string>();
            foreach(var str in _configDict[paraName])
            {
                var tokens = str.Split(new string[] { _sep4 }, StringSplitOptions.RemoveEmptyEntries);
                res.Add(tokens[0], tokens[1]);
            }
            return res;
        }

        public Dictionary<int, double[]> GetIntDoubleArrayDictionary(string paraName)
        {
            CheckParameterExist(paraName);
            Dictionary<int, double[]> res = new Dictionary<int, double[]>();
            foreach (var str in _configDict[paraName])
            {
                var tokens = str.Split(new string[] { _sep4 }, StringSplitOptions.RemoveEmptyEntries);
                var key = int.Parse(tokens[0]);
                var values = new double[tokens.Length - 1];
                for (int i = 1; i < tokens.Length; i++)
                {
                    values[i - 1] = double.Parse(tokens[i]);
                }
                res.Add(key, values);
            }
            return res;
        }

        public bool ContainsParameter(string paraName)
        {
            return _configDict.ContainsKey(paraName);
        }
        #endregion

        Dictionary<string, List<string>> _configDict;
        string _sep4 = null;

        public ConfigureFile(Dictionary<string, List<string>> configDict, char seperator4)
        {
            _sep4 = "";
            _sep4 += seperator4;
            _configDict = configDict;
        }

        public ConfigureFile(Dictionary<string, List<string>> configDict, string seperator4 = null)
        {
            _sep4 = seperator4;
            _configDict = configDict;
        }

        public ConfigureFile(string fileName)
        {
            _configDict = FileOperations.LoadConfigure(fileName);
        }

        private void CheckParameterExist(string paraName)
        {
            if (!_configDict.ContainsKey(paraName))
                throw new Exception("Parameter does not exist!! ParaName: " + paraName);
        }

        public T Get<T>(string paraName)
        {
            return (T)Convert.ChangeType(Get(paraName, typeof(T)), typeof(T));
        }

        public object Get(string paraName, Type type)
        {
            if (type.GetInterfaces().Contains(typeof(IConfigParameter)))
            {
                CheckParameterExist(paraName);
                IConfigParameter obj = (IConfigParameter)Activator.CreateInstance(type);
                obj.Get(_configDict[paraName], _sep4);
                return obj;
            }
            else if (type == typeof(string))
            {
                return GetString(paraName);
            }
            else if (type == typeof(List<string>))
            {
                return GetRawStrings(paraName);
            }
            else if (type == typeof(string[]))
            {
                return GetStringArray(paraName);
            }
            else if (type == typeof(Dictionary<string, int>))
            {
                return GetStringIntDictionary(paraName);
            }
            else if (type == typeof(List<string[]>))
            {
                return GetStringArrayList(paraName);
            }
            else if (type == typeof(bool))
            {
                return GetBool(paraName);
            }
            else if (type == typeof (float))
            {
                return GetFloat(paraName);
            }
            else if (type == typeof(int))
            {
                return GetInt(paraName);
            }
            else if (type == typeof(int[]))
            {
                return GetIntArray(paraName);
            }
            else if (type == typeof(double))
            {
                return GetDouble(paraName);
            }
            else if (type == typeof(double[]))
            {
                return GetDoubleArray(paraName);
            }
            else if(type == typeof(Dictionary<string,string>))
            {
                return GetStringStringDictionary(paraName);
            }
            else if (type == typeof (Dictionary<int, double[]>))
            {
                return GetIntDoubleArrayDictionary(paraName);
            }
            else if (type.IsEnum)
            {
                MethodInfo methodInfo = GetType().GetMethod("GetEnum");
                methodInfo = methodInfo.MakeGenericMethod(new Type[] { type });
                return methodInfo.Invoke(this, new object[] { paraName });
                //return StringOperations.ParseEnum(type, _configDict[paraName][0]);
            }
            else if (type.IsArray && type.GetElementType().IsEnum)
            {
                MethodInfo methodInfo = GetType().GetMethod("GetEnumArray", new []{type});
                methodInfo = methodInfo.MakeGenericMethod(new Type[] { type });
                return methodInfo.Invoke(this, new object[] { paraName });
                //return StringOperations.ParseEnum(type, _configDict[paraName][0]);
            }
            else
                throw new NotImplementedException();
        }

        public static List<string> GetText(object obj, Type type, string seperator4 = "\t")
        {
            if (obj == null)
                return null;

            List<string> strs = new List<string>();
            if(type.GetInterfaces().Contains(typeof(IConfigParameter)))
            {
                IConfigParameter configPara = (IConfigParameter)obj;
                return configPara.GetText(seperator4);
            }
            else if (type == typeof(string))
            {
                strs.Add(obj.ToString());
            }
            else if (type == typeof(List<string>))
            {
                strs = (List<string>)obj;
            }
            else if (type == typeof(string[]))
            {
                strs.Add(StringOperations.GetMergedString((string[])obj, seperator4));
            }
            else if (type == typeof(Dictionary<string, int>))
            {
                Dictionary<string, int> dict = (Dictionary<string, int>)obj;
                foreach (var kvp in dict)
                {
                    strs.Add(kvp.Key + seperator4 + kvp.Value);
                }
            }
            else if (type == typeof(List<string[]>))
            {
                List<string[]> list = (List<string[]>)obj;
                foreach (var stringArray in list)
                {
                    strs.Add(StringOperations.GetMergedString(stringArray, seperator4));
                }
            }
            else if (type == typeof(bool))
            {
                strs.Add(obj.ToString());
            }
            else if (type == typeof (float))
            {
                strs.Add(obj.ToString());
            }
            else if (type == typeof(int))
            {
                strs.Add(obj.ToString());
            }
            else if (type == typeof(int[]))
            {
                strs.Add(StringOperations.GetMergedString((int[])obj, seperator4));
            }
            else if (type == typeof(double))
            {
                strs.Add(obj.ToString());
            }
            else if (type == typeof(double[]))
            {
                strs.Add(StringOperations.GetMergedString((double[])obj, seperator4));
            }
            else if (type == typeof(Dictionary<string, string>))
            {
                foreach (var kvp in (Dictionary<string, string>)obj)
                {
                    strs.Add(kvp.Key + seperator4 + kvp.Value);
                }
            }
            else if (type == typeof (Dictionary<int, double[]>))
            {
                foreach (var kvp in (Dictionary<int, double[]>)obj)
                {
                    string str = kvp.Key.ToString();
                    foreach (var val in kvp.Value)
                    {
                        str += seperator4 + val;
                    }
                    strs.Add(str);
                }
            }
            else if (type.IsEnum)
            {
                strs.Add(obj.ToString());
            }
            else if (type.IsArray && type.GetElementType().IsEnum)
            {
                Array array = (Array)obj;
                string str = "";
                for (int i = 0; i < array.Length; i++)
                {
                    str += array.GetValue(i).ToString() + seperator4;
                }
                strs.Add(str);
            }
            else
                throw new NotImplementedException();
            return strs;
        }
    }

    public class BinaryFileReadWriteHelper
    {
        public static List<T> ReadListFromFile<T>(BinaryReader br, Func<BinaryReader,T> func)
        {
            int cnt = br.ReadInt32();
            if (cnt == -1)
            {
                return null;
            }

            List<T> list = new List<T>();
            for (int i = 0; i < cnt; i++)
            {
                list.Add(func(br));
            }
            return list;
        }

        public static void WriteListToFile<T>(List<T> list, BinaryWriter bw, Action<BinaryWriter, T> action)
        {
            if (list == null)
            {
                bw.Write(-1);
                return;
            }
            bw.Write(list.Count);
            foreach (var ele in list)
            {
                action(bw, ele);
            }
        }

        public static Point ReadPointFromFile(BinaryReader br)
        {
            return new Point(br.ReadDouble(), br.ReadDouble());
        }

        public static void WritePointToFile(BinaryWriter bw, Point pt)
        {
            bw.Write(pt.X);
            bw.Write(pt.Y);
        }

        public static Color ReadColorFromFile(BinaryReader br)
        {
            return Color.FromArgb(br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte());
        }

        public static void WriteColorToFile(Color color, BinaryWriter bw)
        {
            bw.Write(color.A);
            bw.Write(color.R);
            bw.Write(color.G);
            bw.Write(color.B);
        }

        public static T ReadBinaryStorableClassFromFile<T>(BinaryReader br) where T : IBinaryStorable, new ()
        {
            var data = new T();//Activator.CreateInstance<T>();
            data.ReadFromBinaryFile(br);

            return data;
        }

        public static T ReadBinaryStorableClassFromFile<T>(string fileName) where T : IBinaryStorable
        {
            var br = new BinaryReader(File.OpenRead(fileName));

            var data = Activator.CreateInstance<T>();
            data.ReadFromBinaryFile(br);

            br.Close();

            return data;
        }

        public static void WriteDictionaryToFile<T1, T2>(Dictionary<T1, T2> dict, BinaryWriter bw,
       Action<T1, BinaryWriter> T1WriterFunc, Action<T2, BinaryWriter> T2WriterFunc)
        {
            if (dict == null)
            {
                bw.Write(0);
            }
            else
            {
                bw.Write(dict.Count);
                foreach (var kvp in dict)
                {
                    T1WriterFunc(kvp.Key, bw);
                    T2WriterFunc(kvp.Value, bw);
                }
            }

            bw.Flush();
        }

        public static Dictionary<T1, T2> ReadDictionaryFromFile<T1, T2>(BinaryReader br,
            Func<BinaryReader, T1> T1ReadFunc, Func<BinaryReader, T2> T2ReadFunc)
        {
            var dict = new Dictionary<T1, T2>();

            var cnt = br.ReadInt32();

            for (int i = 0; i < cnt; i++)
            {
                dict.Add(T1ReadFunc(br), T2ReadFunc(br));
            }

            return dict;
        }

        public static void WriteDictionaryToFile<T1, T2>(Dictionary<T1, T2> dict, string fileName,
            Action<T1, BinaryWriter> T1WriterFunc, Action<T2, BinaryWriter> T2WriterFunc)
        {
            var bw = new BinaryWriter(File.OpenWrite(fileName));

            if (dict == null)
            {
                bw.Write(0);
            }
            else
            {
                bw.Write(dict.Count);
                foreach (var kvp in dict)
                {
                    T1WriterFunc(kvp.Key, bw);
                    T2WriterFunc(kvp.Value, bw);
                }
            }

            bw.Flush();
            bw.Close();
        }

        public static Dictionary<T1, T2> ReadDictionaryFromFile<T1, T2>(string fileName,
            Func<BinaryReader, T1> T1ReadFunc, Func<BinaryReader, T2> T2ReadFunc)
        {
            var dict = new Dictionary<T1, T2>();

            var br = new BinaryReader(File.OpenRead(fileName));
            var cnt = br.ReadInt32();

            for (int i = 0; i < cnt; i++)
            {
                dict.Add(T1ReadFunc(br), T2ReadFunc(br));
            }

            br.Close();
            return dict;
        }

        public static Dictionary<T1, T2>[,] ReadBinaryStorableDictionaryMatrixFromFile<T1, T2>(BinaryReader br)
            where T1 : IBinaryStorable
            where T2 : IBinaryStorable
        {
            int cnt1 = br.ReadInt32();
            int cnt2 = br.ReadInt32();
            if (cnt1 >= 0 && cnt2 >= 0)
            {
                var dictMat = new Dictionary<T1, T2>[cnt1, cnt2];
                for (int i = 0; i < cnt1; i++)
                {
                    for (int j = 0; j < cnt2; j++)
                    {
                        dictMat[i, j] = ReadBinaryStorableDictionaryFromFile<T1, T2>(br);
                    }
                }

                return dictMat;
            }
            else
                return null;
        }

        public static void WriteBinaryStorableDictionaryMatrixToFile<T1, T2>(Dictionary<T1,T2>[,] dictMat, BinaryWriter bw) where T1:IBinaryStorable where T2:IBinaryStorable
        {
            if(dictMat == null)
            {
                int cnt = -1;
                bw.Write(cnt);
                bw.Write(cnt);
            }
            else
            {
                var cnt1 = dictMat.GetLength(0);
                var cnt2 = dictMat.GetLength(1);
                bw.Write(cnt1);
                bw.Write(cnt2);

                for (int i = 0; i < cnt1; i++)
                {
                    for (int j = 0; j < cnt2; j++)
                    {
                        WriteBinaryStorableDictionaryToFile(dictMat[i, j], bw);
                    }
                }
            }
        }
        

        public static Dictionary<T1,T2> ReadBinaryStorableDictionaryFromFile<T1, T2>(BinaryReader br)
            where T1 : IBinaryStorable
            where T2 : IBinaryStorable
        {
            var count = br.ReadInt32();

            if (count >= 0)
            {
                Dictionary<T1, T2> dict = new Dictionary<T1, T2>();
                for (int i = 0; i < count; i++)
                {
                    T1 key = Activator.CreateInstance<T1>();
                    T2 value = Activator.CreateInstance<T2>();

                    key.ReadFromBinaryFile(br);
                    value.ReadFromBinaryFile(br);

                    dict.Add(key, value);
                }
                return dict;
            }
            else
                return null;
        }
        
        public static void WriteBinaryStorableDictionaryToFile<T1, T2>(Dictionary<T1, T2> dict, BinaryWriter bw)  where T1 :IBinaryStorable where T2:IBinaryStorable
        {
            if (dict == null)
            {
                int count = -1;
                bw.Write(count);
            }
            else
            {
                bw.Write(dict.Count);
                foreach (var kvp in dict)
                {
                    kvp.Key.WriteToBinaryFile(bw);
                    kvp.Value.WriteToBinaryFile(bw);
                }
            }
            bw.Flush();
        }

        public static int[] ReadIntArrayFromFile(BinaryReader br)
        {
            var cnt = br.ReadInt32();
            if (cnt == -1)
                return null;
            else
            {
                var res = new int[cnt];
                for (int i = 0; i < cnt; i++)
                {
                    res[i] = br.ReadInt32();
                }
                return res;
            }
        }

        public static void WriteIntArrayToFile(int[] data, BinaryWriter bw)
        {
            if (data == null)
            {
                bw.Write((int)-1);
            }
            else
            {
                bw.Write(data.Length);
                foreach (var val in data)
                    bw.Write(val);

                bw.Flush();
            }
        }

        public static double[] ReadDoubleArrayFromFile(BinaryReader br)
        {
            var cnt = br.ReadInt32();
            if (cnt == -1)
                return null;
            else
            {
                var res = new double[cnt];
                for (int i = 0; i < cnt; i++)
                {
                    res[i] = br.ReadDouble();
                }
                return res;
            }
        }

        public static void WriteDoubleArrayToFile(double[] data, BinaryWriter bw)
        {
            if (data != null)
            {
                bw.Write(data.Length);
                foreach (var val in data)
                    bw.Write(val);

                bw.Flush();
            }
            else
            {
                bw.Write(-1);
            }
        }

        public static double[,] ReadDoubleMatrixFromFile(BinaryReader br)
        {
            var cnt1 = br.ReadInt32();
            var cnt2 = br.ReadInt32();
            var res = new double[cnt1, cnt2];
            for (int i = 0; i < cnt1; i++)
                for (int j = 0; j < cnt2; j++)
                {
                    res[i, j] = br.ReadDouble();
                }
            return res;
        }

        public static void WriteDoubleMatrixToFile(double[,] data, BinaryWriter bw)
        {
            bw.Write(data.GetLength(0));
            bw.Write(data.GetLength(1));
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    bw.Write(data[i, j]);
                }
            }

            bw.Flush();
        }

        public static List<int> ReadIntListFromFile(BinaryReader br)
        {
            var cnt = br.ReadInt32();
            List<int> res = new List<int>();
            for (int i = 0; i < cnt; i++)
            {
                res.Add(br.ReadInt32());
            }
            return res;
        }

        public static void WriteIntListToFile(List<int> data, BinaryWriter bw)
        {
            bw.Write(data.Count);
            foreach (var val in data)
                bw.Write(val);

            bw.Flush();
        }

        public static Dictionary<int, double> ReadIntDoubleDictionaryFromFile(BinaryReader br)
        {
            var res = new Dictionary<int, double>();
            var cnt = br.ReadInt32();
            for (int i = 0; i < cnt; i++)
            {
                res.Add(br.ReadInt32(), br.ReadDouble());
            }
            return res;
        }



        public static void WriteIntDoubleDictionaryToFile(Dictionary<int, double> data, BinaryWriter bw)
        {
            if (data == null)
            {
                bw.Write(0);
            }
            else
            {
                bw.Write(data.Count);
                foreach (var kvp in data)
                {
                    bw.Write(kvp.Key);
                    bw.Write(kvp.Value);
                }
            }
            bw.Flush();
        }

        public static Dictionary<int, T> ReadIntBinaryStorableDictionaryFromFile<T>(BinaryReader br) where T : IBinaryStorable, new()
        {
            var res = new Dictionary<int, T>();
            var cnt = br.ReadInt32();
            for (int i = 0; i < cnt; i++)
            {
                var key = br.ReadInt32();
                var value = new T();
                value.ReadFromBinaryFile(br);
                res.Add(key, value);
            }
            return res;
        }


        public static void WriteIntBinaryStorableDictionaryToFile<T>(Dictionary<int, T> data, BinaryWriter bw) where T : IBinaryStorable, new()
        {
            if (data == null)
            {
                bw.Write(0);
            }
            else
            {
                bw.Write(data.Count);
                foreach (var kvp in data)
                {
                    bw.Write(kvp.Key);
                    kvp.Value.WriteToBinaryFile(bw);
                }
            }
            bw.Flush();
        }
        public static Dictionary<int, double[]> ReadIntDoubleArrayDictionaryFromFile(BinaryReader br)
        {
            var res = new Dictionary<int, double[]>();
            var cnt = br.ReadInt32();
            if (cnt == 0)
                return res;
            for (int i = 0; i < cnt; i++)
            {
                res.Add(br.ReadInt32(), ReadDoubleArrayFromFile(br));
            }
            return res;
        }

        public static void WriteIntDoubleArrayDictionaryToFile(Dictionary<int, double[]> data, BinaryWriter bw)
        {
            if (data == null)
            {
                bw.Write(0);
            }
            else
            {
                bw.Write(data.Count);
                foreach (var kvp in data)
                {
                    bw.Write(kvp.Key);
                    WriteDoubleArrayToFile(kvp.Value, bw);
                }
            }
            bw.Flush();
        }

        public static Dictionary<int, int> ReadIntIntDictionaryFromFile(BinaryReader br)
        {
            var res = new Dictionary<int, int>();
            var cnt = br.ReadInt32();
            for (int i = 0; i < cnt; i++)
            {
                res.Add(br.ReadInt32(), br.ReadInt32());
            }
            return res;
        }

        public static void WriteIntIntDictionaryToFile(Dictionary<int, int> data, BinaryWriter bw)
        {
            bw.Write(data.Count);
            foreach (var kvp in data)
            {
                bw.Write(kvp.Key);
                bw.Write(kvp.Value);
            }
            bw.Flush();
        }

        public static DateTime ReadDateTimeFromFile(BinaryReader br)
        {
            long dateLong = br.ReadInt64();
            return DateTime.FromBinary(dateLong);
        }

        public static void WriteDateTimeToFile(DateTime dateTime, BinaryWriter bw)
        {
            bw.Write(dateTime.ToBinary());
            bw.Flush();
        }
    }

    public interface IBinaryStorable
    {
        void ReadFromBinaryFile(BinaryReader br);
        void WriteToBinaryFile(BinaryWriter bw);
    }

    /// <summary>
    /// Auto read and write all fields
    /// List<string>: string sep3. string sep3. string
    /// string[]: string sep4. string sep4. string sep4.
    /// </summary>
    public abstract class AbstractConfigure
    {
        string _configureFileName { get; set; }
        string _sep1 { get; set; }
        string _sep2 { get; set; }
        string _sep3 { get; set; }
        string _sep4 { get; set; }


        /// <summary>
        /// File format: ParameterName1 sep2. Val11 sep4. Val12 sep4. sep3. Val21 sep3. sep1 ParameterName2 sep2. Val1 sep3. 
        /// </summary>
        public AbstractConfigure(string configureFileName, 
            string sep1 = "\n\n", string sep2 = "\n", string sep3 = "\n", string sep4 = "\t")
        {
            _configureFileName = configureFileName;
            _sep1 = sep1;
            _sep2 = sep2;
            _sep3 = sep3;
            _sep4 = sep4;
        }

        public void SetConfigureFileName(string configFileName)
        {
            _configureFileName = configFileName;
        }

        public void Read(List<string> fieldNames = null)
        {
            if (fieldNames == null)
            {
                fieldNames = new List<string>();
                foreach (var field in this.GetType().GetFields())
                {
                    if (!field.IsStatic && field.IsPublic)
                        fieldNames.Add(field.Name);
                }
            }

            var configureFile = StringOperations.ParseConfigureString(File.ReadAllText(_configureFileName), _sep1, _sep2, _sep3);
            foreach (var fieldName in fieldNames)
            {
                if (configureFile.ContainsParameter(fieldName))
                {
                    var field = this.GetType().GetField(fieldName);
                    field.SetValue(this, configureFile.Get(fieldName, field.FieldType));
                }
            }
        }

        public void Write(List<string> fieldNames = null)
        {
            if (fieldNames == null)
            {
                fieldNames = new List<string>();
                foreach (var field in this.GetType().GetFields())
                {
                    if (!field.IsStatic && field.IsPublic)
                        fieldNames.Add(field.Name);
                }
            }

            StreamWriter sw = new StreamWriter(_configureFileName);
            foreach (var fieldName in fieldNames)
            {
                var field = this.GetType().GetField(fieldName);
                List<string> configStrs = ConfigureFile.GetText(field.GetValue(this), field.FieldType, _sep4);

                sw.Write(fieldName);
                if (_sep2 == "\n")
                    sw.Write(" //" + field.FieldType + _sep2);
                else
                    sw.Write(_sep2);
                if (configStrs == null)
                    sw.Write("null");
                else
                {
                    int index = 0;
                    foreach (var str in configStrs)
                    {
                        if (index < configStrs.Count - 1)
                            sw.Write(str + _sep3);
                        else
                            sw.Write(str);
                        index++;
                    }
                }
                sw.Write(_sep1);
            }
            sw.Flush();
            sw.Close();
        }

        public void TestReadWrite()
        {
            Trace.WriteLine("Test configure read and write ... ");
            Write();
            Read();
            Trace.WriteLine("Pass test!!");
        }
    }

    public class MyJsonConverter<T> : JsonConverter
    {
        Action<T> _action = null;
        public MyJsonConverter(Action<T> action)
        {
            _action = action;
        }
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
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

    public class SimpleJsonReader
    {
        private JsonTextReader _reader;
        public bool IsReadable { get; protected set; }
        public SimpleJsonReader(string fileName)
        {
            _reader = new JsonTextReader(new StreamReader(File.OpenRead(fileName)));
            IsReadable = true;
        }


        public SimpleJsonReader(TextReader textReader)
        {
            _reader = new JsonTextReader(textReader);
            IsReadable = true;
        }

        public double[] ReadDoubleArray()
        {
            List<double> doubles = new List<double>();
            while (IsReadable = _reader.Read())
            {
                if (_reader.TokenType == JsonToken.Integer || _reader.TokenType == JsonToken.Float)
                {
                    doubles.Add(Convert.ToDouble(_reader.Value));
                }
                else
                {
                    if (_reader.TokenType != JsonToken.StartArray && _reader.TokenType != JsonToken.EndArray)
                        throw new ArgumentException();
                    if (_reader.TokenType == JsonToken.EndArray)
                        break;
                }
            }
            var array = doubles.ToArray();
            doubles = null;
            IsReadable = array.Length != 0;
            return array;
        }

        public double[,] ReadDoubleMatrix()
        {
            throw new NotImplementedException();
        }

        public string ReadPropertyName()
        {
            while (IsReadable = _reader.Read())
            {
                if (_reader.TokenType == JsonToken.PropertyName)
                {
                    return Convert.ToString(_reader.Value);
                }
            }
            IsReadable = false;
            return null;
        }

        public double ReadDoubleEntry()
        {
            while (IsReadable = _reader.Read())
            {
                if (_reader.TokenType == JsonToken.Integer || _reader.TokenType == JsonToken.Float)
                {
                    return Convert.ToDouble(_reader.Value);
                }
                else
                {
                    if (_reader.TokenType != JsonToken.StartArray && _reader.TokenType != JsonToken.EndArray)
                        throw new ArgumentException();
                }
            }
            IsReadable = false;
            return double.NaN;
        }
    }

    public class TempOutput
    {
        private string _fileName;
        private string _tempFileName;
        private StreamReader _tempSr;
        private StreamWriter _tempSw;
        public TempOutput(string fileName)
        {
            _fileName = fileName;
            _tempFileName = StringOperations.GetFileNameWithoutExtension(fileName) + ".tmp";
            _tempSw = new StreamWriter(_tempFileName);
            _tempSr = new StreamReader(_fileName);
        }

        public StreamReader GetStreamReader()
        {
            return _tempSr;
        }

        public StreamWriter GetStreamWriter()
        {
            return _tempSw;
        }

        public void Finish()
        {
            _tempSr.Close();
            _tempSw.Close();
            File.Copy(_tempFileName, _fileName, true);
        }
    }
    #endregion

    public class FileOperations
    {
        public static bool DoNotReachEnd(BinaryReader br)
        {
            return br.BaseStream.Position != br.BaseStream.Length;
        }

        public static double[][] ReadDoubleDoubleArrayFromFile(string fileName)
        {
            int dim2;
            var vecList = ReadDoubleArrayListFromFile(fileName, out dim2);
            return vecList.ToArray();
        }


        public static double[,] ReadDoubleMatrixFromFile(string fileName)
        {
            int dim2;
            var vecList = ReadDoubleArrayListFromFile(fileName, out dim2);
            int dim1 = vecList.Count;
            double[,] mat = new double[dim1, dim2];
            for (int i = 0; i < dim1; i++)
            {
                for (int j = 0; j < dim2; j++)
                {
                    mat[i, j] = vecList[i][j];
                }
            }
            return mat;
        }

        public static void WriteDoubleMatrixFromFile(double[,] mat, string fileName)
        {
            var sep = " ";
            var sw = new StreamWriter(fileName);
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    sw.Write(mat[i, j] + sep);
                }
                sw.WriteLine();
            }
            sw.Flush();
            sw.Close();
        }

        public static double[] ReadDoubleArray(string fileName)
        {
            int dim2;
            var res = ReadDoubleArrayListFromFile(fileName, out dim2);
            if (res.Count == 0)
            {
                return new double[0];
            }
            if (res.Count != 1)
            {
                if (dim2 == 1)
                {
                    double[] res2 = new double[res.Count];
                    for (int i = 0; i < res.Count; i++)
                    {
                        res2[i] = res[i][0];
                    }
                    return res2;
                }
                throw new ArgumentException();
            }

            return res.First();
        }

        public static void WriteDoubleArray(double[] array, string fileName)
        {
            var sep = " ";

            var sw = new StreamWriter(fileName);

            foreach (var value in array)
            {
                sw.Write(value + sep);
            }

            sw.Flush();
            sw.Close();
        }

        private static List<double[]> ReadDoubleArrayListFromFile(string fileName, out int dim2)
        {
            var sep = new[] { ' ' };
            List<double[]> vecList = new List<double[]>();
            dim2 = -1;
            using (var sr = new StreamReader(fileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var tokens = line.Split(sep);
                    int dim = String.IsNullOrEmpty(tokens[tokens.Length - 1]) ? (tokens.Length - 1) : tokens.Length;
                    if (dim2 == -1)
                    {
                        dim2 = dim;
                    }
                    else
                    {
                        if (dim2 != dim)
                        {
                            throw new ArgumentException();
                        }
                    }
                    double[] vec = new double[dim2];
                    for (int i = 0; i < dim2; i++)
                    {
                        vec[i] = double.Parse(tokens[i]);
                    }
                    vecList.Add(vec);
                }
            }
            if (dim2 == -1)
            {
                dim2 = 0;
            }
            return vecList;
        }

        public static void CopyDirectory(string srcDir, string tarDir)
        {
            Directory.CreateDirectory(tarDir);
            tarDir = StringOperations.EnsureFolderEnd(tarDir);
            foreach (var file in Directory.GetFiles(srcDir))
            {
                File.Copy(file, tarDir + StringOperations.GetFileName(file), true);
            }
        }

        public static bool IsEndOfFile(BinaryReader br)
        {
            return br.BaseStream.Position == br.BaseStream.Length;
        }

        public static int GetLineCount(string fileName)
        {
            int lineCount = 0;
            string line;
            using (StreamReader sr = new StreamReader(fileName))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    lineCount++;
                }

                sr.Close();
            }
            return lineCount;
        }

        public static string GetLastLine(string fileName)
        {
            string line;
            string prevLine = null;
            using (StreamReader sr = new StreamReader(fileName))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    prevLine = line;
                }

                sr.Close();
            }
            return prevLine;
        }

        public static bool TryUnzip(string inputFileName, string outputPath)
        {
            SevenZipExtractor.SetLibraryPath(@"C:\Program Files\7-Zip\7z.dll");

            if (inputFileName.EndsWith(".zip"))
            {
                ZipFile zipfile = new ZipFile(inputFileName);
                foreach (ZipEntry entry in zipfile.Entries)
                {
                    entry.Extract(outputPath, ExtractExistingFileAction.OverwriteSilently);
                }
            }
            else if (inputFileName.EndsWith(".7z"))
            {
                SevenZipExtractor szip = new SevenZipExtractor(inputFileName);
                szip.BeginExtractFiles(outputPath, szip.ArchiveFileNames.ToArray());
            }
            else if (inputFileName.EndsWith(".rar"))
            {
                RarArchive archive = RarArchive.Open(inputFileName);
                foreach (RarArchiveEntry entry in archive.Entries)
                {
                    var fileName = outputPath + "\\" + entry.FilePath;
                    EnsureFileFolderExist(fileName);
                    entry.WriteToFile(fileName, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public static void GenerateBackupFile(string fileName)
        {
            var bakFileName = StringOperations.GetFileNameInsertedBeforeFileType(fileName, "-bak" + DateTime.Now.ToString("yyMMdd_hhmmss"));
            File.Copy(fileName, bakFileName, true);
            Trace.WriteLine("File already exist, bak to " + bakFileName);
        }

        public static void ReadJsonFileList<T>(string fileName, Action<T> callbackAction) where T : class
        {
            JsonSerializer ser = new JsonSerializer();
            ser.Converters.Add(new MyJsonConverter<T>(callbackAction));

            ser.Deserialize(new JsonTextReader(new StreamReader(File.OpenRead(fileName))), typeof(List<T>));
            //ser.Deserialize(new JsonTextReader(new StringReader(text)), typeof(List<TempClass>));
        }


        public static T ReadJsonFile<T>(string fileName) where T : class
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(fileName));
        }

        /// <summary>
        /// Consider using http://json2csharp.com/ to transfer json file to c# class
        /// </summary>
        public static void ReadJsonFile<T>(string fileName, Action<T> callbackAction) where T : class 
        {
            var obj = JsonConvert.DeserializeObject<T>(File.ReadAllText(fileName));
            callbackAction(obj);
            //JsonSerializer ser = new JsonSerializer();
            //ser.Converters.Add(new MyJsonConverter<T>(callbackAction));

            //ser.Deserialize(new JsonTextReader(new StreamReader(File.OpenRead(fileName))), typeof(T));
            ////ser.Deserialize(new JsonTextReader(new StringReader(text)), typeof(List<TempClass>));
        }

        public static void CopyFolder(string sourceFolder, string targetFolder)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourceFolder, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourceFolder, targetFolder));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourceFolder, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourceFolder, targetFolder), true);
        }

        public static double GetFolderSize(string folder)
        {
            double folderSize = 0.0f;
            try
            {
                //Checks if the path is valid or not
                if (!Directory.Exists(folder))
                    return folderSize;
                else
                {
                    try
                    {
                        foreach (string file in Directory.GetFiles(folder))
                        {
                            if (File.Exists(file))
                            {
                                FileInfo finfo = new FileInfo(file);
                                folderSize += finfo.Length;
                            }
                        }

                        foreach (string dir in Directory.GetDirectories(folder))
                            folderSize += GetFolderSize(dir);
                    }
                    catch (NotSupportedException e)
                    {
                        Console.WriteLine("Unable to calculate folder size: {0}", e.Message);
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Unable to calculate folder size: {0}", e.Message);
            }
            return folderSize;
        }


        public static void WriteBinaryStorableClassToFile<T>(T data, string fileName) where T : IBinaryStorable
        {
            var bw = new BinaryWriter(File.OpenWrite(fileName));

            data.WriteToBinaryFile(bw);

            bw.Flush();
            bw.Close();
        }

        public static T ReadBinaryStorableClassFromFile<T>(string fileName) where T : IBinaryStorable
        {
            var br = new BinaryReader(File.OpenRead(fileName));

            var data = Activator.CreateInstance<T>();
            data.ReadFromBinaryFile(br);

            br.Close();

            return data;
        }

        public static void WriteSerializableClassToFile<T>(T data, string fileName) where T : class
        {
            Stream stream = File.Open(fileName, FileMode.Create);
            BinaryFormatter bformatter = new BinaryFormatter();

            Console.WriteLine("Writing SerializableClass");
            bformatter.Serialize(stream, data);
            stream.Close();
        }

        public static T LoadSerializableClassFromFile<T>(string fileName) where T : class
        {
            T data = null;
            var stream = File.Open(fileName, FileMode.Open);
            var bformatter = new BinaryFormatter();

            Console.WriteLine("Reading SerializableClass");
            data = (T)bformatter.Deserialize(stream);
            stream.Close();

            return data;
        }

        public static void WriteDictionaryFile<T1, T2>(string fileName, Dictionary<T1, T2> dictionary, char seperator1 = '\t', char seperator2 = ',')
        {
            StreamWriter sw = new StreamWriter(fileName);

            foreach (var kvp in dictionary)
            {
                sw.Write(kvp.Key.ToString() + seperator2 + kvp.Value.ToString() + seperator1);
                sw.Flush();
            }

            sw.Flush();
            sw.Close();
        }

        public static Dictionary<string, string> LoadDictionaryFile(string filename)
        {
            var sr = new StreamReader(filename);
            string line;
            var dict = new Dictionary<string, string>();
            while ((line = sr.ReadLine()) != null)
            {
                var tokens = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                dict.Add(tokens[0], tokens[1]);
            }
            sr.Close();

            return dict;
        }

        public static string[] LoadKeyWordFile(string filename, bool isToLower = true, bool bRemoveRedundant = true,
            Encoding encoding = null)
        {
            StreamReader sr;
            if(encoding == null)
                sr = new StreamReader(filename);
            else
                sr = new StreamReader(filename, encoding);
            string line;
            if (bRemoveRedundant)
            {
                var keywords = new HashSet<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    if (isToLower)
                        line = line.ToLower();
                    keywords.Add(line);
                }
                sr.Close();

                return keywords.ToArray<string>();
            }
            else
            {
                List<string> keywords = new List<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    if (isToLower)
                        line = line.ToLower();
                    keywords.Add(line);
                }
                sr.Close();

                return keywords.ToArray<string>();
            }
        }

        public static IConfigureFile LoadConfigureFile(string filename)
        {
            return new ConfigureFile(filename);
        }

        public static Dictionary<string, List<string>> LoadConfigure(string filename)
        {
            var config = new Dictionary<string, List<string>>();
            StreamReader sr = new StreamReader(filename);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("//") || line.StartsWith("---") || line.Length == 0)
                    continue;
                var paraName = line;
                line = sr.ReadLine();
                List<string> paraVals = new List<string>();
                while (line != null && line.Length > 0 && !(line.StartsWith("//") || line.StartsWith("---")))
                {
                    paraVals.Add(line);
                    line = sr.ReadLine();
                }
                if (!config.ContainsKey(paraName))
                    config.Add(paraName, paraVals);
            }

            sr.Close();
            return config;
        }

        public static void EnsureFolderExist(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        public static void EnsureFileFolderExist(string fileName)
        {
            var index = fileName.LastIndexOf("\\");
            if (index > 0)
            {
                var folder = fileName.Substring(0, index);
                Directory.CreateDirectory(folder);
            }
            else
            {
                index = fileName.LastIndexOf("/");
                if (index > 0)
                {
                    var folder = fileName.Substring(0, index);
                    Directory.CreateDirectory(folder);
                }
            }
        }
    }

    class DirectoryOperations
    {
        public static string GetLastFolderName(string directory)
        {
            var tokens = directory.Split(new char[]{'\\'},StringSplitOptions.RemoveEmptyEntries);
            return tokens[tokens.Length - 1];
        }
    }
}
