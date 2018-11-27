using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    class Interval
    {
        public static Dictionary<int, List<int>> CreateContainDict(List<IInterval> bigIntervalList, List<IInterval> smallIntervalList)
        {
            Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
            int sIndex = 0;
            for(int bIndex = 0; bIndex < bigIntervalList.Count; bIndex++)
            {
                var big = bigIntervalList[bIndex];
                while (sIndex < smallIntervalList.Count && smallIntervalList[sIndex].End() < big.Start())
                    sIndex++;                
                List<int> sList = new List<int>();
                while (sIndex < smallIntervalList.Count && smallIntervalList[sIndex].End() <= big.End())
                {
                    sList.Add(sIndex);
                    sIndex++;
                }
                if (sList.Count > 0)
                    dict.Add(bIndex, sList);
            }
            return dict;
        }
        public static IEnumerable<string> PadIntervals(List<IInterval> seqIntervalList, List<IInterval> padIntervalList)
        {
            int padIntervalIndex = 0;
            for(int seqIntervalIndex = 0; seqIntervalIndex < seqIntervalList.Count; seqIntervalIndex++)
            {
                string s= seqIntervalList[seqIntervalIndex].Value();
                yield return seqIntervalList[seqIntervalIndex].Value();
                while (padIntervalIndex < padIntervalList.Count && padIntervalList[padIntervalIndex].End() <= seqIntervalList[seqIntervalIndex].Start())
                    padIntervalIndex++;
                if (padIntervalIndex < padIntervalList.Count
                    && padIntervalList[padIntervalIndex].Start() >= seqIntervalList[seqIntervalIndex].Start()
                    && padIntervalList[padIntervalIndex].Start() < seqIntervalList[seqIntervalIndex].End())
                {
                    double ps = padIntervalList[padIntervalIndex].Start();
                    double ss = seqIntervalList[seqIntervalIndex].Start();
                    double se= seqIntervalList[seqIntervalIndex].End();
                    string p= padIntervalList[padIntervalIndex].Value();
                    yield return padIntervalList[padIntervalIndex].Value();
                    padIntervalIndex++;
                }
            }
        }
    }

    interface IInterval
    {
        double Start();
        double End();
        string Value();
    }
}
