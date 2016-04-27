using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Lava.Visual;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using System.Numerics;

namespace DataProcess.Utils
{
    public class Correlation
    {
        public static double Pearson(IEnumerable<double> values1, IEnumerable<double> values2)
        {
            return MathNet.Numerics.Statistics.Correlation.Pearson(values1, values2);
        }
    }

    public class Maths
    {
        #region equation
        /// <summary>
        /// Solve ax^3+bx^2+cx+d=0 for x.
        /// Calculation of the 3 roots of a cubic equation according to
        /// http://en.wikipedia.org/wiki/Cubic_function#General_formula_for_roots
        /// Using the complex struct from System.Numerics
        /// Visual Studio 2010, .NET version 4.0
        /// </summary>
        /// <param name="a">real coefficient of x to the 3th power</param>
        /// <param name="b">real coefficient of x to the 2nd power</param>
        /// <param name="c">real coefficient of x to the 1th power</param>
        /// <param name="d">real coefficient of x to the zeroth power</param>
        /// <returns>A list of 3 complex numbers</returns>
        public static List<Complex> SolveCubic(double a, double b, double c, double d)
        {
            const int NRoots = 3;
            double SquareRootof3 = Math.Sqrt(3);
            // the 3 cubic roots of 1
            List<Complex> CubicUnity = new List<Complex>(NRoots) { new Complex(1, 0), new Complex(-0.5, -SquareRootof3 / 2.0), new Complex(-0.5, SquareRootof3 / 2.0) };
            // intermediate calculations
            double DELTA = 18 * a * b * c * d - 4 * b * b * b * d + b * b * c * c - 4 * a * c * c * c - 27 * a * a * d * d;
            double DELTA0 = b * b - 3 * a * c;
            double DELTA1 = 2 * b * b * b - 9 * a * b * c + 27 * a * a * d;
            Complex DELTA2 = -27 * a * a * DELTA;
            Complex C = Complex.Pow((DELTA1 + Complex.Pow(DELTA2, 0.5)) / 2, 1 / 3.0); //Phew...
            List<Complex> R = new List<Complex>(NRoots);
            for (int i = 0; i < NRoots; i++)
            {
                Complex M = CubicUnity[i] * C;
                Complex Root = -1.0 / (3 * a) * (b + M + DELTA0 / M);
                R.Add(Root);
            }
            return R;
        }
        #endregion


        #region vector
        public static int Factorial(int value)
        {
            int res = 1;
            for (; value >= 1; value--)
            {
                res *= value;
            }
            return res;
        }

        public static int[] Copy(int[] v)
        {
            var v2 = new int[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                v2[i] = v[i];
            }
            return v2;
        }

        public static bool[] Copy(bool[] v)
        {
            var v2 = new bool[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                v2[i] = v[i];
            }
            return v2;
        }

