using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DataProcess.Utils;
using System.Runtime.InteropServices;
using System.Windows;

namespace DataProcess.Utils
{

    #region multiple layer hash/Dictionary
    public class TwoLayerDictionary<TPartitionKey, TKey, TValue>
    {
        private Dictionary<TPartitionKey, Dictionary<TKey, TValue>> _pDictionary;
        private readonly Func<TKey, TPartitionKey> _partitionFunc;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="partitionFunction">Function which accepts the Key as Input
        /// and returns back the Partition Key as output.
        /// <para>This function plays very important role for the KeyValuePair distribution
        /// over partitions. Some guildlines for this function:</para>
        /// <para>1. Must be very simple.</para>
        /// <para>2. Must not throw any exception for the range of key values.</para>
        /// <para>3. Ideal for uniformaly distributing KeyValuePair over generated partitions.</para>
        /// <para>Simple examples:</para>
        /// <para>Ex.A. If TKey is Int, then this function could be (x=>x%10) or similar
        /// which will create partitions as 0..9, thus total of 10 partitions</para>
        /// <para>Ex.B. If TKey is String, then this function could be (x=>x.trim()[0]) or similar
        /// which will create partitions as a..zA..Z (assuming string starts with english alphabets),
        /// thus total of 52 partitions</para>
        /// </param>
        public TwoLayerDictionary(Func<TKey, TPartitionKey> partitionFunction)
        {
            _partitionFunc = partitionFunction;
            _pDictionary = new Dictionary<TPartitionKey, Dictionary<TKey, TValue>>();
        }

        public bool TryAdd(TKey key, TValue value)
        {
            var retVal = false;
            TPartitionKey pKey = _partitionFunc(key);
            if (_pDictionary.ContainsKey(pKey))
            {
                var pDict = _pDictionary[pKey];
                if (!pDict.ContainsKey(key))
                {
                    pDict.Add(key, value);
                    retVal = true;
                }
            }
            else
            {
                _pDictionary.Add(pKey, new Dictionary<TKey, TValue> { { key, value } });
                retVal = true;
            }

            return retVal;
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            TPartitionKey pKey = _partitionFunc(key);
            if (_pDictionary.ContainsKey(pKey))
            {
                _pDictionary[pKey][key] = value;
            }
            else
            {
                _pDictionary.Add(pKey, new Dictionary<TKey, TValue> { { key, value } });
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var pKey = _partitionFunc(key);
            if (_pDictionary.ContainsKey(pKey))
            {
                var pDict = _pDictionary[pKey];
                if (pDict.ContainsKey(key))
                {
                    value = pDict[key];
                    return true;
                }
            }
            value = default(TValue);
            return false;
        }

        public bool Remove(TKey key)
        {
            var retVal = false;
            TPartitionKey pKey = _partitionFunc(key);
            if (_pDictionary.ContainsKey(pKey))
            {
                var pDict = _pDictionary[pKey];
                retVal = pDict.Remove(key);

                if (pDict.Count == 0)
                    _pDictionary.Remove(pKey);
            }
            return retVal;
        }

        public void DropAllPartition()
        {
            _pDictionary = new Dictionary<TPartitionKey, Dictionary<TKey, TValue>>();
        }

        public bool DropPartitionByPartitionKey(TPartitionKey pKey)
        {
            return _pDictionary.Remove(pKey);
        }

        public bool DropPartitionWhichHasKey(TKey key)
        {
            TPartitionKey pKey = _partitionFunc(key);
            return DropPartitionByPartitionKey(pKey);
        }

        public IEnumerable<TPartitionKey> AllPartitionKey
        {
            get
            {
                return _pDictionary.Keys;
            }
        }

        /// <summary>
        /// Gets all the existing Keys inside the partition identified by the partition key.
        /// </summary>
        /// <param name="pKey">Partition key</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public IEnumerable<TKey> AllKeyInsidePartition(TPartitionKey pKey)
        {
            return _pDictionary[pKey].Keys;
        }

        /// <summary>
        /// Gets all the existing values inside the partition identified by the partition key.
        /// </summary>
        /// <param name="pKey">Partition key</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public IEnumerable<TValue> AllValuesInsidePartition(TPartitionKey pKey)
        {
            return _pDictionary[pKey].Values;
        }

        /// <summary>
        /// Gets all the existing KeyValuePair inside the partition identified by the partition key.
        /// </summary>
        /// <param name="pKey">Partition key</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public IEnumerable<KeyValuePair<TKey, TValue>> GetPartitionByPartitionKey(TPartitionKey pKey)
        {
            return _pDictionary[pKey];
        }

        /// <summary>
        /// This function also checks whether the partition key exists or not.
        /// </summary>
        /// <param name="key">Key on which partition key will be computed</param>
        /// <param name="pKey">Parititon key computed from given key, if exists, else default value.</param>
        public bool TryGetExistingPartitionKeyOfKey(TKey key, out TPartitionKey pKey)
        {
            pKey = _partitionFunc(key);
            if (_pDictionary.ContainsKey(pKey))
            {
                return true;
            }
            else
            {
                pKey = default(TPartitionKey);
                return false;
            }
        }

        /// <summary>
        /// Gets the count of all existing KeyValuePair inside the partition identified by the partition key.
        /// </summary>
        /// <param name="pKey">Partition key</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public int RecordCountInsidePartitionByPartitionKey(TPartitionKey pKey)
        {
            return _pDictionary[pKey].Count;
        }

        /// <summary>
        /// Gets the count of all existing KeyValuePair inside the partition identified by given key.
        /// </summary>
        /// <param name="pKey">Partition key</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public int RecordCountInsidePartitionWhichContainsKey(TKey key)
        {
            var pKey = _partitionFunc(key);
            return RecordCountInsidePartitionByPartitionKey(pKey);
        }

        public long TotalRecordCount
        {
            get
            {
                return _pDictionary.Sum(x => (long)x.Value.Count);
            }
        }

        public int TotalPartitionCount
        {
            get
            {
                return _pDictionary.Count;
            }
        }

        public bool ContainsPartition(TPartitionKey pKey)
        {
            return _pDictionary.ContainsKey(pKey);
        }

        public bool ContainsKey(TKey key)
        {
            var pKey = _partitionFunc(key);
            return _pDictionary.ContainsKey(pKey) && _pDictionary[pKey].ContainsKey(key);
        }

        /// <summary>
        /// Get the value based on given key.
        /// </summary>
        /// <param name="key">Key</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public TValue this[TKey key]
        {
            get
            {
                var pKey = _partitionFunc(key);
                return _pDictionary[pKey][key];
            }
            set
            {
                AddOrUpdate(key, value);
            }
        }
    }



