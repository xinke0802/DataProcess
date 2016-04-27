using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.Utils
{
    public class WeightedSum
    {
        private double sum;
        private double cnt;

        public void AddNumber(double num, double numCnt)
        {
            sum += num*numCnt;
            cnt += numCnt;
        }

        public double GetAverge()
        {
            return cnt == 0 ? 0 : (sum/cnt);
        }

        public double GetSum()
        {
            return sum;
        }

        public double GetCount()
        {
            return cnt;
        }
    }


    public class DoubleStatistics
    {
        double min = double.MaxValue;
        double max = double.MinValue;
        double sum = 0;
        double sumsquare = 0;
        double cnt = 0;

        public void AddNumber(double num)
        {
            if (num > max)
                max = num;
            if (num < min)
                min = num;
            sum += num;
            sumsquare += num * num;
            cnt++;
        }

        public double GetMin()
        {
            return min;
        }

        public double GetMax()
        {
            return max;
        }

        public double GetAverage()
        {
            return sum / cnt;
        }

        public double GetTotalCount()
        {
            return cnt;
        }

        public double GetSum()
        {
            return sum;
        }

        public double GetStd()
        {
            return Math.Sqrt(sumsquare / cnt - sum * sum / cnt / cnt);
        }

        public string ToString(string name = null)
        {
            string str = "";
            str += String.Format("------------" + name + " Statistics" + "------------\n");
            str += String.Format("Min:{0}\n", GetMin());
            str += String.Format("Max:{0}\n", GetMax());
            str += String.Format("Avg:{0}\n", GetAverage());
            str += String.Format("Std:{0}\n", GetStd());
            str += String.Format("Cnt:{0}\n", GetTotalCount());
            //str += String.Format("Cnt:{0}\n", cnt);

            return str;
        }

        public string ToStringMiddle()
        {
            string str = "";
            //str += String.Format("Min:{0}\t", GetMin());
            //str += String.Format("Max:{0}\t", GetMax());
            //str += String.Format("Avg:{0}\t", GetAverage());
            //str += String.Format("Std:{0}\t", GetStd());
            str += String.Format("{0}+-{1} [{2},{3}]", Math.Round(GetAverage(), 4), Math.Round(GetStd(), 4),
                Math.Round(GetMin(), 4), Math.Round(GetMax(), 4));

            return str;
        }

        public string ToStringShort()
        {
            string str = "";
            str += String.Format("{0}+-{1}", Math.Round(GetAverage(), 4), Math.Round(GetStd(), 4));
            return str;
        }
    }

    public class DoubleHistogram
    {
        private int[] _dataCntInBin = null;
        private double[] _binStarts = null;
        private double _start;
        private int _binCnt;
        private double _binSize;
        //private double[] _binEnds = null;
        public DoubleHistogram(IEnumerable<double> data, double binSize)
        {
            CalculateHistogram(data, binSize);
        }

        public DoubleHistogram(IEnumerable<double> data, int binCnt)
        {
            if (data.Min() == data.Max())
            {
                _binSize = 1;
                _start = data.Min() - 0.5;
                _binCnt = binCnt;
                _binStarts = Util.GetDoubleArray(_start, _start + binCnt - 0.5, 1);
                _dataCntInBin = new int[binCnt];
                _dataCntInBin[0] = data.Count();
            }
            else
            {
                double appBinSize = (data.Max() - data.Min()) / binCnt;
                _binCnt = binCnt;
                CalculateHistogram(data, (data.Max() - data.Min()) / binCnt + 1e-1 * appBinSize);
            }
        }

        /// <summary>
        /// [binStart binEnd)
        /// </summary>
        /// <returns></returns>
        private void CalculateHistogram(IEnumerable<double> data, double binSize)
        {
            _start = data.Min();
            _binSize = binSize;
            double end = data.Max();
            //if (_binCnt == 0)
            {
                _binCnt = (int)Math.Floor((end - _start) / binSize) + 1;
                //if (_binCnt <= 0)
                //{
                //    _binCnt = 1;
                //}
            }
            //if (_binSize == 0)
            //{
            //    _binSize = 1;
            //}

            int[] cnt = new int[_binCnt];
            double[] binStarts = new double[_binCnt];
            //double[] binEnds = new double[binCnt];

            for (int i = 0; i < _binCnt; i++)
            {
                binStarts[i] = _start + binSize * i;
                //binEnds[i] = start + binSize * (i + 1);
            }
            foreach (var num in data)
            {
                //var index = (int) Math.Floor((num - _start)/binSize);
                //if (index >= cnt.Length || index < 0)
                //    throw new ArgumentException();
                cnt[(int)Math.Floor((num - _start) / binSize)]++;
            }

            _dataCntInBin = cnt;
            _binStarts = binStarts;
            //_binEnds = binEnds;
        }

        public int GetBinIndex(double val)
        {
            return (int)Math.Floor((val - _start) / _binSize);
        }

        public int[] GetDataCountInBin()
        {
            return _dataCntInBin;
        }

        public void PrintToFile(string fileName)
        {
            var sw = new StreamWriter(fileName);
            PrintToFile(sw);
            sw.Flush();
            sw.Close();
        }

        public void PrintToFile(StreamWriter sw)
        {

            for (int iBin = 0; iBin < _dataCntInBin.Length; iBin++)
            {
                sw.Write(_binStarts[iBin] + ",");
                sw.Write(_dataCntInBin[iBin] + "\n");
            }
            sw.Flush();
        }

    }

    public class DiscreteEntropy<T>
    {
        protected double _logBase;
        protected Dictionary<T, int> _countDictionary;
        public DiscreteEntropy(IEnumerable<T> items, double logBase = 2)
        {
            _logBase = logBase;

            var counter = new Counter<T>();
            foreach (var item in items)
            {
                counter.Add(item);
            }
            _countDictionary = counter.GetCountDictionary();
        }

        public DiscreteEntropy(Dictionary<T, int> countDictionary, double logBase = 2)
        {
            _countDictionary = countDictionary;
            _logBase = logBase;
        }

        public double GetEntropy()
        {
            double totalCount = _countDictionary.Values.Sum();
            double entropy = 0;
            foreach (var kvp in _countDictionary)
            {
                double prob = kvp.Value/totalCount;
                if (prob == 0)
                    continue;
                entropy -= prob*Math.Log(prob);
            }
            entropy /= Math.Log(_logBase);
            return entropy;
        }
    }

    public class ContinuousEntropy
    {
        private IEnumerable<double> _items;
        private int _binCnt;
        private double _logBase;
        public ContinuousEntropy(IEnumerable<double> items, int binCnt = 100, double logBase = 2)
        {
            _items = items;
            _binCnt = binCnt;
            _logBase = logBase;
        }

        public double GetEntropy()
        {
            DoubleHistogram histogram = new DoubleHistogram(_items, _binCnt);
            var dataCntInBin = histogram.GetDataCountInBin();
            return new DiscreteEntropy<int>(Util.GetDictionary(dataCntInBin), _logBase).GetEntropy();
        }
    }

    public class MultipleContinuousEntropy
    {
        private IEnumerable<IEnumerable<double>> _xlist;
        private IEnumerable<int> _binCntList;
        private double _logBase;
        private int _xLen;
        public MultipleContinuousEntropy(IEnumerable<IEnumerable<double>> xlist, IEnumerable<int> binCntList, double logBase = 2)
        {
            if(xlist.Count() != binCntList.Count())
                throw new ArgumentException();
            if(xlist.Count() < 2)
                throw new ArgumentException();
            int len = xlist.ElementAt(0).Count();
            foreach (var x in xlist)
            {
                if (x.Count() != len)
                    throw new ArgumentException();
            }
            _xLen = len;

            _xlist = xlist;
            _binCntList = binCntList;
            _logBase = logBase;
        }

        public double GetEntropy()
        {
            var xlistLen = _xlist.Count();
            DoubleHistogram[] histograms = new DoubleHistogram[xlistLen];
            for (int i = 0; i < xlistLen; i++)
            {
                histograms[i] = new DoubleHistogram(_xlist.ElementAt(i), _binCntList.ElementAt(i));
            }

            if (xlistLen == 2)
            {
                Dictionary<Tuple<int, int>, int> countDict = new Dictionary<Tuple<int, int>, int>();
                for (int j = 0; j < _xLen; j++)
                {
                    var tuple = new Tuple<int, int>(histograms[0].GetBinIndex(_xlist.ElementAt(0).ElementAt(j)), histograms[1].GetBinIndex(_xlist.ElementAt(1).ElementAt(j)));
                    if (!countDict.ContainsKey(tuple))
                        countDict.Add(tuple, 1);
                    else
                        countDict[tuple]++;
                }
                return new DiscreteEntropy<Tuple<int,int>>(countDict, _logBase).GetEntropy();
                
            }
            else if (xlistLen == 3)
            {
                Dictionary<Tuple<int, int, int>, int> countDict = new Dictionary<Tuple<int, int, int>, int>();
                for (int j = 0; j < _xLen; j++)
                {
                    var tuple = new Tuple<int, int, int>(histograms[0].GetBinIndex(_xlist.ElementAt(0).ElementAt(j)), histograms[1].GetBinIndex(_xlist.ElementAt(1).ElementAt(j)),
                        histograms[2].GetBinIndex(_xlist.ElementAt(2).ElementAt(j)));
                    if (!countDict.ContainsKey(tuple))
                        countDict.Add(tuple, 1);
                    else
                        countDict[tuple]++;
                }
                return new DiscreteEntropy<Tuple<int, int, int>>(countDict, _logBase).GetEntropy();
            }
            else
                throw new NotImplementedException();

        }
    }

    public class DiscreteMutualInformation<T1, T2>
    {
        private IEnumerable<T1> _items1;
        private IEnumerable<T2> _items2;
        private double _logBase;
        public DiscreteMutualInformation(IEnumerable<T1> items1, IEnumerable<T2> items2, double logBase = 2)
        {
            if(items1.Count() != items2.Count())
                throw new ArgumentException();

            _items1 = items1;
            _items2 = items2;
            _logBase = logBase;
        }

        public double GetMutualInformation()
        {
            Counter<Tuple<T1, T2>> counter = new Counter<Tuple<T1, T2>>();
            Counter<T1> counter1 = new Counter<T1>();
            Counter<T2> counter2 = new Counter<T2>();

            var enum1 = _items1.GetEnumerator();
            var enum2 = _items2.GetEnumerator();
            while (enum1.MoveNext() && enum2.MoveNext())
            {
                counter1.Add(enum1.Current);
                counter2.Add(enum2.Current);
                counter.Add(Tuple.Create(enum1.Current, enum2.Current));
            }

            var dict1 = counter1.GetCountDictionary();
            var dict2 = counter2.GetCountDictionary();
            var dict = counter.GetCountDictionary();

            double mutualInfo = 0;
            double count = _items1.Count();
            foreach (var kvp in dict)
            {
                var x = kvp.Key.Item1;
                var y = kvp.Key.Item2;
                double pxy = kvp.Value/count;
                double px = dict1[x]/count;
                double py = dict2[y]/count;
                mutualInfo += pxy*Math.Log(pxy/px/py);
            }
            mutualInfo /= Math.Log(_logBase);

            ///Another method: H(X)+H(Y)-H(X,Y)
            //List<Tuple<T1,T2>> items = new SupportClass.EquatableList<Tuple<T1, T2>>();
            //enum1.Reset();
            //enum2.Reset();
            //while (enum1.MoveNext() && enum2.MoveNext())
            //{
            //    items.Add(Tuple.Create(enum1.Current, enum2.Current));
            //}
            //var entropy1 = new DiscreteEntropy<T1>(_items1, _logBase).GetEntropy();
            //var entropy2 = new DiscreteEntropy<T2>(_items2, _logBase).GetEntropy();
            //var entropy = new DiscreteEntropy<Tuple<T1, T2>>(items, _logBase).GetEntropy();

            //if(Math.Abs(entropy1+entropy2-entropy-mutualInfo)>1e-3)
            //    throw new ArgumentException();

            ///Another method: H(X)+H(Y)-H(X,Y)
            //entropy1 = new DiscreteEntropy<T1>(counter1.GetCountDictionary(), _logBase).GetEntropy();
            //entropy2 = new DiscreteEntropy<T2>(counter2.GetCountDictionary(), _logBase).GetEntropy();
            //entropy = new DiscreteEntropy<Tuple<T1, T2>>(counter.GetCountDictionary(), _logBase).GetEntropy();
            //if (Math.Abs(entropy1 + entropy2 - entropy - mutualInfo) > 1e-3)
            //    throw new ArgumentException();

            return mutualInfo;
        }
    }

    public class ContinousMutualInformation
    {
        private IEnumerable<double> _items1;
        private IEnumerable<double> _items2;
        private int _binCnt1, _binCnt2;
        private double _logBase;

        /// <summary>
        /// Make sure binCnt1 = number of different values for discrete data (e.g. set binCnt = 2 for binary data)
        /// </summary>
        public ContinousMutualInformation(IEnumerable<double> items1, IEnumerable<double> items2, int binCnt1 = 100,
            int binCnt2 = 100, double logBase = 2)
        {
            if (items1.Count() != items2.Count())
                throw new ArgumentException();

            _items1 = items1;
            _items2 = items2;
            _binCnt1 = binCnt1;
            _binCnt2 = binCnt2;
            _logBase = logBase;
        }

        public double GetMutualInformation()
        {
            double start1 = _items1.Min();
            var binSize1 = (_items1.Max() - start1)/_binCnt1 + 1e-3;
            double start2 = _items2.Min();
            var binSize2 = (_items2.Max() - start2) / _binCnt2 + 1e-3;
            Func<double, int> transformFunc1 = (val) => (int)Math.Floor((val - start1) / binSize1);
            Func<double, int> transformFunc2 = (val) => (int)Math.Floor((val - start2) / binSize2);

            List<int> discreteItems1 = new List<int>();
            foreach (var item1 in _items1)
            {
                discreteItems1.Add(transformFunc1(item1));
            }

            List<int> discreteItems2 = new List<int>();
            foreach (var item2 in _items2)
            {
                discreteItems2.Add(transformFunc2(item2));
            }

            return new DiscreteMutualInformation<int, int>(discreteItems1, discreteItems2, _logBase).GetMutualInformation();
        }
    }

    public class StatisticsUtils
    {

    }
}
