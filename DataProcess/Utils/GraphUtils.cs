using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Lava.Data;
using MathNet.Numerics.LinearAlgebra.Solvers;

namespace DataProcess.Utils
{
    public class IsolateNodeCalculator<T>
    {
        private HashSet<T> _nodes;
        private HashSet<T> _isoNodes;
        private Func<T, IEnumerable<T>> _neigFunc;

        public IsolateNodeCalculator(IEnumerable<T> nodes, Func<T, IEnumerable<T>> neighFunc)
        {
            _nodes = new HashSet<T>(nodes);
            _neigFunc = neighFunc;

            _isoNodes = new HashSet<T>();
            foreach (var node in nodes)
            {
                bool isIso = true;
                foreach (var neigh in neighFunc(node))
                {
                    if (_nodes.Contains(neigh))
                    {
                        isIso = false;
                        break;
                    }
                }
                if (isIso)
                    _isoNodes.Add(node);
            }
        }

        /// <summary>
        /// Incrementally update isoNodes
        /// </summary>
        public void AddNode(T node)
        {
            bool isIso = true;
            foreach (var neigh in _neigFunc(node))
            {
                if (_nodes.Contains(neigh))
                {
                    isIso = false;
                    if (_isoNodes.Contains(neigh))
                        _isoNodes.Remove(neigh);
                }
            }

            if (isIso)
                _isoNodes.Add(node);
            _nodes.Add(node);
        }

        public HashSet<T> GetIsolatedNodes()
        {
            return _isoNodes;
        }
    }

    public class BridgeCalculator<T1, T2>
    {
        //private List<T1> _nodes;
        //private List<T2> _edges;
        private Func<T2, T1> _edge2SourceNodeFunc;
        Func<T2, T1> _edge2TargetNodeFunc;

        private Dictionary<T1, int> _node2IdDict;
        private Dictionary<Tuple<int, int>, T2> _indices2EdgeDict;
        //private Dictionary<T2, int> _edge2IdDict;

        private BridgeGraph _bridgeGraph;
        public BridgeCalculator(IEnumerable<T1> nodes, IEnumerable<T2> edges,
            Func<T2, T1> edge2SourceNodeFunc, Func<T2, T1> edge2TargetNodeFunc)
        {
            //_nodes = nodes;
            //_edges = edges;
            _edge2SourceNodeFunc = edge2SourceNodeFunc;
            _edge2TargetNodeFunc = edge2TargetNodeFunc;

            _node2IdDict = new Dictionary<T1, int>();
            //_edge2IdDict = new Dictionary<T2, int>();
            int index = 0;
            foreach (var node in nodes)
            {
                _node2IdDict.Add(node, index++);
            }
            //index = 0;
            //foreach (var edge in edges)
            //{
            //    _edge2IdDict.Add(edge, index++);
            //}
            //Initialize graph
            _bridgeGraph = new BridgeGraph(nodes.Count());
            _indices2EdgeDict = new Dictionary<Tuple<int, int>, T2>();
            foreach (var edge in edges)
            {
                var tuple = GetEdge(edge);
                _bridgeGraph.AddEdge(tuple.Item1, tuple.Item2);
            }
        }

        public void AddEdge(T2 edge)
        {
            var tuple = GetEdge(edge);
            _bridgeGraph.AddEdge(tuple.Item1, tuple.Item2);
        }

        public void RemoveEdge(T2 edge)
        {
            var tuple = GetEdge(edge);
            _bridgeGraph.RemoveEdge(tuple.Item1, tuple.Item2);
            _indices2EdgeDict.Remove(tuple);
        }

        private Tuple<int, int> GetEdge(T2 edge)
        {
            var node1 = _node2IdDict[_edge2SourceNodeFunc(edge)];
            var node2 = _node2IdDict[_edge2TargetNodeFunc(edge)];
            if (node1 > node2)
            {
                Util.Swap(ref node1, ref node2);
            }
            var tuple = Tuple.Create(node1, node2);
            if (!_indices2EdgeDict.ContainsKey(tuple))
            {
                _indices2EdgeDict.Add(Tuple.Create(node1, node2), edge);
            }
            return tuple;
        }

        public HashSet<T2> GetBridges()
        {
            HashSet<T2> bridges = new HashSet<T2>();
            foreach (var bridge in _bridgeGraph.GetBridges())
            {
                if (bridge.Item1 <= bridge.Item2)
                {
                    bridges.Add(_indices2EdgeDict[bridge]);
                }
                else
                {
                    bridges.Add(_indices2EdgeDict[Tuple.Create(bridge.Item2, bridge.Item1)]);
                }
            }
            return bridges;
        }

        public static void Test()
        {
            var graph = new Graph();
            var node0 = graph.AddNode();
            var node1 = graph.AddNode();
            var node2 = graph.AddNode();
            var node3 = graph.AddNode();
            var node4 = graph.AddNode();
            var node5 = graph.AddNode();

            var edge0 = graph.AddEdge(node0, node1);
            var edge1 = graph.AddEdge(node0, node2);
            var edge2 = graph.AddEdge(node1, node2);
            var edge3 = graph.AddEdge(node1, node3);
            var edge4 = graph.AddEdge(node3, node4);
            var edge5 = graph.AddEdge(node3, node5);

            var bridgeCalc = new BridgeCalculator<INode, IEdge>(graph.Nodes, graph.Edges,
                edge => edge.SourceNode, edge => edge.TargetNode);
            foreach (var bridge in bridgeCalc.GetBridges())
            {
                Console.WriteLine("bridge: " + bridge.Row);
            }
        }

    }