    public class TwoLayerHashSet<T1, T2>
    {
        Dictionary<T1, HashSet<T2>> _dict = new Dictionary<T1, HashSet<T2>>();

        public void Add(T1 value1, T2 value2)
        {
            HashSet<T2> set;
            if (_dict.TryGetValue(value1, out set))
            {
                set.Add(value2);
            }
            else
            {
                set = new HashSet<T2>();
                set.Add(value2);
                _dict.Add(value1, set);
            }
        }

        public bool Contains(T1 value1, T2 value2)
        {
            HashSet<T2> set;
            if (_dict.TryGetValue(value1, out set))
            {
                if (set.Contains(value2))
                {
                    return true;
                }
            }
            return false;
        }

        public int Count()
        {
            return _dict.Sum(kvp => kvp.Value.Count);
        }

        public static void Test()
        {
            var hash = new TwoLayerHashSet<string, string>();

            hash.Add("a", "1");
            hash.Add("a", "3");
            hash.Add("b", "4");

            Console.Write(hash.Count()+",");
            Console.Write(hash.Contains("a", "2")+",");
            Console.Write(hash.Contains("a", "3")+",");

            hash.Add("z", "xx");
            hash.Add("b", "4");
            hash.Add("a", "1");

            Console.Write(hash.Count());

            Console.WriteLine("\n---Correct answer:\n3,False,True,4");
        }
    }


    public class ThreeLayerHashSet<T1, T2, T3>
    {
        Dictionary<T1, Dictionary<T2, HashSet<T3>>> _dict = new Dictionary<T1, Dictionary<T2, HashSet<T3>>>();

        public void Add(T1 value1, T2 value2, T3 value3)
        {
            Dictionary<T2, HashSet<T3>> dict;
            if (_dict.TryGetValue(value1, out dict))
            {
                HashSet<T3> set;
                if (dict.TryGetValue(value2, out set))
                {
                    set.Add(value3);
                }
                else
                {
                    set = new HashSet<T3>();
                    set.Add(value3);
                    dict.Add(value2, set);
                }
            }
            else
            {
                dict = new Dictionary<T2, HashSet<T3>>();
                var set = new HashSet<T3>();
                set.Add(value3);
                dict.Add(value2, set);
                _dict.Add(value1, dict);
            }
        }

        public bool Contains(T1 value1, T2 value2, T3 value3)
        {
            Dictionary<T2, HashSet<T3>> dict;
            if (_dict.TryGetValue(value1, out dict))
            {
                HashSet<T3> set;
                if (dict.TryGetValue(value2, out set))
                {
                    if (set.Contains(value3))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public int Count()
        {
            int count = 0;
            foreach (var kvp in _dict)
            {
                count += (int)kvp.Value.Sum(kvp2=>kvp2.Value.Count);
            }
            return count;
        }

        public static void Test()
        {
            var hash = new ThreeLayerHashSet<string, string, int>();

            hash.Add("a", "1", 1);
            hash.Add("a", "3", 3);
            hash.Add("b", "4", 4);
            hash.Add("b", "4", 3);

            Console.Write(hash.Count() + ",");
            Console.Write(hash.Contains("a", "2", 2) + ",");
            Console.Write(hash.Contains("a", "3", 3) + ",");
            Console.Write(hash.Contains("b", "4", 3) + ",");

            hash.Add("z", "xx", 0);
            hash.Add("b", "4", 4);
            hash.Add("a", "1", 1);

            Console.Write(hash.Count());

            Console.WriteLine("\n---Correct answer:\n4,False,True,True,5");
        }
    }
    #endregion

    #region counter
    public class Counter<T>
    {
        Dictionary<T, int> counter;
        private int _totalCount = 0;

        public Counter()
        {
            counter = new Dictionary<T, int>();
        }

        public Counter(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }


        public int TotalCount {
            get { return _totalCount; }
        }

        public void Add(T obj)
        {
            if (counter.ContainsKey(obj))
                counter[obj]++;
            else
                counter.Add(obj, 1);
            _totalCount++;
        }

        public void Add(T obj, int num)
        {
            if (counter.ContainsKey(obj))
                counter[obj]+=num;
            else
                counter.Add(obj, num);
            _totalCount+=num;
        }

        public List<T> GetMostFreqObjs(int objNum)
        {
            List<T> freqObjs = new List<T>();
            int cnt = 0;
            foreach (var kvp in counter.OrderBy(kvp => -kvp.Value))
            {
                freqObjs.Add(kvp.Key);
                if (++cnt >= objNum)
                    break;
            }
            return freqObjs;
        }

        public Dictionary<T, int> GetCountDictionary()
        {
            return counter;
        }

        public Dictionary<T, int> GetSortedCountDictioanry()
        {
            Dictionary<T, int> dict = new Dictionary<T, int>();
            foreach (var kvp in counter.OrderByDescending(kvp => kvp.Value))
                dict.Add(kvp.Key, kvp.Value);
            return dict;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in GetSortedCountDictioanry())
            {
                sb.Append(kvp.Key + "\t" + kvp.Value + "\n");
            }
            return sb.ToString();
        }
    }

    public class WeightedCounter<T>
    {
        Dictionary<T, double> counter;
        private double _totalCount = 0;


        public WeightedCounter()
        {
            counter = new Dictionary<T, double>();
        }

        public static WeightedCounter<T2> GetCounter<T1, T2>(Dictionary<T1, double> weightedItemDict, Func<T1, T2> item2AttributeFunc)
        {
            WeightedCounter<T2> counter = new WeightedCounter<T2>();
            foreach (var kvp in weightedItemDict)
            {
                counter.Add(item2AttributeFunc(kvp.Key), kvp.Value);
            }
            return counter;
        }


        public double TotalCount
        {
            get { return _totalCount; }
        }

        public void Add(T obj)
        {
            if (counter.ContainsKey(obj))
                counter[obj]++;
            else
                counter.Add(obj, 1);
            _totalCount++;
        }

        public void Add(T obj, double weight)
        {
            if (counter.ContainsKey(obj))
                counter[obj] += weight;
            else
                counter.Add(obj, weight);
            _totalCount += weight;
        }

        public List<T> GetMostFreqObjs(int objNum = int.MaxValue)
        {
            List<T> freqObjs = new List<T>();
            int cnt = 0;
            foreach (var kvp in counter.OrderBy(kvp => -kvp.Value))
            {
                freqObjs.Add(kvp.Key);
                if (++cnt >= objNum)
                    break;
            }
            return freqObjs;
        }

        public Dictionary<T, double> GetCountDictionary()
        {
            return counter;
        }

        public Dictionary<T, double> GetSortedCountDictionary()
        {
            Dictionary<T, double> dict = new Dictionary<T, double>();
            foreach (var kvp in counter.OrderByDescending(kvp => kvp.Value))
            {
                dict.Add(kvp.Key, kvp.Value);
            }
            return dict;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in GetSortedCountDictionary())
            {
                sb.Append(kvp.Key + "\t" + kvp.Value + "\n");
            }
            return sb.ToString();
        }

        public void Clear()
        {
            counter.Clear();
            counter = null;
        }
    }

