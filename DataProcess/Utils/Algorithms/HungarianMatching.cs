using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.Utils.Algorithms
{
    public class HungarianMatching<T>
    {
        private Dictionary<T, T> _matchingDict;
        private IEnumerable<T> _items1;
        private IEnumerable<T> _items2;
        public HungarianMatching(IEnumerable<T> items1, IEnumerable<T> items2, Func<T, T, double> costFunc,
            double item1NotMatchCost, double item2NotMatchCost)
        {
            _items1 = items1;
            _items2 = items2;

            var item1Array = items1.ToArray();
            var item2Array = items2.ToArray();
            var item1Cnt = item1Array.Length;
            var item2Cnt = item2Array.Length;
            var itemCnt = item1Cnt + item2Cnt;
            double[,] costMatrix = new double[itemCnt, itemCnt];

            for (int i = 0; i < item1Cnt; i++)
            {
                for (int j = 0; j < item2Cnt; j++)
                {
                    costMatrix[i, j] = costFunc(item1Array[i], item2Array[j]);
                }
            }
            for (int i = 0; i < item1Cnt; i++)
            {
                for (int j = item2Cnt; j < itemCnt; j++)
                {
                    costMatrix[i, j] = item1NotMatchCost;
                }
            }
            for (int i = item1Cnt; i < itemCnt; i++)
            {
                for (int j = 0; j < item2Cnt; j++)
                {
                    costMatrix[i, j] = item2NotMatchCost;
                }
            }
            for (int i = item1Cnt; i < itemCnt; i++)
            {
                for (int j = item2Cnt; j < itemCnt; j++)
                {
                    costMatrix[i, j] = 0;
                }
            }

            var matching = new HungarianMatching(costMatrix);
            int[] matchingResult;
            matching.execute(out matchingResult);
            _matchingDict = new Dictionary<T, T>();
            for (int item1Index = 0; item1Index < item1Cnt; item1Index++)
            {
                var item2Index = matchingResult[item1Index];
                if (item2Index < item2Cnt)
                {
                    _matchingDict.Add(item1Array[item1Index], item2Array[item2Index]);
                }
            }
        }

        public Dictionary<T, T> GetMatchedPairs()
        {
            return _matchingDict;
        }

        public List<T> GetNotMatchedPairs(bool isGetNotMatchedItems1)
        {
            List<T> notMatchedItems = new List<T>();
            if (isGetNotMatchedItems1)
            {
                HashSet<T> matchedItems1 = new HashSet<T>(_matchingDict.Keys);
                foreach (var item in _items1)
                {
                    if (!matchedItems1.Contains(item))
                    {
                        notMatchedItems.Add(item);
                    }
                }
            }
            else
            {
                HashSet<T> matchedItems2 = new HashSet<T>(_matchingDict.Values);
                foreach (var item in _items2)
                {
                    if (!matchedItems2.Contains(item))
                    {
                        notMatchedItems.Add(item);
                    }
                }
            }
            return notMatchedItems;
        }
    }

    public class HungarianMatching
    {
        private double[][] costMatrix;
        private int rows, cols, dim;
        private double[] labelByWorker, labelByJob;
        private int[] minSlackWorkerByJob;
        private double[] minSlackValueByJob;
        private int[] matchJobByWorker, matchWorkerByJob;
        private int[] parentWorkerByCommittedJob;
        private bool[] committedWorkers;
        private double[] reduceByWorkers; //Xiting: record the reduce value
        private double[] reduceByJobs; //Xiting: record the reduce value
        //private int[] result;
        //private int[] reverseResult;

        /**
         * Construct an instance of the algorithm.
         * 
         * @param costMatrix
         *            the cost matrix, where matrix[i][j] holds the cost of
         *            assigning worker i to job j, for all i, j. The cost matrix
         *            must not be irregular in the sense that all rows must be the
         *            same length.
         */
        public HungarianMatching(double[,] costMatrix)
        {
            this.dim = Math.Max(costMatrix.GetLength(0), costMatrix.GetLength(1));
            this.rows = costMatrix.GetLength(0);
            this.cols = costMatrix.GetLength(1);
            this.costMatrix = new double[this.dim][];
            for (int i = 0; i < this.costMatrix.Length; i++)
                this.costMatrix[i] = new double[this.dim];
            for (int w = 0; w < this.dim; w++)
            {
                if (w < costMatrix.Length)
                {
                    this.costMatrix[w] = copyOf(costMatrix, w, this.dim);
                }
                else
                {
                    this.costMatrix[w] = new double[this.dim];
                }
            }
            labelByWorker = new double[this.dim];
            labelByJob = new double[this.dim];
            minSlackWorkerByJob = new int[this.dim];
            minSlackValueByJob = new double[this.dim];
            committedWorkers = new bool[this.dim];
            parentWorkerByCommittedJob = new int[this.dim];
            matchJobByWorker = new int[this.dim];
            fill(matchJobByWorker, -1);
            matchWorkerByJob = new int[this.dim];
            fill(matchWorkerByJob, -1);
            reduceByWorkers = new double[this.dim];
            reduceByJobs = new double[this.dim];
            //result = new int[rows];
            //reverseResult = new int[cols];
        }

        #region incremental
        public int[] incExecute(List<ChangedCost> changedCosts)
        {
            foreach (var changedCost in changedCosts)
            {
                if (changedCost is ChangedUnitCost)
                {
                    ChangedUnitCost changedUnitCost = changedCost as ChangedUnitCost;
                    changeUnitCost(changedUnitCost.Row, changedUnitCost.Col, changedUnitCost.GetVal());
                }
                else if (changedCost is ChangedRowCost)
                {
                    ChangedRowCost changedRowCost = changedCost as ChangedRowCost;
                    changeRowCost(changedRowCost.Row, changedRowCost.GetVal(), changedRowCost.StartCol, changedRowCost.EndCol);
                }
                else if (changedCost is ChangedColCost)
                {
                    ChangedColCost changedColCost = changedCost as ChangedColCost;
                    changeColCost(changedColCost.Col, changedColCost.GetVal(), changedColCost.StartRow, changedColCost.EndRow);
                }
                else
                    throw new NotImplementedException();
            }

            int w = fetchUnmatchedWorker();
            while (w < dim)
            {
                initializePhase(w);
                executePhase();
                w = fetchUnmatchedWorker();
            }

            return GetResult();
        }

        public int[] GetResult()
        {
            var result = new int[rows];
            for (int w = 0; w < result.Length; w++)
            {
                result[w] = matchJobByWorker[w];
                if (result[w] >= cols)
                    result[w] = -1;
            }

            return result;
        }

        public int[] GetReversedResult()
        {
            var reverseResult = new int[cols];
            for (int j = 0; j < reverseResult.Length; j++)
            {
                reverseResult[j] = matchWorkerByJob[j];
                if (reverseResult[j] >= rows)
                    reverseResult[j] = -1;
            }

            return reverseResult;
        }

        private int changeUnitCost(int w, int j, double newcost)
        {
            newcost -= reduceByWorkers[w] + reduceByJobs[j];
            double oldcost = costMatrix[w][j];
            costMatrix[w][j] = newcost;

            if (newcost > oldcost && matchJobByWorker[w] == j)
            {
                removeMatch(w, j);
                return 1;
            }
            else if (newcost < oldcost && (labelByWorker[w] + labelByJob[j] > newcost))
            {
                double minLabel = double.MaxValue;
                double label;
                for (int i = 0; i < dim; i++)
                {
                    label = costMatrix[w][i] - labelByJob[i];
                    if (label < minLabel)
                        minLabel = label;
                }
                labelByWorker[w] = minLabel;

                if (matchJobByWorker[w] != j && matchJobByWorker[w] != -1)
                {
                    removeMatch(w, matchJobByWorker[w]);
                    return 1;
                }
            }

            return 0;
        }

        private int changeRowCost(int w, double[] newcosts, int startj, int endj)
        {
            double reduceByWorker = reduceByWorkers[w];
            double[] costMatrixW = costMatrix[w];
            for (int j = startj; j < endj; j++)
            {
                double newcost = newcosts[j - startj];
                newcost -= reduceByWorker + reduceByJobs[j];
                costMatrixW[j] = newcost;
            }

            double label, minLabel = double.MaxValue;
            for (int j = 0; j < dim; j++)
            {
                label = costMatrixW[j] - labelByJob[j];
                if (label < minLabel)
                    minLabel = label;
            }
            labelByWorker[w] = minLabel;

            if (matchJobByWorker[w] != -1)
            {
                removeMatch(w, matchJobByWorker[w]);
                return 1;
            }

            return 0;
        }

        private int changeColCost(int j, double[] newcosts, int startw, int endw)
        {
            double reduceByJob = reduceByJobs[j];
            for (int w = startw; w < endw; w++)
            {
                double newcost = newcosts[w - startw];
                newcost -= reduceByWorkers[w] + reduceByJob;
                costMatrix[w][j] = newcost;
            }

            double label, minLabel = double.MaxValue;
            for (int w = 0; w < dim; w++)
            {
                label = costMatrix[w][j] - labelByWorker[w];
                if (label < minLabel)
                    minLabel = label;
            }
            labelByJob[j] = minLabel;

            if (matchWorkerByJob[j] != -1)
            {
                removeMatch(matchWorkerByJob[j], j);
                return 1;
            }

            return 0;
        }

        private void removeMatch(int w, int j)
        {
            matchJobByWorker[w] = -1;
            matchWorkerByJob[j] = -1;
        }
        #endregion

        public void PrintCostMatrix()
        {
            Console.WriteLine("----------------------------------------------");
            for (int w = 0; w < dim; w++)
            {
                for (int j = 0; j < dim; j++)
                {
                    Console.Write("{0}\t", costMatrix[w][j] + reduceByWorkers[w] + reduceByJobs[j]);
                }
                Console.WriteLine();
            }
        }

        public double[,] GetCostMatrix()
        {
            double[,] costMatrixOut = new double[dim, dim];
            for (int w = 0; w < dim; w++)
            {
                for (int j = 0; j < dim; j++)
                {
                    costMatrixOut[w, j] = costMatrix[w][j] + reduceByWorkers[w] + reduceByJobs[j];
                }
            }
            return costMatrixOut;
        }

        private void fill(int[] array, int value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        private void fill(bool[] array, bool value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        private double[] copyOf(double[,] mat, int row, int dim)
        {
            double[] array2 = new double[dim];
            for (int i = 0; i < mat.GetLength(1); i++)
                array2[i] = mat[row, i];
            return array2;
        }

        private double[] copyOf(double[] array, int dim)
        {
            double[] array2 = new double[dim];
            for (int i = 0; i < array.Length; i++)
                array2[i] = array[i];
            return array2;
        }

        private int[] copyOf(int[] array, int dim)
        {
            int[] array2 = new int[dim];
            for (int i = 0; i < array.Length; i++)
                array2[i] = array[i];
            return array2;
        }


        /**
         * Compute an initial feasible solution by assigning zero labels to the
         * workers and by assigning to each job a label equal to the minimum cost
         * among its incident edges.
         */
        protected void computeInitialFeasibleSolution()
        {
            for (int j = 0; j < dim; j++)
            {
                labelByJob[j] = Double.MaxValue;
            }
            for (int w = 0; w < dim; w++)
            {
                for (int j = 0; j < dim; j++)
                {
                    if (costMatrix[w][j] < labelByJob[j])
                    {
                        labelByJob[j] = costMatrix[w][j];
                    }
                }
            }
        }


        /**
         * Execute the algorithm.
         * 
         * @return the minimum cost matching of workers to jobs based upon the
         *         provided cost matrix. A matching value of -1 indicates that the
         *         corresponding worker is unassigned.
         */
        public double execute(out int[] result)
        {
            /*
             * Heuristics to improve performance: Reduce rows and columns by their
             * smallest element, compute an initial non-zero dual feasible solution
             * and create a greedy matching from workers to jobs of the cost matrix.
             */
            reduce();
            computeInitialFeasibleSolution();
            greedyMatch();


            int w = fetchUnmatchedWorker();
            while (w < dim)
            {
                initializePhase(w);
                executePhase();
                w = fetchUnmatchedWorker();
            }

            result = copyOf(matchJobByWorker, rows);
            for (w = 0; w < result.Length; w++)
            {
                if (result[w] >= cols)
                {
                    result[w] = -1;
                }
            }

            double cost = 0;
            for (w = 0; w < rows; w++)
            {
                var j = result[w];
                if (j != -1)
                    cost += costMatrix[w][j] + reduceByWorkers[w] + reduceByJobs[j];
            }

            return cost;
        }


        /**
         * Execute a single phase of the algorithm. A phase of the Hungarian
         * algorithm consists of building a set of committed workers and a set of
         * committed jobs from a root unmatched worker by following alternating
         * unmatched/matched zero-slack edges. If an unmatched job is encountered,
         * then an augmenting path has been found and the matching is grown. If the
         * connected zero-slack edges have been exhausted, the labels of committed
         * workers are increased by the minimum slack among committed workers and
         * non-committed jobs to create more zero-slack edges (the labels of
         * committed jobs are simultaneously decreased by the same amount in order
         * to maintain a feasible labeling).
         * <p>
         * 
         * The runtime of a single phase of the algorithm is O(n^2), where n is the
         * dimension of the internal square cost matrix, since each edge is visited
         * at most once and since increasing the labeling is accomplished in time
         * O(n) by maintaining the minimum slack values among non-committed jobs.
         * When a phase completes, the matching will have increased in size.
         */
        protected void executePhase()
        {
            while (true)
            {
                int minSlackWorker = -1, minSlackJob = -1;
                double minSlackValue = Double.MaxValue;
                for (int j = 0; j < dim; j++)
                {
                    if (parentWorkerByCommittedJob[j] == -1)
                    {
                        if (minSlackValueByJob[j] < minSlackValue)
                        {
                            minSlackValue = minSlackValueByJob[j];
                            minSlackWorker = minSlackWorkerByJob[j];
                            minSlackJob = j;
                        }
                    }
                }
                if (minSlackValue > 0)
                {                        //Min(~T, S)>0 <=> Neig(S)==T in Equality Graph
                    updateLabeling(minSlackValue);
                }
                parentWorkerByCommittedJob[minSlackJob] = minSlackWorker;
                if (matchWorkerByJob[minSlackJob] == -1)
                {
                    /*
                     * An augmenting path has been found.
                     */
                    int committedJob = minSlackJob;
                    int parentWorker = parentWorkerByCommittedJob[committedJob];
                    while (true)
                    {
                        int temp = matchJobByWorker[parentWorker];
                        match(parentWorker, committedJob);
                        committedJob = temp;
                        if (committedJob == -1)
                        {
                            break;
                        }
                        parentWorker = parentWorkerByCommittedJob[committedJob];
                    }
                    return;
                }
                else
                {
                    /*
                     * Update slack values since we increased the size of the
                     * committed workers set.
                     */
                    int worker = matchWorkerByJob[minSlackJob];
                    committedWorkers[worker] = true;
                    for (int j = 0; j < dim; j++)
                    {
                        if (parentWorkerByCommittedJob[j] == -1)
                        {
                            double slack = costMatrix[worker][j]
                                            - labelByWorker[worker] - labelByJob[j];
                            if (minSlackValueByJob[j] > slack)
                            {
                                minSlackValueByJob[j] = slack;
                                minSlackWorkerByJob[j] = worker;
                            }
                        }
                    }
                }
            }
        }


        /**
         * 
         * @return the first unmatched worker or {@link #dim} if none.
         */
        protected int fetchUnmatchedWorker()
        {
            int w;
            for (w = 0; w < dim; w++)
            {
                if (matchJobByWorker[w] == -1)
                {
                    break;
                }
            }
            return w;
        }


        /**
         * Find a valid matching by greedily selecting among zero-cost matchings.
         * This is a heuristic to jump-start the augmentation algorithm.
         */
        protected void greedyMatch()
        {
            for (int w = 0; w < dim; w++)
            {
                for (int j = 0; j < dim; j++)
                {
                    if (matchJobByWorker[w] == -1
                                    && matchWorkerByJob[j] == -1
                                    && costMatrix[w][j] - labelByWorker[w] - labelByJob[j] == 0)
                    {
                        match(w, j);
                    }
                }
            }
        }


        /**
         * Initialize the next phase of the algorithm by clearing the committed
         * workers and jobs sets and by initializing the slack arrays to the values
         * corresponding to the specified root worker.
         * 
         * @param w
         *            the worker at which to root the next phase.
         */
        protected void initializePhase(int w)
        {
            fill(committedWorkers, false);
            fill(parentWorkerByCommittedJob, -1);
            committedWorkers[w] = true;
            for (int j = 0; j < dim; j++)
            {
                minSlackValueByJob[j] = costMatrix[w][j] - labelByWorker[w]
                                - labelByJob[j];
                minSlackWorkerByJob[j] = w;
            }
        }



        /**
         * Helper method to record a matching between worker w and job j.
         */
        protected void match(int w, int j)
        {
            matchJobByWorker[w] = j;
            matchWorkerByJob[j] = w;
        }


        /**
         * Reduce the cost matrix by subtracting the smallest element of each row
         * from all elements of the row as well as the smallest element of each
         * column from all elements of the column. Note that an optimal assignment
         * for a reduced cost matrix is optimal for the original cost matrix.
         */
        protected void reduce()
        {
            for (int w = 0; w < dim; w++)
            {
                double min = Double.MaxValue;
                for (int j = 0; j < dim; j++)
                {
                    if (costMatrix[w][j] < min)
                    {
                        min = costMatrix[w][j];
                    }
                }
                for (int j = 0; j < dim; j++)
                {
                    costMatrix[w][j] -= min;
                }
                reduceByWorkers[w] = min;
            }
            {
                double[] min = new double[dim];
                for (int j = 0; j < dim; j++)
                {
                    min[j] = Double.MaxValue;
                }
                for (int w = 0; w < dim; w++)
                {
                    for (int j = 0; j < dim; j++)
                    {
                        if (costMatrix[w][j] < min[j])
                        {
                            min[j] = costMatrix[w][j];
                        }
                    }
                }
                for (int w = 0; w < dim; w++)
                {
                    for (int j = 0; j < dim; j++)
                    {
                        costMatrix[w][j] -= min[j];
                    }
                }
                reduceByJobs = min;
            }
        }


        /**
         * Update labels with the specified slack by adding the slack value for
         * committed workers and by subtracting the slack value for committed jobs.
         * In addition, update the minimum slack values appropriately.
         */
        protected void updateLabeling(double slack)
        {
            for (int w = 0; w < dim; w++)
            {
                if (committedWorkers[w])
                {
                    labelByWorker[w] += slack;
                }
            }
            for (int j = 0; j < dim; j++)
            {
                if (parentWorkerByCommittedJob[j] != -1)
                {
                    labelByJob[j] -= slack;
                }
                else
                {
                    minSlackValueByJob[j] -= slack;
                }
            }
        }
    }

    public abstract class ChangedCost
    {
        public int Row;
        public int Col;
        public object Val;
    }

    public class ChangedRowCost : ChangedCost
    {
        public int StartCol, EndCol;

        public ChangedRowCost(int row, double[] newVals, int startCol = -1, int endCol = -1)
        {
            Row = row;
            Val = newVals;

            if (startCol < 0)
            {
                startCol = 0;
                endCol = newVals.Length;
            }

            StartCol = startCol;
            EndCol = endCol;
        }

        public double[] GetVal()
        {
            return Val as double[];
        }

        public override string ToString()
        {
            string str = string.Format("Row [R{0}] ||| ", Row);
            var val = GetVal();
            for (int i = 0; i < val.Length; i++)
            {
                str += string.Format("[C{0}]{1},\t", (StartCol + i),
                    (val[i] == double.MaxValue ? "Max" : val[i].ToString("#0.00")));
            }
            return str;
        }
    }

    public class ChangedColCost : ChangedCost
    {
        public int StartRow, EndRow;

        public ChangedColCost(int col, double[] newVals, int startRow = -1, int endRow = -1)
        {
            Col = col;
            Val = newVals;

            if (startRow < 0)
            {
                startRow = 0;
                endRow = newVals.Length;
            }

            StartRow = startRow;
            EndRow = endRow;
        }

        public double[] GetVal()
        {
            return Val as double[];
        }

        public override string ToString()
        {
            string str = string.Format("Col [C{0}] ||| ", Col);
            var val = GetVal();
            for (int i = 0; i < val.Length; i++)
            {
                str += string.Format("[R{0}]{1},\t", (StartRow + i),
                    (val[i] == double.MaxValue ? "Max" : val[i].ToString("#0.00")));
            }
            return str;
        }
    }

    public class ChangedUnitCost : ChangedCost
    {
        public ChangedUnitCost(int row, int col, double newVal)
        {
            Row = row;
            Col = col;
            Val = newVal;
        }

        public double GetVal()
        {
            return (double)Val;
        }

        public override string ToString()
        {
            var val = GetVal();
            string str = string.Format("Unit ||| [R{0}] [C{1}] {2}", Row, Col, (val == double.MaxValue ? "Max" : val.ToString("#0.00")));
            return str;
        }
    }
}

