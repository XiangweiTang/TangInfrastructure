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
        
    }

    interface IInterval
    {
        double Start();
        double End();
        string Value();
    }
}