    public class TwoLayerWeightedCounter<TPartition, TKey>
    {
        TwoLayerDictionary<TPartition, TKey, double> counter;
        private double _totalCount = 0;

        public TwoLayerWeightedCounter(Func<TKey, TPartition> func)
        {
            counter = new TwoLayerDictionary<TPartition, TKey, double>(func);
        }

        public double TotalCount
        {
            get { return _totalCount; }
        }

        public void Add(TKey obj)
        {
            if (counter.ContainsKey(obj))
            {
                counter.AddOrUpdate(obj, counter[obj] + 1);
            }
            else
            {
                counter.AddOrUpdate(obj, 1);
            }
            _totalCount++;
        }

        public void Add(TKey obj, double weight)
        {
            if (counter.ContainsKey(obj))
            {
                counter.AddOrUpdate(obj, counter[obj] + weight);
            }
            else
            {
                counter.AddOrUpdate(obj, weight);
            }
            _totalCount += weight;
        }

        //public List<TKey> GetMostFreqObjs(int objNum)
        //{
        //    List<TKey> freqObjs = new List<TKey>();
        //    int cnt = 0;
        //    foreach (var kvp in counter.OrderBy(kvp => -kvp.Value))
        //    {
        //        freqObjs.Add(kvp.Key);
        //        if (++cnt >= objNum)
        //            break;
        //    }
        //    return freqObjs;
        //}

        public TwoLayerDictionary<TPartition, TKey, double> GetCountDictionary()
        {
            return counter;
        }

        //public Dictionary<TKey, double> GetSortedCountDictionary()
        //{
        //    Dictionary<TKey, double> dict = new Dictionary<TKey, double>();
        //    foreach (var kvp in counter.OrderByDescending(kvp => kvp.Value))
        //    {
        //        dict.Add(kvp.Key, kvp.Value);
        //    }
        //    return dict;
        //}

        //public override string ToString()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (var kvp in GetSortedCountDictionary())
        //    {
        //        sb.Append(kvp.Key + "\t" + kvp.Value + "\n");
        //    }
        //    return sb.ToString();
        //}

        //public void Clear()
        //{
        //    counter.Clear();
        //    counter = null;
        //}
    }
    #endregion

    #region lexicon
    public class Lexicon<T>
    {
        private List<T> _vocabulary;
        public IList<T> Vocabulary
        {
            get { return _vocabulary.AsReadOnly(); }
        }

        private Dictionary<T, int> _invertedDictionary;

        public Dictionary<T, int> InvertedDictionary
        {
            get { return _invertedDictionary; }
        }

        public Lexicon()
        {
            _vocabulary = new List<T>();
            _invertedDictionary = new Dictionary<T, int>();
        }


        public Lexicon(T[] vocabulary)
        {
            Initialize(vocabulary);
        }

        public int Count()
        {
            return Vocabulary.Count;
        }

        public Dictionary<T, double> GetStringVector(Dictionary<int, double> vector)
        {
            return vector.ToDictionary(kvp => _vocabulary[kvp.Key], kvp => kvp.Value);
        }

        private void Initialize(T[] vocab)
        {
            _vocabulary = new List<T>(vocab);
            _invertedDictionary = Util.GetInvertedDictionary(_vocabulary);
        }

        public int GetWordIndex(T word)
        {
            int index;
            if (!_invertedDictionary.TryGetValue(word, out index))
            {
                index = _invertedDictionary.Count;
                _invertedDictionary.Add(word, index);
                _vocabulary.Add(word);
            }
            return index;
        }

        public T GetWord(int wordIndex)
        {
            return _vocabulary[wordIndex];
        }
    }

    #endregion

    public static class Util
    {
        public static T[] GetSubArray<T>(T[] array, int startIndexInc, int endIndexInc)
        {
            T[] res = new T[endIndexInc - startIndexInc + 1];
            for (int i = startIndexInc; i <= endIndexInc; i++)
            {
                res[i - startIndexInc] = array[i];
            }

            return res;
        }