        public static double[,] Copy(double[,] mat)
        {
            double[,] mat2 = new double[mat.GetLength(0), mat.GetLength(1)];
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    mat2[i, j] = mat[i, j];
                }
            }
            return mat2;
        }

        public static Dictionary<int, double> GetVectorMean(Dictionary<int, double> v0, Dictionary<int, double> v1)
        {
            Dictionary<int, double> v = new Dictionary<int, double>();
            foreach (var kvp in v0)
            {
                double weight1;
                if (!v1.TryGetValue(kvp.Key, out weight1))
                    weight1 = 0;
                v.Add(kvp.Key, (kvp.Value + weight1) / 2);
            }

            foreach (var kvp in v1)
            {
                if (!v0.ContainsKey(kvp.Key))
                    v.Add(kvp.Key, kvp.Value / 2);
            }
            return v;
        }


        //public static double GetVectorNorm1(Dictionary<int, double> v0, Dictionary<int, double> v1)
        //{
        //    double distance = 0;

        //    foreach (var kvp in v0)
        //    {
        //        int word0 = kvp.Key;
        //        double weight0 = kvp.Value;
        //        double weight1;
        //        if (!v1.TryGetValue(word0, out weight1))
        //            weight1 = 0;
        //        distance += Math.Abs(weight0 - weight1);
        //    }

        //    foreach (var kvp in v1)
        //    {
        //        int word1 = kvp.Key;
        //        double weight1 = kvp.Value;
        //        if (!v0.ContainsKey(word1))
        //            distance += Math.Abs(weight1);
        //    }

        //    return distance;
        //}

        //public static double GetVectorNorm2(Dictionary<int, double> v0, Dictionary<int, double> v1)
        //{
        //    double distance = 0;

        //    foreach (var kvp in v0)
        //    {
        //        int word0 = kvp.Key;
        //        double weight0 = kvp.Value;
        //        double weight1;
        //        if (!v1.TryGetValue(word0, out weight1))
        //            weight1 = 0;
        //        distance += (weight0 - weight1) * (weight0 - weight1);
        //    }

        //    foreach (var kvp in v1)
        //    {
        //        int word1 = kvp.Key;
        //        double weight1 = kvp.Value;
        //        if (!v0.ContainsKey(word1))
        //            distance += weight1 * weight1;
        //    }

        //    return Math.Sqrt(distance);
        //}

        public static double GetVectorLength(IDictionary<int, double> v)
        {
            double squareLen = 0;
            foreach (var val in v.Values)
                squareLen += val * val;
            return Math.Sqrt(squareLen);
        }

        public static double GetVectorLength(double[] v)
        {
            double squareLen = 0;
            foreach (var val in v)
                squareLen += val * val;
            return Math.Sqrt(squareLen);
        }

        #endregion

        #region ordered vector

        public static double GetVectorNormSQ(Dictionary<int, double> v)
        {
            double sum = 0;
            foreach (var kvp in v)
                sum += kvp.Value * kvp.Value;
            return sum;
        }

        public static bool IsOrderedVectorSame(Dictionary<int, double> v0, Dictionary<int, double> v1)
        {
            if (v0 == null || v1 == null)
            {
                if (v0 == null && v1 == null)
                    return true;
                else
                    return false;
            }

            if (v0.Count != v1.Count)
                return false;

            var enum0 = v0.GetEnumerator();
            var enum1 = v1.GetEnumerator();
            while (enum0.MoveNext())
            {
                enum1.MoveNext();
                var kvp0 = enum0.Current;
                var kvp1 = enum1.Current;

                if (kvp0.Key != kvp1.Key || kvp0.Value != kvp1.Value)
                    return false;
            }

            return true;
        }

        public static Dictionary<int, double> GetOrderedVectorMean(Dictionary<int, double> v0, Dictionary<int, double> v1)
        {
            var dict = GetOrderedVectorAddition(v0, v1);
            var dict2 = new Dictionary<int, double>();
            foreach (var kvp in dict)
                dict2.Add(kvp.Key, kvp.Value / 2);
            return dict2;
        }

        public static Dictionary<int, double> GetOrderedVectorAddition(Dictionary<int, double> v0, Dictionary<int, double> v1)
        {
            if (v0.Count == 0)
                return new Dictionary<int, double>(v1);
            if (v1.Count == 0)
                return new Dictionary<int, double>(v0);

            Dictionary<int, double> v = new Dictionary<int, double>();

            var dictEnum0 = v0.GetEnumerator();
            var dictEnum1 = v1.GetEnumerator();

            bool bAddTail0 = true;
            bool bAddTail1 = true;

            dictEnum0.MoveNext();
            dictEnum1.MoveNext();
            while (true)
            {
                if (dictEnum0.Current.Key == dictEnum1.Current.Key)
                {
                    v.Add(dictEnum0.Current.Key, (dictEnum0.Current.Value + dictEnum1.Current.Value));
                    if (!dictEnum0.MoveNext())
                    {
                        bAddTail0 = false;
                        if (!dictEnum1.MoveNext())
                            bAddTail1 = false;
                        break;
                    }
                    if (!dictEnum1.MoveNext())
                    {
                        bAddTail1 = false;
                        break;
                    }
                }
                else if (dictEnum0.Current.Key > dictEnum1.Current.Key)
                {
                    v.Add(dictEnum1.Current.Key, dictEnum1.Current.Value);
                    if (!dictEnum1.MoveNext())
                    {
                        bAddTail1 = false;
                        break;
                    }
                }
                else
                {
                    v.Add(dictEnum0.Current.Key, dictEnum0.Current.Value);
                    if (!dictEnum0.MoveNext())
                    {
                        bAddTail0 = false;
                        break;
                    }
                }
            }

            if (bAddTail0)
            {
                do
                {
                    v.Add(dictEnum0.Current.Key, dictEnum0.Current.Value);
                }
                while (dictEnum0.MoveNext());
            }

            if (bAddTail1)
            {
                do
                {
                    v.Add(dictEnum1.Current.Key, dictEnum1.Current.Value);
                }
                while (dictEnum1.MoveNext());
            }

            return v;
        }

        public static Dictionary<int, double> GetOrderedVectorAddition(IEnumerable<Dictionary<int, double>> orderedVectors)
        {
            Dictionary<int,double> sum = new Dictionary<int, double>();
            foreach (var vector in orderedVectors)
                sum = GetOrderedVectorAddition(vector, sum);
            return sum;
        }

        public static Dictionary<int, double> GetOrderedVectorSubtraction(Dictionary<int, double> v0, Dictionary<int, double> v1)
        {
            Dictionary<int, double> v2 = GetVectorMultiply(v1, -1.0);
            return GetOrderedVectorAddition(v0, v2);
        }

        public static Dictionary<int, double> GetVectorMatrixMultiplication(Dictionary<int, double> v, Dictionary<int, Dictionary<int, double>> m0)
        {
            Dictionary<int, Dictionary<int, double>> m = Util.GetInverse2DDictionary(m0);
            Dictionary<int, double> res = new Dictionary<int, double>();
            foreach (var kvp in m)
            {
                double t = GetOrderedVectorDotProduct(v, kvp.Value);
                if (t != 0)
                {
                    res.Add(kvp.Key, t);
                }
            }
            return res;
        }

        public static Dictionary<int, double> GetMatrixVectorMultiplication(Dictionary<int, Dictionary<int, double>> m, Dictionary<int, double> v)
        {
            Dictionary<int, double> res = new Dictionary<int, double>();
            foreach (var kvp in m)
            {
                double t = GetOrderedVectorDotProduct(kvp.Value, v);
                if (t != 0)
                {
                    res.Add(kvp.Key, t);
                }
            }

            return res;
        }

        public static Dictionary<int, double> GetInversedMatrixVectorMultiplication(Dictionary<int, Dictionary<int, double>> m0, Dictionary<int, double> v)
        {
            Dictionary<int, double> res = new Dictionary<int, double>();
            foreach (var kvp in v)
            {
                int key = kvp.Key;
                if (m0.ContainsKey(key) == false)
                    continue;
                foreach (var kvp2 in m0[key])
                {
                    int row = kvp2.Key;
                    if (res.ContainsKey(row) == false)
                        res.Add(row, 0);
                    res[row] += kvp.Value * kvp2.Value;
                }
            }
            res = SortUtils.EnsureSortedByKey(res);
            return res;
        }

        public static Dictionary<int, Dictionary<int, double>> GetVectorVectorMultiplication(Dictionary<int, double> v0, Dictionary<int, double> v1)
        {
            Dictionary<int, Dictionary<int, double>> res = new Dictionary<int, Dictionary<int, double>>();
            foreach (var kvp0 in v0)
            {
                int row = kvp0.Key;
                res.Add(row, new Dictionary<int, double>());
                foreach (var kvp1 in v1)
                {
                    int col = kvp1.Key;
                    res[row].Add(col, kvp0.Value * kvp1.Value);
                }
            }
            return res;
        }

        public static Dictionary<int, Dictionary<int, double>> GetMatrixMatrixMultiplication(Dictionary<int, Dictionary<int, double>> m0, Dictionary<int, Dictionary<int, double>> m1)
        {
            Dictionary<int, Dictionary<int, double>> res = new Dictionary<int, Dictionary<int, double>>();
            Dictionary<int, Dictionary<int, double>> m2 = Util.GetInverse2DDictionary(m1);
            foreach (var kvp0 in m0)
            {
                int row = kvp0.Key;
                foreach (var kvp2 in m2)
                {
                    int col = kvp2.Key;
                    double t = GetOrderedVectorDotProduct(kvp0.Value, kvp2.Value);
                    if (t != 0)
                    {
                        if (res.ContainsKey(row) == false)
                            res.Add(row, new Dictionary<int, double>());
                        res[row].Add(col, t);
                    }
                }
            }
            return res;
        }

        public static int GetNumberOfNonzeros(Dictionary<int, Dictionary<int, double>> m)
        {
            int counter = 0;
            foreach (var kvp1 in m)
            {
                counter += kvp1.Value.Count;
            }
            return counter;
        }

        public static double[,] GetMatrixAddition(double[,] mat1, double[,] mat2)
        {
            double[,] mat = new double[mat1.GetLength(0), mat1.GetLength(1)];
            for (int i = 0; i < mat1.GetLength(0); i++)
            {
                for (int j = 0; j < mat1.GetLength(1); j++)
                {
                    mat[i, j] = mat1[i, j] + mat2[i, j];
                }
            }
            return mat;
        }

        public static Dictionary<int, Dictionary<int, double>> GetMatrixAddition(Dictionary<int, Dictionary<int, double>> m0, Dictionary<int, Dictionary<int, double>> m1)
        {
            if (m0.Count == 0)
                return new Dictionary<int, Dictionary<int, double>>(m1);
            if (m1.Count == 0)
                return new Dictionary<int, Dictionary<int, double>>(m0);

            Dictionary<int, Dictionary<int, double>> res = new Dictionary<int, Dictionary<int, double>>();

            var enum0 = m0.GetEnumerator();
            var enum1 = m1.GetEnumerator();

            bool tail0 = false;
            bool tail1 = false;

            enum0.MoveNext();
            enum1.MoveNext();

            while (true)
            {
                if (enum0.Current.Key == enum1.Current.Key)
                {
                    res.Add(enum0.Current.Key, GetOrderedVectorAddition(enum0.Current.Value, enum1.Current.Value));
                    if (enum0.MoveNext() == false)
                        tail0 = true;
                    if (enum1.MoveNext() == false)
                        tail1 = true;
                }
                else if (enum0.Current.Key > enum1.Current.Key)
                {
                    res.Add(enum1.Current.Key, enum1.Current.Value);
                    if (enum1.MoveNext() == false)
                        tail1 = true;
                }
                else if (enum0.Current.Key < enum1.Current.Key)
                {
                    res.Add(enum0.Current.Key, enum0.Current.Value);
                    if (enum0.MoveNext() == false)
                        tail0 = true;
                }

                if (tail0 == true || tail1 == true)
                    break;
            }

            if (tail0 && tail1)
            {
                tail0 = false;
                tail1 = false;
            }

            if (tail0)
            {
                while (true)
                {
                    res.Add(enum1.Current.Key, enum1.Current.Value);
                    if (enum1.MoveNext() == false)
                        break;
                }
            }

            if (tail1)
            {
                while (true)
                {
                    res.Add(enum0.Current.Key, enum0.Current.Value);
                    if (enum0.MoveNext() == false)
                        break;
                }
            }

            return res;
        }

        public static Dictionary<int, Dictionary<int, double>> GetMatrixSubtraction(Dictionary<int, Dictionary<int, double>> m0, Dictionary<int, Dictionary<int, double>> m1)
        {
            Dictionary<int, Dictionary<int, double>> m2 = GetMatrixMultiply(m1, -1.0);
            return GetMatrixAddition(m0, m2);
        }

        public static Dictionary<int, Dictionary<int, double>> GetMatrixMultiply(Dictionary<int, Dictionary<int, double>> m, double factor)
        {
            Dictionary<int, Dictionary<int, double>> res = new Dictionary<int, Dictionary<int, double>>();
            foreach (var kvp in m)
            {
                res.Add(kvp.Key, GetVectorMultiply(kvp.Value, factor));
            }
            return res;
        }

        public static double GetMatrixMax(Dictionary<int, Dictionary<int, double>> m)
        {
            return m.Max(m2 => m2.Value.Max(m3 => m3.Value));
        }

        public static Dictionary<int, Dictionary<int, double>> CholeskyDecomposition(Dictionary<int, Dictionary<int, double>> m)
        {
            Dictionary<int, Dictionary<int, double>> res = new Dictionary<int, Dictionary<int, double>>();
            Dictionary<int, Dictionary<int, double>> t = new Dictionary<int, Dictionary<int, double>>();
            Dictionary<int, Dictionary<int, double>> M = Util.GetInverse2DDictionary(m);

            res.Add(0, new Dictionary<int, double>(M[0]));

            int d = M.Count;
            for (int i = 0; i < d; i++)
            {
                // Console.WriteLine("i=" + i);
                double root = Math.Sqrt(res[i][i]);
                res[i] = Maths.GetVectorMultiply(res[i], 1.0 / root);

                List<int> keys = new List<int>(res[i].Keys);
                foreach (int key in keys)
                {
                    if (res[i][key] == 0)
                    {
                        res[i].Remove(key);
                    }
                }

                if (i == d - 1)
                    break;

                t.Add(i, new Dictionary<int, double>(res[i]));
                for (int j = 0; j <= i; j++)
                {
                    if (t[j].ContainsKey(i) == true)
                    {
                        t[j].Remove(i);
                    }
                }

                res[i + 1] = new Dictionary<int, double>();
                foreach (var kvp in M[i + 1])
                {
                    if (kvp.Key >= i + 1)
                    {
                        res[i + 1].Add(kvp.Key, kvp.Value);
                    }
                }
                for (int j = 0; j <= i; j++)
                {
                    if (t[j].ContainsKey(i + 1) == false)
                    {
                        continue;
                    }
                    foreach (var kvp in t[j])
                    {
                        if (res[i + 1].ContainsKey(kvp.Key) == false)
                        {
                            res[i + 1].Add(kvp.Key, 0);
                        }
                        res[i + 1][kvp.Key] -= kvp.Value * t[j][i + 1];
                    }
                }
            }

            // res = Util.GetInverse2DDictionary(res);
            return res;
        }

        public static Dictionary<int, double> GetOrderedVectorProduct(IDictionary<int, double> v0, IDictionary<int, double> v1)
        {
            var productDict = new Dictionary<int, double>();

            var dictEnum0 = v0.GetEnumerator();
            var dictEnum1 = v1.GetEnumerator();

            dictEnum0.MoveNext();
            dictEnum1.MoveNext();
            while (true)
            {
                if (dictEnum0.Current.Key == dictEnum1.Current.Key)
                {
                    productDict.Add(dictEnum0.Current.Key, dictEnum0.Current.Value * dictEnum1.Current.Value);
                    if (!dictEnum0.MoveNext() || !dictEnum1.MoveNext())
                        break;
                }
                else if (dictEnum0.Current.Key > dictEnum1.Current.Key)
                {
                    if (!dictEnum1.MoveNext())
                        break;
                }
                else
                {
                    if (!dictEnum0.MoveNext())
                        break;
                }
            }

            return productDict;
        }

        public static double GetOrderedVectorCosineSimilarity(Dictionary<int, double> v0, Dictionary<int, double> v1)
        {
            return GetOrderedVectorDotProduct(v0, v1)/GetVectorLength(v0)/GetVectorLength(v1);
        }

        public static double GetOrderedVectorDotProduct(IDictionary<int, double> v0, IDictionary<int, double> v1)
        {
            double DotProduct = 0;

            var dictEnum0 = v0.GetEnumerator();
            var dictEnum1 = v1.GetEnumerator();

            dictEnum0.MoveNext();
            dictEnum1.MoveNext();
            while (true)
            {
                if (dictEnum0.Current.Key == dictEnum1.Current.Key)
                {
                    DotProduct += dictEnum0.Current.Value * dictEnum1.Current.Value;
                    if (!dictEnum0.MoveNext() || !dictEnum1.MoveNext())
                        break;
                }
                else if (dictEnum0.Current.Key > dictEnum1.Current.Key)
                {
                    if (!dictEnum1.MoveNext())
                        break;
                }
                else
                {
                    if (!dictEnum0.MoveNext())
                        break;
                }
            }

            return DotProduct;
        }

        public static bool IsOrderedVectorsContainSameElements(IEnumerable<int> v0, IEnumerable<int> v1)
        {
            var enum0 = v0.GetEnumerator();
            var enum1 = v1.GetEnumerator();

            enum0.MoveNext();
            enum1.MoveNext();
            while (true)
            {
                if (enum0.Current == enum1.Current)
                {
                    return true;
                }
                else if (enum0.Current > enum1.Current)
                {
                    if (!enum1.MoveNext())
                        return false;
                }
                else
                {
                    if (!enum0.MoveNext())
                        return false;
                }
            }
        }

        public static double GetVectorDotProduct(double[] v0, double[] v1)
        {
            if (v0.Length != v1.Length)
                throw new Exception("Input vectors must be of same length!");

            double DotProduct = 0;

            for (int i = 0; i < v0.Length; i++)
            {
                DotProduct += v0[i] * v1[i];
            }

            return DotProduct;
        }

        public static Dictionary<int, double> GetNormalizedVector(Dictionary<int, double> vector)
        {
            var normVec = new Dictionary<int, double>();
            var norm = GetVectorLength(vector);
            foreach (var kvp in vector)
                normVec.Add(kvp.Key, kvp.Value/norm);
            return normVec;
        }

        public static Dictionary<int, double> GetVectorMultiply(Dictionary<int, double> vector, double factor)
        {
            var newVector = new Dictionary<int, double>();

            foreach (var kvp in vector)
                newVector.Add(kvp.Key, factor * kvp.Value);

            return newVector;
        }

        //public static double GetOrderedVectorDotProduct(SortedDictionary<int, double> v0, SortedDictionary<int, double> v1)
        //{
        //    double DotProduct = 0;

        //    var dictEnum0 = v0.GetEnumerator();
        //    var dictEnum1 = v1.GetEnumerator();

        //    dictEnum0.MoveNext();
        //    dictEnum1.MoveNext();
        //    while (true)
        //    {
        //        if (dictEnum0.Current.Key == dictEnum1.Current.Key)
        //        {
        //            DotProduct += dictEnum0.Current.Value * dictEnum1.Current.Value;
        //            if (!dictEnum0.MoveNext() || !dictEnum1.MoveNext())
        //                break;
        //        }
        //        else if (dictEnum0.Current.Key > dictEnum1.Current.Key)
        //        {
        //            if (!dictEnum1.MoveNext())
        //                break;
        //        }
        //        else
        //        {
        //            if (!dictEnum0.MoveNext())
        //                break;
        //        }
        //    }

        //    return DotProduct;
        //}

        #endregion

        #region Matrix
        public static double[,] GetMergeMatrixVertical(double[,] matrix1, double[,] matrix2)
        {
            if (matrix1 == null && matrix2 == null)
                return null;
            if (matrix1 == null)
            {
                return GetCopyMatrix(matrix2);
            }
            if (matrix2 == null)
            {
                return GetCopyMatrix(matrix1);
            }
            if (matrix1.GetLength(1) != matrix2.GetLength(1))
                throw new NotImplementedException();
            int n1 = matrix1.GetLength(0), n2 = matrix2.GetLength(0), m = matrix1.GetLength(1);
            double[,] matrix = new double[n1 + n2, m];
            for (int i = 0; i < n1; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    matrix[i, j] = matrix1[i, j];
                }
            }
            for (int i = 0; i < n2; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    matrix[n1 + i, j] = matrix2[i, j];
                }
            }
            return matrix;
        }

        public static double[,] GetMergeMatrixHorizontal(double[,] matrix1, double[,] matrix2)
        {
            if (matrix1 == null && matrix2 == null)
                return null;
            if (matrix1 == null)
            {
                return GetCopyMatrix(matrix2);
            }
            if (matrix2 == null)
            {
                return GetCopyMatrix(matrix1);
            }
            if (matrix1.GetLength(0) != matrix2.GetLength(0))
                throw new NotImplementedException();
            int n = matrix1.GetLength(0), m1 = matrix1.GetLength(1), m2 = matrix2.GetLength(1);
            double[,] matrix = new double[n, m1 + m2];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m1; j++)
                {
                    matrix[i, j] = matrix1[i, j];
                }
            }
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m2; j++)
                {
                    matrix[i, m1 + j] = matrix2[i, j];
                }
            }
            return matrix;
        }

        public static double[,] GetCopyMatrix(double[,] matrix)
        {
            if (matrix == null)
                return null;
            double[,] copyMatrix = new double[matrix.GetLength(0), matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    copyMatrix[i, j] = matrix[i, j];
                }
            }
            return copyMatrix;
        }

        public static void SetMatrixRowColumn<T>(T[,] matrix, T[] vector, int rowColIndex, bool isRow = true)
        {
            if (isRow)
            {
                for (int i = 0; i < matrix.GetLength(1); i++)
                {
                    matrix[rowColIndex, i] = vector[i];
                }
            }
            else
            {
                for (int i = 0; i < matrix.GetLength(0); i++)
                {
                    matrix[i, rowColIndex] = vector[i];
                }
            }
        }

        public static T[] GetMatrixRowColumn<T>(T[,] matrix, int rowColIndex, bool isRow = true)
        {
            T[] vector = null;
            if (isRow)
            {
                vector = new T[matrix.GetLength(1)];
                for (int i = 0; i < matrix.GetLength(1); i++)
                {
                    vector[i] = matrix[rowColIndex, i];
                }
            }
            else
            {
                vector = new T[matrix.GetLength(0)];
                for (int i = 0; i < matrix.GetLength(0); i++)
                {
                    vector[i] = matrix[i, rowColIndex];
                }
            }
            return vector;
        }
        #endregion

        public static double Truncate(double  value,  double min, double  max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        /// <summary>
        /// Find the y so that (a2 - y -- b2) if (a1 - x -- b1)
        /// </summary>
        public static double GetIntermediateNumber(double a1, double b1, double a2, double b2, double x)
        {
            var lambda = GetLambda(a1, b1, x);
            return GetIntermediateNumber(a2, b2, lambda);
        }


        /// <summary>
        /// a - (lambda) - p - (1-lambda) - c
        /// </summary>
        public static double GetLambda(double a, double b, double p)
        {
            return (p - a)/(b - a);
        }

        /// <summary>
        /// a - (lambda) - p - (1-lambda) - c
        /// </summary>
        public static double GetIntermediateNumber(double a, double b, double lambda)
        {
            return lambda*(b - a) + a;
        }

        public static void Swap(ref int num0, ref int num1)
        {
            int temp = num0;
            num0 = num1;
            num1 = temp;
        }

        /// <summary>
        /// Return: Is swaped
        /// </summary>
        public static bool OrderAscend(ref int num0, ref int num1)
        {
            if (num0 > num1)
            {
                int temp = num0;
                num0 = num1;
                num1 = temp;

                return true;
            }
            return false;
        }

        public static void Transpose(double[,] mat)
        {
            if (mat.GetLength(0) != mat.GetLength(1))
                throw new Exception("Error input!! Input to Maths.Transpose() should be SQUARED matrix!!");

            double temp;
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                for (int j = i + 1; j < mat.GetLength(1); j++)
                {
                    temp = mat[i, j];
                    mat[i, j] = mat[j, i];
                    mat[j, i] = temp;
                }
            }
        }

        public static int GetObtusAngleIndex(Point[] pts)
        {
            if (IsAngleObtus(pts[0], pts[1], pts[2]))
                return 0;
            if (IsAngleObtus(pts[1], pts[2], pts[0]))
                return 1;
            if (IsAngleObtus(pts[2], pts[0], pts[1]))
                return 2;
            return -1;
        }

        public static bool IsAngleObtus(Point pt1, Point pt2, Point pt3)
        {
            return ((pt2.X - pt1.X) * (pt3.X - pt1.X) + (pt2.Y - pt1.Y) * (pt3.Y - pt1.Y)) < 0;
        }

        public static Point[] GetMidPerpendicular(double x1, double y1, double x2, double y2)
        {
            Point midPoint = new Point((x1 + x2) / 2, (y1 + y2) / 2);
            Point otherPoint = new Point(midPoint.X + y1 - y2, midPoint.Y + x2 - x1);
            return new Point[] { midPoint, otherPoint };
        }

        public static double GetDistance(double x0, double y0, double x1, double y1)
        {
            return Math.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1));
        }

        public static double GetDistance(Point2D pt1, Point2D pt2)
        {
            return Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
        }

        public static void GetPolarCoordinate(double x, double y, out double r, out double angle)
        {
            r = Math.Sqrt(x * x + y * y);
            angle = Math.Atan2(y, x);
        }

        public static void GetCartesianCoordinate(double r, double angle, out double x, out double y)
        {
            x = r * Math.Cos(angle);
            y = r * Math.Sin(angle);
        }


        public static double Min(double[,] mat, Func<double, bool> isFilteredFunc = null)
        {
            var min = double.MaxValue;
            foreach (var val in mat)
            {
                if (isFilteredFunc != null && isFilteredFunc(val))
                    continue;
                min = Math.Min(min, val);
            }
            return min;
        }



        public static Dictionary<T, double> GetNormalizedVector<T>(Dictionary<T, double> dictionary)
        {
            var norm = Math.Sqrt(dictionary.Sum(kvp => kvp.Value*kvp.Value));
            var dict = new Dictionary<T, double>();
            foreach (var kvp in dictionary)
            {
                dict.Add(kvp.Key, kvp.Value / norm);
            }
            return dict;
        }
    }

}
