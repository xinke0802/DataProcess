using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataProcess.Utils;

namespace DataProcess.DataAnalysis
{
    public class KmeansLexicon
    {
        public Dictionary<int, string> Index2WordDict = new Dictionary<int, string>();
        public Dictionary<string, int> Word2IndexDict = new Dictionary<string, int>();
    }

    public class KmeansDoc
    {
        static int MaxDocIndex = 0;

        Dictionary<int, double> _orgVector;
        public KmeansDoc(string title, string body, string category, KmeansLexicon lexicon)
        {
            Title = title;
            Body = body;
            Category = category;
            KmeansLexicon = lexicon;
            Index = MaxDocIndex++;
        }

        /// <summary>
        /// For centroid nodes
        /// </summary>
        public KmeansDoc(Dictionary<int, double> vector)
        {
            _orgVector = vector;
            Vector = vector;
            Norm = Maths.GetVectorLength(Vector);
        }

        public int Index { get; protected set; }
        public string Title { get; protected set; }
        public string Body { get; protected set; }
        public string Category { get; protected set; }

        public Dictionary<int, double> Vector { get; protected set; }
        public double Norm { get; protected set; }

        public KmeansLexicon KmeansLexicon { get; protected set; }

        public void UpdateVector(Dictionary<int, Dictionary<int, double>> G)
        {
            Vector = Maths.GetInversedMatrixVectorMultiplication(G, Vector);

            Vector = SortUtils.EnsureSortedByKey(Vector);
            Norm = Maths.GetVectorLength(Vector);
            // Vector = Maths.GetVectorMultiply(Vector, 1.0 / Norm);
            // Norm = 1;
        }

        public void MostFrequent(int num)
        {
            Dictionary<int, double> vec = new Dictionary<int, double>();
            Vector = SortUtils.EnsureSortedByValue(Vector);
            int ct = 0;
            foreach (var kvp in Vector)
            {
                ct++;
                // Console.Write(kvp.Value + " ");
                vec.Add(kvp.Key, kvp.Value);
                if (ct == num)
                    break;
            }
            vec = SortUtils.EnsureSortedByKey(vec);
            Vector = vec;
            Norm = Maths.GetVectorLength(Vector);
        }

        public void Parse(TokenizeConfig tokenizeConfig)
        {
            var text = Title + " " + Title + " " + Title + " " + Body;
            var words = NLPOperations.Tokenize(text, tokenizeConfig);
            int wordIndex = 0;
            Counter<int> counter = new Counter<int>();
            foreach (var word in words)
            {
                if (!KmeansLexicon.Word2IndexDict.TryGetValue(word, out wordIndex))
                {
                    wordIndex = KmeansLexicon.Word2IndexDict.Count;
                    KmeansLexicon.Word2IndexDict.Add(word, wordIndex);
                    KmeansLexicon.Index2WordDict.Add(wordIndex, word);
                }
                counter.Add(wordIndex);
            }
            Vector = SortUtils.EnsureSortedByKey(counter.GetCountDictionary().ToDictionary(kvp2 => kvp2.Key, kvp2 => (double)kvp2.Value));
            Norm = Maths.GetVectorLength(Vector);
            Vector = Maths.GetVectorMultiply(Vector, 1.0 / Norm);
            Norm = 1;
            _orgVector = Vector;
        }

        public static readonly Func<List<KmeansDoc>, KmeansDoc> CentroidFunc = (docs) =>
        {
            Dictionary<int, double> vecSum = new Dictionary<int, double>();

            if (docs.Count == 0)
                return new KmeansDoc(vecSum);
            else if (docs.Count == 1)
                return new KmeansDoc(docs.First().Vector);
            else
            {
                foreach (var doc in docs)
                    vecSum = Maths.GetOrderedVectorAddition(vecSum, doc.Vector);

                int d = docs.Count;
                double norm = Maths.GetVectorLength(vecSum);
                vecSum = Maths.GetVectorMultiply(vecSum, 1.0 / norm);

                return new KmeansDoc(vecSum);
            }
        };

        public static readonly Func<KmeansDoc, KmeansDoc, double> DistanceFunc = (doc1, doc2) =>
        {
            //double dotProd = Maths.GetOrderedVectorDotProduct(doc1.Vector, doc2.Vector);
            //return -dotProd / doc1.Norm / doc2.Norm;

            double dis = doc1.Norm * doc1.Norm + doc2.Norm * doc2.Norm;
            dis -= 2.0 * Maths.GetOrderedVectorDotProduct(doc1.Vector, doc2.Vector);
            return dis;
        };
    }