        public static double[] GetInterplotedArray(Dictionary<int, double> sortedDict, int[] interpXVals)
        {
            var dictEnum = sortedDict.GetEnumerator();
            var xEnum = interpXVals.GetEnumerator();

            dictEnum.MoveNext();
            xEnum.MoveNext();

            int prevXVal = -1;
            double prevYVal = double.NaN;
            double[] yVals = new double[interpXVals.Length];
            int index = 0;

            while(true)
            {
                var interpXVal = (int)xEnum.Current;
                if (dictEnum.Current.Key == interpXVal)
                {
                    yVals[index] = dictEnum.Current.Value;
                    if (!xEnum.MoveNext())
                    {
                        break;
                    }
                    index++;
                }
                else if (dictEnum.Current.Key > interpXVal)
                {
                    double lambda = Maths.GetLambda(prevXVal, dictEnum.Current.Key, interpXVal);
                    yVals[index] = Maths.GetIntermediateNumber(prevYVal, dictEnum.Current.Value, lambda);
                    if (!xEnum.MoveNext())
                    {
                        break;
                    }
                    index++;
                }
                else
                {
                    prevXVal = dictEnum.Current.Key;
                    prevYVal = dictEnum.Current.Value;
                    dictEnum.MoveNext();
                }
            }

            return yVals;
        }
        
        public static T2 GetInitializedValue<T1, T2>(Dictionary<T1, T2> dict, T1 key, Func<T2> construcFunc)
        {
            T2 value;
            if (!dict.TryGetValue(key, out value))
            {
                value = construcFunc();
                dict.Add(key, value);
            }
            return value;
        }

        public static T2 GetInitializedValue<T1, T2>(Dictionary<T1, T2> dict, T1 key) where T2 : new()
        {
            T2 value;
            if (!dict.TryGetValue(key, out value))
            {
                value = new T2();
                dict.Add(key, value);
            }
            return value;
        }

        #region get method extension
        /// <summary>
        /// Search for a method by name and parameter types.  
        /// Unlike GetMethod(), does 'loose' matching on generic
        /// parameter types, and searches base interfaces.
        /// </summary>
        /// <exception cref="AmbiguousMatchException"/>
        public static MethodInfo GetMethodExt(this Type thisType,
                                                string name,
                                                params Type[] parameterTypes)
        {
            return GetMethodExt(thisType,
                                name,
                                BindingFlags.Instance
                                | BindingFlags.Static
                                | BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.FlattenHierarchy,
                                parameterTypes);
        }

        /// <summary>
        /// Search for a method by name, parameter types, and binding flags.  
        /// Unlike GetMethod(), does 'loose' matching on generic
        /// parameter types, and searches base interfaces.
        /// </summary>
        /// <exception cref="AmbiguousMatchException"/>
        public static MethodInfo GetMethodExt(this Type thisType,
                                                string name,
                                                BindingFlags bindingFlags,
                                                params Type[] parameterTypes)
        {
            MethodInfo matchingMethod = null;

            // Check all methods with the specified name, including in base classes
            GetMethodExt(ref matchingMethod, thisType, name, bindingFlags, parameterTypes);

            // If we're searching an interface, we have to manually search base interfaces
            if (matchingMethod == null && thisType.IsInterface)
            {
                foreach (Type interfaceType in thisType.GetInterfaces())
                    GetMethodExt(ref matchingMethod,
                                 interfaceType,
                                 name,
                                 bindingFlags,
                                 parameterTypes);
            }

            return matchingMethod;
        }

        private static void GetMethodExt(ref MethodInfo matchingMethod,
                                            Type type,
                                            string name,
                                            BindingFlags bindingFlags,
                                            params Type[] parameterTypes)
        {
            // Check all methods with the specified name, including in base classes
            foreach (MethodInfo methodInfo in type.GetMember(name,
                                                             MemberTypes.Method,
                                                             bindingFlags))
            {
                // Check that the parameter counts and types match, 
                // with 'loose' matching on generic parameters
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                if (parameterInfos.Length == parameterTypes.Length)
                {
                    int i = 0;
                    for (; i < parameterInfos.Length; ++i)
                    {
                        if (!parameterInfos[i].ParameterType
                                              .IsSimilarType(parameterTypes[i]))
                            break;
                    }
                    if (i == parameterInfos.Length)
                    {
                        if (matchingMethod == null)
                            matchingMethod = methodInfo;
                        else
                            throw new AmbiguousMatchException(
                                   "More than one matching method found!");
                    }
                }
            }
        }

        /// <summary>
        /// Special type used to match any generic parameter type in GetMethodExt().
        /// </summary>
        public class T
        { }

        /// <summary>
        /// Determines if the two types are either identical, or are both generic 
        /// parameters or generic types with generic parameters in the same
        ///  locations (generic parameters match any other generic paramter,
        /// but NOT concrete types).
        /// </summary>
        private static bool IsSimilarType(this Type thisType, Type type)
        {
            // Ignore any 'ref' types
            if (thisType.IsByRef)
                thisType = thisType.GetElementType();
            if (type.IsByRef)
                type = type.GetElementType();

            // Handle array types
            if (thisType.IsArray && type.IsArray)
                return thisType.GetElementType().IsSimilarType(type.GetElementType());

            // If the types are identical, or they're both generic parameters 
            // or the special 'T' type, treat as a match
            if (thisType == type || ((thisType.IsGenericParameter || thisType == typeof(T))
                                 && (type.IsGenericParameter || type == typeof(T))))
                return true;

            // Handle any generic arguments
            if (thisType.IsGenericType && type.IsGenericType)
            {
                Type[] thisArguments = thisType.GetGenericArguments();
                Type[] arguments = type.GetGenericArguments();
                if (thisArguments.Length == arguments.Length)
                {
                    for (int i = 0; i < thisArguments.Length; ++i)
                    {
                        if (!thisArguments[i].IsSimilarType(arguments[i]))
                            return false;
                    }
                    return true;
                }
            }

            return false;
        }
        #endregion

