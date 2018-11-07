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
        public Dictionary<string, TextGridItem[]> ItemDict = new Dictionary<string, TextGridItem[]>();

        public TextGrid(string path)
        {
            var list = File.ReadLines(path);
            Set(list);
        }
        public TextGrid(IEnumerable<string> list)
        {
            Set(list);
        }

        private void Set(IEnumerable<string> list)
        {
            ItemList = Parse(list).ToList();
            ItemDict = ItemList.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToArray());
            SpeakerStartDict = ItemDict["SPK"].Cast<TextGridInterval>().ToDictionary(x => x.XMin, x => x.Text);
        }

        private IEnumerable<TextGridItem> Parse(IEnumerable<string> list)
        {
            TextGridItem currentItem = new TextGridItem();
            TextGridInterval currentInterval = new TextGridInterval();
            TextGridText currentText = new TextGridText();
            int currentTier = 0;
            string currentName = string.Empty;
            bool inInterval = false;
            foreach(string line in list)
            {
                if (InItemListReg.IsMatch(line))
                {
                    currentTier= int.Parse(InItemListReg.Match(line).Groups[1].Value);
                    currentItem.TierIndex = currentTier;
                    continue;
                }
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
