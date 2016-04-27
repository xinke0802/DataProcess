using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lava.Data;

namespace DataProcess.DataTransform
{
    public class RoseTreeDataWriter
    {
        private ITree _tree;
        private string _topicIDColumn;
        private string _topicWordsColumn;

        private Dictionary<INode, int> _treeIndexDict;

        public RoseTreeDataWriter(ITree tree, string topicIDColumn, string topicWordsColumn)
        {
            this._tree = tree;
            this._topicIDColumn = topicIDColumn;
            this._topicWordsColumn = topicWordsColumn;

            this._treeIndexDict = GetTreeIndexDict();
        }


        public void WriteTree(string fileName, string[] dictionary = null, bool isOrderByCount = false)
        {
            var drawTree = new StreamWriter(fileName);
            drawTree.WriteLine("digraph G \n {graph[ \n rankdir = \"TD\"];");

            var nodelist = new List<INode>();
            nodelist.Add(_tree.Root);

            int depth = 0;

            while (nodelist.Count != 0)
            {
                int nodelistcount = nodelist.Count;
                for (int i = 0; i < nodelistcount; i++)
                {
                    var node = nodelist[0];

                    if (_tree.GetChildCount(node) != 0)
                    {
                        foreach (var child in _tree.GetChildren(node))
                        {
                            drawTree.WriteLine(_treeIndexDict[node] + "->" + _treeIndexDict[child]);
                            nodelist.Add(child);
                        }
                    }
                    drawTree.Write(_treeIndexDict[node] + "[color = grey, label =\"");

                    DrawNode(depth, node, drawTree, dictionary, isOrderByCount);

                    drawTree.WriteLine("\"" + ", shape=\"record\"];");

                    nodelist.RemoveAt(0);
                }
                depth++;
            }

            drawTree.WriteLine("}");
            drawTree.Flush();
            drawTree.Close();
        }

        void DrawNode(int depth, INode node, StreamWriter drawTree, string[] dictionary = null, bool isOrderByCount = false)
        {
            drawTree.Write("-{0}-\\n", node.Get<int>(_topicIDColumn));
            var dict = new Dictionary<int, double>(node.Get<Dictionary<int, double>>(_topicWordsColumn));

            if (isOrderByCount)
            {
                foreach (var kvp in dict.OrderByDescending(kvp => kvp.Value))
                {
                    drawTree.Write("{0}({1})\\n", (dictionary == null ? kvp.Key.ToString() : dictionary[kvp.Key]), kvp.Value.ToString());
                }
            }
            else
            {
                foreach (var kvp in dict)
                {
                    drawTree.Write("{0}({1})\\n", (dictionary == null ? kvp.Key.ToString() : dictionary[kvp.Key]), kvp.Value.ToString());
                }
            }
        }

        private Dictionary<INode, int> GetTreeIndexDict()
        {
            var res = new Dictionary<INode, int>();

            var nodelist = new List<INode>();
            nodelist.Add(_tree.Root);
            int tree_index = 0;
            var depth = 0;

            //HashSet<int> mergetreeindices = new HashSet<int>();
            while (nodelist.Count != 0)
            {
                int nodelistcount = nodelist.Count;
                depth++;
                for (int i = 0; i < nodelistcount; i++)
                {
                    var node = nodelist[0];
                    res.Add(node, tree_index);
                    tree_index++;
                    if (_tree.GetChildCount(node) != 0)
                        nodelist.AddRange(_tree.GetChildren(node));
                    nodelist.Remove(node);
                }
            }
            return res;
        }


    }

}
