using Lava.Data;
using Lava.Visual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DataProcess.DataAnalysis
{
    /// <summary>
    /// A simple layout algorithm for tree.
    /// 
    /// Use Anchor Point as layout left-top point.
    /// ignore Bounds.
    /// ignore edges' location, you should use VisualEdgeTable or 
    /// other node-triggered visual table to drive edges' location by nodes'.
    /// </summary>
    public class TreeLayout : AbstractLayout, ILayout
    {
        #region Configures
        /// <summary>
        /// horizontal margin of nodes
        /// </summary>
        public double NodeMarginHorizontal { get; set; }
        /// <summary>
        /// vertical margin of nodes
        /// </summary>
        public double NodeMarginVertical { get; set; }

        /// <summary>
        /// the tree to layout
        /// </summary>
        private ITree _tree;
        /// <summary>
        /// the table store node location
        /// the row index in location table is the SAME in tree.
        /// </summary>
        private ITable _nodesLoc;
        /// <summary>
        /// node x column name in node location table
        /// </summary>
        private string _nodeXKey;
        /// <summary>
        /// node y column name in node location table
        /// </summary>
        private string _nodeYKey;

        /// <summary>
        /// default anchor at point (0,0)
        /// </summary>
        public static readonly Point DefaultAnchor = new Point(0, 0);
        #endregion

        #region ctor.
       /// <summary>
        /// Create a default tree layout activity.
        /// Record tree node location in the assigned node location table
        /// Assign the X, Y styles of the node location table.
        /// 
        /// assign node width and height.
        /// assign from which anchor to layout the tree
        /// </summary>
        /// <param name="tree">the tree to layout</param>
        /// <param name="nodesLoc">the node location table</param>
        /// <param name="nodeXKey">X style name (column name)</param>
        /// <param name="nodeYKey">Y style name (column name)</param>
        /// <param name="nodeWidth">the node width</param>
        /// <param name="nodeHeight">the node height</param>
        /// <param name="anchor">the anchor point</param>
        public TreeLayout(ITree tree, ITable nodesLoc = null, string nodeXKey = null, string nodeYKey = null,
            Point? anchor = null, double nodeWidth = 5, double nodeHeight = 5)
        {
            _tree = tree;
            _nodesLoc = nodesLoc == null ? tree.Graph.NodeTable : nodesLoc;
            _nodeXKey = nodeXKey == null ? Styles.X : nodeXKey;
            _nodeYKey = nodeYKey == null ? Styles.Y : nodeYKey;
            Anchor = anchor == null ? DefaultAnchor : anchor.Value;
            NodeMarginHorizontal = nodeWidth;
            NodeMarginVertical = nodeHeight;
        }
        #endregion

        #region algorithm

        /// <summary>
        /// Layout the tree.
        /// </summary>
        /// <returns>is successful</returns>
        public override bool Layout()
        {
            Dictionary<int, int> leftBrotherRow = new Dictionary<int, int>();
            Dictionary<int, int> nodeDepth = new Dictionary<int, int>();
            BreadthFirstSearchForLeftBrother(leftBrotherRow, nodeDepth);

            Dictionary<int, double> nodePosition = _nodesLoc.Rows.ToDictionary(row => row, row => (double)0);
            DepthFirstSearchForLayout(_tree.Root.Row, leftBrotherRow, nodePosition);

            double minX = nodePosition.Min(kvp => kvp.Value);
            foreach (var nrow in _tree.Graph.NodeRows)
            {
                _nodesLoc.Set(nrow, _nodeXKey, Anchor.X + nodePosition[nrow] - minX);
                _nodesLoc.Set(nrow, _nodeYKey, Anchor.Y + nodeDepth[nrow] * NodeMarginVertical);
            }

            return true;
        }

        private void BreadthFirstSearchForLeftBrother(Dictionary<int,int> leftBrotherRow, Dictionary<int, int> nodeDepth)
        {
            Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();
            queue.Enqueue(new Tuple<int, int>(_tree.Root.Row, 0));

            Tuple<int, int> prevNodeDepthTuple = new Tuple<int, int>(-1, -1);
            int count = 0;
            while (queue.Count != 0)
            {
                var nodeDepthTuple = queue.Dequeue();
                count++;
                nodeDepth[nodeDepthTuple.Item1] = nodeDepthTuple.Item2;

                int childCount = _tree.GetChildCount(_tree.Node(nodeDepthTuple.Item1));

                foreach (var child in _tree.GetChildren(_tree.Node(nodeDepthTuple.Item1)))
                {
                    queue.Enqueue(new Tuple<int, int>(child.Row, nodeDepthTuple.Item2 + 1));
                }

                if (prevNodeDepthTuple.Item2 == nodeDepthTuple.Item2)
                {
                    leftBrotherRow[nodeDepthTuple.Item1] = prevNodeDepthTuple.Item1;
                }
                else
                {
                    leftBrotherRow[nodeDepthTuple.Item1] = -1;
                }

                prevNodeDepthTuple = nodeDepthTuple;
            }
        }

        private void DepthFirstSearchForLayout(int nrow, Dictionary<int, int> leftBrotherRow, Dictionary<int, double> nodePosition)
        {
            if (leftBrotherRow[nrow] != -1)
            {
                if (nodePosition[leftBrotherRow[nrow]] + NodeMarginHorizontal > nodePosition[nrow])
                {
                    nodePosition[nrow] = nodePosition[leftBrotherRow[nrow]] + NodeMarginHorizontal;
                }
            }

            int childCount = _tree.GetChildCount(_tree.Node(nrow));
            int childIndex = 0;
            foreach (var child in _tree.GetChildren(_tree.Node(nrow)))
            {
                double childPos = nodePosition[nrow] - NodeMarginHorizontal * (childCount - 1) / 2 + NodeMarginHorizontal * childIndex;
                nodePosition[child.Row] = childPos;
                DepthFirstSearchForLayout(child.Row, leftBrotherRow, nodePosition);
                childIndex++;
            }

            if (childCount != 0)
            {
                nodePosition[nrow] = (nodePosition[_tree.GetChildren(_tree.Node(nrow))[0].Row] + nodePosition[_tree.GetChildren(_tree.Node(nrow))[childCount - 1].Row]) / 2;
            }
        }

        #endregion
    }
}