    class BridgeGraph
    {
        private int V; // No. of vertices

        // Array  of lists for Adjacency List Representation
        private List<int>[] adj;
        private int time = 0;
        private static int NIL = -1;

        // Constructor
        public BridgeGraph(int v)
        {
            V = v;
            adj = new List<int>[v];
            for (int i = 0; i < v; ++i)
                adj[i] = new List<int>();
        }

        // Function to add an edge into the graph
        public void AddEdge(int v, int w)
        {
            adj[v].Add(w); // Add w to v's list.
            adj[w].Add(v); //Add v to w's list
        }

        public void RemoveEdge(int v, int w)
        {
            adj[v].Remove(w); // Remove w to v's list.
            adj[w].Remove(v); // Remove v to w's list
        }


        // A recursive function that finds and prints bridges
        // using DFS traversal
        // u --> The vertex to be visited next
        // visited[] --> keeps tract of visited vertices
        // disc[] --> Stores discovery times of visited vertices
        // parent[] --> Stores parent vertices in DFS tree
        private List<Tuple<int, int>> bridgeUtil(int u, bool[] visited, int[] disc,
            int[] low, int[] parent)
        {
            List<Tuple<int,int>> bridges = new List<Tuple<int, int>>();

            // Count of children in DFS Tree
            int children = 0;

            // Mark the current node as visited
            visited[u] = true;

            // Initialize discovery time and low value
            disc[u] = low[u] = ++time;

            // Go through all vertices aadjacent to this
            var i = adj[u].GetEnumerator();
            while (i.MoveNext())
            {
                int v = i.Current; // v is current adjacent of u

                // If v is not visited yet, then make it a child
                // of u in DFS tree and recur for it.
                // If v is not visited yet, then recur for it
                if (!visited[v])
                {
                    parent[v] = u;
                    bridges.AddRange(bridgeUtil(v, visited, disc, low, parent));

                    // Check if the subtree rooted with v has a
                    // connection to one of the ancestors of u
                    low[u] = Math.Min(low[u], low[v]);

                    // If the lowest vertex reachable from subtree
                    // under v is below u in DFS tree, then u-v is
                    // a bridge
                    if (low[v] > disc[u])
                    {
                        bridges.Add(Tuple.Create(u, v));
                        //Console.WriteLine(u + " " + v);
                    }
                }

                // Update low value of u for parent function calls.
                else if (v != parent[u])
                    low[u] = Math.Min(low[u], disc[v]);
            }

            return bridges;
        }


        // DFS based function to find all bridges. It uses recursive
        // function bridgeUtil()
        public List<Tuple<int, int>> GetBridges()
        {
            // Mark all the vertices as not visited
            bool[] visited = new bool[V];
            int[] disc = new int[V];
            int[] low = new int[V];
            int[] parent = new int[V];


            // Initialize parent and visited, and ap(articulation point)
            // arrays
            for (int i = 0; i < V; i++)
            {
                parent[i] = NIL;
                visited[i] = false;
            }

            // Call the recursive helper function to find Bridges
            // in DFS tree rooted with vertex 'i'
            List<Tuple<int, int>> bridges = new List<Tuple<int, int>>();
            for (int i = 0; i < V; i++)
            {
                if (visited[i] == false)
                {
                    bridges.AddRange(bridgeUtil(i, visited, disc, low, parent));
                }
            }
            return bridges;
        }

        public static void Test()
        {
            // Create graphs given in above diagrams
            Console.WriteLine("Bridges in first graph ");
            BridgeGraph g1 = new BridgeGraph(5);
            g1.AddEdge(1, 0);
            g1.AddEdge(0, 2);
            g1.AddEdge(2, 1);
            g1.AddEdge(0, 3);
            g1.AddEdge(3, 4);
            foreach (var bridge in g1.GetBridges())
            {
                Console.WriteLine("bridge: " + bridge.Item1 + " " + bridge.Item2);
            }

            Console.WriteLine("Bridges in Second graph");
            BridgeGraph g2 = new BridgeGraph(4);
            g2.AddEdge(0, 1);
            g2.AddEdge(1, 2);
            g2.AddEdge(2, 3);
            foreach (var bridge in g2.GetBridges())
            {
                Console.WriteLine("bridge: " + bridge.Item1 + " " + bridge.Item2);
            }

            Console.WriteLine("Bridges in Third graph ");
            BridgeGraph g3 = new BridgeGraph(7);
            g3.AddEdge(0, 1);
            g3.AddEdge(1, 2);
            g3.AddEdge(2, 0);
            g3.AddEdge(1, 3);
            g3.AddEdge(1, 4);
            g3.AddEdge(1, 6);
            g3.AddEdge(3, 5);
            g3.AddEdge(4, 5);
            foreach (var bridge in g3.GetBridges())
            {
                Console.WriteLine("bridge: " + bridge.Item1 + " " + bridge.Item2);
            }
        }
    }

// This code is contributed by Aakash Hasija

    internal class GraphUtils
    {

    }
}
