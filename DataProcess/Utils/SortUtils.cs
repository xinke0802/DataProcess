using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.Utils
{

    #region heap sort
    public class HeapSortDouble<T>
    {
        int heapSize;
        //bool bDescending;
        int factor = 1;

        MinHeapDouble mhd;
        bool isSort = false;

        private int _index = 0;
        List<T> _itemList = new List<T>();
        public HeapSortDouble(int heapSize, bool bDescending = true)
        {
            if (heapSize == 0)
                throw new ArgumentException();
            this.heapSize = heapSize;
            //this.bDescending = bDescending;
            if (!bDescending)
                factor = -1;

            mhd = new MinHeapDouble(heapSize);
            for (int i = 0; i < heapSize; i++)
                mhd.insert(-1, double.MinValue);
        }

        public void Insert(T key, double value)
        {
            _itemList.Add(key);
            value *= factor;

            if (value > mhd.min() || (value == mhd.min() && _index < mhd.getIndices()[0]))
                mhd.changeMin(_index, value);

            isSort = false;
            _index++;
        }

        public Dictionary<T, double> GetSortedDictionary()
        {
            if (!isSort)
            {
                MinHeapDouble.heapSort(mhd);
                isSort = true;
            }

            int[] indices = mhd.getIndices();
            double[] values = mhd.getValues();

            Dictionary<T, double> dict = new Dictionary<T, double>();
            for (int i = 0; i < heapSize; i++)
                if (indices[i] != -1)
                    dict.Add(_itemList[indices[i]], factor * values[i]);

            return dict;
        }

        public List<T> GetTopIndices()
        {
            if (!isSort)
            {
                MinHeapDouble.heapSort(mhd);
                isSort = true;
            }

            int[] indices = mhd.getIndices();
            var itemList = new List<T>();

            Dictionary<int, double> dict = new Dictionary<int, double>();
            for (int i = 0; i < heapSize; i++)
                if (indices[i] != -1)
                    itemList.Add(_itemList[indices[i]]);

            return itemList;
        }
    }


    public class HeapSortDouble
    {
        int heapSize;
        //bool bDescending;
        int factor = 1;

        MinHeapDouble mhd;
        bool isSort = false;

        public HeapSortDouble(int heapSize, bool bDescending = true)
        {
            if(heapSize == 0)
                throw new ArgumentException();
            this.heapSize = heapSize;
            //this.bDescending = bDescending;
            if (!bDescending)
                factor = -1;

            mhd = new MinHeapDouble(heapSize);
            for (int i = 0; i < heapSize; i++)
                mhd.insert(-1, double.MinValue);
        }

        public void Insert(int key, double value)
        {
            value *= factor;

            if (value > mhd.min() || (value == mhd.min() && key < mhd.getIndices()[0]))
                mhd.changeMin(key, value);

            isSort = false;
        }

        public Dictionary<int, double> GetSortedDictionary()
        {
            if (!isSort)
            {
                MinHeapDouble.heapSort(mhd);
                isSort = true;
            }

            int[] indices = mhd.getIndices();
            double[] values = mhd.getValues();

            Dictionary<int, double> dict = new Dictionary<int, double>();
            for (int i = 0; i < heapSize; i++)
                if (indices[i] != -1)
                    dict.Add(indices[i], factor * values[i]);

            return dict;
        }

        public List<int> GetTopIndices()
        {
            if (!isSort)
            {
                MinHeapDouble.heapSort(mhd);
                isSort = true;
            }

            int[] indices = mhd.getIndices();
            var indexList = new List<int>();

            Dictionary<int, double> dict = new Dictionary<int, double>();
            for (int i = 0; i < heapSize; i++)
                if (indices[i] != -1)
                    indexList.Add(indices[i]);

            return indexList;
        }
    }

    class MinHeapDouble
    {
        public int[] indices = null;
        public double[] values = null;
        public int size = 0;
        public int capacity = 0;

        public MinHeapDouble(int[] indices, double[] values, int cap)
        {
            if (indices.Length != values.Length)
            {
                Console.WriteLine("Dimensions does not match!");
            }
            this.indices = (int[])indices.Clone();
            this.values = (double[])values.Clone();
            size = indices.Length;
            capacity = cap;
        }

        public MinHeapDouble(int cap)
        {
            this.indices = new int[cap];
            this.values = new double[cap];
            capacity = cap;
            size = 0;
        }

        public int heapSize()
        {
            return size;
        }

        public bool isLeaf(int pos)
        {
            return (pos >= (size / 2)) && (pos < size);
        }

        public int leftChild(int pos)
        {
            if (pos > size / 2)
            {
                Console.WriteLine("Position has no left child");
            }
            return 2 * pos + 1;
        }

        public int rightChild(int pos)
        {
            if (pos > (size - 1) / 2)
            {
                Console.WriteLine("Position has no right child");
            }
            return 2 * pos + 2;
        }

        public int parent(int pos)
        { // Return position for parent
            if (pos < 0)
            {
                Console.WriteLine("Position has no parent");
            }
            return ((pos - 1) / 2);
        }

        public void swap(int index1, int index2)
        {
            int index = indices[index1];
            double value = values[index1];
            indices[index1] = indices[index2];
            values[index1] = values[index2];
            indices[index2] = index;
            values[index2] = value;
        }

        public void insert(int index, double value)
        {
            if (size >= capacity)
            {
                Console.WriteLine("Get heap max capacity!");
                return;
            }
            int curr = size++;
            if (size > indices.Length)
            {
                int[] newindices = new int[size];
                double[] newvalues = new double[size];
                for (int i = 0; i < size - 1; ++i)
                {
                    newindices[i] = indices[i];
                    newvalues[i] = values[i];
                }
                indices = newindices;
                values = newvalues;
            }
            indices[size - 1] = index;
            values[size - 1] = value;

            while (curr != 0 && values[curr] < values[parent(curr)])
            {
                swap(curr, parent(curr));
                curr = parent(curr);
            }
        }

        public void buildheap()
        {
            for (int i = (size / 2) - 1; i >= 0; --i)
            {
                heapify(i);
            }
        }

        private void heapify(int pos)
        {
            if (pos > size)
                return;
            int left = (pos + 1) * 2 - 1;
            int right = (pos + 1) * 2;

            int minidx = pos;
            if (left < size && cmp(left, pos) < 0)
                minidx = left;
            if (right < size && cmp(right, pos) <= 0 && cmp(right, left) <= 0)
                minidx = right;
            if (minidx != pos)
            {
                // swap them and recurse on the subtree rooted at minidx
                swap(minidx, pos);
                heapify(minidx);
            }
        }

        protected int cmp(int pos1, int pos2)
        {
            if (values[pos1] > values[pos2])
                return 1;
            else if (values[pos1] < values[pos2])
                return -1;
            else if (indices[pos1] < indices[pos2])
                return 1;
            else if (indices[pos1] > indices[pos2])
                return -1;
            else return 0;
        }

        public void changeMin(int index, double value)
        {
            if (size < 0)
            {
                Console.WriteLine("Changing: Empty heap");
            }
            indices[0] = index;
            values[0] = value;
            heapify(0);   // Put new heap root val in correct place
        }

        public int removeMin()
        {
            if (size < 0)
            {
                Console.WriteLine("Removing: Empty heap");
            }
            --size;
            swap(0, size); // Swap maximum with last value
            if (size != 0)      // Not on last element
                heapify(0);   // Put new heap root val in correct place
            return indices[size];
        }

        public double min()
        {
            return values[0];
        }

        public int[] getIndices()
        {
            return indices;
        }

        public double[] getValues()
        {
            return values;
        }

        public static int[] heapSort(MinHeapDouble minHeap)
        {
            int[] newIndices = new int[minHeap.getIndices().Length];
            int orgsize = minHeap.size;
            for (int i = 0; i < orgsize; ++i)
            { // Now sort
                newIndices[i] = minHeap.removeMin();   // removeMax places max value at end of heap
            }
            return newIndices;
        }
    }


    #endregion

    public class ReverseIntComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return -x.CompareTo(y);
        }
    }

    public class SortUtils
    {
        public static void SortNumberStrings(List<string> strs)
        {
            List<double> numbers = new List<double>();
            foreach (var str in strs)
            {
                numbers.Add(Double.Parse(str));
            }
            numbers.Sort();

            strs.Clear();
            foreach (var number in numbers)
            {
                strs.Add(number.ToString());
            }
        }

        public static Dictionary<T1, T2> GetReOrderedDictionary<T1, T2>(Dictionary<T1, T2> dict, IEnumerable<T1> orderedKeys)
        {
            var dict2 = new Dictionary<T1, T2>();
            foreach (var orderedKey in orderedKeys)
            {
                dict2.Add(orderedKey, dict[orderedKey]);
            }
            return dict2;
        }

        public static Dictionary<T, int> GetRankDictionary<T>(IEnumerable<T> elements, Func<T, double> scoreFunc)
        {
            Dictionary<T, double> scoreDict = new Dictionary<T, double>();
            foreach (var ele in elements)
                scoreDict.Add(ele, scoreFunc(ele));
            scoreDict = EnsureSortedByValue(scoreDict);

            Dictionary<T, int> rankDict = new Dictionary<T,int>();
            int rank = 0;
            foreach (var kvp in scoreDict)
                rankDict.Add(kvp.Key, rank++);
            return rankDict;
        }

        public static List<T> MergeSortedByImportanceEnumerables<T>(IEnumerable<IEnumerable<T>> lists)
        {
            //Calculate score (sum(listLen - itemIndex))
            var scoreDict = new Dictionary<T, int>();
            foreach (var list in lists)
            {
                var listLen = list.Count();
                var itemIndex = 0;
                foreach (var item in list)
                {
                    if (!scoreDict.ContainsKey(item))
                        scoreDict.Add(item, 0);
                    //Score = sum(listLen - itemIndex)
                    scoreDict[item] += listLen - itemIndex;
                    itemIndex++;
                }
            }

            //sort 
            var mergedList = new List<T>();
            foreach (var kvp in scoreDict.OrderByDescending(kvp => kvp.Value))
                mergedList.Add(kvp.Key);
            return mergedList;
        }

        public static void GetSortedElementsChanges<T>(IEnumerable<T> sortedList1, IEnumerable<T> sortedList2, out List<T> deletedElements, out List<T> addedElements) where T : IComparable
        {
            deletedElements = new List<T>();
            addedElements = new List<T>();

            //normally
            var enum1 = sortedList1.GetEnumerator();
            var enum2 = sortedList2.GetEnumerator();
            bool hasNext1 = enum1.MoveNext(), hasNext2 = enum2.MoveNext();
            while (hasNext1 && hasNext2)
            {
                var matchInfo1 = enum1.Current;
                var matchInfo2 = enum2.Current;

                var compare = matchInfo1.CompareTo(matchInfo2);
                //the two match infos are the same
                if (compare == 0)
                {
                    hasNext1 = enum1.MoveNext();
                    hasNext2 = enum2.MoveNext();
                    continue;
                }
                
                if (compare < 0)
                {
                    //prev node should be deleted
                    deletedElements.Add(matchInfo1);
                    hasNext1 = enum1.MoveNext();
                    continue;
                }
                else
                {
                    //new node should be added
                    addedElements.Add(matchInfo2);
                    hasNext2 = enum2.MoveNext();
                }
            }
            //deal with the tails
            while (hasNext1)
            {
                deletedElements.Add(enum1.Current);
                hasNext1 = enum1.MoveNext();
            }
            while (hasNext2)
            {
                addedElements.Add(enum2.Current);
                hasNext2 = enum2.MoveNext();
            }
        }

        public static List<T> EnsureSorted<T>(IEnumerable<T> listOrg) where T : IComparable
        {
            List<T> list = (listOrg is List<T>) ? (listOrg as List<T>) : new List<T>(listOrg);

            if (list.Count() <= 1)
                return list;

            bool isSorted = true;
            T prevValue = list.First();
            bool isTestKey = false;
            foreach (var value in list)
            {
                if (isTestKey)
                {
                    if (prevValue.CompareTo(value) > 0)
                    {
                        isSorted = false;
                        break;
                    }
                }
                else
                    isTestKey = true;
                prevValue = value;
            }

            if (!isSorted)
                list.Sort();

            return list;
        }

        public static bool IsSortedByKey<T1, T2>(Dictionary<T1, T2> dict) where T1 : IComparable
        {
            bool isSorted = true;
            T1 prevKey = Activator.CreateInstance<T1>();
            bool isTestKey = false;
            foreach (var kvp in dict)
            {
                if (isTestKey)
                {
                    if (prevKey.CompareTo(kvp.Key) > 0)
                    {
                        isSorted = false;
                        break;
                    }
                }
                else
                    isTestKey = true;
                prevKey = kvp.Key;
            }

            return isSorted;
        }

        public static Dictionary<T1, T2> EnsureSortedByKey<T1, T2>(Dictionary<T1, T2> dict) where T1: IComparable
        {
            if (dict.Count == 0)
                return dict;

            bool isSorted = true;
            T1 prevKey = dict.First().Key; //Activator.CreateInstance<T1>();
            bool isTestKey = false;
            foreach (var kvp in dict)
            {
                if (isTestKey)
                {
                    if (prevKey.CompareTo(kvp.Key) > 0)
                    {
                        isSorted = false;
                        break;
                    }
                }
                else
                    isTestKey = true;
                prevKey = kvp.Key;
            }

            if (!isSorted)
                dict = dict.OrderBy(kvp => kvp.Key).ToDictionary(x => x.Key, x => x.Value);
            return dict;
        }

        public static bool IsMatSortedByKey<T1, T2, T3>(Dictionary<T1, Dictionary<T2, T3>> dict) where T1 : IComparable
            where T2 : IComparable
        {
            if (IsSortedByKey(dict))
            {
                bool isSorted = true;
                foreach (var kvp in dict)
                {
                    if (!IsSortedByKey(kvp.Value))
                    {
                        isSorted = false;
                        break;
                    }
                }
                return isSorted;
            }
            return false;
        }

        public static Dictionary<T1, Dictionary<T2,T3>> EnsureMatSortedByKey<T1,T2,T3>(Dictionary<T1,Dictionary<T2,T3>> dict) where T1: IComparable where T2:IComparable
        {
            if (IsMatSortedByKey(dict))
                return dict;

            dict = EnsureSortedByKey(dict);
            Dictionary<T1, Dictionary<T2, T3>> dict2 = new Dictionary<T1, Dictionary<T2, T3>>();
            foreach (var kvp in dict)
            {
                dict2.Add(kvp.Key, EnsureSortedByKey(kvp.Value));
            }
            return dict2;
        }

        public static Dictionary<T1, T2> EnsureSortedByValue<T1, T2>(Dictionary<T1, T2> dict, bool isAscending = false) where T2: IComparable
        {
            bool isSorted = true;
            T2 prevValue = Activator.CreateInstance<T2>();
            bool isTest = false;
            int factor = isAscending ? 1 : -1;
            foreach (var kvp in dict)
            {
                if (isTest)
                {
                    if (prevValue.CompareTo(kvp.Value) * factor > 0)
                    {
                        isSorted = false;
                        break;
                    }
                }
                else
                    isTest = true;
                prevValue = kvp.Value;
            }

            if (!isSorted)
                if(isAscending)
                    dict = dict.OrderBy(kvp => kvp.Value).ToDictionary(x => x.Key, x => x.Value);
                else
                    dict = dict.OrderByDescending(kvp => kvp.Value).ToDictionary(x => x.Key, x => x.Value);
            return dict;
        }
    }
}
