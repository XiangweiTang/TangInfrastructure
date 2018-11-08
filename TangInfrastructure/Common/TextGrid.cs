using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace TangInfrastructure
{
    class TextGrid
    {
        Regex InItemListReg = new Regex("item\\s*\\[([0-9]+)\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        Regex NameReg = new Regex("name\\s*=\\s*\"(.*)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        Regex IntervalReg = new Regex("intervals\\s*\\[([0-9]+)\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        Regex PointReg = new Regex("points\\s*\\[([0-9]+)\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        char[] Trims = { ' ', '"' };
        public List<TextGridItem> ItemList = new List<TextGridItem>();
        Dictionary<double, string> SpeakerStartDict = new Dictionary<double, string>();
        public Dictionary<string, List<TextGridItem>> ItemDict = new Dictionary<string, List<TextGridItem>>();
        public List<TextGridInterval> SpkList => ItemDict["SPK"].Cast<TextGridInterval>().ToList();
        public List<TextGridInterval> SylList => ItemDict["SYL"].Cast<TextGridInterval>().ToList();
        public List<TextGridInterval> CcList => ItemDict["CC"].Cast<TextGridInterval>().ToList();
        public List<TextGridInterval> IfList => ItemDict["IF"].Cast<TextGridInterval>().ToList();
        public List<TextGridText> BiList => ItemDict["BI"].Cast<TextGridText>().ToList();
        public bool RunRebuild = true;
        public TextGrid(string path)
        {
            var list = File.ReadLines(path);
            Set(list);
        }
        public TextGrid(IEnumerable<string> list)
        {
            Set(list);
        }
        List<string> Header = new List<string>();
        public IEnumerable<string> ReBuild()
        {
            double xmin = double.Parse(Header[3].Split('=')[1].Trim());
            double xmax = double.Parse(Header[4].Split('=')[1].Trim());
            foreach (string line in Header)
                yield return line;
            foreach(var item in ItemDict)
            {
                var first = item.Value[0];
                yield return $"\titem [{first.TierIndex}]:";
                string className = first.Type + "Tier";
                yield return GetEqual("class", $"\"{className}\"", "\t\t");
                yield return GetEqual("name", $"\"{item.Key}\"", "\t\t");
                yield return GetEqual("xmin", xmin, "\t\t");
                yield return GetEqual("xmax", xmax, "\t\t");
                if (first.Type == "Interval")
                {
                    yield return GetEqual($"intervals: size", item.Value.Count, "\t\t");
                    var list = item.Value.Cast<TextGridInterval>().ToList();
                    int n = 1;
                    foreach(var interval in list)
                    {
                        yield return $"\t\tintervals [{n}]:";
                        yield return GetEqual("xmin", interval.XMin, "\t\t\t");
                        yield return GetEqual("xmax", interval.XMax, "\t\t\t");
                        yield return GetEqual("text", $"\"{interval.Text}\"", "\t\t\t");
                        n++;
                    }
                }
                else
                {
                    yield return GetEqual($"points: size", item.Value.Count, "\t\t");
                    var list = item.Value.Cast<TextGridText>().ToList();
                    int n = 1;
                    foreach(var text in list)
                    {
                        yield return $"\t\tpoints [{n}]:";
                        yield return GetEqual("number", text.Number, "\t\t\t");
                        yield return GetEqual("mark", $"\"{text.Mark}\"", "\t\t\t");
                        n++;
                    }
                }
            }
        }

        private string GetEqual(string key, object value, string prefix)
        {
            return prefix + string.Join(" = ", key, value);
        }
        private void Set(IEnumerable<string> list)
        {
            ItemList = Parse(list).ToList();
            ItemDict = ItemList.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToList());
            SpeakerStartDict = SpkList.ToDictionary(x => x.XMin, x => x.Text);
            if (RunRebuild)
            {
                InsertBI();
                var ccSeps = CcList.SelectMany(x => new double[] { x.XMax, x.XMax }).Distinct().ToList();
                SplitSilSyl(ccSeps);
                SplitSilIf(ccSeps);
            }
        }

        private void InsertBI()
        {
            double min = SpkList[1].XMin;
            double max = SpkList[SpkList.Count - 2].XMax;
            ItemDict["BI"].Insert(0, new TextGridText { TierIndex = 7, Index = 0, Name = "BI", Type = "Text", Number = min, Mark = "4" });
            int n = ItemDict["BI"].Count;
            ItemDict["BI"].Add(new TextGridText { TierIndex = 7, Index = n, Name = "BI", Type = "Text", Number = max, Mark = "4" });
        }

        private void SplitSilSyl(List<double> ccSeps)
        {
            int ccIndex = 0;
            for (int i = 0; i < SylList.Count - 1; i++)
            {
                var item = SylList[i];
                if (ccIndex + 1 >= ccSeps.Count)
                    break;
                while (item.XMin > ccSeps[ccIndex])
                    ccIndex++;
                if (item.XMax < ccSeps[ccIndex])
                    continue;
                if (item.XMax > ccSeps[ccIndex + 1] && item.XMin < ccSeps[ccIndex])
                {
                    item.XMax = ccSeps[ccIndex + 1];
                    item.XMin = ccSeps[ccIndex];
                    SylList[i - 1].XMax = item.XMin;
                    SylList[i + 1].XMin = item.XMax;
                    continue;
                }
                if (item.XMax > ccSeps[ccIndex] && item.XMin < ccSeps[ccIndex])
                {
                    item.XMin = ccSeps[ccIndex];
                    SylList[i - 1].XMax = item.XMin;
                    continue;
                }
            }
        }
        private void SplitSilIf(List<double> ccSeps)
        {
            int ccIndex = 0;
            for(int i = 0; i < IfList.Count - 1; i++)
            {
                var item = IfList[i];
                if (ccIndex + 1 >= ccSeps.Count)
                    break;
                while (item.XMin > ccSeps[ccIndex])
                    ccIndex++;
                if (item.XMax < ccSeps[ccIndex])
                    continue;
                if (item.XMax > ccSeps[ccIndex + 1] && item.XMin < ccSeps[ccIndex])
                {
                    item.XMax = ccSeps[ccIndex + 1];
                    item.XMin = ccSeps[ccIndex];
                    IfList[i - 1].XMax = item.XMin;
                    IfList[i + 1].XMin = item.XMax;
                    continue;
                }
                if (item.XMax > ccSeps[ccIndex] && item.XMin < ccSeps[ccIndex])
                {
                    item.XMin = ccSeps[ccIndex];
                    IfList[i - 1].XMax = item.XMin;
                }
            }
        }


        private IEnumerable<TextGridItem> Parse(IEnumerable<string> list)
        {
            TextGridItem currentItem = new TextGridItem();
            TextGridInterval currentInterval = new TextGridInterval();
            TextGridText currentText = new TextGridText();
            int currentTier = 0;
            string currentName = string.Empty;
            bool inInterval = false;
            bool inHeader = true;
            foreach(string line in list)
            {
                if (InItemListReg.IsMatch(line))
                {
                    currentTier= int.Parse(InItemListReg.Match(line).Groups[1].Value);
                    currentItem.TierIndex = currentTier;
                    inHeader = false;
                    continue;
                }
                if (inHeader)
                    Header.Add(line);
                if (NameReg.IsMatch(line))
                {   
                    currentName= NameReg.Match(line).Groups[1].Value;
                    currentItem.Name = currentName;
                    continue;
                }

                if (IntervalReg.IsMatch(line))
                {
                    inInterval = true;
                    currentItem.Index = int.Parse(IntervalReg.Match(line).Groups[1].Value);
                    currentInterval = new TextGridInterval(currentItem);
                    currentInterval.IsSet = true;
                    currentItem = new TextGridItem { Name = currentName,TierIndex=currentTier };
                    continue;
                }
                if (line.Trim().StartsWith("xmin")&&inInterval)
                {
                    Sanity.Requires(currentInterval.IsSet, "Invalid format.");
                    currentInterval.XMin = double.Parse(line.Split('=')[1].Trim());
                    continue;
                }
                if (line.Trim().StartsWith("xmax")&&inInterval)
                {
                    Sanity.Requires(currentInterval.IsSet, "Invalid format.");
                    currentInterval.XMax = double.Parse(line.Split('=')[1].Trim());
                    continue;
                }
                if (line.Trim().StartsWith("text"))
                {
                    Sanity.Requires(currentInterval.IsSet, "Invalid format.");
                    currentInterval.Text = line.Split('=')[1].Trim(Trims);
                    inInterval = false;
                    yield return currentInterval;
                    continue;
                }

                if (PointReg.IsMatch(line))
                {
                    currentItem.Index = int.Parse(PointReg.Match(line).Groups[1].Value);
                    currentText = new TextGridText(currentItem);
                    currentText.IsSet = true;
                    currentItem = new TextGridItem { Name = currentName, TierIndex = currentTier };
                }
                if (line.Trim().StartsWith("number"))
                {
                    Sanity.Requires(currentText.IsSet, "Invalid format");
                    currentText.Number = double.Parse(line.Split('=')[1].Trim(Trims));
                    continue;
                }
                if (line.Trim().StartsWith("mark"))
                {
                    Sanity.Requires(currentText.IsSet, "Invalid format");
                    currentText.Mark = line.Split('=')[1].Trim(Trims);
                    yield return currentText;
                    continue;
                }
            }
        }

        public IEnumerable<string> CreateChunkByChar(string sessionId, string audioPath)
        {
            return ItemDict["SYL"].Cast<TextGridInterval>().Select(x => GetSpeakerId(x, sessionId, audioPath));
        }

        private string GetSpeakerId(TextGridInterval interval, string sessionId, string audioPath)
        {
            string speakerId = SpeakerStartDict.Last(x => x.Key <= interval.XMin).Value;
            if (speakerId != "A" && speakerId != "B")
                speakerId = "U";
            return string.Join("\t", interval.Index.ToString("000000"), speakerId, sessionId, interval.XMin, interval.XMax, interval.Text, audioPath);
        }

        public Dictionary<int,List<int>> MatchInterval(string bigKey, string smallKey)
        {
            var bigIntervals = ItemDict[bigKey].Cast<TextGridInterval>().ToArray();
            var smallIntervals = ItemDict[smallKey].Cast<TextGridInterval>().ToArray();
            Dictionary<int, List<int>> mappingDict = new Dictionary<int, List<int>>();
            int j = 0;
            List<int> currentList = new List<int>();
            for(int i = 0; i < smallIntervals.Length; i++)
            {
                if (smallIntervals[i].XMax > bigIntervals[j].XMin && smallIntervals[i].XMin < bigIntervals[j].XMax)
                {
                    currentList.Add(i);
                }
                else
                {
                    mappingDict.Add(j, currentList.ToList());
                    currentList.Clear();
                    currentList.Add(i);
                    j++;
                }
            }
            if (currentList.Count > 0 && j < bigIntervals.Length)
                mappingDict.Add(j, currentList.ToList());
            return mappingDict;
        }

        public Dictionary<int,List<int>> MatchIntervalText(string intervalKey, string textKey)
        {
            var intervals = ItemDict[intervalKey].Cast<TextGridInterval>().ToArray();
            var texts = ItemDict[textKey].Cast<TextGridText>().ToArray();
            List<int> currentList = new List<int>();
            Dictionary<int, List<int>> mappingDict = new Dictionary<int, List<int>>();
            int j = 0;
            for(int i = 0; i < intervals.Length; i++)
            {
                if (intervals[i].XMin < texts[j].Number)
                {
                    currentList.Add(i);
                }
                else
                {
                    mappingDict.Add(j, currentList.ToList());
                    currentList.Clear();
                    currentList.Add(i);
                    j++;
                }
                if (j >= texts.Length)
                    break;
            }
            if (currentList.Count > 0 && j < texts.Length)
                mappingDict.Add(j, currentList.ToList());
            return mappingDict;
        }
    }
    class TextGridItem
    {
        public int TierIndex { get; set; } = 0;
        public int Index { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsSet { get; set; } = false;
        public TextGridItem() { }
        public TextGridItem(TextGridItem item)
        {
            TierIndex = item.TierIndex;
            Index = item.Index;
            Name = item.Name;
            IsSet = item.IsSet;
        }
    }
    class TextGridInterval : TextGridItem
    {
        public double XMin { get; set; } = 0;
        public double XMax { get; set; } = 0;
        public string Text { get; set; } = string.Empty;
        public TextGridInterval() { }
        public TextGridInterval(TextGridItem item):base(item)
        {
            Type = "Interval";            
        }
    }
    class TextGridText : TextGridItem
    {
        public double Number { get; set; } = 0.0;
        public string Mark { get; set; } = string.Empty;
        public TextGridText() { }
        public TextGridText(TextGridItem item):base(item)
        {            
            Type = "Text";
        }
    }
}
