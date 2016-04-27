using DataProcess.DataAnalysis;
using DataProcess.Utils;
using Lava.Data;
using Lava.Visual;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DataProcess.DataVisualization
{
    #region Data structures

    class TreeNodeVisualTableScheme : TableScheme
    {
        private static readonly string _schemeProperty = "TreeNodeVisualTableScheme";

        //VisualGraph _graph;
        IGraph _dataGraph;
        string[] _keywords;
        public TreeNodeVisualTableScheme(IVisualTable visTable, IGraph dataGraph, string[] keywords)
            : base(visTable, _schemeProperty)
        {
            //_graph = graph;
            _dataGraph = dataGraph;
            _keywords = keywords;

            visTable.AddConstantColumn<IRender>(Styles.Render, new TreeNodeRender());
            visTable.EnsureStyles(new string[] { Styles.X, Styles.Y, Styles.Visible, Styles.Hover, Styles.Layer });
            //graph.NodeTable.RemoveColumn(Styles.Layer);
            //graph.NodeTable.AddColumn<int>(Styles.Layer, 0);
        }

        //public VisualGraph GetVisualGraph()
        //{
        //    return _graph;
        //}
        public IGraph GetDataGraph()
        {
            return _dataGraph;
        }
        public string[] GetKeywords()
        {
            return _keywords;
        }

        //Get scheme
        public static TreeNodeVisualTableScheme Get(ITable nodeTable)
        {
            return (TreeNodeVisualTableScheme)Get(nodeTable, _schemeProperty);
        }
    }

    class TreeNodeLook : AbstractLook, ILook
    {
        public static double ShowKeywordNodeWidth = 40;
        public static double NoKeywordNodeWidth = 20;
        public static double NodeMinWidth = 20;
        public static double NodeHeight = 30;
        public static double NodeMinHeight = 20;
        public static double TextSize = 11;

        TreeNodeVisualTableScheme _scheme;
        TreeNodeScheme _dataScheme;
        INode _dataNode;

        public TreeNodeLook(IVisualItem vi)
            : base(vi)
        {
            _scheme = TreeNodeVisualTableScheme.Get(vi.Table);
            _dataNode = _scheme.GetDataGraph().GetNode(vi.Row);
            _dataScheme = TreeNodeScheme.Get(_dataNode.Table);
        }

        protected override void Paint(DrawingContext context)
        {
            //VisualNode visNode = (VisualNode)VisualItem;
            //var tree = visNode.Graph.GetSpanningTree(0);

            string label = GetLabel();

            var formattedText = new FormattedText(label,
                         new System.Globalization.CultureInfo("en-us"), FlowDirection.LeftToRight,
                         new Typeface(new FontFamily("Arial"), FontStyles.Normal,
                             FontWeights.Normal, FontStretches.Normal), TextSize, Brushes.DarkBlue)
                             {
                                 //LineHeight = lineHeight,
                                 TextAlignment = TextAlignment.Center
                             };

            double NodeWidth = Math.Max(NodeMinWidth, formattedText.Width);
            double NodeHeight = Math.Max(NodeMinHeight, formattedText.Height);

            var userLableType = _dataScheme.GetUserLabelType(_dataNode);
            int correctCnt, totalCnt;
            _dataScheme.GetCorrectDocCount(_dataNode, out correctCnt, out totalCnt);
            var correctRatio = totalCnt == 0 ? 0 : ((double)correctCnt / totalCnt);
            var pen = userLableType == UserLabelType.Relevant ? new Pen(Brushes.Red, 3) : userLableType == UserLabelType.Mixed ? new Pen(Brushes.Yellow, 2) : new Pen(Brushes.Black, 1);
            var brush = new SolidColorBrush() { Color = Lava.Util.InterpolatorLib.Interp(Colors.White, Colors.Red, correctRatio) };

            context.DrawRectangle(brush, pen,
                new Rect(-NodeWidth * 0.5, -NodeHeight * 0.5, NodeWidth, NodeHeight));
            context.DrawText(formattedText, new Point(0, -NodeHeight * 0.5));

            if(VisualItem.Hover)
            {
                var formattedText2 = new FormattedText(GetTooltipString(),
                  new System.Globalization.CultureInfo("en-us"), FlowDirection.LeftToRight,
                  new Typeface(new FontFamily("Arial"), FontStyles.Normal,
                      FontWeights.Normal, FontStretches.Normal), TextSize, Brushes.Black)
                {
                    //LineHeight = lineHeight,
                    TextAlignment = TextAlignment.Center,
                    MaxTextWidth = 200
                };
                double NodeWidth2 = Math.Max(NodeMinWidth, formattedText2.Width);
                double NodeHeight2 = Math.Max(NodeMinHeight, formattedText2.Height);
                context.DrawRectangle(Brushes.LightYellow, null,
                    new Rect(0, 0, NodeWidth2 + 10, NodeHeight2));
                context.DrawText(formattedText2, new Point(0, 0));
            }

            int layer = VisualItem.Hover ? 100 : 10;
            if (VisualItem.Layer != layer)
                VisualItem.Layer = layer;
        }

        private string GetLabel()
        {
            var leafDocCnt = _dataScheme.GetTopicLeafDocIndices(_dataNode).Count;
            var words = _dataScheme.GetTopicWords(_dataNode);
            string label = leafDocCnt.ToString() + "\n";
            int index = 0;
            foreach(var word in words.Keys)
            {
                label += word + "\n";
                if (++index == 3)
                    break;
            }
            return label;
        }

        private string GetTooltipString()
        {
            var words = _dataScheme.GetTopicWords(_dataNode);
            string label = "";
            int index = 0;
            foreach (var word in words.Keys)
            {
                label += word + ", ";
                if (++index == 20)
                    break;
            }
            var keywords = _scheme.GetKeywords();
            if(keywords != null && keywords.Length > 0)
            {
                label += "\n-------------------------\n";
                Dictionary<string, double> keywordsDict = new Dictionary<string, double>();
                foreach(var keyword in keywords)
                {
                    double value;
                    var keyword2 = keyword.ToLower();
                    if(words.TryGetValue(keyword2, out value))
                    {
                        if(!keywordsDict.ContainsKey(keyword2))
                        {
                            keywordsDict.Add(keyword2, value);
                        }
                    }
                }
                keywordsDict = SortUtils.EnsureSortedByValue(keywordsDict);
                label += StringOperations.GetMergedString(keywordsDict, ' ');
            }
            return label;
        }
    }

    class TreeNodeRender : AbstractRender, IRender
    {
        public TreeNodeRender()
            : base()
        {
            //StyleUpdaters[Styles.Hover] = look => true;
        }

        public override ILook CreateLook(IVisualItem vi)
        {
            ILook look = new TreeNodeLook(vi)
            {
                Visible = vi.Visible,
                ListenMouse = vi.ListenMouse,
                X = vi.X,
                Y = vi.Y,
            };
            return look;
        }
    }
    

    class SimpleEdgeLook : AbstractLook, ILook
    {
        public SimpleEdgeLook(IVisualItem vi)
            : base(vi)
        {
        }

        protected override void Paint(DrawingContext context)
        {
            Pen pen = new Pen()
            {
                Brush = VisualItem.FillBrush,
                Thickness = 1
            };

            context.DrawLine(pen,
                new Point(VisualItem.X1, VisualItem.Y1),
                new Point(VisualItem.X2, VisualItem.Y2));

            VisualItem.Layer = 1;
        }
    }

    public class SimpleEdgeRender : AbstractRender, IRender
    {
        public SimpleEdgeRender()
            : base()
        {
            //TO DO: Register StyleUpdaters here
            StyleUpdaters[Styles.X1] = look => { return true; };
            StyleUpdaters[Styles.Y1] = look => { return true; };
            StyleUpdaters[Styles.X2] = look => { return true; };
            StyleUpdaters[Styles.Y2] = look => { return true; };
        }

        public override ILook CreateLook(IVisualItem vi)
        {
            ILook look = new SimpleEdgeLook(vi)
            {
                Visible = vi.Visible,
                ListenMouse = vi.ListenMouse,
                X = vi.X,
                Y = vi.Y,
            };

            return look;
        }
    }

    #endregion

    class TreeVisualization
    {
        TreeWindow _window;
        Display _display;
        Visualization _vis;
        List<IVisualTable> _visNodeTables;
        List<IVisualTable> _visEdgeTables;
        List<TreeNodeVisualTableScheme> _visNodeSchemes;
        List<TreeNodeScheme> _dataNodeSchemes;

        string[] _keywords;
        List<ITree> _trees;

        string _titleField = BingNewsFields.NewsArticleHeadline;
        string _bodyField = BingNewsFields.NewsArticleDescription;

        VectorGenerator _vecGen;
        Application _app;

        public TreeVisualization(List<ITree> trees, string[] keywords = null)
        {
            _trees = trees;
            _dataNodeSchemes = trees.ConvertAll(tree => TreeNodeScheme.Get(tree.Graph.NodeTable));

            if(keywords == null)
            {
                keywords = new string[0];
            }
            _keywords = keywords;

            _vecGen = new VectorGenerator(new TokenizeConfig(TokenizerType.Standard, StopWordsFile.EN));

            _window = new TreeWindow();
            _display = _window.TreeDisplay;
            _vis = new Visualization(_display);

            int treeIndex = 0;
            _visNodeTables = new List<IVisualTable>();
            _visEdgeTables = new List<IVisualTable>();
            _visNodeSchemes = new List<TreeNodeVisualTableScheme>();
            foreach (var tree in _trees)
            {
                var visNodeTable = _vis.BuildVisualTable("VisNodeTable" + treeIndex, tree.Graph.NodeTable, _display);
                var visEdgeTable = _vis.BuildVisualTable("VisEdgeTable" + treeIndex, tree.Graph.EdgeTable, _display);

                var visNodeScheme = new TreeNodeVisualTableScheme(visNodeTable, tree.Graph, keywords);

                visEdgeTable.EnsureStyles(Styles.X1, Styles.X2, Styles.Y1, Styles.Y2, Styles.Visible, Styles.Layer);
                visEdgeTable.AddConstantColumn<IRender>(Styles.Render, new SimpleEdgeRender());

                _visNodeTables.Add(visNodeTable);
                _visEdgeTables.Add(visEdgeTable);
                _visNodeSchemes.Add(visNodeScheme);

                treeIndex++;
            }

            Show();
        }

        private void Show() 
        {
            int treeIndex = 0;
            double prevMaxY = 0;
            foreach(var tree in _trees)
            {
                var visNodeTable = _visNodeTables[treeIndex];
                //Layout
                var layout = new TreeLayout(tree, visNodeTable)
                {
                    NodeMarginHorizontal = 60,
                    NodeMarginVertical = 150
                };
                layout.Layout();

                foreach (var visItem in visNodeTable.Items)
                {
                    visItem.X += 10;
                    visItem.Y += 50 + prevMaxY;
                }
                prevMaxY = visNodeTable.Items.Max(item => item.Y);
                foreach (var visItem in visNodeTable.Items)
                {
                    IVisualItem visParentItem;
                    var visEdge = GetEdgeVisItem(visItem, out visParentItem);
                    if (visEdge != null)
                    {
                        visEdge.X1 = visItem.X;
                        visEdge.Y1 = visItem.Y;
                        visEdge.X2 = visParentItem.X;
                        visEdge.Y2 = visParentItem.Y;
                    }
                }

                treeIndex++;
            }

            //Interactions
            InitInteractions();

            //Show
            _app = new Application();
            _app.Run(_window);
            //_window.Show();

            Console.ReadKey();
        }

        private void InitInteractions()
        {
            #region Lava
            DocContent.Keywords = _keywords;
            _display.RatEnter += (s, e) =>
            {
                if (e.Look is TreeNodeLook)
                {
                    e.VisualItem.Hover = true;
                    e.VisualItem.Repaint();
                }
            };
            _display.RatLeave += (s, e) =>
            {
                if (e.Look is TreeNodeLook)
                {
                    e.VisualItem.Hover = false;
                    e.VisualItem.Repaint();
                }
            };
            _display.RatClick += (s, e) =>
            {
                if (InteractionHelper.IsControlKeyPressed())
                {
                    if (e.Look is TreeNodeLook)
                    {
                        UserLabelType type = UserLabelType.NotRelevant;
                        var dataNode = GetNodeByVisNodeItem(e.VisualItem);
                        var treeIndex = GetNodeTreeIndex(e.VisualItem);
                        var _datanodeScheme = _dataNodeSchemes[treeIndex];
                        var tree = _trees[treeIndex];
                        UserLabelType prevType = _datanodeScheme.GetUserLabelType(dataNode);

                        switch (prevType)
                        {
                            case UserLabelType.NotRelevant:
                                type = UserLabelType.Relevant;
                                break;
                            case UserLabelType.Relevant:
                                type = UserLabelType.NotRelevant;
                                break;
                            case UserLabelType.Mixed:
                                type = UserLabelType.Relevant;
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        foreach (var desNode in tree.BFS(dataNode))
                        {
                            _datanodeScheme.SetUserLabelType(desNode, type);
                        }

                        INode ancNode = tree.GetParent(dataNode);
                        while (ancNode != null)
                        {
                            //Update user label type
                            Counter<UserLabelType> counter = new Counter<UserLabelType>();
                            foreach (var child in tree.GetChildren(ancNode))
                            {
                                counter.Add(_datanodeScheme.GetUserLabelType(child));
                            }
                            var dict = counter.GetCountDictionary();
                            if (dict.Count == 1)
                            {
                                _datanodeScheme.SetUserLabelType(ancNode, dict.Keys.ElementAt(0));
                            }
                            else
                                _datanodeScheme.SetUserLabelType(ancNode, UserLabelType.Mixed);

                            ancNode = tree.GetParent(ancNode);
                        }

                        _visNodeTables[treeIndex].RepaintAll();
                    }
                }
                else
                {
                    if (e.Look is TreeNodeLook)
                    {
                        var treeIndex = GetNodeTreeIndex(e.VisualItem);
                        var indexReader = _dataNodeSchemes[treeIndex].GetIndexReader();
                        if (indexReader == null)
                            return;

                        var dataNode = _visNodeSchemes[treeIndex].GetDataGraph().GetNode(e.VisualItem.Row);
                        var docIndices = RandomUtils.GetRandomSamples(_dataNodeSchemes[treeIndex].GetTopicLeafDocIndices(dataNode), 50, new Random(0));
                        List<Doc> documents = new List<Doc>();
                        foreach (var docIndex in docIndices)
                        {
                            documents.Add(GetDocument(indexReader, docIndex));
                        }

                        var docListPopup = new DocList(documents);
                        docListPopup.Owner = _window;
                        //startupLocation = PointToScreen(startupLocation);
                        //docListPopup.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        docListPopup.WindowStartupLocation = WindowStartupLocation.Manual;
                        int mouseX, mouseY;
                        AutoControlUtils.GetMousePosition(out mouseX, out mouseY);
                        docListPopup.Left = mouseX - 4;
                        docListPopup.Top = mouseY - 4;
                        docListPopup.Show();
                    }
                }
            };
            #endregion

            #region WPF
            //_window.TestCurrentButton.Click += (s, e) =>
            //    {
            //        var indexReader = _dataNodeSchemes.GetIndexReader();
            //        if(indexReader == null)
            //            return;

            //        InitializeFeatureVectors();
            //        _dataNodeSchemes.ClearDocCount();

            //        int correctCnt = 0;
            //        int totalCnt = 0;
            //        foreach(var docIndex in _dataNodeSchemes.GetTopicLeafDocIndices(_tree.Root))
            //        {
            //            var node = GetNode(docIndex);
            //            var groundTruthLabel = _dataNodeSchemes.GetUserLabelType(node);
            //            if (groundTruthLabel == UserLabelType.Mixed)
            //                continue;
            //            INode bestMatch;
            //            var projLabel = GetProjectedUserLabelType(indexReader.Document(docIndex), out bestMatch);
            //            if (groundTruthLabel == projLabel)
            //            {
            //                correctCnt++;
            //                //INode ancNode = node;
            //                //while (ancNode != null)
            //                //{
            //                //    _dataNodeScheme.IncrementCorrectDocCount(ancNode);
            //                //    ancNode = _tree.GetParent(ancNode);
            //                //}
            //            }
            //            //{
            //            //    INode ancNode = node;
            //            //    while (ancNode != null)
            //            //    {
            //            //        _dataNodeScheme.IncrementTotalDocCount(node);
            //            //        ancNode = _tree.GetParent(ancNode);
            //            //    }
            //            //}
            //            if(groundTruthLabel == UserLabelType.Relevant)
            //            {
            //                _dataNodeSchemes.IncrementCorrectDocCount(bestMatch);
            //                INode ancNode = bestMatch;
            //                while (ancNode != null)
            //                {
            //                    _dataNodeSchemes.IncrementTotalDocCount(ancNode);
            //                    ancNode = _tree.GetParent(ancNode);
            //                }
            //            }
            //            totalCnt++;
            //        }
            //        foreach(var node in _tree.Graph.Nodes)
            //            _dataNodeSchemes.SetTotalDocCount(node, totalCnt);
                    
            //        _window.TestCurrentTextBlock.Text = (100 * correctCnt / totalCnt) + "% (" + correctCnt + ", " + totalCnt + ") ";
            //        _visNodeTable.RepaintAll();
            //    };
            //_window.RemoveNoiseButton.Click += (s, e) =>
            //    {
            //        string indexPath = _window.IndexPathTextBox.Text;
            //        int sampleNumber = int.Parse(_window.SampleNumberTextBox.Text);
            //        var thread = new Thread(() => RemoveNoiseByCurrentLabel(indexPath, sampleNumber));
            //        thread.Start();
            //    };
            //_window.SaveUserLabelsButton.Click += (s, e) =>
            //    {
            //        var sw = new GetStreamWriter(_dataNodeSchemes.GetBRTFileName() + ".label");
            //        foreach(var node in _tree.Graph.Nodes)
            //        {
            //            var topicID = _dataNodeSchemes.GetTopicID(node);
            //            var type = _dataNodeSchemes.GetUserLabelType(node);
            //            sw.WriteLine(topicID + "\t" + type.ToString());
            //        }
            //        sw.Flush();
            //        sw.Close();
            //    };
            //_window.LoadUserLabelsButton.Click += (s, e) =>
            //{
            //    var sr = new StreamReader(_dataNodeSchemes.GetBRTFileName() + ".label");
            //    string line;
            //    while ((line = sr.ReadLine()) != null && line.Length > 0) 
            //    {
            //        var tokens = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            //        var topicID = int.Parse(tokens[0]);
            //        var type = (UserLabelType)StringOperations.ParseEnum(typeof(UserLabelType), tokens[1]);
            //        var node = _dataNodeSchemes.GetNodeByTopicID(topicID);
            //        _dataNodeSchemes.SetUserLabelType(node, type);
            //    }
            //    sr.Close();
            //    _visNodeTable.RepaintAll();
            //};
            #endregion
        }

        int _rCnt, _nCnt;
        //private void RemoveNoiseByCurrentLabel(string indexPath, int sampleNumber)
        //{
        //    InitializeFeatureVectors();

        //    IndexReader indexReader2 = LuceneOperations.GetIndexReader(indexPath);
        //    int docNum2 = indexReader2.NumDocs();

        //    indexPath = StringOperations.EnsureFolderEndNotWrapped(indexPath);
        //    IndexWriter indexWriterR = LuceneOperations.GetIndexWriter(indexPath + "_R" + sampleNumber);
        //    IndexWriter indexWriterN = LuceneOperations.GetIndexWriter(indexPath + "_NR" + sampleNumber);
        //    int rCnt = 0, nCnt = 0;
        //    Console.WriteLine("Sample {0} out of {1}", sampleNumber, docNum2);
        //    var samples = RandomUtils.GetRandomSamples(0, docNum2 - 1, sampleNumber);
        //    Console.WriteLine("Sample doc cnt: " + samples.Length);

        //    ProgramProgress progress = new ProgramProgress(samples.Length);
        //    foreach (var docIndex in samples)
        //    {
        //        var document = indexReader2.Document(docIndex);
        //        INode temp;
        //        var type = GetProjectedUserLabelType(document, out temp);
        //        if (type == UserLabelType.Relevant)
        //        {
        //            indexWriterR.AddDocument(document);
        //            rCnt++;
        //        }
        //        else if (type == UserLabelType.NotRelevant)
        //        {
        //            indexWriterN.AddDocument(document);
        //            nCnt++;
        //        }
        //        else
        //            throw new ArgumentException();
        //        progress.PrintIncrementExperiment();
        //    }

        //    indexWriterR.Optimize();
        //    indexWriterR.Close();
        //    indexWriterN.Optimize();
        //    indexWriterN.Close();

        //    _rCnt = rCnt;
        //    _nCnt = nCnt;

        //    _app.Dispatcher.Invoke(() => _window.RemoveNoiseTextBlock.Text = string.Format("R: {0}, N: {1}", rCnt, nCnt));
        //}

        //private INode GetNode(int docIndex)
        //{
        //    foreach(var node in _tree.BFS(_tree.Root).Reverse())
        //    {
        //        if (_dataNodeSchemes.GetTopicLeafDocIndices(node).Contains(docIndex))
        //        {
        //            return node;
        //        }
        //    }
        //    throw new ArgumentException();
        //}

        //private UserLabelType GetProjectedUserLabelType(Document document, out INode bestMatch)
        //{
        //    var vector = _vecGen.GetFeatureVector(document);
        //    INode parent = _tree.Root;
        //    bestMatch = null;
        //    while(parent != null)
        //    {
        //        INode _bestMatch = null;
        //        double _bestSimi = double.MinValue;
        //        foreach(var child in _tree.GetChildren(parent))
        //        {
        //            var simi = SparseVectorList.Cosine(_vectorDict[child], vector);
        //            if(simi > _bestSimi)
        //            {
        //                _bestSimi = simi;
        //                _bestMatch = child;
        //            }
        //        }
        //        var type = _dataNodeSchemes.GetUserLabelType(_bestMatch);
        //        if (type == UserLabelType.Mixed)
        //            parent = _bestMatch;
        //        else
        //        {
        //            bestMatch = _bestMatch;
        //            return type;
        //        }
        //    }
        //    throw new ArgumentException();
        //}

        //bool _isInitializeFeatureVectors = false;
        //Dictionary<INode, SparseVectorList> _vectorDict = new Dictionary<INode, SparseVectorList>();
        //private void InitializeFeatureVectors()
        //{
        //    if(!_isInitializeFeatureVectors)
        //    {
        //        _vectorDict = new Dictionary<INode, SparseVectorList>();
        //        foreach(var node in _tree.Graph.Nodes)
        //        {
        //            var vector = _vecGen.GetFeatureVector(_dataNodeSchemes.GetTopicWords(node).ToDictionary(kvp => kvp.Key, kvp => (int)Math.Round(kvp.Value)));
        //            _vectorDict.Add(node, vector);
        //        }

        //        _isInitializeFeatureVectors = true;
        //    }
        //}

        private Doc GetDocument(IndexReader indexReader, int docIndex)
        {
            var document = indexReader.Document(docIndex);
            var doc = new Doc()
            {
                Index = docIndex,
                Title = document.Get(_titleField),
                Content = document.Get(_bodyField),
            };
            return doc;
        }

        private IVisualItem GetEdgeVisItem(IVisualItem nodeVisItem, out IVisualItem visParentNode)
        {
            var treeIndex = GetNodeTreeIndex(nodeVisItem);
            var dataNode = GetNodeByVisNodeItem(nodeVisItem);
            var parentNode = _trees[treeIndex].GetParent(dataNode);
            if(parentNode == null)
            {
                visParentNode = null;
                return null;
            }
            var dataEdge = LavaUtils.GetGraphEdge(_trees[treeIndex].Graph, dataNode, parentNode);
            visParentNode = _visNodeTables[treeIndex].GetItem(parentNode.Row);
            return _visEdgeTables[treeIndex].GetItem(dataEdge.Row);
        }

        private INode GetNodeByVisNodeItem(IVisualItem nodeVisItem)
        {
            var treeIndex = GetNodeTreeIndex(nodeVisItem);
            return _visNodeSchemes[treeIndex].GetDataGraph().GetNode(nodeVisItem.Row);
        }

        private int GetNodeTreeIndex(IVisualItem nodeVisItem)
        {
            return _visNodeTables.IndexOf(nodeVisItem.VisualTable);
        }
    }
}