        public static void WriteLine(string str, ConsoleColor foregroundColor, ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.WriteLine(str);
            Console.ResetColor();
        }

        public static double[][] Transpose(double[][] mat)
        {
            if (mat == null || mat.Length == 0)
            {
                return mat;
            }


            var len1 = mat.Length;
            var len2 = mat.First().Length;

            double[][] mat2 = Util.GetInitializedArray(len2, () => new double[len1]);
            for (int i = 0; i < len1; i++)
            {
                for (int j = 0; j < len2; j++)
                {
                    mat2[j][i] = mat[i][j];
                }
            }
            return mat2;
        }

        public static double[][] ConvertMatrixToArrayArray(double[,] mat)
        {
            double[][] res = Util.GetInitializedArray(mat.GetLength(0), () => new double[mat.GetLength(1)]);
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    res[i][j] = mat[i, j];
                }
            }
            return res;
        }

        public static void AddRange<T>(HashSet<T> set,  IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                set.Add(item);
            }
        }

        public static double BinarySearch(double y, Func<double, double> monoX2yFunc, double xMin = 0, double xMax = 1, double eps = 1e-6)
        {
            Func<double, double> x2yFunc;
            if (monoX2yFunc(xMin) > monoX2yFunc(xMax))
            {
                x2yFunc = x => -monoX2yFunc(x);
                y = -y;
            }
            else
            {
                x2yFunc = x => monoX2yFunc(x);
            }

            do
            {
                double mid = (xMin + xMax) / 2;
                if (y > x2yFunc(mid) + eps)
                {
                    xMin = mid;
                }
                else if (y < x2yFunc(mid) - eps)
                {
                    xMax = mid;
                }
                else
                {
                    return mid;
                }
            } while (xMin < xMax);
            throw new ArgumentException("Cannot find proper x!");
        }

        public static T[][] NewMatrix<T>(int rowCnt, int colCnt)
        {
            return GetInitializedArray(rowCnt, i => new T[colCnt]);
        }

        public static T Max<T>(IEnumerable<T> source, IComparer<T> comparer)
        {
            if (source.Count() == 0)
            {
                throw new ArgumentException();
            }

            var max = source.First();
            foreach (var item in source)
            {
                if (comparer.Compare(item, max) > 0)
                {
                    max = item;
                }
            }
            return max;
        }

        public static T Min<T>(IEnumerable<T> source, IComparer<T> comparer) 
        {
            if (source.Count() == 0)
            {
                throw new ArgumentException();
            }

            var min = source.First();
            foreach (var item in source)
            {
                if (comparer.Compare(item, min) < 0)
                {
                    min = item;
                }
            }
            return min;
        }

        public static object Cast(Type type, object obj)
        {
            return Convert.ChangeType(obj, type);
        }

