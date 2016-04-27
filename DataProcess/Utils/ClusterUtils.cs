using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataProcess.DataAnalysis;

namespace DataProcess.Utils
{
    public class ClusterUtils
    {
        public static int[] KmeansClustering(Dictionary<int, double>[] docVectors, int clusterNumber)
        {
            KmeansDoc[] docs = new KmeansDoc[docVectors.Length];
            for (int idoc = 0; idoc < docVectors.Length; idoc++)
            {
                docs[idoc] = new KmeansDoc(docVectors[idoc]);
            }

            return KMeans.Cluster(docs.ToList(), clusterNumber);
        }
    }
}
