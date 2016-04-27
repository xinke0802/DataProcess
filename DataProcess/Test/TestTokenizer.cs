using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DataProcess.Utils;

namespace DataProcess.Test
{
    class TestTokenizer
    {


        #region import dlls
        [StructLayout(LayoutKind.Explicit)]
        struct result_t
        {
            [FieldOffset(0)]
            public int start;
            [FieldOffset(4)]
            public int length;
            [FieldOffset(8)]
            public int sPos1;
            [FieldOffset(12)]
            public int sPos2;
            [FieldOffset(16)]
            public int sPos3;
            [FieldOffset(20)]
            public int sPos4;
            [FieldOffset(24)]
            public int sPos5;
            [FieldOffset(28)]
            public int sPos6;
            [FieldOffset(32)]
            public int sPos7;
            [FieldOffset(36)]
            public int sPos8;
            [FieldOffset(40)]
            public int sPos9;
            [FieldOffset(44)]
            public int sPos10;
            //[FieldOffset(12)] public int sPosLow;
            [FieldOffset(48)]
            public int POS_id;
            [FieldOffset(52)]
            public int word_ID;
            [FieldOffset(56)]
            public int word_type;
            [FieldOffset(60)]
            public int weight;
        }

        const string path = @"Utils\Lib\NLPIR\NLPIR.dll";//设定dll的路径
        const string datapath = @"Utils\Lib\NLPIR\";
        //对函数进行申明
        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_Init")]
        static extern bool NLPIR_Init(String sInitDirPath, int encoding, String sLicenseCode);

        //特别注意，C语言的函数NLPIR_API const char * NLPIR_ParagraphProcess(const char *sParagraph,int bPOStagged=1);必须对应下面的申明
        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_ParagraphProcess")]
        static extern IntPtr NLPIR_ParagraphProcess(String sParagraph, int bPOStagged = 1);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_Exit")]
        static extern bool NLPIR_Exit();

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_ImportUserDict")]
        static extern int NLPIR_ImportUserDict(String sFilename);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_FileProcess")]
        static extern bool NLPIR_FileProcess(String sSrcFilename, String sDestFilename, int bPOStagged = 1);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_FileProcessEx")]
        static extern bool NLPIR_FileProcessEx(String sSrcFilename, String sDestFilename);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_GetParagraphProcessAWordCount")]
        static extern int NLPIR_GetParagraphProcessAWordCount(String sParagraph);
        //NLPIR_GetParagraphProcessAWordCount
        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_ParagraphProcessAW")]
        static extern void NLPIR_ParagraphProcessAW(int nCount, [Out, MarshalAs(UnmanagedType.LPArray)] result_t[] result);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_AddUserWord")]
        static extern int NLPIR_AddUserWord(String sWord);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_SaveTheUsrDic")]
        static extern int NLPIR_SaveTheUsrDic();

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_DelUsrWord")]
        static extern int NLPIR_DelUsrWord(String sWord);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_Start")]
        static extern bool NLPIR_NWI_Start();

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_Complete")]
        static extern bool NLPIR_NWI_Complete();

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_AddFile")]
        static extern bool NLPIR_NWI_AddFile(String sText);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_AddMem")]
        static extern bool NLPIR_NWI_AddMem(String sText);

        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_NWI_GetResult")]
        static extern IntPtr NLPIR_NWI_GetResult(bool bWeightOut = false);

        [DllImport(path, CharSet = CharSet.Ansi, EntryPoint = "NLPIR_NWI_Result2UserDict")]
        static extern uint NLPIR_NWI_Result2UserDict();

        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_GetKeyWords")]
        static extern IntPtr NLPIR_GetKeyWords(String sText, int nMaxKeyLimit = 50, bool bWeightOut = false);

        [DllImport(path, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi, EntryPoint = "NLPIR_GetFileKeyWords")]
        static extern IntPtr NLPIR_GetFileKeyWords(String sFilename, int nMaxKeyLimit = 50, bool bWeightOut = false);
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        #endregion


