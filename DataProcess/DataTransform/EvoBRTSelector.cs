using DataProcess.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataProcess.DataTransform
{
    class EvoBRTSelector
    {
        public void Start()
        {
            string path = @"D:\Project\StreamingRoseRiver\Obama\Tree1-5000-1e-100-LastYear\";
            
            HashSet<int> removeIndices = Util.GetHashSet(Util.GetIntArray(0, 70));
            HashSet<int> selectIndices = Util.GetHashSet(Util.GetIntArray(71, 124));
            int substractCount = removeIndices.Count;

            //foreach (var subPath in Directory.GetDirectories(path))
            {
                foreach (var fullFileName in Directory.GetFiles(path)) //Directory.GetFiles(subPath))
                {
                    var fileName = StringOperations.GetFileName(fullFileName);
                    var fileFolder = StringOperations.GetFolder(fullFileName);

                    int index = GetIndexInFile(fileName);
                    if(index == -1 || removeIndices.Contains(index))
                    {
                        File.Delete(fullFileName);
                    }
                    else
                    {
                        File.Copy(fullFileName, StringOperations.EnsureFolderEnd(fileFolder) + ReplaceFileNameIndex(fileName, index - substractCount), true);
                        File.Delete(fullFileName);
                    }
                }
            }
        }

        public int GetIndexInFile(string fileName)
        {
            var indexStr = Regex.Replace(fileName, "[^0-9]+", string.Empty);
            int index;
            if (int.TryParse(indexStr, out index))
            {
                return index;
            }
            else
                return -1;
        }

        public string ReplaceFileNameIndex(string fileName, int newIndex)
        {
            return Regex.Replace(fileName, "[0-9]+", newIndex.ToString());
        }

    }
}
