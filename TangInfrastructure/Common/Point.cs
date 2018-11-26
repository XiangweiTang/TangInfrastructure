using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    class Point
    {
        public static Dictionary<int,List<int>> CreateContainDict(List<IInterval> intervals, List<IPoint> points)
        {
            Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
            int pointIndex = 0;
            for(int intervalIndex = 0; intervalIndex < intervals.Count; intervalIndex++)
            {
                if (pointIndex < points.Count && points[pointIndex].Position() > intervals[intervalIndex].End())
                    continue;
                while (pointIndex < points.Count && points[pointIndex].Position() <= intervals[intervalIndex].Start())
                    pointIndex++;
                List<int> list = new List<int>();
                while(pointIndex<points.Count
                    && points[pointIndex].Position()>intervals[intervalIndex].Start()
                    && points[pointIndex].Position() < intervals[intervalIndex].End())
                {
                    list.Add(pointIndex);
                    pointIndex++;
                }
                if (list.Count > 0)
                    dict.Add(intervalIndex, list);
            }
            return dict;
        }

        public static IEnumerable<string> InsertPoint(List<IInterval> intervals, List<IPoint> points)
        {
            int pointIndex = 0;
            double max = intervals[intervals.Count - 1].End();
            for(int intervalIndex = 0; intervalIndex < intervals.Count; intervalIndex++)
            {
                var interval = intervals[intervalIndex];
                yield return interval.Value();
                if (pointIndex >= points.Count)
                    continue;
                if (points[pointIndex].Position() >= max)
                    continue;
                while (pointIndex < points.Count && points[pointIndex].Position() <= intervals[intervalIndex].Start())
                    pointIndex++;

                if (pointIndex < points.Count)
                {
                    var point = points[pointIndex];
                    //  [Start   End=Position]
                    //  [Start   Position    End]
                    //  End]    Position    [Start
                    if((point.Position()==interval.End())||
                        (point.Position()<=interval.End()&&point.Position()>=interval.Start())||
                        (point.Position()>=interval.End()&&point.Position()<=intervals[intervalIndex+1].Start())
                        )
                    {
                        yield return point.Value();
                        pointIndex++;
                    }
                }                
            }
        }
    }

    interface IPoint
    {
        double Position();
        string Value();
    }
}