        public static void PutAll<K,V>(Dictionary<K,V> dict, Dictionary<K, V> copyDict)
        {
            foreach (var kvp in copyDict)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        public static bool RetainAll<T>(Collection<T> collection, IEnumerable<T> c)
        {
            Collection<T> removeCollection = new Collection<T>();
            foreach (var item in collection)
            {
                if (!c.Contains(item))
                {
                    removeCollection.Add(item);
                }
            }

            foreach (var removeItem in removeCollection)
            {
                collection.Remove(removeItem);
            }

            return removeCollection.Count > 0;
        }

        public static void AddAll<T>(Collection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        public static void RemoveAll<T>(Collection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Remove(item);
            }
        }

        public static T RemoveLast<T>(List<T> list)
        {
            var last = list.Last();
            list.Remove(last);
            return last;
        }

        public static bool IsInEffectiveTimeSpan(DateTime dateTime, DateTime startDate, DateTime endDate)
        {
            return (dateTime.CompareTo(startDate)) * (dateTime.CompareTo(endDate)) <= 0;
        }

        public static bool IsMonotonous(IEnumerable<double> value)
        {
            if (value == null || value.Count() <= 2)
            {
                return true;
            }
            double factor = value.ElementAt(1) - value.ElementAt(0);
            double lastV = double.NaN;
            foreach (var v in value)
            {
                if (!double.IsNaN(lastV))
                {
                    if ((v - lastV)*factor < 0)
                    {
                        return false;
                    }
                }
                lastV = v;
            }
            return true;
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public static void ProgramFinishHalt()
        {
            Console.WriteLine("All Finish");
            Console.ReadLine();
            Application.Current.Shutdown();
        }

        public static double Truncate(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static int GetHashCode(IEnumerable<int> nums)
        {
            int hashCode = 0;
            foreach (var num in nums)
            {
                hashCode ^= (num + 0.5).GetHashCode();
            }
            return hashCode;
        }

        public static Dictionary<int, Dictionary<int, double>> Copy(Dictionary<int, Dictionary<int, double>> dict1)
        {
            Dictionary<int, Dictionary<int, double>> dict2 = new Dictionary<int, Dictionary<int, double>>();
            foreach (var kvp in dict1)
            {
                dict2.Add(kvp.Key, new Dictionary<int, double>(kvp.Value));
            }
            return dict2;
        }

        public static void AddToArray<T>(List<T>[] array, T obj, int index)
        {
            if (array[index] == null)
                array[index] = new List<T>();
            array[index].Add(obj);
        }

        public static int GetDictionaryIndex<T>(Dictionary<T, int> dict, T key)
        {
            int value;
            if (!dict.TryGetValue(key, out value))
            {
                value = dict.Count;
                dict.Add(key, value);
            }
            return value;
        }

        public static double PairAvg(Tuple<double, double> tuple)
        {
            return (tuple.Item1 + tuple.Item2)/2;
        }

        public static double PairMax(Tuple<double, double> tuple)
        {
            return Math.Max(tuple.Item1, tuple.Item2);
        }

        public static bool IsEquals<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            if (list1 == null || list2 == null)
                return list1 == list2;
            if (list1.Count() != list2.Count())
                return false;

            //list1 = new List<T>(list1);
            //list2 = new List<T>(list2);
            for (int i = 0; i < list1.Count(); i++)
            {
                if (!list1.ElementAt(i).Equals(list2.ElementAt(i)))
                    return false;
            }
            return true;
        }

        public static void SetEach<T>(T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

        public static T[] GetInitializedArray<T>(int cnt, params object[] args)
        {
            var array = new T[cnt];
            for (int i = 0; i < cnt; i++)
            {
                array[i] = (T) Activator.CreateInstance(typeof (T), args);
            }
            return array;
        }

        public static T[] GetInitializedArray<T>(int cnt, Func<T> func)
        {
            var array = new T[cnt];
            for (int i = 0; i < cnt; i++)
            {
                array[i] = func();
            }
            return array;
        }

        public static T[] GetInitializedArray<T>(int cnt) where T: new()
        {
            var array = new T[cnt];
            for (int i = 0; i < cnt; i++)
            {
                array[i] = new T();
            }
            return array;
        }


        public static T[] GetInitializedArray<T>(int cnt, Func<int, T> func)
        {
            var array = new T[cnt];
            for (int i = 0; i < cnt; i++)
            {
                array[i] = func(i);
            }
            return array;
        }

        /// <summary>
        /// Have not tested
        /// </summary>
        public static bool IsSame<T1, T2>(Dictionary<T1, List<T2>> dict1, Dictionary<T1, List<T2>> dict2) 
        {
            if (dict1 == null || dict2 == null)
                return dict1 == dict2;
            if (dict1.Count != dict2.Count)
                return false;

            for (int i = 0; i < dict1.Count; i++)
            {
                var kvp1 = dict1.ElementAt(i);
                var kvp2 = dict2.ElementAt(i);

                if (!kvp1.Key.Equals(kvp2.Key) || !IsSame(kvp1.Value, kvp2.Value)) 
                    return false;
            }

            return true;
        }


        public static bool IsSame<T>(IEnumerable<T> list1, IEnumerable<T> list2) //where T : IComparable
        {
            if (list1 == null || list2 == null)
                return list1 == list2;
            if (list1.Count() != list2.Count())
                return false;

            //list1 = new List<T>(list1);
            //list2 = new List<T>(list2);
            for (int i = 0; i < list1.Count(); i++)
            {
                //if (list1.ElementAt(i).CompareTo(list2.ElementAt(i)) != 0)
                if (!list1.ElementAt(i).Equals(list2.ElementAt(i)))
                    return false;
            }
            return true;
        }

        public static bool IsSame(IEnumerable<Point> list1, IEnumerable<Point> list2)
        {
            if (list1 == null || list2 == null)
                return list1 == list2;
            if (list1.Count() != list2.Count())
                return false;

            //list1 = new List<Point>(list1);
            //list2 = new List<Point>(list2);
            for (int i = 0; i < list1.Count(); i++)
            {
                if (list1.ElementAt(i) != (list2.ElementAt(i)))
                    return false;
            }
            return true;
        }


        public static void GetIncrementalItems<T>(List<T> prevItems, List<T> curItems, out List<T> oldItems,
            out List<T> oldAndNewItems,
            out List<T> newItems) where T : IComparable
        {
            prevItems = SortUtils.EnsureSorted(new List<T>(prevItems));
            curItems = SortUtils.EnsureSorted(new List<T>(curItems));

            var TList1 = prevItems;
            var TList2 = curItems;

            oldItems = new List<T>();
            newItems = new List<T>();
            oldAndNewItems = new List<T>();

            //when there is no previous matching result
            if (TList1 == null)
            {
                newItems = TList2;
                return;
            }

            //normally
            var enum1 = TList1.GetEnumerator();
            var enum2 = TList2.GetEnumerator();
            bool hasNext1 = enum1.MoveNext(), hasNext2 = enum2.MoveNext();
            while (hasNext1 && hasNext2)
            {
                var T1 = enum1.Current;
                var T2 = enum2.Current;

                var compare = T1.CompareTo(T2);
                //the two match infos are the same
                if (compare == 0)
                {
                    hasNext1 = enum1.MoveNext();
                    hasNext2 = enum2.MoveNext();
                    continue;
                }

                //only uncertainty differences
                if (T1.CompareTo(T2) == 0)
                {
                    oldAndNewItems.Add(T1);
                    hasNext1 = enum1.MoveNext();
                    hasNext2 = enum2.MoveNext();
                    continue;
                }

                if (compare < 0)
                {
                    //prev node should be deleted
                    oldItems.Add(T1);
                    hasNext1 = enum1.MoveNext();
                    continue;
                }
                else
                {
                    //new node should be added
                    newItems.Add(T2);
                    hasNext2 = enum2.MoveNext();
                }
            }
            //deal with the tails
            while (hasNext1)
            {
                oldItems.Add(enum1.Current);
                hasNext1 = enum1.MoveNext();
            }
            while (hasNext2)
            {
                newItems.Add(enum2.Current);
                hasNext2 = enum2.MoveNext();
            }
        }

        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();

        public static void PrintTraceToConsole()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        public static int[] GetIntArray(int startIndex, int endIndex, int deltaIndex = 1)
        {
            var cnt = (endIndex - startIndex)/deltaIndex + 1;
            var array = new int[cnt];
            for (int i = 0; i < cnt; i++)
            {
                array[i] = startIndex + deltaIndex*i;
            }
            return array;
        }

        public static double[] GetDoubleArray(double startVal, double endVal, double deltaVal)
        {
            int cnt = (int) Math.Floor(((endVal - startVal)/deltaVal + 1));
            double[] array = new double[cnt];
            for (int i = 0; i < cnt; i++)
            {
                array[i] = startVal + deltaVal*i;
            }
            return array;
        }

        private static Dictionary<string, Thread> _timerThreadDict = new Dictionary<string, Thread>();

        public static void SetupTimer(double waitSeconds, string key, Action endEvent)
        {
            if (!_timerThreadDict.ContainsKey(key))
                _timerThreadDict.Add(key, null);
            var thread = _timerThreadDict[key];
            if (thread != null)
                thread.Abort();
            thread = new Thread(() =>
            {
                Thread.Sleep((int) (1000*waitSeconds));
                endEvent();
            });
            _timerThreadDict[key] = thread;
            thread.Start();
        }

        public static T[,] Copy<T>(T[,] mat)
        {
            var res = new T[mat.GetLength(0), mat.GetLength(1)];
            Array.Copy(mat, res, mat.Length);
            return res;
        }

        public static T[] Copy<T>(T[] array)
        {
            var res = new T[array.Length];
            Array.Copy(array, res, array.Length);
            return res;
        }

        public static Dictionary<T1, T2> Copy<T1, T2>(Dictionary<T1, T2> dictionary)
        {
            var copyDict = new Dictionary<T1, T2>();
            foreach (var kvp in dictionary)
                copyDict.Add(kvp.Key, kvp.Value);
            return copyDict;
        }

        public static HashSet<T> GetHashSet<T>(IEnumerable<T> list)
        {
            var hash = new HashSet<T>();
            foreach (var item in list)
                hash.Add(item);
            return hash;
        }

        public static void AddToSet<TKey, TValue>(Dictionary<TKey, HashSet<TValue>> dict, TKey key, TValue val)
        {
            if (dict.ContainsKey(key)) dict[key].Add(val);
            else dict[key] = new HashSet<TValue> {val};
        }

        public static TValue GetFromDict<TKey1, TKey2, TValue>(Dictionary<TKey1, Dictionary<TKey2, TValue>> dict,
            TKey1 key1, TKey2 key2) where TValue : class
        {
            Dictionary<TKey2, TValue> dict2;
            if (dict.TryGetValue(key1, out dict2))
            {
                TValue value;
                if (dict2.TryGetValue(key2, out value))
                {
                    return value;
                }
            }
            return null;
        }

        public static void AddToDict<TKey1, TKey2, TValue>(Dictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1,
            TKey2 key2, TValue value)
        {
            Dictionary<TKey2, TValue> dict2;
            if (!dict.TryGetValue(key1, out dict2))
            {
                dict2 = new Dictionary<TKey2, TValue>();
                dict.Add(key1, dict2);
            }
            dict2[key2] = value;
        }


        public static void RemoveFromList<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key, TValue val)
        {
            if (dict.ContainsKey(key)) dict[key].Remove(val);
        }

        public static void AddToList<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key, TValue val)
        {
            if (dict.ContainsKey(key)) dict[key].Add(val);
            else dict[key] = new List<TValue> {val};
        }

        public static void AddToListRemoveDuplicate<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key, TValue val)
        {
            if (dict.ContainsKey(key))
            {
                if (!dict[key].Contains(val))
                {
                    dict[key].Add(val);
                }
            }
            else dict[key] = new List<TValue> { val };
        }

