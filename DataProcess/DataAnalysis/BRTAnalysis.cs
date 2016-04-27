using DataProcess.DataVisualization;
using DataProcess.Utils;
using Lava.Data;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataProcess.DataAnalysis
{
    #region Data structures
    interface ITreeNode
    {
        int CorpusID { get; }
        int TopicID { get; }
        IDictionary<int, double> TopicWords { get; }
        IEnumerable<ITreeNode> Children { get; }
        ITreeNode Parent { get; }
        int ChildCount { get; }
        int TreeLevel { get; }

        bool IsFiltered { get; }
    }

    enum UserLabelType { Relevant, NotRelevant, Mixed };

    class TreeNodeScheme : TableScheme
    {
        readonly static string _schemeProperty = "TreeNodeScheme";
        readonly int _topicIDColumn;
        readonly int _topicWordsColumn;
        readonly int _interTreeEdgesColumn;
        readonly int _topicLeafDocIndexColumn;
        readonly int _docIndexColumn;
        readonly int _userLabelTypeColumn;
        readonly int _correctDocCountColumn;
        readonly int _allDocCountColumn;

        public readonly string TopicIDColumn;
        public readonly string TopicWordsColumn;

        IGraph _graph;
        public TreeNodeScheme(IGraph graph, string topicIDColumn = null, string topicWordsColumn = null, bool isContraintTree = false)
            : base(graph.NodeTable, _schemeProperty)
        {
            if (topicIDColumn == null)
                topicIDColumn = "TopicID";
            if (topicWordsColumn == null)
                topicWordsColumn = "TopicWords";
            _isContraintTree = isContraintTree;

            _graph = graph;
            _topicIDColumn = AddColumn<int>(topicIDColumn);
            _topicWordsColumn = AddColumn<Dictionary<string, double>>(topicWordsColumn);
            _interTreeEdgesColumn = AddColumn<Dictionary<int, Tuple<double, double>>>("InterTreeEdges");
            _topicLeafDocIndexColumn = AddColumn<List<int>>("LeafIDs");
            _docIndexColumn = AddColumn<int>("DocIndex", -1);
            _userLabelTypeColumn = AddColumn<UserLabelType>("UserLabelType", UserLabelType.NotRelevant);
            _correctDocCountColumn = AddColumn<int>("CorrectCount", 0);
            _allDocCountColumn = AddColumn<int>("AllCount", 0);

            TopicIDColumn = topicIDColumn;
            TopicWordsColumn = topicWordsColumn;
        }

        //Topic id
        public int GetTopicID(INode node)
        {
            return node.Get<int>(_topicIDColumn);
        }
        public void SetTopicID(INode node, int topicID)
        {
            node.Set<int>(_topicIDColumn, topicID);
        }
        //Topic id - index
        public INode GetNodeByTopicID(int topicID)
        {
            var index = _table.Index<int>(_topicIDColumn);
            var row = index.First(topicID);
            if (row >= 0)
                return _graph.GetNode(row);
            else
                return null;
        }
        public INode GetNodeByRow(int row)
        {
            return _graph.GetNode(row);
        }

        //Topic words - get,set
        public Dictionary<string, double> GetTopicWords(INode node)
        {
            return node.Get<Dictionary<string, double>>(_topicWordsColumn);
        }
        public void SetTopicWords(INode node, Dictionary<string, double> topicWords)
        {
            node.Set<Dictionary<string, double>>(_topicWordsColumn, topicWords);
        }

        //Inter tree edges
        public Dictionary<int, Tuple<double, double>> GetInterTreeEdges(INode node)
        {
            return node.Get<Dictionary<int, Tuple<double, double>>>(_interTreeEdgesColumn);
        }
        public void SetInterTreeEdges(INode node, Dictionary<int, Tuple<double, double>> interTreeEdges)
        {
            node.Set<Dictionary<int, Tuple<double, double>>>(_interTreeEdgesColumn, interTreeEdges);
        }

        //Topic leaf docIndices
        public List<int> GetTopicLeafDocIndices(INode node)
        {
            return node.Get<List<int>>(_topicLeafDocIndexColumn);
        }
        public void SetTopicLeafDocIndices(INode node, List<int> docIndices)
        {
            node.Set<List<int>>(_topicLeafDocIndexColumn, docIndices);
        }

        //XmlDoc index
        public int GetDocIndex(INode node)
        {
            return node.Get<int>(_docIndexColumn);
        }
        public void SetDocIndex(INode node, int docIndex)
        {
            node.Set<int>(_docIndexColumn, docIndex);
        }

        //User label type
        public UserLabelType GetUserLabelType(INode node)
        {
            return node.Get<UserLabelType>(_userLabelTypeColumn);
        }
        public void SetUserLabelType(INode node, UserLabelType labelType)
        {
            node.Set<UserLabelType>(_userLabelTypeColumn, labelType);
        }

        IndexReader _indexReader;
        //Index reader
        public IndexReader GetIndexReader()
        {
            return _indexReader;
        }
        public void SetIndexReader(IndexReader indexReader)
        {
            _indexReader = indexReader;
        }

        //XmlDoc Count
        public void IncrementCorrectDocCount(INode node)
        {
            node.Set<int>(_correctDocCountColumn, node.Get<int>(_correctDocCountColumn) + 1);
        }
        public void IncrementTotalDocCount(INode node)
        {
            node.Set<int>(_allDocCountColumn, node.Get<int>(_allDocCountColumn) + 1);
        }
        public void SetCorrectDocCount(INode node, int correctCnt)
        {
            node.Set<int>(_correctDocCountColumn, correctCnt);
        }
        public void SetTotalDocCount(INode node, int totalCnt)
        {
            node.Set<int>(_allDocCountColumn, totalCnt);
        }
        public void GetCorrectDocCount(INode node, out int correctCnt, out int allCount)
        {
            correctCnt = node.Get<int>(_correctDocCountColumn);
            allCount = node.Get<int>(_allDocCountColumn);
        }
        public void ClearDocCount()
        {
            foreach(var node in _graph.Nodes)
            {
                SetCorrectDocCount(node, 0);
                SetTotalDocCount(node, 0);
            }
        }

        string _brtFile;
        public void SetBRTFileName(string brtFile)
        {
            _brtFile = brtFile;
        }
        public string GetBRTFileName()
        {
            return _brtFile;
        }


        private bool _isContraintTree;
        public bool GetIsContraintTree()
        {
            return _isContraintTree;
        }

        //Get scheme
        public static TreeNodeScheme Get(ITable nodeTable)
        {
            return (TreeNodeScheme)Get(nodeTable, _schemeProperty);
        }
    }
 

    class TreeDataParser
    {
        string _filename;
        string _topicIDColumn;
        string _topicWordsColumn;
        bool _isLoadTopicWords;
        bool _isRemoveDocuments;
        private bool _isContraintTree;
        ITree _tree = null;
        TreeNodeScheme _scheme;
        public TreeDataParser(string filename, bool isRemoveDocuments = true, bool isLoadTopicWords = true, bool isContraintTree = false)
        {
            _filename = filename;
            _topicIDColumn = "ID";
            _topicWordsColumn = "Words";
            _isRemoveDocuments = isRemoveDocuments;

            _isLoadTopicWords = isLoadTopicWords;
            _isContraintTree = isContraintTree;
        }

        public ITree GetTree()
        {
            if (_tree == null)
            {
                var graph = new Graph(true);
                var scheme = new TreeNodeScheme(graph, _topicIDColumn, _topicWordsColumn, _isContraintTree);
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
                        //var fakeNodeID = line.Substring(0, bracketIndex);

                        var quoteIndex1 = line.IndexOf('"');
                        var quoteIndex2 = line.IndexOf('"', quoteIndex1 + 1);
                        var content = line.Substring(quoteIndex1 + 1, quoteIndex2 - quoteIndex1 - 1);
                        var realNodeID = content.Substring(1, content.IndexOf("-", 2) - 1);
                        var row = nodeID2Row[int.Parse(realNodeID)];
                        var node = graph.GetNode(row);
                        if (_isContraintTree)
                        {
                            var interEdges = GetInterTreeEdges(content);
                            scheme.SetInterTreeEdges(node, interEdges);
                        }
                        else
                        {
                            var words = isLoadTopicWords ? GetWordDict(content) : null;
                            scheme.SetTopicWords(node, words);
                        }
                        scheme.SetTopicID(node, int.Parse(realNodeID));
                        scheme.SetDocIndex(node, GetDocIndex(content));
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
                _scheme = scheme;

                CalculateDocuments();

                if (_isRemoveDocuments)
                    RemoveDocuments();
            }
            return _tree;
        }

        
        private void CalculateDocuments()
        {
            var scheme = _scheme;
            var nodeList = _tree.BFS(_tree.Root);
            foreach (var node in nodeList)
            {
                scheme.SetTopicLeafDocIndices(node, new List<int>());
                if (scheme.GetDocIndex(node) >= 0)
                {
                    scheme.GetTopicLeafDocIndices(node).Add(scheme.GetDocIndex(node));
                }
            }

            foreach(var node in nodeList.Reverse())
            {
                var docList = scheme.GetTopicLeafDocIndices(node);
                var parent = _tree.GetParent(node);
                if (parent != null)
                    scheme.GetTopicLeafDocIndices(parent).AddRange(docList);
            }
        }

        private void RemoveDocuments()
        {
            var scheme = _scheme;
            var nodeList = _tree.BFS(_tree.Root);
            foreach(var node in nodeList.Reverse())
            {
                if(scheme.GetDocIndex(node) >= 0)
                {
                    _tree.Graph.RemoveNode(node);
                }
            }
            _tree = _tree.Graph.GetSpanningTree(_tree.Root);
        }

        private Dictionary<string, double> GetWordDict(string content)
        {
            var wordDict = new Dictionary<string, double>();

            int index0 = content.IndexOf("\\n") + 2, index1;
            int wordCnt = 0;
            while ((index1 = content.IndexOf("\\n", index0)) >= 0)
            {
                var parenthIdx1 = content.IndexOf('(', index0);
                if (parenthIdx1 < 0) break;
                var parenthIdx2 = content.IndexOf(')', parenthIdx1);
                var word = content.Substring(index0, parenthIdx1 - index0);
                var frq = double.Parse(content.Substring(parenthIdx1 + 1, parenthIdx2 - parenthIdx1 - 1));
                wordDict.Add(word, frq);
                wordCnt++;
                index0 = index1 + 2;
            }

            return SortUtils.EnsureSortedByValue(wordDict);
        }

        private Dictionary<int, Tuple<double, double>> GetInterTreeEdges(string content)
        {
            var interTreeEdges = new Dictionary<int, Tuple<double, double>>();

            int index0 = content.IndexOf("\\n") + 2, index1;
            var sep = new char[] {','};
            while ((index1 = content.IndexOf("\\n", index0)) >= 0)
            {
                var parenthIdx1 = content.IndexOf('(', index0);
                if (parenthIdx1 < 0) break;
                var parenthIdx2 = content.IndexOf(')', parenthIdx1);
                var id = int.Parse(content.Substring(index0 + 1, parenthIdx1 - index0 - 1));
                var subStr = content.Substring(parenthIdx1 + 1, parenthIdx2 - parenthIdx1 - 1);
                var tokens = subStr.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                var weight1 = double.Parse(tokens[0]);
                var weight2 = double.Parse(tokens[1]);
                interTreeEdges.Add(id, Tuple.Create(weight1, weight2));
                index0 = index1 + 2;
            }

            return interTreeEdges;
        }

        private int GetDocIndex(string content)
        {
            int index0 = content.IndexOf('[');
            if (index0 >= 0)
            {
                int index1 = content.IndexOf(']', index0 + 1);
                return int.Parse(content.Substring(index0 + 1, index1 - index0 - 1));
            }
            return -1;
        }
    }

    class TreeDataWriter
    {
        private ITree _tree;
        private TreeNodeScheme _scheme;
        private string _brtFileName;
        public TreeDataWriter(ITree tree, string brtFileName)
        {
            _tree = tree;
            _brtFileName = brtFileName;
            _scheme = TreeNodeScheme.Get(_tree.Graph.NodeTable);
        }

        public void WriteTree()
        {
            FileOperations.EnsureFileFolderExist(_brtFileName);
            var sw = new StreamWriter(_brtFileName);
            sw.WriteLine("digraph G \n {graph[ \n rankdir = \"TD\"];");

            TreeUtils.BreadthFirstTraversal(_tree.Root, (node=>_tree.GetChildren(node)), (node, curLevel) =>
            {
                var children = _tree.GetChildren(node);
                if (children != null)
                {
                    for (int j = 0; j < children.Count; j++)
                    {
                        sw.WriteLine(node.Row + "->" + children[j].Row);
                    }
                }
                sw.Write(node.Row + "[color = grey, label =\"");

                DrawNode(node, sw);

                sw.WriteLine("\"" + ", shape=\"record\"];");

                return true;
            });
            
            sw.WriteLine("}");
            sw.Flush();
            sw.Close();
        }


        private void DrawNode(INode node, StreamWriter sw)
        {
            if (_scheme.GetIsContraintTree())
            {
                sw.Write("-{0}-\\n", _scheme.GetTopicID(node));
                foreach (var kvp in _scheme.GetInterTreeEdges(node))
                {
                    sw.Write("~{0} ({1},{2})~\\n", kvp.Key, kvp.Value.Item1, kvp.Value.Item2);
                }
            }
            else
            {
                if (_tree.GetChildCount(node) == 0)
                {
                    sw.Write("-{0}-\\n", _scheme.GetTopicID(node));
                    sw.Write("[{0}]\\n", _scheme.GetDocIndex(node));
                }
                else
                {
                    sw.Write("-{0}-\\n", _scheme.GetTopicID(node));
                    foreach (var kvp in _scheme.GetTopicWords(node))
                    {
                        sw.Write("{0}({1})\\n", kvp.Key, kvp.Value);
                    }
                }
            }
        }

    }

    class DocumentProjectTuple
    {
        public int ProjectNodeID { get; protected set; }
        public int ProjectNodeParentID { get; protected set; }
        public float CosineSimilarity { get; protected set; }

        public DocumentProjectTuple()
        {
            
        }

        public DocumentProjectTuple(int projectNodeID, int projectParentNodeID, float cosineSimilarity)
        {
            ProjectNodeID = projectNodeID;
            ProjectNodeParentID = projectParentNodeID;
            CosineSimilarity = cosineSimilarity;
        }

        public void Read(BinaryReader br)
        {
            ProjectNodeID = br.ReadInt32();
            ProjectNodeParentID = br.ReadInt32();
            CosineSimilarity = br.ReadSingle();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(ProjectNodeID);
            bw.Write(ProjectNodeParentID);
            bw.Write(CosineSimilarity);
        }
    }

    class DocumentProjectInfo
    {
        public int TupleCount { get; protected set; }
        public int DocumentID { get; protected set; }
        public int DocumentParentID { get; protected set; }
        public List<DocumentProjectTuple> DocumentProjectTupleList { get; protected set; }

        public DocumentProjectInfo()
        {
            
        }

        public DocumentProjectInfo(int tupleCount, int documentID, int documentParentID, List<DocumentProjectTuple> tupleList)
        {
            TupleCount = tupleCount;
            DocumentID = documentID;
            DocumentParentID = documentParentID;
            DocumentProjectTupleList = tupleList;
        }

        public void Read(BinaryReader br)
        {
            TupleCount = br.ReadInt32();
            DocumentID = br.ReadInt32();
            DocumentParentID = br.ReadInt32();
            DocumentProjectTupleList = new SupportClass.EquatableList<DocumentProjectTuple>();
            for (int i = 0; i < TupleCount; i++)
            {
                var tuple = new DocumentProjectTuple();
                tuple.Read(br);
                DocumentProjectTupleList.Add(tuple);
            }
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(TupleCount);
            bw.Write(DocumentID);
            bw.Write(DocumentParentID);
            foreach (var tuple in DocumentProjectTupleList)
            {
                tuple.Write(bw);
            }
        }
    }
    #endregion

    class BRTAnalysis
    {
        public static void AnalyzeTreeStructure()
        {
            //string folder = @"D:\Project\StreamingRoseRiver\EbolaCaseStudyFinal\RoseRiver\Data\KddInfovisGraphicsIndex_Lucene_a=0.003_sm=1-test\";
            string folder = @"D:\Project\StreamingRoseRiver\EbolaCaseStudyFinal\Trees3\";
            int index = 0;
            DoubleStatistics stat = new DoubleStatistics();
            while (true)
            {
                var fileName = folder + index + ".gv";
                if (!File.Exists(fileName))
                {
                    break;
                }
                var tree = ReadTree(fileName);
                stat.AddNumber(tree.Graph.NodeCount);
                Console.WriteLine(tree.Graph.NodeCount);
                //Console.WriteLine(index + "\t" + stat.ToStringShort());
                //Console.Write(stat.GetAverage() + "\t");
                index++;
            }
        }

        public static void GenerateCopiedData()
        {
            //string inputFolder = @"D:\Project\StreamingRoseRiver\EbolaCaseStudyFinal\RoseRiver\Data\KddInfovisGraphicsIndex_Lucene_a=0.003_sm=1\";
            string inputFolder = @"D:\Project\StreamingRoseRiver\EbolaCaseStudyFinal\Trees3\";
            if(!Directory.Exists(inputFolder))
            {
                inputFolder = @"D:\Documents\roseriver\RoseRiver\RoseRiver\Data\Ebola\Trees3\";
            }
            int[] copyFactors = new[] { 11 }; //Util.GetIntArray(2, 10); //new int[] { 2, 5, 10, 20, 50 };
            int largestTopicID = 10000;

            int index = 0;
            while (true)
            {
                var fileName = inputFolder + index + ".gv";
                if (!File.Exists(fileName))
                {
                    break;
                }

                var tree = ReadTree(inputFolder + index + ".gv", false);
                foreach (var copyFactor in copyFactors)
                {
                    string outputFolder = inputFolder.Substring(0, inputFolder.Length - 1) + "-CopyFactor" + copyFactor + "\\";
                    WriteTree(GetCopiedTree(tree, copyFactor, ref largestTopicID), outputFolder + index + ".gv");
                }

                if (index != 0)
                {
                    var ctree = ReadTree(inputFolder + index + "c_i.gv", false, true);
                    foreach (var copyFactor in copyFactors)
                    {
                        string outputFolder = inputFolder.Substring(0, inputFolder.Length - 1) + "-CopyFactor" + copyFactor + "\\";
                        WriteTree(GetCopiedTree(ctree, copyFactor, ref largestTopicID), outputFolder + index + "c_i.gv");
                    }

                    var projInfos = ReadProjectManyInfos(inputFolder + index + "c_proj_many.bin");
                    foreach (var copyFactor in copyFactors)
                    {
                        string outputFolder = inputFolder.Substring(0, inputFolder.Length - 1) + "-CopyFactor" + copyFactor + "\\";
                        WriteProjectManyInfos(GetCopiedProjectManyInfos(projInfos, copyFactor, largestTopicID), outputFolder + index + "c_proj_many.bin");
                    }
                }

                Console.WriteLine(index);
                index++;
            }
        }

        public static void Entry1()
        {
            string folder = @"D:\Project\StreamingRoseRiver\EbolaCaseStudyFinal\RoseRiver\Data\KddInfovisGraphicsIndex_Lucene_a=0.003_sm=1-test\";
            int largestTopicID = 10000;

            //0
            //largestTopicID = -1;
            var tree = BRTAnalysis.ReadTree(folder + "0.gv.bak", false);
            BRTAnalysis.WriteTree(BRTAnalysis.GetCopiedTree(tree, 2, ref largestTopicID), folder + "0.gv");

            //1
            //largestTopicID = -1;
            tree = BRTAnalysis.ReadTree(folder + "1.gv.bak", false);
            BRTAnalysis.WriteTree(BRTAnalysis.GetCopiedTree(tree, 2, ref largestTopicID), folder + "1.gv");
            var ctree = BRTAnalysis.ReadTree(folder + "1c_i.gv.bak", false, true);
            BRTAnalysis.WriteTree(BRTAnalysis.GetCopiedTree(ctree, 2, ref largestTopicID), folder + "1c_i.gv");
            var projInfos = BRTAnalysis.ReadProjectManyInfos(folder + "1c_proj_many.bin.bak");
            BRTAnalysis.WriteProjectManyInfos(BRTAnalysis.GetCopiedProjectManyInfos(projInfos, 2, largestTopicID), folder + "1c_proj_many.bin");

            Util.ProgramFinishHalt();
        }

        #region tree
        public static void VisualizeTree(string brtFile, string luceneIndex = null, string[] keywords = null, bool isRemoveLeafNodes = true)
        {
            VisualizeTree(new string[] { brtFile }, luceneIndex, keywords, isRemoveLeafNodes);
        }

        public static void VisualizeTree(IEnumerable<string> brtFiles, string luceneIndex = null, string[] keywords = null, bool isRemoveLeafNodes = true)
        {
            List<ITree> trees = new List<ITree>();
            foreach (var brtFile in brtFiles)
            {
                //Read tree from file
                TreeDataParser parser = new TreeDataParser(brtFile, isRemoveLeafNodes);
                var tree = parser.GetTree();
                Trace.WriteLine(tree.GetDepth(tree.Root));
                if (luceneIndex != null)
                {
                    var scheme = TreeNodeScheme.Get(tree.Graph.NodeTable);
                    scheme.SetIndexReader(LuceneOperations.GetIndexReader(luceneIndex));
                    scheme.SetBRTFileName(brtFile);
                }
                trees.Add(tree);
            }

            //Print analyze info
            DoubleStatistics depthStat = new DoubleStatistics();
            DoubleStatistics internalNodeStat = new DoubleStatistics();
            foreach(var tree in trees)
            {
                depthStat.AddNumber(tree.BFS(tree.Root).Max(node =>
                {
                    int depth = 0;
                    INode ancestor = node;
                    while ((ancestor = tree.GetParent(ancestor)) != null)
                    {
                        depth++;
                    }
                    return depth;
                }) + 1);
                internalNodeStat.AddNumber(tree.BFS(tree.Root).Count());
            }
            Console.WriteLine(depthStat.ToString());
            Console.WriteLine(internalNodeStat.ToString());

            //Visualize tree
            Thread NetServer = new Thread(new ThreadStart(() =>
            {
                TreeVisualization treeVis = new TreeVisualization(trees, keywords);
            }));
            NetServer.SetApartmentState(ApartmentState.STA);
            NetServer.IsBackground = true;
            NetServer.Start();
            System.Windows.Threading.Dispatcher.Run();
        }

        
        public static ITree ReadTree(string brtFile, bool isRemoveLeafNodes = true, bool isContraintTree = false)
        {
            TreeDataParser parser = new TreeDataParser(brtFile, isRemoveLeafNodes, true, isContraintTree);
            var tree = parser.GetTree();
            return tree;
        }

        public static void WriteTree(ITree tree, string brtFile)
        {
            var writer = new TreeDataWriter(tree, brtFile);
            writer.WriteTree();
        }


        public static ITree GetCopiedTree(ITree tree, int copyFactor, ref int largestTopicID)
        {
            if (copyFactor < 2)
            {
                throw new NotImplementedException();
            }

            var scheme = TreeNodeScheme.Get(tree.Graph.NodeTable);
            var graph2 = new Graph();
            var scheme2 = new TreeNodeScheme(graph2, isContraintTree: scheme.GetIsContraintTree());

            //root
            var root2 = graph2.AddNode();

            //below
            var nodeBFSList = TreeUtils.GetBreathFirstTraversalList(tree.Root, node => tree.GetChildren(node));
            if (largestTopicID == -1)
            {
                largestTopicID = nodeBFSList.Max(node => scheme.GetTopicID(node));
            }
            var offset = 0;
            for (int iFactor = 0; iFactor < copyFactor; iFactor++)
            {
                foreach (var node in nodeBFSList)
                {
                    //Add node
                    var node2 = graph2.AddNode();
                    scheme2.SetTopicID(node2, scheme.GetTopicID(node) + offset);
                    scheme2.SetDocIndex(node2, scheme.GetDocIndex(node));
                    var words = scheme.GetTopicWords(node);
                    if (words != null)
                    {
                        scheme2.SetTopicWords(node2, new Dictionary<string, double>(words));
                    }
                    var interTreeLinks = scheme.GetInterTreeEdges(node);
                    if (interTreeLinks != null)
                    {
                        var interTreeLinks2 = new Dictionary<int, Tuple<double, double>>();
                        foreach (var kvp in interTreeLinks)
                        {
                            interTreeLinks2.Add(kvp.Key + offset, Tuple.Create(kvp.Value.Item1, kvp.Value.Item2));
                        }
                        scheme2.SetInterTreeEdges(node2, interTreeLinks2);
                    }

                    //Add edge
                    var parent = tree.GetParent(node);
                    INode parent2;
                    if (parent == null)
                    {
                        parent2 = root2;
                    }
                    else
                    {
                        parent2 = scheme2.GetNodeByTopicID(scheme.GetTopicID(parent) + offset);
                    }
                    graph2.AddEdge(parent2, node2);
                }

                offset += largestTopicID + 100;
            }

            //set root
            scheme2.SetTopicID(root2, 999999);
            if (scheme2.GetIsContraintTree())
            {
                var interTreeLinks2 = new Dictionary<int, Tuple<double, double>>();
                interTreeLinks2.Add(999999, Tuple.Create(1.0, 1.0));
                scheme2.SetInterTreeEdges(root2, interTreeLinks2);
            }
            else
            {
                var words = scheme.GetTopicWords(tree.Root);
                var words2 = new Dictionary<string, double>();
                foreach (var kvp in words)
                {
                    words2.Add(kvp.Key, kvp.Value*copyFactor);
                }
                scheme2.SetTopicWords(root2, words2);
            }

            return graph2.GetSpanningTree(root2);
        }
        #endregion

        #region proj_many file
        public static List<DocumentProjectInfo> ReadProjectManyInfos(string fileName)
        {
            List<DocumentProjectInfo> infos = new List<DocumentProjectInfo>();
            var br = new BinaryReader(File.OpenRead(fileName));
            while (!FileOperations.IsEndOfFile(br))
            {
                var info = new DocumentProjectInfo();
                info.Read(br);
                infos.Add(info);
            }
            br.Close();

            return infos;
        }

        public static void WriteProjectManyInfos(List<DocumentProjectInfo> infos, string fileName)
        {
            var bw = new BinaryWriter(File.OpenWrite(fileName));

            foreach (var info in infos)
            {
                info.Write(bw);
            }

            bw.Flush();
            bw.Close();
        }

        public static List<DocumentProjectInfo> GetCopiedProjectManyInfos(List<DocumentProjectInfo> infos, int copyFactor, int largestTopicID)
        {
            List<DocumentProjectInfo> infos2 = new List<DocumentProjectInfo>();
            int offset = 0;
            for (int iFactor = 0; iFactor < copyFactor; iFactor++)
            {
                foreach (var info in infos)
                {
                    var tupleList2 = new List<DocumentProjectTuple>();
                    foreach (var tuple in info.DocumentProjectTupleList)
                    {
                        DocumentProjectTuple tuple2;
                        if (tuple.ProjectNodeID >= 0)
                        {
                            tuple2 = new DocumentProjectTuple(tuple.ProjectNodeID + offset,
                                tuple.ProjectNodeParentID + offset, tuple.CosineSimilarity);
                        }
                        else if(tuple.ProjectNodeID == -1)
                        {
                            tuple2 = new DocumentProjectTuple(tuple.ProjectNodeID, tuple.ProjectNodeParentID + offset, tuple.CosineSimilarity);
                        }
                        else if (tuple.ProjectNodeID == -2)
                        {
                            tuple2 = new DocumentProjectTuple(tuple.ProjectNodeID, tuple.ProjectNodeParentID, tuple.CosineSimilarity);
                        }
                        else
                        {
                            throw new ArgumentException();
                        }
                        tupleList2.Add(tuple2);
                    }
                    var info2 = new DocumentProjectInfo(info.TupleCount, info.DocumentID + offset, info.DocumentParentID + offset, tupleList2);
                    infos2.Add(info2);
                }

                offset += largestTopicID + 100;
            }
            return infos2;
        }
        #endregion
    }
}
