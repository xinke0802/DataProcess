using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace DataProcess.Utils
{
    #region string search
    /// <summary>
    /// Interface containing all methods to be implemented
    /// by string search algorithm
    /// </summary>
    public interface IStringSearchAlgorithm
    {
        #region Methods & Properties

        /// <summary>
        /// List of keywords to search for
        /// </summary>
        string[] Keywords { get; set; }


        /// <summary>
        /// Searches passed text and returns all occurrences of any keyword
        /// </summary>
        /// <param name="text">Text to search</param>
        /// <returns>Array of occurrences</returns>
        StringSearchResult[] FindAll(string text);

        /// <summary>
        /// Searches passed text and returns first occurrence of any keyword
        /// </summary>
        /// <param name="text">Text to search</param>
        /// <returns>First occurrence of any keyword (or StringSearchResult.Empty if text doesn't contain any keyword)</returns>
        StringSearchResult FindFirst(string text);

        /// <summary>
        /// Searches passed text and returns true if text contains any keyword
        /// </summary>
        /// <param name="text">Text to search</param>
        /// <returns>True when text contains any keyword</returns>
        bool ContainsAny(string text);

        #endregion
    }

    /// <summary>
    /// Search using string.IndexOf (for comparsion)
    /// </summary>
    class IndexOfSearch : IStringSearchAlgorithm
    {
        #region IStringSearchAlgorithm Members

        string[] _keywords;

        public string[] Keywords
        {
            get { return _keywords; }
            set { _keywords = value; }
        }

        public StringSearchResult[] FindAll(string text)
        {
            //Xiting
            List<StringSearchResult> res = new List<StringSearchResult>();
            foreach (string kwd in _keywords)
            {
                int i = -1;
                while ((i = text.IndexOf(kwd, i + 1)) >= 0)
                {
                    res.Add(new StringSearchResult(i, kwd));
                }
            }
            return res.ToArray();
        }

        public StringSearchResult FindFirst(string text)
        {
            foreach (string kwd in _keywords)
            {
                int i = text.IndexOf(kwd);
                if (i != -1) return new StringSearchResult(i, kwd);
            }
            return StringSearchResult.Empty;
        }

        public bool ContainsAny(string text)
        {
            foreach (string kwd in _keywords)
            {
                int i = text.IndexOf(kwd);
                if (i != -1) return true;
            }
            return false;
        }

        #endregion
    }


    /// <summary>
    /// Search using regular expressions (for comparsion)
    /// </summary>
    class RegexSearch : IStringSearchAlgorithm
    {
        #region IStringSearchAlgorithm Members

        string[] _keywords;
        Regex _reg;

        public string[] Keywords
        {
            get { return _keywords; }
            set
            {
                _keywords = value;
                _reg = new Regex(@"\b" + "(" + string.Join("|", value) + ")" + @"\b", RegexOptions.None);
            }
        }

        public StringSearchResult[] FindAll(string text)
        {
            throw new NotImplementedException();
        }

        public StringSearchResult FindFirst(string text)
        {
            throw new NotImplementedException();
        }

        public bool ContainsAny(string text)
        {
            return _reg.Match(text).Success;
        }

        #endregion
    }

    /// <summary>
    /// Structure containing results of search 
    /// (keyword and position in original text)
    /// </summary>
    public struct StringSearchResult
    {
        #region Members

        private int _index;
        private string _keyword;

        /// <summary>
        /// Initialize string search result
        /// </summary>
        /// <param name="index">Index in text</param>
        /// <param name="keyword">Found keyword</param>
        public StringSearchResult(int index, string keyword)
        {
            _index = index; _keyword = keyword;
        }


        /// <summary>
        /// Returns index of found keyword in original text
        /// </summary>
        public int Index
        {
            get { return _index; }
        }


        /// <summary>
        /// Returns keyword found by this result
        /// </summary>
        public string Keyword
        {
            get { return _keyword; }
        }


        /// <summary>
        /// Returns empty search result
        /// </summary>
        public static StringSearchResult Empty
        {
            get { return new StringSearchResult(-1, ""); }
        }

        #endregion
    }


    /// <summary>
    /// Class for searching string for one or multiple 
    /// keywords using efficient Aho-Corasick search algorithm
    /// </summary>
    public class StringSearch : IStringSearchAlgorithm
    {
        #region Objects

        /// <summary>
        /// Tree node representing character and its 
        /// transition and failure function
        /// </summary>
        class TreeNode
        {
            #region Constructor & Methods

            /// <summary>
            /// Initialize tree node with specified character
            /// </summary>
            /// <param name="parent">Parent node</param>
            /// <param name="c">Character</param>
            public TreeNode(TreeNode parent, char c)
            {
                _char = c; _parent = parent;
                _results = new ArrayList();
                _resultsAr = new string[] { };

                _transitionsAr = new TreeNode[] { };
                _transHash = new Hashtable();
            }


            /// <summary>
            /// Adds pattern ending in this node
            /// </summary>
            /// <param name="result">Pattern</param>
            public void AddResult(string result)
            {
                if (_results.Contains(result)) return;
                _results.Add(result);
                _resultsAr = (string[])_results.ToArray(typeof(string));
            }

            /// <summary>
            /// Adds trabsition node
            /// </summary>
            /// <param name="node">Node</param>
            public void AddTransition(TreeNode node)
            {
                _transHash.Add(node.Char, node);
                TreeNode[] ar = new TreeNode[_transHash.Values.Count];
                _transHash.Values.CopyTo(ar, 0);
                _transitionsAr = ar;
            }


            /// <summary>
            /// Returns transition to specified character (if exists)
            /// </summary>
            /// <param name="c">Character</param>
            /// <returns>Returns TreeNode or null</returns>
            public TreeNode GetTransition(char c)
            {
                return (TreeNode)_transHash[c];
            }


            /// <summary>
            /// Returns true if node contains transition to specified character
            /// </summary>
            /// <param name="c">Character</param>
            /// <returns>True if transition exists</returns>
            public bool ContainsTransition(char c)
            {
                return GetTransition(c) != null;
            }

            #endregion
            #region Properties

            private char _char;
            private TreeNode _parent;
            private TreeNode _failure;
            private ArrayList _results;
            private TreeNode[] _transitionsAr;
            private string[] _resultsAr;
            private Hashtable _transHash;

            /// <summary>
            /// Character
            /// </summary>
            public char Char
            {
                get { return _char; }
            }


            /// <summary>
            /// Parent tree node
            /// </summary>
            public TreeNode Parent
            {
                get { return _parent; }
            }


            /// <summary>
            /// Failure function - descendant node
            /// </summary>
            public TreeNode Failure
            {
                get { return _failure; }
                set { _failure = value; }
            }


            /// <summary>
            /// Transition function - list of descendant nodes
            /// </summary>
            public TreeNode[] Transitions
            {
                get { return _transitionsAr; }
            }


            /// <summary>
            /// Returns list of patterns ending by this letter
            /// </summary>
            public string[] Results
            {
                get { return _resultsAr; }
            }

            #endregion
        }

        #endregion
        #region Local fields

        /// <summary>
        /// Root of keyword tree
        /// </summary>
        private TreeNode _root;

        /// <summary>
        /// Keywords to search for
        /// </summary>
        private string[] _keywords;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize search algorithm (Build keyword tree)
        /// </summary>
        /// <param name="keywords">Keywords to search for</param>
        public StringSearch(string[] keywords)
        {
            Keywords = keywords;
            InitializeAllowedSet();
        }


        /// <summary>
        /// Initialize search algorithm with no keywords
        /// (Use Keywords property)
        /// </summary>
        public StringSearch()
        {
            InitializeAllowedSet();
        }

        private void InitializeAllowedSet()
        {
            AllowedChars.Add(',');
            AllowedChars.Add(' ');
            AllowedChars.Add('.');
            AllowedChars.Add('\'');
            AllowedChars.Add('\"');
            AllowedChars.Add('?');
            AllowedChars.Add(':');
            AllowedChars.Add(';');
        }

        #endregion
        #region Implementation

        /// <summary>
        /// Build tree from specified keywords
        /// </summary>
        void BuildTree()
        {
            // Build keyword tree and transition function
            _root = new TreeNode(null, ' ');
            foreach (string p in _keywords)
            {
                // add pattern to tree
                TreeNode nd = _root;
                foreach (char c in p)
                {
                    TreeNode ndNew = null;
                    foreach (TreeNode trans in nd.Transitions)
                        if (trans.Char == c) { ndNew = trans; break; }

                    if (ndNew == null)
                    {
                        ndNew = new TreeNode(nd, c);
                        nd.AddTransition(ndNew);
                    }
                    nd = ndNew;
                }
                nd.AddResult(p);
            }

            // Find failure functions
            ArrayList nodes = new ArrayList();
            // level 1 nodes - fail to root node
            foreach (TreeNode nd in _root.Transitions)
            {
                nd.Failure = _root;
                foreach (TreeNode trans in nd.Transitions) nodes.Add(trans);
            }
            // other nodes - using BFS
            while (nodes.Count != 0)
            {
                ArrayList newNodes = new ArrayList();
                foreach (TreeNode nd in nodes)
                {
                    TreeNode r = nd.Parent.Failure;
                    char c = nd.Char;

                    while (r != null && !r.ContainsTransition(c)) r = r.Failure;
                    if (r == null)
                        nd.Failure = _root;
                    else
                    {
                        nd.Failure = r.GetTransition(c);
                        foreach (string result in nd.Failure.Results)
                            nd.AddResult(result);
                    }

                    // add child nodes to BFS list 
                    foreach (TreeNode child in nd.Transitions)
                        newNodes.Add(child);
                }
                nodes = newNodes;
            }
            _root.Failure = _root;
        }


        #endregion
        #region Parameters
        /// <summary>
        /// Is the leading/ending chars besides white space allowed or not.
        /// </summary>
        private bool isStrictMatch = true;
        public bool ISStrictMath
        {
            get { return isStrictMatch; }
            set
            {
                isStrictMatch = value;
            }
        }

        static public HashSet<char> AllowedChars = new HashSet<char>();

        #endregion
        #region Methods & Properties

        /// <summary>
        /// Keywords to search for (setting this property is slow, because
        /// it requieres rebuilding of keyword tree)
        /// </summary>
        public string[] Keywords
        {
            get { return _keywords; }
            set
            {
                _keywords = value;
                BuildTree();
            }
        }


        /// <summary>
        /// Searches passed text and returns all occurrences of any keyword
        /// </summary>
        /// <param name="text">Text to search</param>
        /// <returns>Array of occurrences</returns>
        public StringSearchResult[] FindAll(string text)
        {
            //StringSearchResult[] searchResults = null;
            ArrayList ret = new ArrayList();
            TreeNode ptr = _root;
            int index = 0;

            while (index < text.Length)
            {
                TreeNode trans = null;
                while (trans == null)
                {
                    trans = ptr.GetTransition(text[index]);
                    if (ptr == _root) break;
                    if (trans == null) ptr = ptr.Failure;
                }
                if (trans != null) ptr = trans;

                //foreach(string found in ptr.Results)
                //    ret.Add(new StringSearchResult(index-found.Length+1,found));
                //modified by Yingcai to match the whole keywords
                //if found
                if (ptr.Results.Length > 0)
                {
                    ArrayList newRet = GetValidResults(ptr, text, index);
                    ret.AddRange((ICollection)newRet);
                }

                index++;
            }
            return (StringSearchResult[])ret.ToArray(typeof(StringSearchResult));
        }


        /// <summary>
        /// Searches passed text and returns first occurrence of any keyword
        /// </summary>
        /// <param name="text">Text to search</param>
        /// <returns>First occurrence of any keyword (or StringSearchResult.Empty if text doesn't contain any keyword)</returns>
        public StringSearchResult FindFirst(string text)
        {
            throw new NotImplementedException();
            //ArrayList ret=new ArrayList();
            //TreeNode ptr=_root;
            //int index=0;

            //while(index<text.Length)
            //{
            //    TreeNode trans=null;
            //    while(trans==null)
            //    {
            //        trans=ptr.GetTransition(text[index]);
            //        if (ptr==_root) break;
            //        if (trans==null) ptr=ptr.Failure;
            //    }
            //    if (trans!=null) ptr=trans;

            //    foreach (string found in ptr.Results)
            //        return new StringSearchResult(index - found.Length + 1, found);
            //    index++;
            //}
            //return StringSearchResult.Empty;
        }

        ///// <summary>
        ///// This function is to check whether the found result is valid or not
        ///// </summary>
        ///// <returns></returns>
        //private bool IsTheResultValid(TreeNode ptr, string text, int index)
        //{
        //    //if the following char is a letter, return false
        //    if (index < text.Length - 1)
        //    {
        //        if (Char.IsLetter(text[index + 1]))
        //        {
        //            return false;
        //        }
        //    }
        //    //check if the leading char of the results is a letter or not                    
        //    bool isMatched = true;
        //    foreach (string str in ptr.Results)
        //    {
        //        if (index - str.Length >= 0)
        //        {
        //            if (Char.IsLetter(text[index - str.Length]))
        //            {
        //                isMatched = false;
        //            }
        //        }
        //        else //the leading char is at the zero index
        //        {
        //            isMatched = true;
        //            break;
        //        }
        //    }
        //    return isMatched;
        //}


        /// <summary>
        /// This function is to check whether the found result is valid or not
        /// This function will return empty list if no result
        /// </summary>
        /// <returns></returns>
        private ArrayList GetValidResults(TreeNode ptr, string text, int index)
        {
            ArrayList ret = new ArrayList();

            foreach (string found in ptr.Results)
            {
                bool isMatched = true;
                if (index < text.Length - 1)
                {
                    if (isStrictMatch == true)
                    {
                        if (!AllowedChars.Contains(text[index + 1]))
                        {
                            isMatched = false;
                        }
                    }
                    else
                    {
                        if (Char.IsLetterOrDigit(text[index + 1]))
                        {
                            isMatched = false;
                        }
                    }
                }
                if (index - found.Length >= 0)
                {
                    if (isStrictMatch == true)
                    {
                        if (!AllowedChars.Contains(text[index - found.Length]))
                        {
                            isMatched = false;
                        }
                    }
                    else
                    {
                        if (Char.IsLetterOrDigit(text[index - found.Length]))
                        {
                            isMatched = false;
                        }
                    }
                }
                if (isMatched)
                {
                    ret.Add(new StringSearchResult(index - found.Length + 1, found));
                }
            }
            return ret;

        }

        /// <summary>
        /// Searches passed text and returns true if text contains any keyword
        /// </summary>
        /// <param name="text">Text to search</param>
        /// <returns>True when text contains any keyword</returns>
        public bool ContainsAny(string text)
        {
            TreeNode ptr = _root;
            int index = 0;

            while (index < text.Length)
            {
                TreeNode trans = null;
                while (trans == null)
                {
                    trans = ptr.GetTransition(text[index]);
                    if (ptr == _root) break;
                    if (trans == null) ptr = ptr.Failure;
                }
                if (trans != null) ptr = trans;

                //modified by Yingcai to match the whole keywords
                //if found
                if (ptr.Results.Length > 0)
                {
                    ArrayList searchResults = GetValidResults(ptr, text, index);
                    if (searchResults.Count > 0)
                    {
                        return true;
                    }
                }
                index++;
            }
            return false;
        }

        #endregion


    }
    #endregion

    public class StringOperations
    {
        public static string GetPercentageString(int effCount, int totalCount)
        {
            return string.Format("{0} out of {1} ({2}%)", effCount, totalCount, 100*effCount/totalCount);
        }

        public static string GetFirstLetterCapital(string str)
        {
            return Strings.StrConv(str, VbStrConv.ProperCase, System.Globalization.CultureInfo.CurrentCulture.LCID);
        }

        public static long? TryParseLong(string str)
        {
            long value;
            if (long.TryParse(str, out value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public static int GetLongestCommonSubstring(string str1, string str2)
        {
            if (String.IsNullOrEmpty(str1) || String.IsNullOrEmpty(str2))
                return 0;

            int[,] num = new int[str1.Length, str2.Length];
            int maxlen = 0;

            for (int i = 0; i < str1.Length; i++)
            {
                for (int j = 0; j < str2.Length; j++)
                {
                    if (str1[i] != str2[j])
                        num[i, j] = 0;
                    else
                    {
                        if ((i == 0) || (j == 0))
                            num[i, j] = 1;
                        else
                            num[i, j] = 1 + num[i - 1, j - 1];

                        if (num[i, j] > maxlen)
                        {
                            maxlen = num[i, j];
                        }
                    }
                }
            }
            return maxlen;
        }

        public static int GetLongestAscendingSubstring(string str1, string str2)
        {
            int[,] table;
            return GetLCSInternal(str1, str2, out table);
        }

        private static int GetLCSInternal(string str1, string str2, out int[,] matrix)
        {
            matrix = null;

            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            {
                return 0;
            }

            int[,] table = new int[str1.Length + 1, str2.Length + 1];
            for (int i = 0; i < table.GetLength(0); i++)
            {
                table[i, 0] = 0;
            }
            for (int j = 0; j < table.GetLength(1); j++)
            {
                table[0, j] = 0;
            }

            for (int i = 1; i < table.GetLength(0); i++)
            {
                for (int j = 1; j < table.GetLength(1); j++)
                {
                    if (str1[i - 1] == str2[j - 1])
                        table[i, j] = table[i - 1, j - 1] + 1;
                    else
                    {
                        if (table[i, j - 1] > table[i - 1, j])
                            table[i, j] = table[i, j - 1];
                        else
                            table[i, j] = table[i - 1, j];
                    }
                }
            }

            matrix = table;
            return table[str1.Length, str2.Length];
        }

        public static string ConvertByteArrayToString(byte[] source)
        {
            return source != null ? System.Text.Encoding.UTF8.GetString(source).TrimEnd('\0') : null;
        }

        public static string ConvertNullStringToEmpty(string str)
        {
            return str == null ? "" : str;
        }

        public static double ParseDoubleWithNan(string str)
        {
            return str.ToLower() == "nan" ? double.NaN : double.Parse(str);
        }

        public static int GetKeywordCount(string text, IEnumerable<string> keywords)
        {
            if (keywords == null || keywords.Count() == 0)
                return 0;

            text = text.ToLower();
            int cnt = 0;
            foreach (var keyword in keywords)
            {
                int index = -1;
                while ((index = text.IndexOf(keyword, index + 1)) >= 0)
                {
                    cnt++;
                }
            }

            return cnt;
        }

        public static string[] SplitTextIntoSentences(string text)
        {
            return text.Split(new char[] {'.', '?', '!'}, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string GetLeadingSentences(string plain, int leadingSentenceCount = 6)
        {
            if (plain == null)
                return null;
            var contents = plain.Split('.', '?', '!');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < leadingSentenceCount && i < contents.Length; i++)
            {
                sb.Append(contents[i]);
                sb.Append('.');
            }
            return sb.ToString();
        }

        public static string GetFileNameInsertedBeforeFileType(string fileName, string insertStr)
        {
            var index = fileName.LastIndexOf('.');
            return fileName.Substring(0, index) + insertStr + fileName.Substring(index);
        }

        public static string GetFolder(string fullFileName)
        {
            return fullFileName.Substring(0, fullFileName.LastIndexOf("\\") + 1);
        }

        public static string GetFileName(string fullFileName)
        {
            return Path.GetFileName(fullFileName);
        }

        public static string GetFullFileNameWithoutExtension(string fullFileName)
        {
            return Path.GetDirectoryName(fullFileName) + "\\" + Path.GetFileNameWithoutExtension(fullFileName);
        }

        /// <summary>
        /// Does not include the full path of the file
        /// </summary>
        public static string GetFileNameWithoutExtension(string fullFileName)
        {
            return Path.GetFileNameWithoutExtension(fullFileName);
        }

        public static string EnsureFolderEnd(string folder)
        {
            if (folder == null)
                return null;
            if (!folder.EndsWith("\\")) folder += "\\";
            return folder;
        }

        public static string EnsureFolderEndNotWrapped(string folder)
        {
            if (folder.EndsWith("\\")) folder = folder.Substring(0, folder.Length - 1);
            return folder;
        }

        public static string GetVariableName<T>(Expression<Func<T>> memberExpression)
        {
            MemberExpression expressionBody = (MemberExpression)memberExpression.Body;
            return expressionBody.Member.Name;
        }

        public static string GetMergedString<T1, T2>(IEnumerable<KeyValuePair<T1, T2>> dict, char level1Seperator = '\t', char level2Seperator = ',')
        {
            string str = "";

            if (dict == null || dict.Count() == 0)
                return str;

            int index = 0;
            int cnt = dict.Count();
            foreach (var kvp in dict)
            {
                if (index == cnt - 1)
                    break;
                str += kvp.Key.ToString() + level2Seperator + kvp.Value + level1Seperator;
                index++;
            }
            var lastKvp = dict.ElementAt(index);
            str += lastKvp.Key.ToString() + level2Seperator + lastKvp.Value;

            return str;
        }

        public static string GetMergedString<T1, T2>(Dictionary<T1, T2> dict, char level1Seperator = '\t', char level2Seperator = ',')
        {
            string str = "";

            if (dict == null || dict.Count == 0)
                return str;

            int index = 0;
            int cnt = dict.Count;
            foreach (var kvp in dict)
            {
                if (index == cnt - 1)
                    break;
                str += kvp.Key.ToString() + level2Seperator + kvp.Value + level1Seperator;
                index++;
            }
            var lastKvp = dict.ElementAt(index);
            str += lastKvp.Key.ToString() + level2Seperator + lastKvp.Value;

            return str;
        }

        public static string GetMergedString<T>(IEnumerable<T> array, char seperator = '\t') //where T : struct
        {
            if (array == null)
                return null;
            string str = "";
            str += seperator;
            return GetMergedString<T>(array as ICollection<T>, str);
        }

        public static string GetMergedString<T>(IEnumerable<T> array, string seperator) //where T : struct
        {
            return GetMergedString<T>(array as ICollection<T>, seperator);
        }

        //public static string GetMergedString<T>(List<T> list, char seperator = '\t') //where T : struct
        //{
        //    return GetMergedString<T>(list as ICollection<T>, seperator);
        //}

        public static string WrapWithDash(string str = "")
        {
            return "------------ " + str + " ------------";
        }

        private static string GetMergedString<T>(ICollection<T> collection, string seperator) //where T : struct
        {
            if (collection == null)
                return null;
            else if(collection.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            int index = 0;
            foreach (var val in collection)
            {
                if (index == collection.Count - 1)
                    break;
                sb.Append(val.ToString() + seperator);
                index++;
            }
            sb.Append(collection.ElementAt(collection.Count - 1));

            return sb.ToString();
        }

        public static string GetMergedString<T1, T2>(IDictionary<T1, T2> dict, char level1Seperator = '\t', char level2Seperator = ',')
        {
            int index = 0, cnt = dict.Count;
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in dict)
            {
                if (index == dict.Count - 1)
                    break;
                sb.Append(kvp.Key.ToString() + level2Seperator + kvp.Value.ToString() + level1Seperator);
                index++;
            }
            var lastKvp = dict.ElementAt(cnt - 1);
            sb.Append(lastKvp.Key.ToString() + level2Seperator + lastKvp.Value);

            return sb.ToString();
        }

        public static ConfigureFile ParseConfigureString(string configStr, 
            char level1Seperator = '\t', char level2Seperator = ',', char level3Seperator = ';', char level4Seperator = '|')
        {
            var dict = ParseStringStringDictionary(configStr, level1Seperator, level2Seperator);
            var configDict = new Dictionary<string, List<string>>();
            foreach (var kvp in dict)
            {
                string paraName = kvp.Key;
                if (paraName.Contains(" //"))
                {
                    int index = paraName.IndexOf(" //");
                    paraName = paraName.Substring(0, index);
                }

                var tokens = kvp.Value.Split(level3Seperator);
                configDict.Add(paraName, tokens.ToList<string>());
            }
            return new ConfigureFile(configDict, level4Seperator);
        }

        public static ConfigureFile ParseConfigureString(string configStr, 
            string level1Seperator, string level2Seperator = "\n", string level3Seperator = "\n", string level4Seperator = "\t")
        {
            var dict = ParseStringStringDictionary(configStr, level1Seperator, level2Seperator);
            var configDict = new Dictionary<string, List<string>>();
            foreach (var kvp in dict)
            {
                string paraName = kvp.Key;
                if(paraName.Contains(" //"))
                {
                    int index = paraName.IndexOf(" //");
                    paraName = paraName.Substring(0, index);
                }

                var tokens = kvp.Value.Split(new string[] { level3Seperator }, StringSplitOptions.RemoveEmptyEntries);
                configDict.Add(paraName, tokens.ToList<string>());
            }
            return new ConfigureFile(configDict, level4Seperator);
        }

        public static bool TryParseEnum(Type enumType, string name, out object res)
        {
            foreach (var obj in Enum.GetValues(enumType))
            {
                if (Enum.GetName(enumType, obj) == name)
                {
                    res = obj;
                    return true;
                }
            }
            res = null;
            return false;
        }

        public static T ParseEnum<T>(string name)
        {
            return (T)StringOperations.ParseEnum(typeof(T), name);
        }

        public static object ParseEnum(Type enumType, string name)
        {
            foreach (var obj in Enum.GetValues(enumType))
            {
                if (Enum.GetName(enumType, obj) == name)
                    return obj;
            }
            throw new Exception(string.Format("Error parse enum by name! enumType >>{0}<< does not contain a value of >>{1}<<", enumType, name));
        }

        public static DateTime ParseDateTimeStringSystem(
            string timeString, string formatString)
        {
            return DateTime.ParseExact(timeString, formatString, System.Globalization.CultureInfo.InvariantCulture);
        }


        public static DateTime ParseDateTimeString(
    string timeString, string formatString)
        {
            var seperator = new char[] { ' ', '-', '\\', '/', ':', '\t' };

            string[] timetokens = timeString.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            string[] formattokens = formatString.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            int day = DateTime.Now.Day;
            int hour = 0, minute = 0, second = 0;

            for (int i = 0; i < formattokens.Length; i++)
            {
                var format = formattokens[i];
                if (format.StartsWith("pm", StringComparison.CurrentCultureIgnoreCase) ||
                    format.StartsWith("am", StringComparison.CurrentCultureIgnoreCase))
                {
                    var time = timetokens[i];
                    if(time.StartsWith("pm", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (hour < 12)
                            hour += 12;
                    }
                    else if(time.StartsWith("am", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (hour == 12)
                            hour -= 12;
                    }
                    else
                        throw new Exception("Did not recognize time string!");
                }
                else
                {
                    var time = int.Parse(timetokens[i]);
                    if (format.StartsWith("y", StringComparison.CurrentCultureIgnoreCase))
                        year = time;
                    else if (format.StartsWith("M"))
                        month = time;
                    else if (format.StartsWith("d", StringComparison.CurrentCultureIgnoreCase))
                        day = time;
                    else if (format.StartsWith("h", StringComparison.CurrentCultureIgnoreCase))
                        hour = time;
                    else if (format.StartsWith("m"))
                        minute = time;
                    else if (format.StartsWith("s", StringComparison.CurrentCultureIgnoreCase))
                        second = time;
                    else
                        throw new Exception("Did not recognize time format string!");
                }
            }

            return new DateTime(year, month, day, hour, minute, second);
        }

        public static string[] ParseStringArray(string str, char seperatChar1 = '\t')
        {
            var tokens = str.Split(new char[] { seperatChar1 }, StringSplitOptions.RemoveEmptyEntries);
            return tokens;
        }

        public static int[] ParseIntArray(string str, char[] seperatChars)
        {
            var tokens = str.Split(seperatChars, StringSplitOptions.RemoveEmptyEntries);
            var nums = new int[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                nums[i] = int.Parse(tokens[i]);
            }
            return nums;
        }

        public static int[] ParseIntArray(string str, char seperatChar1 = '\t')
        {
            return ParseIntArray(str, new char[] { seperatChar1 });
        }
        
        public static Dictionary<string, int> ParseStringIntDictionary(string str,
    char seperatChar1 = '\t', char seperatChar2 = ',')
        {
            var seperator1 = new char[] { seperatChar1 };
            var seperator2 = new char[] { seperatChar2 };
            var tokens1 = str.Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, int>();
            foreach (var token1 in tokens1)
            {
                var tokens2 = token1.Split(seperator2, StringSplitOptions.RemoveEmptyEntries);
                if (tokens2.Length != 2)
                {
                    Console.WriteLine("ParseStringIntDictionary ignored: " + token1);
                    continue;
                }
                //dict.Add(tokens2[0], int.Parse(tokens2[1]));
                dict[tokens2[0]] = int.Parse(tokens2[1]);
            }
            return dict;
        }

        public static Dictionary<int, string> ParseIntStringDictionary(string str,
char seperatChar1 = '\t', char seperatChar2 = ',')
        {
            var seperator1 = new char[] { seperatChar1 };
            var seperator2 = new char[] { seperatChar2 };
            var tokens1 = str.Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<int, string>();
            foreach (var token1 in tokens1)
            {
                var tokens2 = token1.Split(seperator2, StringSplitOptions.RemoveEmptyEntries);
                dict.Add(int.Parse(tokens2[0]), tokens2[1]);
            }
            return dict;
        }

        public static Dictionary<int, int> ParseIntIntDictionary(string str,
char seperatChar1 = '\t', char seperatChar2 = ',')
        {
            var seperator1 = new char[] { seperatChar1 };
            var seperator2 = new char[] { seperatChar2 };
            var tokens1 = str.Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<int, int>();
            foreach (var token1 in tokens1)
            {
                var tokens2 = token1.Split(seperator2, StringSplitOptions.RemoveEmptyEntries);
                dict.Add(int.Parse(tokens2[0]), int.Parse(tokens2[1]));
            }
            return dict;
        }

        public static Dictionary<string, double> ParseStringDoubleDictionary(string str,
            char seperatChar1 = '\t', char seperatChar2 = ',')
        {
            var seperator1 = new char[] { seperatChar1 };
            var seperator2 = new char[] { seperatChar2 };
            var tokens1 = str.Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, double>();
            foreach (var token1 in tokens1)
            {
                var tokens2 = token1.Split(seperator2, StringSplitOptions.RemoveEmptyEntries);
                dict.Add(tokens2[0], double.Parse(tokens2[1]));
            }
            return dict;
        }

        public static Dictionary<string, string> ParseStringStringDictionary(string str,
            char[] level1Seperators, char level2Seperator)
        {
            var seperator2 = new char[] { level2Seperator };
            var tokens1 = str.Split(level1Seperators, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, string>();
            foreach (var token1 in tokens1)
            {
                var tokens2 = token1.Split(seperator2);
                if (tokens2.Length > 2)
                {
                    dict[tokens2[0]] = StringOperations.GetMergedString(tokens2.Skip(1).ToList(), level2Seperator);
                }
                else
                    dict[tokens2[0]] = tokens2[1];
            }
            return dict;
        }

        public static Dictionary<string, string> ParseStringStringDictionary(string str,
            char level1Seperator = '\t', char level2Seperator = ',')
        {
            var seperator1 = new char[] { level1Seperator };
            var seperator2 = new char[] { level2Seperator };

            return ParseStringStringDictionary(str, new []{level1Seperator}, level2Seperator);
        }

        public static Dictionary<string, string> ParseStringStringDictionary(string str,
            string level1Seperator, string level2Seperator)
        {
            var seperator1 = new string[] { level1Seperator };
            var seperator2 = new string[] { level2Seperator };
            var tokens1 = str.Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, string>();
            foreach (var token1 in tokens1)
            {
                var tokens2 = token1.Split(seperator2, StringSplitOptions.RemoveEmptyEntries);
                if(tokens2.Length > 2)
                {
                    dict.Add(tokens2[0], StringOperations.GetMergedString(tokens2.Skip(1).ToList(), level2Seperator));
                }
                else
                    dict.Add(tokens2[0], tokens2[1]);
            }
            return dict;
        }
    }
}
