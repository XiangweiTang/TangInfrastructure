﻿using System;
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
        public List<TextGridInterval> StList => ItemDict["ST"].Cast<TextGridInterval>().ToList();        
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

        public void InsertBiToCc(string outputPath)
        {
            File.WriteAllLines(outputPath, InsertBiToCc());
        }

        public IEnumerable<string> InsertBiToCc()
        {
            var dict = Interval.CreateContainDict(CcList.Cast<IInterval>().ToList(), SylList.Cast<IInterval>().ToList());
            List<IPoint> biList = BiList.Cast<IPoint>().ToList();
            foreach(var item in dict)
            {
                string sentence = StringProcess.CleanupTag(CcList[item.Key].Text);
                int sntLength = sentence.Length;
                var sylList = item.Value.Select(x => SylList[x] as IInterval).ToList();                
                var cleanList = sylList.Select(x => x.Value()).Where(x => !StringProcess.IsTag(x)).ToList();
                int count = cleanList.Count;
                bool suffixFlag = count < sentence.Length && ValidSuffix(sentence, count);
                if (sentence.Length == 0)
                    continue;
                if (count == sntLength||suffixFlag)
                {
                    var newList = suffixFlag ? IntervalTransform(ReorgString(sentence), sylList) : IntervalTransform(sentence, sylList);
                    var merged = Point.InsertPoint(newList.ToList(), biList);
                    string trans = string.Join(" ", merged);
                    yield return trans;
                }
            }
        }    
        
        public void InsertStToCc(string outputPath)
        {
            File.WriteAllLines(outputPath, InsertStToCc());
        }

        public IEnumerable<string> InsertStToCc()
        {
            var dict = Interval.CreateContainDict(CcList.Cast<IInterval>().ToList(), SylList.Cast<IInterval>().ToList());
            List<IInterval> stList = StList.Cast<IInterval>().ToList();
            foreach(var item in dict)
            {
                string sentence = StringProcess.CleanupTag(CcList[item.Key].Text);
                int sntLength = sentence.Length;
                var sylList = item.Value.Select(x => SylList[x] as IInterval).ToList();
                var cleanList = sylList.Select(x => x.Value()).Where(x => !StringProcess.IsTag(x)).ToList();
                int count = cleanList.Count();
                bool suffixFlag = count < sentence.Length && ValidSuffix(sentence, count);
                if (sentence.Length == 0)
                    continue;
                if (count == sntLength || suffixFlag)
                {
                    var newList = suffixFlag ? IntervalTransform(ReorgString(sentence), sylList) : IntervalTransform(sentence, sylList);
                    var merged = Interval.PadIntervals(newList.ToList(), stList);
                    
                    string trans= string.Join(" ", merged);
                    yield return trans;
                }
            }
        }
        private bool ValidSuffix(string snt, int count)
        {
            if (snt.Replace("儿", "").Length == count)
                return true;
            if (snt.Replace("呃", "").Length == count)
                return true;
            return snt.Replace("啊", "").Length == count;
        }

        private IEnumerable<string> ReorgString(string snt)
        {
            char c;
            if (snt.Contains('儿'))
                c = '儿';
            else if (snt.Contains('呃'))
                c = '呃';
            else
                c = '啊';
            for(int i = 0; i < snt.Length; i++)
            {
                if (i < snt.Length - 1 && snt[i + 1] == c)
                {
                    yield return snt.Substring(i, 2);
                    i++;
                }
                else
                    yield return snt[i].ToString();
            }
        }

        private IEnumerable<IInterval> IntervalTransform<T>(IEnumerable<T> collection, IEnumerable<IInterval> intervalList)
        {
            int i = 0;
            var array = collection.ToArray();
            foreach (var interval in intervalList)
            {
                if (!StringProcess.IsTag(interval.Value()))
                {
                    TextGridInterval ti = new TextGridInterval(interval, array[i].ToString());
                    yield return ti as IInterval;
                    i++;
                }
            }
        }

        private IEnumerable< IEnumerable<string>> _ReBuild()
        {
            TaggingBi();
            TaggingSyl();
            TaggingcC();
            TaggingSt();
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

        private void TaggingSt()
        {
            foreach(TextGridItem tgi in ItemDict["ST"])
            {
                string text = tgi.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    string tagging = $"<ST{text}>";
                    tgi.UpdateText(tagging);
                }
            }
        }

        private void TaggingBi()
        {
            foreach(TextGridItem tgi in ItemDict["BI"])
            {
                string text = tgi.Text;
                string tagging = $"<BI{text}>";
                tgi.UpdateText(tagging);
            }
        }

        private void TaggingcC()
        {
            foreach(TextGridItem tgi in ItemDict["CC"])
            {
                string tagText = StringProcess.NormXSil(tgi.Text);
                string overlapText = StringProcess.NormOverlap(tagText);
                string cleanText = StringProcess.CleanupChsString(overlapText, true);
                tgi.UpdateText(cleanText);
            }
        }

        private void TaggingSyl()
        {
            foreach(TextGridItem tgi in ItemDict["SYL"])
            {
                if (tgi.Text.Contains('/') || tgi.Text.Contains('\\'))
                    tgi.UpdateText("<overlap>");
                else
                {
                    string tagText = StringProcess.NormXSil(tgi.Text);
                    string cleanText = StringProcess.CleanupSpace(tagText);
                    tgi.UpdateText(cleanText);
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
        public TextGridInterval(IInterval interval, string value)
        {
            XMin = interval.Start();
            XMax = interval.End();
            Text = value;
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
