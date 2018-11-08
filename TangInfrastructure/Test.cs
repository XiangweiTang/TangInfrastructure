using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TangInfrastructure
{
    class Test
    {
        char[] Sep = { ' ', '/' };
        Config Cfg = new Config();
        Regex NumReg = new Regex("[0-9]+", RegexOptions.Compiled);
        Regex ValidReg = new Regex("^[a-zA-Z_]*$", RegexOptions.Compiled);
        public Test(string[] args)
        {
            string path = @"D:\XiangweiTang\在职毕业设计\自然对话-银行";
            //var list = Directory.EnumerateFiles(path, "*.textgrid").SelectMany(x => Intervals(x, "CC", "SYL")).ToList();            
            string path1 = @"D:\XiangweiTang\在职毕业设计\自然对话-银行\NDYH0002.TextGrid";
            TextGrid tg = new TextGrid(path1);
            var list = tg.ReBuild();
            File.WriteAllLines(@"D:\XiangweiTang\在职毕业设计\自然对话-银行\1.textgrid", list);
        }

        private IEnumerable<Tuple<string,string>> Intervals(string path, string bigKey, string smallKey)
        {
            TextGrid tg = new TextGrid(path);
            var dict = tg.MatchInterval(bigKey, smallKey);
            var list = dict.Select(x => new { big = (tg.ItemDict[bigKey][x.Key] as TextGridInterval).Text, small = x.Value.Select(y => (tg.ItemDict[smallKey][y] as TextGridInterval).Text).Aggregate((p, q) => p + " " + q) });
            var newList= list.Select(x => new Tuple<string, string>(CleanupTrans( x.big),CleanupSyl( x.small)));
            foreach(var t in newList)
            {
                if (t.Item1.Contains("儿") && t.Item1.Length == t.Item2.Split(' ').Length + 1)
                    continue;
                if (t.Item1.Length == 0 && t.Item2.Length == 0)
                    continue;
                if (t.Item1.Length == t.Item2.Split(' ').Length)
                    continue;
                yield return t;
            }
        }
        private IEnumerable<string> IntervalGroups(string path,string intervalKey, string textKey)
        {
            TextGrid tg = new TextGrid(path);
            var dict = tg.MatchIntervalText(intervalKey, textKey);
            var list = dict.Select(x => string.Join(" ",x.Value.Select(y=> (tg.ItemDict[intervalKey][y] as TextGridInterval).Text)));
            return list;
        }

        private string CleanupTrans(string s)
        {
            return new string(s.Where(x => x >= '一' && x <= '龟').ToArray());
        }

        private string CleanupSyl(string s)
        {
            return string.Join(" ", s.Split(Sep, StringSplitOptions.RemoveEmptyEntries).Where(x => !x.Contains("sil") && x != "xx" && x != "x"&&x!="xxx"));
        }

        private void SplitByChar()
        {
            string folderPath = @"D:\XiangweiTang\在职毕业设计\自然对话-银行";
            string outputFolder = @"D:\XiangweiTang\Data\ByChar\TcFolder";
            var list = Directory.EnumerateFiles(folderPath, "*.textgrid");
            foreach (string path in list)
            {
                FileInfo file = new FileInfo(path);
                string fileName = file.Name.Replace(file.Extension, string.Empty);
                string wavPath = path.Replace(file.Extension, ".wav");
                TextGrid tg = new TextGrid(path);
                var tcList = tg.CreateChunkByChar(fileName, wavPath);
                string filePath = Path.Combine(outputFolder, fileName + ".txt");
                File.WriteAllLines(filePath, tcList);
            }
        }

        private void CleanupPhons()
        {
            var dict = File.ReadLines("PhonCleanDict.txt").ToDictionary(x => x.Split('\t')[0], x => x.Split('\t')[1]);
            foreach (string path in Directory.EnumerateFiles(@"D:\XiangweiTang\Data\ByChar\TcFolder"))
            {
                var list = File.ReadAllLines(path).Select(x => new TcLine(x))
                    .Select(x =>
                    {
                        string text = x.Text;
                        if (dict.ContainsKey(text))
                            text = dict[text];
                        else
                            text = NumReg.Replace(text, string.Empty).ToLower();
                        x.UpdateText(text);
                        return x;
                    })
                    .Where(x => ValidReg.IsMatch(x.Text))
                    .Select(x => x.Output);
                File.WriteAllLines(path, list);
            }
        }

        private void CutByPhon()
        {
            var list = Directory.EnumerateFiles(@"D:\XiangweiTang\Data\ByChar\TcFolder").Shuffle();
            int testCount = Convert.ToInt32(list.Length * 0.1);
            var test = list.ArrayTake(testCount);
            var train = list.ArraySkip(testCount);

            string testFolder = @"D:\XiangweiTang\Data\ByChar\Test";
            string trainFolder = @"D:\XiangweiTang\Data\ByChar\Train";
            Parallel.ForEach(test, new ParallelOptions { MaxDegreeOfParallelism = 10 }, tcPath =>
             {
                 CutByPhon(tcPath, testFolder);
             });
            Parallel.ForEach(train, new ParallelOptions { MaxDegreeOfParallelism = 10 }, tcPath =>
             {
                 CutByPhon(tcPath, trainFolder);
             });
        }

        private void CutByPhon(string tcPath, string outputFolder)
        {
            Console.WriteLine("Processing " + tcPath);
            var list = File.ReadLines(tcPath).Select(x => new TcLine(x));
            foreach(var tcLine in list)
            {
                string inputPath = tcLine.SrcAudioPath;
                string phon = tcLine.Text;
                if (phon.Contains(' '))
                    continue;
                string phonFolder = Path.Combine(outputFolder, tcLine.Text);
                Directory.CreateDirectory(phonFolder);
                string outputAudioPath = Path.Combine(phonFolder, $"{tcLine.SpeakerId.Replace("/","")}_{tcLine.SessionId}_{tcLine.FileName}.wav");
                string args = string.Join(" ", inputPath, outputAudioPath, "trim", tcLine.StartTime, tcLine.Duration);
                Common.RunFile(Cfg.SoxPath, args);
            }
        }
    }
}