        public static void AddToList<TKey, TValue>(SortedDictionary<TKey, List<TValue>> dict, TKey key, TValue val)
        {
            if (dict.ContainsKey(key)) dict[key].Add(val);
            else dict[key] = new List<TValue> { val };
        }


        public static void AddToListNoRedundant<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key, TValue val)
        {
            if (dict.ContainsKey(key))
            {
                if (!dict[key].Contains(val))
                    dict[key].Add(val);
            }
            else dict[key] = new List<TValue> {val};
        }


        /// <summary>
        /// Returning array is arranged so that the elements with large scores are put in the front
        /// </summary>
        public static T[] GetElementsWithMaximumScores<T>(IEnumerable<T> elements, double selectRatio, Func<T, double> scoreFunc)
        {
            var selectCnt = (int)Math.Round(elements.Count() * selectRatio);
            if (selectCnt == 0)
                return new T[0];
            HeapSortDouble sort = new HeapSortDouble(selectCnt);
            int index = 0;
            foreach (var ele in elements)
                sort.Insert(index++, scoreFunc(ele));
            var topIndices = sort.GetTopIndices();
            T[] selectedElements = new T[topIndices.Count];
            index = 0;
            foreach (var eleIndex in topIndices)
            {
                selectedElements[index] = elements.ElementAt(eleIndex);
                index++;
            }
            return selectedElements;
        }

        public static T GetElementWithMaximumScore<T>(IEnumerable<T> elements, out double maxScore, Func<T, double> scoreFunc)
        {
            if (elements.Count() == 0)
                throw new Exception("Error GetElementWithLargestScore()!");

            var maxElement = elements.ElementAt(0);
            maxScore = double.MinValue;

            foreach (var element in elements)
            {
                var score = scoreFunc(element);
                if (maxScore < score)
                {
                    maxElement = element;
                    maxScore = score;
                }
            }
            
            return maxElement;
        }

        public static Dictionary<T, double> MergeDictionary<T>(Dictionary<T, double> dict1, Dictionary<T, double> dict2)
        {
            var dict = new Dictionary<T, double>(dict1);
            foreach (var kvp in dict2)
            {
                if (dict.ContainsKey(kvp.Key))
                    dict[kvp.Key] += kvp.Value;
                else
                {
                    dict.Add(kvp.Key, kvp.Value);
                }
            }
            return dict;
        }

        public static T GetElementWithMaximumScore<T>(IEnumerable<T> elements, Func<T, double> scoreFunc)
        {
            double maxScore;
            return GetElementWithMaximumScore<T>(elements, out maxScore, scoreFunc);
        }


        public static List<T> GetSalientItemsByPercentage<T>(IEnumerable<T> items, double percentage, Func<T, double> salienceFunc)
        {
            int selItemCnt = (int) Math.Max(0, Math.Min(items.Count(), Math.Round(items.Count()*percentage)));
            if(selItemCnt == 0)
                return new List<T>();

            HeapSortDouble hsd = new HeapSortDouble(selItemCnt);
            T[] itemArray = items.ToArray();
            for (int i = 0; i < itemArray.Length; i++)
            {
                hsd.Insert(i, salienceFunc(itemArray[i]));
            }
            int cnt = 0;
            List<T> selectItems = new SupportClass.EquatableList<T>();
            foreach (var topIndex in hsd.GetTopIndices())
            {
                selectItems.Add(itemArray[topIndex]);
                if (++cnt >= selItemCnt)
                    break;
            }
            return selectItems;
        }

        public static T1[] GetFirstFewKeys<T1, T2>(IDictionary<T1, T2> dict, int keyNum)
        {
            keyNum = (int)Math.Min(keyNum, dict.Count);
            T1[] array = new T1[keyNum];
            int index = 0;
            foreach (var key in dict.Keys)
            {
                array[index] = key;
                if (++index >= keyNum)
                    break;
            }
            return array;
        }

        public static Dictionary<T3, T2> GetFirstFewElements<T1, T2, T3>(IDictionary<T1, T2> dict, Func<T1, T3> convertKeyFunc, int eleNum)
        {
            eleNum = (int)Math.Min(eleNum, dict.Count);
            var dict2 = new Dictionary<T3, T2>();
            int index = 0;
            foreach (var kvp in dict)
            {
                dict2.Add(convertKeyFunc(kvp.Key), kvp.Value);
                if (++index >= eleNum)
                    break;
            }
            return dict2;
        }

        public static Dictionary<T1, T2> GetFirstFewElements<T1, T2>(IDictionary<T1, T2> dict, int eleNum)
        {
            eleNum = (int)Math.Min(eleNum, dict.Count);
            var dict2 = new Dictionary<T1, T2>();
            int index = 0;
            foreach (var kvp in dict)
            {
                dict2.Add(kvp.Key, kvp.Value);
                if (++index >= eleNum)
                    break;
            }
            return dict2;
        }

        #region Inverse dictionary
        public static Dictionary<T1, T2> GetInverseDictionary<T1, T2>(Dictionary<T2, T1> dict)
        {
            var invDict = new Dictionary<T1, T2>();
            foreach (var kvp in dict)
                invDict.Add(kvp.Value, kvp.Key);
            return invDict;
        }

        public static Dictionary<T1, T2> GetInverseDictionary<T2, T1>(Dictionary<T2, List<T1>> dict)
        {
            var invDict = new Dictionary<T1, T2>();
            foreach (var kvp in dict)
            {
                foreach(var element in kvp.Value)
                    invDict.Add(element, kvp.Key);
            }
            return invDict;
        }

        public static Dictionary<T, int> GetInvertedDictionary<T>(IEnumerable<T> array)
        {
            var invDict = new Dictionary<T, int>();
            for (int i = 0; i < array.Count(); i++)
            {
                invDict.Add(array.ElementAt(i), i);
            }
            return invDict;
        }

        public static Dictionary<int, T> GetDictionary<T>(IEnumerable<T> list)
        {
            var dict = new Dictionary<int, T>();
            int index = 0;
            foreach (var ele in list)
                dict.Add(index++, ele);
            return dict;
        }

        public static Dictionary<T1, T2> GetInverseDictionary<T1, T2>(IEnumerable<T2> keyList, Func<T2, IEnumerable<T1>> valueListFunc)
        {
            var invDict = new Dictionary<T1, T2>();
            foreach (var key in keyList)
            {
                foreach (var element in valueListFunc(key))
                    invDict.Add(element, key);
            }
            return invDict;
        }
        #endregion

        public static void Swap<T>(ref T val1, ref T val2)
        {
            T temp = val1;
            val1 = val2;
            val2 = temp;
        }

        public static bool IsSubset<T>(IEnumerable<T> set, IEnumerable<T> subset)
        {
            var hash = GetHashSet<T>(set);
            foreach (var ele in subset)
                if (!set.Contains(ele))
                    return false;
            return true;
        }

        public static T[,] GetMergedMatrix<T>(T[,] mat1, T[,] mat2)
        {
            if (mat1.GetLength(1) != mat2.GetLength(1))
                throw new NotImplementedException();

            var mat = new T[mat1.GetLength(0) + mat2.GetLength(0), mat1.GetLength(1)];
            int matindex = 0;
            for (int i = 0; i < mat1.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    mat[matindex, j] = mat1[i, j];
                }
                matindex++;
            }
            for (int i = 0; i < mat2.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    mat[matindex, j] = mat2[i, j];
                }
                matindex++;
            }
            return mat;
        }

        public static Dictionary<int, Dictionary<int, T>> GetInverse2DDictionary<T>(Dictionary<int, Dictionary<int, T>> dict)
        {
            var invDict = new Dictionary<int, Dictionary<int, T>>();
            foreach (var kvp1 in dict)
            {
                int key1 = kvp1.Key;
                foreach (var kvp2 in kvp1.Value)
                {
                    int key2 = kvp2.Key;
                    T val = kvp2.Value;
                    if (invDict.ContainsKey(key2) == false)
                        invDict.Add(key2, new Dictionary<int, T>());
                    invDict[key2].Add(key1, val);
                }
            }
            return EnsureSorted2DDictionary(invDict);
        }

        public static Dictionary<int, Dictionary<int, T>> EnsureSorted2DDictionary<T>(Dictionary<int, Dictionary<int, T>> dict)
        {
            var resDict = new Dictionary<int, Dictionary<int, T>>(dict);
            List<int> keys = new List<int>(resDict.Keys);
            foreach (var key in keys)
            {
                resDict[key] = SortUtils.EnsureSortedByKey(dict[key]);
            }
            resDict = SortUtils.EnsureSortedByKey(dict);
            return resDict;
        }
    }
}