    public class KMeans
    {
        public static int[] Cluster(List<KmeansDoc> docs, int numClusters,
            Func<List<KmeansDoc>, KmeansDoc> centroidFunc = null, Func<KmeansDoc, KmeansDoc, double> distanceFunc = null, KmeansDoc[] initialCentroids = null)
        {
            if (centroidFunc == null)
                centroidFunc = KmeansDoc.CentroidFunc;
            if (distanceFunc == null)
                distanceFunc = KmeansDoc.DistanceFunc;

            bool changed = true;
            bool success = true;
            int[] clustering = InitClustering(docs.Count, numClusters, 0);
            int maxCount = Math.Min(30, docs.Count * 10);
            int ct = 0;
            KmeansDoc[] centroids = new KmeansDoc[numClusters];
            while (changed == true && success == true && ct < maxCount)
            {
                ++ct;
                Console.WriteLine("ite: " + ct);
                success = UpdateMeans(docs, clustering, centroids, centroidFunc);
                if (ct == 1 && initialCentroids != null) centroids = initialCentroids;
                changed = UpdateClustering(docs, clustering, centroids, distanceFunc);
            }
            return clustering;
        }

        private static int[] InitClustering(int numTuples, int numClusters, int seed)
        {
            Random random = new Random(seed);
            int[] clustering = new int[numTuples];
            for (int i = 0; i < numClusters; ++i)
                clustering[i] = i;
            for (int i = numClusters; i < clustering.Length; ++i)
                clustering[i] = random.Next(0, numClusters);
            return clustering;
        }

        private static double[][] Allocate(int numClusters, int numColumns)
        {
            double[][] result = new double[numClusters][];
            for (int k = 0; k < numClusters; ++k)
                result[k] = new double[numColumns];
            return result;
        }

        private static bool UpdateMeans(List<KmeansDoc> docs, int[] clustering, KmeansDoc[] centroid, Func<List<KmeansDoc>, KmeansDoc> centroidFunc)
        {
            Dictionary<int, List<KmeansDoc>> docDict = new Dictionary<int, List<KmeansDoc>>();
            for (int i = 0; i < clustering.Length; i++)
            {
                Util.AddToList(docDict, clustering[i], docs[i]);
            }

            for (int i = 0; i < centroid.Length; i++)
            {
                if (!docDict.ContainsKey(i))
                {
                    Console.WriteLine("does not contain the key");
                    return false;
                }
            }

            foreach (var kvp in docDict)
            {
                centroid[kvp.Key] = centroidFunc(kvp.Value);
            }
            return true;
        }

        private static bool UpdateClustering(List<KmeansDoc> docs, int[] clustering, KmeansDoc[] centroid, Func<KmeansDoc, KmeansDoc, double> distanceFunc)
        {
            int numClusters = centroid.Length;
            bool changed = false;

            int[] newClustering = new int[clustering.Length];
            Array.Copy(clustering, newClustering, clustering.Length);

            double[] distances = new double[numClusters];

            for (int i = 0; i < docs.Count; ++i)
            {
                for (int k = 0; k < numClusters; ++k)
                    distances[k] = distanceFunc(docs[i], centroid[k]);

                int newClusterID = MinIndex(distances);
                if (newClusterID != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterID;
                }
            }

            if (changed == false)
                return false;

            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < docs.Count; ++i)
            {
                int cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false;

            Array.Copy(newClustering, clustering, newClustering.Length);
            return true; // no zero-counts and at least one change
        }

        private static int MinIndex(double[] distances)
        {
            int indexOfMin = 0;
            double smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k];
                    indexOfMin = k;
                }
            }
            return indexOfMin;
        }


        static void ShowData(double[][] data, int decimals,
      bool indices, bool newLine)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                if (indices) Console.Write(i.ToString().PadLeft(3) + " ");
                for (int j = 0; j < data[i].Length; ++j)
                {
                    if (data[i][j] >= 0.0) Console.Write(" ");
                    Console.Write(data[i][j].ToString("F" + decimals) + " ");
                }
                Console.WriteLine("");
            }
            if (newLine) Console.WriteLine("");
        }

        //static void ShowVector(int[] vector, bool newLine)
        //{
        //    for (int i = 0; i < vector.Length; ++i)
        //        Console.Write(vector[i] + " ");
        //    if (newLine) Console.WriteLine("\n");
        //}

        //static void ShowClustered(double[][] data, int[] clustering,
        //  int numClusters, int decimals)
        //{
        //    for (int k = 0; k < numClusters; ++k)
        //    {
        //        Console.WriteLine("===================");
        //        for (int i = 0; i < data.Length; ++i)
        //        {
        //            int clusterID = clustering[i];
        //            if (clusterID != k) continue;
        //            Console.Write(i.ToString().PadLeft(3) + " ");
        //            for (int j = 0; j < data[i].Length; ++j)
        //            {
        //                if (data[i][j] >= 0.0) Console.Write(" ");
        //                Console.Write(data[i][j].ToString("F" + decimals) + " ");
        //            }
        //            Console.WriteLine("");
        //        }
        //        Console.WriteLine("===================");
        //    } // k
        //}
    }

}
