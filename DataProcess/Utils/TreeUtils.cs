using Lava.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lava.Util;

namespace DataProcess.Utils
{
    public class TreeUtils
    {
        public static T GetAncestorNodeInCollection<T>(T node, HashSet<T> collection, Func<T, T> parentFunc, T excludedRoot)
        {
            var ancestor = node;
            while (!ancestor.Equals(excludedRoot) && !collection.Contains(ancestor))
            {
                ancestor = parentFunc(ancestor);
            }
            return ancestor;
        }

        public static bool TryGetAncestorNodeInCollection<T>(T node, HashSet<T> collection, Func<T, T> parentFunc, out T ancestor)
        {
            ancestor = node;
            while (ancestor != null && !collection.Contains(ancestor))
            {
                ancestor = parentFunc(ancestor);
            }
            if (ancestor != null && collection.Contains(ancestor))
            {
                return true;
            }
            return false;
        }

        public static T GetAncestorNodeInCollection<T>(T node, HashSet<T> collection, Func<T, T> parentFunc, bool isContainSelf = true)
        {
            var ancestor = isContainSelf ? node : parentFunc(node);
            while (ancestor != null && !collection.Contains(ancestor))
            {
                ancestor = parentFunc(ancestor);
            }
            return ancestor;
        }

        public static IList<INode> PathToRoot(ITree tree, INode node)
        {
            var path = new List<INode> { node };
            while (node != tree.Root) path.Add(node = tree.GetParent(node));
            return path;
        }

        public static List<T> GetLeaves<T>(T root, Func<T, IEnumerable<T>> getChildrenFunc)
        {
            var list = new List<T>();
            BreadthFirstTraversal<T>(root, getChildrenFunc, (node, level) =>
            {
                var children = getChildrenFunc(node);
                if (children == null || !children.Any())
                {
                    list.Add(node);
                }
                return true;
            });
            return list;
        }

        public static List<T> GetBreathFirstTraversalList<T>(T root, Func<T, IEnumerable<T>> getChildrenFunc)
        {
            var list = new List<T>();
            BreadthFirstTraversal<T>(root, getChildrenFunc, (node, level) =>
            {
                //if (list.Contains(node))
                //{
                //    var sw = new StreamWriter("n.txt");
                //    foreach (var n in list)
                //    {
                //        sw.WriteLine(n);
                //    }
                //    sw.Flush();
                //    sw.Close();
                //}
                list.Add(node);
                return true;
            });
            return list;
        }

        public static List<T> GetReverseBreathFirstTraversalList<T>(T root, Func<T, IEnumerable<T>> getChildrenFunc)
        {
            var list = GetBreathFirstTraversalList(root, getChildrenFunc);
            list.Reverse();
            return list;
        }

        public static void PrintTree<T>(T root, Func<T, IEnumerable<T>> getChildrenFunc, Func<T, string> nodeStringFunc, PrintType printType = PrintType.Console, StreamWriter sw = null)
        {
            DebugUtils.PrintString("Original", printType, sw);
            int prevLevel = -1;
            TreeUtils.BreadthFirstTraversal(root, getChildrenFunc, (node, level) =>
            {
                if (prevLevel != level)
                {
                    prevLevel = level;
                    DebugUtils.PrintString("\n", printType, sw);
                }
                DebugUtils.PrintString(nodeStringFunc(node) + "\t", printType, sw);
                return true;
            });
            DebugUtils.PrintString("\n", printType, sw);
        }

        public static void BreadthFirstTraversal<T>(T root, Func<T, IEnumerable<T>> getChildrenFunc, Func<T, int, bool> callbackFunc)
        {
            int curLevel = 1;
            var curLevelNodes = new List<T>();
            var nextLevelNodes = new List<T>();
            curLevelNodes.Add(root);

            bool isTraverse = true;
            while (curLevelNodes.Count != 0) 
            {
                foreach (var node in curLevelNodes)
                {
                    //Deal with the current node
                    if (!callbackFunc(node, curLevel))
                    {
                        isTraverse = false;
                        break;
                    }
                    //Add its children to list
                    var children = getChildrenFunc(node);
                    if (children != null && children.Count<T>() > 0)
                    {
                        nextLevelNodes.AddRange(children);
                    }
                }

                //Break if terminated
                if (!isTraverse)
                    break;

                //Set up next level
                curLevelNodes.Clear();
                curLevelNodes = null;

                curLevelNodes = nextLevelNodes;
                nextLevelNodes = new List<T>();
                curLevel++;
            }
        }
    }
}
