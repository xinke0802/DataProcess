using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcess.Utils
{
    public class RandomUtils
    {
        public static T[] GetRandomSamples<T>(IEnumerable<T> items, int sampleNum, Random random = null)
        {
            if (sampleNum >= items.Count())
                return items.ToArray();
            else
            {
                var ids = GetRandomSamples(0, items.Count(), sampleNum, random);
                return Array.ConvertAll(ids, id => items.ElementAt(id));
                //return GetRandomOrderItems(items, random).Take(sampleNum).ToArray();
            }
        }

        public static int[] GetRandomSamples(int startIndex, int endIndex, int sampleNum, Random random = null)
        {
            if (sampleNum < 0)
                return Util.GetIntArray(startIndex, endIndex);

            if (sampleNum == endIndex - startIndex + 1)
            {
                return Util.GetIntArray(startIndex, endIndex);
            }

            if (random == null)
                random = new Random();

            sampleNum = Math.Min(sampleNum, (endIndex - startIndex + 1));
            HashSet<int> samples = new HashSet<int>();
            while (samples.Count < sampleNum)
            {
                samples.Add(random.Next(startIndex, endIndex));
            }
            return samples.ToArray();
        }

        public static T[] GetRandomOrderItems<T>(IEnumerable<T> items, Random random = null)
        {
            if (random == null)
                random = new Random();

            int itemCnt = items.Count();
            bool[] isSelected = new bool[itemCnt];

            T[] orderedItems = new T[itemCnt];

            int freeItemCnt = itemCnt;
            int pointer = 0;
            while(freeItemCnt > 0)
            {
                int dPointer = random.Next(freeItemCnt);
                for (int i = 0; i < dPointer; i++)
                {
                    incrementPointer(ref pointer, isSelected);
                }
                isSelected[pointer] = true;
                orderedItems[freeItemCnt - 1] = items.ElementAt(pointer);
                if (freeItemCnt > 1)
                    incrementPointer(ref pointer, isSelected);
                
                freeItemCnt--;
            }
            return orderedItems;
        }

        private static void incrementPointer(ref int pointer, bool[] isSelected)
        {
            do
            {
                pointer++;
                if (pointer >= isSelected.Length)
                    pointer -= isSelected.Length;
            }
            while (isSelected[pointer]);
        }
    }
}
