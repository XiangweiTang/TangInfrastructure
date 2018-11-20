using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;

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
        public List<TextGridPoint> BiList => ItemDict["BI"].Cast<TextGridPoint>().ToList();
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

        public void Rebuild(string outputPath)
        {
            var list = _ReBuild().SelectMany(x => x);
            File.WriteAllLines(outputPath, list);
        }

        private IEnumerable< IEnumerable<string>> _ReBuild()
        {
            double xmin = double.Parse(Header.Single(x => x.Contains("xmin")).Split('=')[1].Trim());
            double xmax = double.Parse(Header.Single(x => x.Contains("xmax")).Split('=')[1].Trim());
            yield return Header;
            foreach (var item in ItemDict)
            {
                yield return OutputTierHeader(xmin, xmax, item.Value, "\t");                
            }
        }

        private IEnumerable<string> OutputTierHeader(double xmin, double xmax, List<TextGridItem> list, string tabOffset)
        {
            Sanity.Requires(list.Count > 0, "The tier is empty.");
            var first = list[0];
            string classLine, sizeLine;            
            switch (first.Type)
            {
                case TextGridItemType.Interval:
                    classLine = $"{tabOffset}\tclass = \"IntervalTier\"";
                    sizeLine = $"{tabOffset}\tintervals: size = {list.Count}";
                    break;
                case TextGridItemType.Point:
                    classLine = $"{tabOffset}\tclass = \"TextTier\"";
                    sizeLine = $"{tabOffset}\tpoints: size = {list.Count}";
                    break;
                default:
                    throw new TangInfrastructureException("Invalid text grid type: " + first.Type.ToString());
            }
            yield return $"{tabOffset}item [{first.TierIndex}]:";
            yield return classLine;
            yield return $"{tabOffset}\tname = \"{first.Name}\"";
            yield return $"{tabOffset}\txmin = {xmin}";
            yield return $"{tabOffset}\txmax = {xmax}";
            yield return sizeLine;

            var outputList = list.SelectMany(x => x.ToTextGrid(tabOffset + "\t"));
            foreach (string line in outputList)
                yield return line;
        }

        private void Set(IEnumerable<string> list)
        {
            ItemList = Parse(list).ToList();
            ItemDict = ItemList.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToList());
            var ccSylDict = Interval.CreateContainDict(CcList.Cast<IInterval>().ToList(), SylList.Cast<IInterval>().ToList());
            var ccBiDict = Point.CreateContainDict(CcList.Cast<IInterval>().ToList(), BiList.Cast<IPoint>().ToList());
            foreach(var item in ccSylDict)
            {
                int ccIndex = item.Key;
                var sylIndices = item.Value;
                string c = CcList[ccIndex].Text;
                string s = string.Join(" ", sylIndices.Select(x => SylList[x].Text));
                if (ccBiDict.ContainsKey(item.Key))
                {
                    var biIndices = ccBiDict[item.Key];
                    var intervals = sylIndices.Select(x => SylList[x] as IInterval).ToList();
                    var points = biIndices.Select(x => BiList[x] as IPoint).ToList();
                    var wordList = Point.InsertPoint(intervals, points).ToList();
                }
            }
        }

        private void TaggingBi()
        {
            foreach(TextGridItem tgi in ItemDict["BI"])
            {
                string text = tgi.Text;
                string tagging = $"<BI{text}";
                tgi.UpdateText(tagging);
            }
        }

        private void TaggingcC()
        {
            foreach(TextGridItem tgi in ItemDict["CC"])
            {
                string text = tgi.Text;
                //TODO
            }
        }

        public IEnumerable<string> MatchWords()
        {
            var dict = MatchInterval("CC", "SYL");
            int biIndex = 0;
            foreach(var item in dict)
            {
                if (biIndex >= BiList.Count - 1)
                    break;
                var cc = CcList[item.Key];
                cc.Text = Common.CleanupTrans(cc.Text);

                var syls = item.Value
                    .Select(x => { var t = SylList[x]; t.Text = Common.CleanupSyl(t.Text); return t; })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                    .ToList();
                

                var ccs = Common.SplitWords(cc.Text).Select(x => x.ToString()).ToList();
                
                if (ccs.Count == 0 && syls.Count == 0)
                {

                }
                else if (ccs.Count == syls.Count)
                {
                    while (BiList[biIndex].Point < syls[0].XMin)
                        biIndex++;
                    List<string> wordList = new List<string>();
                    List<string> currentWordList = new List<string>();
                    List<string> sylList = new List<string>();
                    List<string> currentSylList = new List<string>();
                    double min = -1;
                    double max = 0;
                    for(int i = 0; i < syls.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(syls[i].Text))
                            continue;
                        if(syls[i].XMin>=BiList[biIndex].Point)
                        {
                            if (currentWordList.Count > 0)
                            {
                                string word = string.Join(" ", currentWordList);
                                string syl = string.Join(" ", currentSylList);
                                yield return string.Join("\t", item.Key, word, syl.Replace("*", "").Replace("?", ""), min, max - min);
                                min = -1;
                                wordList.Add(string.Join(" ", currentWordList));
                                sylList.Add(string.Join(" ", currentSylList));
                            }
                            currentWordList.Clear();
                            currentSylList.Clear();
                            biIndex++;
                        }
                        if (min < 0)
                            min = syls[i].XMin;
                        max = syls[i].XMax;
                        currentWordList.Add(ccs[i]);
                        currentSylList.Add(syls[i].Text);
                    }
                    wordList.Add(string.Join(" ", currentWordList));
                    sylList.Add(string.Join(" ", currentSylList));
                }
                else
                {
                    //yield return string.Join("\t", item.Key, string.Join(" ",ccs), string.Join(" ",syls.Select(x=>x.Text)));
                }
            }
        }

        private IEnumerable<TextGridItem> Parse(IEnumerable<string> list)
        {
            TextGridItem currentItem = new TextGridItem();
            TextGridInterval currentInterval = new TextGridInterval();
            TextGridPoint currentText = new TextGridPoint();
            int currentTier = 0;
            string currentName = string.Empty;
            bool inInterval = false;
            bool inHeader = true;
            foreach (string line in list)
            {
                if (InItemListReg.IsMatch(line))
                {
                    currentTier = int.Parse(InItemListReg.Match(line).Groups[1].Value);
                    currentItem.TierIndex = currentTier;
                    inHeader = false;
                    continue;
                }
                if (inHeader)
                    Header.Add(line);
                if (NameReg.IsMatch(line))
                {
                    currentName = NameReg.Match(line).Groups[1].Value;
                    currentItem.Name = currentName;
                    continue;
                }

                if (IntervalReg.IsMatch(line))
                {
                    inInterval = true;
                    currentItem.Index = int.Parse(IntervalReg.Match(line).Groups[1].Value);
                    currentInterval = new TextGridInterval(currentItem);
                    currentInterval.IsSet = true;
                    currentItem = new TextGridItem { Name = currentName, TierIndex = currentTier };
                    continue;
                }
                if (line.Trim().StartsWith("xmin") && inInterval)
                {
                    Sanity.Requires(currentInterval.IsSet, "Invalid format.");
                    currentInterval.XMin = double.Parse(line.Split('=')[1].Trim());
                    continue;
                }
                if (line.Trim().StartsWith("xmax") && inInterval)
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
                    currentText = new TextGridPoint(currentItem);
                    currentText.IsSet = true;
                    currentItem = new TextGridItem { Name = currentName, TierIndex = currentTier };
                }
                if (line.Trim().StartsWith("number"))
                {
                    Sanity.Requires(currentText.IsSet, "Invalid format");
                    currentText.Point = double.Parse(line.Split('=')[1].Trim(Trims));
                    continue;
                }
                if (line.Trim().StartsWith("mark"))
                {
                    Sanity.Requires(currentText.IsSet, "Invalid format");
                    currentText.Text = line.Split('=')[1].Trim(Trims);
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

        public Dictionary<int, List<int>> MatchInterval(string bigKey, string smallKey)
        {
            var bigIntervals = ItemDict[bigKey].Cast<TextGridInterval>().ToArray();
            var smallIntervals = ItemDict[smallKey].Cast<TextGridInterval>().ToArray();
            Dictionary<int, List<int>> mappingDict = new Dictionary<int, List<int>>();
            int j = 0;
            List<int> currentList = new List<int>();
            for (int i = 0; i < smallIntervals.Length; i++)
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

        public Dictionary<int, List<int>> MatchIntervalText(string intervalKey, string textKey)
        {
            var intervals = ItemDict[intervalKey].Cast<TextGridInterval>().ToArray();
            var texts = ItemDict[textKey].Cast<TextGridPoint>().ToArray();
            List<int> currentList = new List<int>();
            Dictionary<int, List<int>> mappingDict = new Dictionary<int, List<int>>();
            int j = 0;
            for (int i = 0; i < intervals.Length; i++)
            {
                if (intervals[i].XMin < texts[j].Point)
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
        public TextGridItemType Type { get; set; } = TextGridItemType.NA;
        public bool IsSet { get; set; } = false;
        public string Text { get; set; } = string.Empty;
        public TextGridItem() { }
        public TextGridItem(TextGridItem item)
        {
            Sanity.Requires(Type == item.Type, "The two TextGridItem are different.");
            TierIndex = item.TierIndex;
            Index = item.Index;
            Name = item.Name;
            IsSet = item.IsSet;
            Text = item.Text;
        }

        public void UpdateText(string text)
        {
            Text = text;
        }

        public virtual IEnumerable<string> ToTextGrid(string tabOffset)
        {
            yield return string.Empty;
        }
    }
    class TextGridInterval : TextGridItem, IInterval
    {
        public double XMin { get; set; } = 0;
        public double XMax { get; set; } = 0;
        public TextGridInterval() { }
        public TextGridInterval(TextGridItem item):base(item)
        {
            Type = TextGridItemType.Interval;     
        }

        public void SetAsTag()
        {
            Text = $"{Name}{Text}";
        }

        public double Start()
        {
            return XMin;
        }

        public double End()
        {
            return XMax;
        }

        public string Value()
        {
            return Text;
        }

        public override IEnumerable<string> ToTextGrid(string tabOffset)
        {
            yield return $"{tabOffset}intervals [{Index}]:";
            yield return $"{tabOffset}\txmin = {XMin}";
            yield return $"{tabOffset}\txmax = {XMax}";
            yield return $"{tabOffset}\ttext = \"{Text}\"";
        }
    }
    class TextGridPoint : TextGridItem, IPoint
    {
        public double Point { get; set; } = 0.0;
        public TextGridPoint() { }
        public TextGridPoint(TextGridItem item):base(item)
        {
            Type = TextGridItemType.Point;
        }

        public double Position()
        {
            return Point;
        }

        public string Value()
        {
            return Text;
        }

        public override IEnumerable<string> ToTextGrid(string tabOffset)
        {
            yield return $"{tabOffset}points [{Index}]:";
            yield return $"{tabOffset}\tnumber = {Point}";
            yield return $"{tabOffset}\tmark = \"{Text}\"";
        }
    }
    enum TextGridItemType
    {
        NA,
        Interval,
        Point,
    }
}
