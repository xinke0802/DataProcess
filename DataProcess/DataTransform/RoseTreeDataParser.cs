using System;
using System.Collections.Generic;
using System.IO;
using Lava.Data;
using DataProcess.DataAnalysis;

namespace DataProcess.DataTransform
{
    public class RoseTreeDataParser
    {
        string _filename;
        string _topicIDColumn;
        string _topicWordsColumn;
        bool _isLoadTopicWords;
        ITree _tree = null;

        public RoseTreeDataParser(string filename, string topicIDColumn, string topicWordsColumn, bool isLoadTopicWords = true)
        {
            _filename = filename;
            _topicIDColumn = topicIDColumn;
            _topicWordsColumn = topicWordsColumn;

            _isLoadTopicWords = isLoadTopicWords;
        }

        public ITree GetTree()
        {
            if (_tree == null)
            {
                var graph = new Graph(true);
                //var scheme = new TreeNodeScheme(graph, _topicIDColumn, _topicWordsColumn);
                graph.NodeTable.AddColumn<int>(_topicIDColumn);
                graph.NodeTable.AddColumn<Dictionary<int, double>>(_topicWordsColumn);

                var isLoadTopicWords = _isLoadTopicWords;

                var nodeID2Row = new Dictionary<int, int>();
                var gvEdges = new List<Tuple<string, string>>();
                var gvID2nodeID = new Dictionary<string, int>();
                var gvID2DocID = new Dictionary<string, int>();
                var allLines = File.ReadAllLines(_filename);
                for (int i = 3; i < allLines.Length; i++)
                {
                    var line = allLines[i];
                    if (line == null || line.StartsWith("}"))
                        break;
                    var arrowIndex = line.IndexOf("->");
                    // edge
                    if (arrowIndex > 0)
                    {
                        var gvParent = line.Substring(0, arrowIndex);
                        var gvChild = line.Substring(arrowIndex + 2);
                        gvEdges.Add(new Tuple<string, string>(gvParent, gvChild));
                    }
                    // node
                    else
                    {
                        var bracketIndex = line.IndexOf('[');
                        var gvID = line.Substring(0, bracketIndex);

                        var quoteIndex1 = line.IndexOf('"');
                        var quoteIndex2 = line.IndexOf('"', quoteIndex1 + 1);
                        var content = line.Substring(quoteIndex1 + 1, quoteIndex2 - quoteIndex1 - 1);
                        //Add node
                        var nodeID = int.Parse(content.Substring(1, content.IndexOf("-", 2) - 1));
                        var row = graph.NodeTable.AddRow();
                        gvID2nodeID.Add(gvID, nodeID);
                        nodeID2Row.Add(nodeID, row);                    //var arrs = content.Split(new string[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries);
                        //Trace.WriteLine(string.Format("{0}\t{1}", row, nodeID));
                        if (content.Contains("\\n\\n"))
                        {
                            // document node
                            gvID2DocID.Add(gvID, nodeID);
                        }
                    }
                }//gvID2nodeID, nodeID2Row, gvID2DocID, docID2Doc are ready

                allLines = File.ReadAllLines(_filename);
                for (int k = 3; k < allLines.Length; k++)
                {
                    var line = allLines[k];
                    if (line == null || line.StartsWith("}"))
                        break;
                    var index = line.IndexOf("->");
                    // edge
                    if (index > 0)
                        continue;
                    // node, get word dict
                    else
                    {
                        var bracketIndex = line.IndexOf('[');
                        var fakeNodeID = line.Substring(0, bracketIndex);

                        var quoteIndex1 = line.IndexOf('"');
                        var quoteIndex2 = line.IndexOf('"', quoteIndex1 + 1);
                        var content = line.Substring(quoteIndex1 + 1, quoteIndex2 - quoteIndex1 - 1);
                        var realNodeID = content.Substring(1, content.IndexOf("-", 2) - 1);
                        var words = isLoadTopicWords ? GetWordDict(content) : null;
                        var row = nodeID2Row[int.Parse(realNodeID)];
                        var node = graph.GetNode(row);
                        node.Set(_topicIDColumn, int.Parse(realNodeID));
                        node.Set(_topicWordsColumn, words);
                    }
                }

                //Tree edges
                foreach (var gvEdge in gvEdges)
                {
                    var pRow = nodeID2Row[gvID2nodeID[gvEdge.Item1]];
                    int nodeID = -1;
                    if (gvID2nodeID.TryGetValue(gvEdge.Item2, out nodeID))
                    {
                        graph.AddEdge(pRow, nodeID2Row[nodeID]);
                    }
                }

                _tree = graph.GetSpanningTree(0);
            }
            return _tree;
        }

        private static Dictionary<int, double> GetWordDict(string content)
        {
            var wordDict = new Dictionary<int, double>();

            int index0 = content.IndexOf("\\n") + 2, index1;
            int wordCnt = 0;
            while ((index1 = content.IndexOf("\\n", index0)) >= 0)
            {
                var parenthIdx1 = content.IndexOf('(', index0);
                if (parenthIdx1 < 0) break;
                var parenthIdx2 = content.IndexOf(')', parenthIdx1);
                var word = content.Substring(index0, parenthIdx1 - index0);
                var frq = double.Parse(content.Substring(parenthIdx1 + 1, parenthIdx2 - parenthIdx1 - 1));
                wordDict.Add(int.Parse(word), frq);
                wordCnt++;
                index0 = index1 + 2;
            }

            return wordDict;
        }
    }

}
