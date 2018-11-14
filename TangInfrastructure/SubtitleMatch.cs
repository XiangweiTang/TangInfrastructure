using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace TangInfrastructure
{
    class SubtitleMatch
    {
        static Regex TimeStrReg = new Regex("([0-9:,]*)\\s*-->\\s*([0-9:,]*)", RegexOptions.Compiled);
        public static IEnumerable<SubtitleLine> ConvertToSubtitle(IEnumerable<string> list, double offset=0)
        {
            bool startSubtitle = false;
            double currentStart = 0;
            double currentEnd = 0;
            List<string> currentContent = new List<string>();
            foreach(string line in list)
            {
                if (TimeStrReg.IsMatch(line))
                {
                    startSubtitle = true;
                    currentStart = ToTimePair(line).Item1;
                    currentEnd = ToTimePair(line).Item2;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(line))
                {
                    startSubtitle = false;
                    string s = string.Join(" ", currentContent);
                    if (!string.IsNullOrWhiteSpace(s))
                        yield return new SubtitleLine() { StartTime = currentStart+offset, EndTime = currentEnd+offset, Content = s };
                    currentContent = new List<string>();
                    continue;
                }
                if (startSubtitle)
                {
                    currentContent.Add(line);
                }
            }
        }

        private static Tuple<double, double> ToTimePair(string line)
        {
            double start = TimeToDouble(TimeStrReg.Match(line).Groups[1].Value);
            double end = TimeToDouble(TimeStrReg.Match(line).Groups[2].Value);
            return new Tuple<double, double>(start, end);
        }
        private static double TimeToDouble(string timeStr)
        {
            var split = timeStr.Split(':');
            double second = double.Parse(split.Last().Replace(',', '.'));
            if (split.Length == 2)
            {
                second += int.Parse(split[0]) * 60;
            }
            if(split.Length==3)
            {
                second += int.Parse(split[0]) * 3600 + int.Parse(split[1]) * 60;
            }
            return second;
        }

        public static IEnumerable<Tuple<SubtitleLine, SubtitleLine>> SubtitleZip(SubtitleLine[] list1, SubtitleLine[] list2)
        {
            int j = 0;
            for(int i = 0; i < list1.Length; i++)
            {
                if (list1[i].EndTime < list2[j].StartTime)
                    continue;
                while (j<list2.Length&& list1[i].StartTime > list2[j].EndTime)
                    j++;
                if (j >= list2.Length)
                    break;
                if (IsOverlap(list1[i], list2[j]))
                {
                    j = GetMatchedIndex(list1[i], list2, j);
                    yield return new Tuple<SubtitleLine, SubtitleLine>(list1[i], list2[j]);
                    j++;
                    if (j >= list2.Length)
                        break;
                }
            }
        }

        private static int GetMatchedIndex(SubtitleLine s1, SubtitleLine[] list2, int j)
        {
            int k = j;
            double diff = double.MaxValue;
            int currentIndex = k;
            while (IsOverlap(s1, list2[k]) && k < list2.Length)
            {
                double currentDiff = OverlapSize(s1, list2[k]);
                if (currentDiff < diff)
                    return k;
                k++;
            }
            return j;
        }

        private static double OverlapSize(SubtitleLine s1, SubtitleLine s2)
        {
            return Math.Abs(s1.StartTime - s2.StartTime) + Math.Abs(s1.EndTime - s2.EndTime);
        }

        private static bool IsOverlap(SubtitleLine s1, SubtitleLine s2)
        {
            return s1.StartTime <= s2.EndTime && s1.EndTime >= s2.StartTime;
        }
    }

    class SubtitleLine
    {
        public double StartTime = 0;
        public double EndTime = 0;
        public string Content = string.Empty;

        public string Overview => string.Join(" ", StartTime, EndTime, Content);        
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is SubtitleLine))
                return false;
            return Matches(this, obj as SubtitleLine);
        }
        public override int GetHashCode()
        {
            return 0;
        }
        public static bool Matches(SubtitleLine sl1, SubtitleLine sl2)
        {
            return sl1.StartTime <= sl2.EndTime && sl2.StartTime <= sl1.EndTime;
        }
        public SubtitleLine()
        {
        }
    }
}
