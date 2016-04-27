using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.Utils.Algorithms
{
    public class MaximizeCoverageSelection
    {
        //double[,] CMat;     //coverage
        //int[] iniS;         //
        //int K;              //maximum selected nodes

        /// <summary>
        /// Given a coverage matrix, select a set of columns that maximize the following 
        /// expression: sum_i max_j(coverageMatrix[i,j])
        /// The greedy algorithm acheives (1-1/e) approximation 
        /// Reference: An analysis of approximations for maximizing submodular set functions
        /// Reference: Diversity Maximization Under Matroid Constraints
        /// </summary>
        /// <param name="coverageMatrix">Each column represents a node for selection, each row represents a feature to cover</param>
        /// <param name="selectedNum">The number of representative nodes</param>
        /// <param name="initialSelections">the nodes that are representative nodes previously</param>
        /// <returns></returns>
        public static int[] GetRepresentativeNodes(double[,] coverageMatrix,
            int selectedNum, int[] initialSelections = null)
        {
            var CMat = coverageMatrix;
            var K = selectedNum;
            var iniS = initialSelections;

            int ICnt = CMat.GetLength(0);
            int JCnt = CMat.GetLength(1);
            int[] S = new int[K];
            _deltaCov = new double[K];
            double[] maxCovByI = new double[ICnt];
            bool[] bSelectedByJ = new bool[JCnt];

            //consider initial selections
            if (iniS != null)
            {
                if (initialSelections.Length <= selectedNum)
                {
                    for (int iter = 0; iter < iniS.Length; iter++)
                    {
                        int s = iniS[iter];
                        for (int i = 0; i < ICnt; i++)
                        {
                            if (CMat[i, s] > maxCovByI[i])
                                maxCovByI[i] = CMat[i, s];
                        }
                        S[iter] = s;
                        bSelectedByJ[s] = true;
                    }
                }
                else
                {
                    //Can only select from initialSelections
                    for (int j = 0; j < JCnt; j++)
                        bSelectedByJ[j] = true;
                    foreach (var initialSelection in iniS)
                        bSelectedByJ[initialSelection] = false;
                    iniS = null;
                }
            }

            //start greedily pick
            for (int iter = (iniS == null ? 0 : iniS.Length); iter < K; iter++)
            {
                double maxDeltaCov = double.MinValue;
                int maxDeltaCovJ = -1;

                for (int j = 0; j < JCnt; j++)
                {
                    if (bSelectedByJ[j])
                        continue;
                    double deltaCov = 0;
                    for (int i = 0; i < ICnt; i++)
                    {
                        double delta = CMat[i, j] - maxCovByI[i];
                        if (delta > 0)
                            deltaCov += delta;
                    }
                    if (deltaCov > maxDeltaCov)
                    {
                        maxDeltaCov = deltaCov;
                        maxDeltaCovJ = j;
                    }
                }

                //set select
                int s = maxDeltaCovJ;
                for (int i = 0; i < ICnt; i++)
                {
                    if (CMat[i, s] > maxCovByI[i])
                        maxCovByI[i] = CMat[i, s];
                }
                S[iter] = s;
                _deltaCov[iter] = maxDeltaCov;
                bSelectedByJ[s] = true;
            }

            return S;
        }

        private static double[] _deltaCov;
        public static int[] GetRepresentativeNodes(double[,] coverageMatrix,
            int selectedNum, out double[] deltaCov, int[] initialSelections = null)
        {
            var repNodes = GetRepresentativeNodes(coverageMatrix, selectedNum, initialSelections);
            deltaCov = _deltaCov;
            return repNodes;
        }
    }
}