        #region CWB helper
        class ChineseWordBreaker
        {
            public ChineseWordBreaker(string splitResourceFile)
            {
                string chiParameters = "-tenglish 0 -numthre 11 -trepneforchilm 0 -twn 0 -dnne 1 -tcs 1 -tmb 1 ";
                chiParameters += @"-datapath " + splitResourceFile;
                engine = new Microsoft.MT.Common.Tokenization.SegRT(chiParameters);
            }

            public string GetResult(string sentence)
            {
                string result = engine.preSegSent(sentence);

                return result;
            }

            private void Parse(string second, Dictionary<int, string> replaces)
            {
                string[] items = second.Split(splitCharSet2, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in items)
                {
                    int index = item.IndexOf("|||");
                    if (index == -1)
                        continue;

                    try
                    {
                        int id = Int32.Parse(item.Substring(0, index).Trim());

                        index = item.LastIndexOf("|||");
                        if (index == -1)
                            continue;

                        index += 3;
                        string value = item.Substring(index, item.Length - index).Trim();

                        if (!replaces.ContainsKey(id))
                        {
                            replaces.Add(id, value);
                        }
                    }
                    catch //(Exception ex)
                    {
                        replaces = new Dictionary<int, string>();
                        return;
                    }
                }
            }
            public static readonly char[] splitCharSet = new char[] { ' ' };
            public static readonly char[] splitCharSet2 = new char[] { '{', '}' };

            private Microsoft.MT.Common.Tokenization.SegRT engine = null;

            public static readonly char[] splitCharset = new char[] { ' ' };
        }
        #endregion


        public static void Test()
        {
            string indexPath = @"C:\Users\v-xitwan\Desktop\temp\WeiboIndex\WeiboSortByHotIndex_Time_RemoveNoise2_RemoveSimilar2";
            var reader = LuceneOperations.GetIndexReader(indexPath);
            //var keywords = new string[]{"街","信","死","女","清","刷","骂","愿","爱","查","舰","版","通","岁","撕"};

            //foreach (var keyword in keywords)
            {
                var sw = new StreamWriter(@"C:\Users\v-xitwan\Desktop\temp\WeiboIndex\TestTokenizer" + "Stat" + ".txt", false,
                    Encoding.UTF8);
                //ChineseWordBreaker chineseWordBreaker = new ChineseWordBreaker(@"Utils\Lib\WordBreaker\");
                int cnt1 = 0, cnt2 = 0;
                int cnt1all = 0, cnt2all = 0;

                for (int iDoc = 0; iDoc < reader.NumDocs(); iDoc++)
                {
                    string sentence = reader.Document(iDoc).Get("NewsArticleDescription");

                    var words1 = NLPOperations.Tokenize(sentence, new TokenizeConfig(TokenizerType.ICTCLAS, StopWordsFile.CH));
                    var words2 = NLPOperations.Tokenize(sentence, new TokenizeConfig(TokenizerType.ChineseWordBreaker, StopWordsFile.CH));

                    //bool isPrint = false;
                    //foreach (var word in words1)
                    //    if (word.Length == 1)
                    //    {
                    //        isPrint = true;
                    //        cnt1++;
                    //    }
                    //foreach (var word in words2)
                    //    if (word.Length == 2)
                    //    {
                    //        isPrint = true;
                    //        cnt2++;
                    //    }
                    cnt1all += words1.Count;
                    cnt2all += words2.Count;

                    //if (isPrint)
                    //{
                    //    sw.WriteLine("-------------{0}-------------", iDoc);
                    //    sw.WriteLine(sentence);
                    //    sw.WriteLine("[ICT]\t" + StringOperations.GetMergedString(words1));
                    //    sw.WriteLine("[CWB]\t" + StringOperations.GetMergedString(words2));

                    //    sw.WriteLine("[ICT--]\t" + Marshal.PtrToStringAnsi(NLPIR_ParagraphProcess(sentence, 1)));
                    //    //sw.WriteLine("[CWB--]\t" + chineseWordBreaker.GetResult(sentence));
                    //    sw.WriteLine();

                    //    sw.Flush();
                    //}

                }

                sw.WriteLine("cnt1 = " + cnt1);
                sw.WriteLine("cnt2 = " + cnt2);
                sw.WriteLine("cnt1all = " + cnt1all);
                sw.WriteLine("cnt2all = " + cnt2all);

                sw.Flush();
                sw.Close(); 
            }
        }
    }
}
