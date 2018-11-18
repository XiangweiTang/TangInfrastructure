using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

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
            OpusProcessing.ExtractTcLine(@"D:\XiangweiTang\Data\OpusXml", @"D:\XiangweiTang\Data\OpusTxt");
        }



        private void MergeSubtitlesRandomSample(string chsPath, string enuPath, string outputPath, double enuOffset, bool shuffle)
        {
            var chsArray = SubtitleMatch.ConvertToSubtitle(File.ReadLines(chsPath,Encoding.GetEncoding("GB2312")), 0).ToArray();
            var enuArray = SubtitleMatch.ConvertToSubtitle(File.ReadLines(enuPath), enuOffset).ToArray();

            var list = SubtitleMatch.SubtitleZip(chsArray, enuArray);
            var outputList = shuffle ? list.Select(x => x.Item1.Overview + "\t" + x.Item2.Overview).Shuffle().Take(20) : list.Select(x => x.Item1.Content + "\t" + x.Item2.Content);
            File.WriteAllLines(outputPath, outputList);
            string path = @"D:\Download\zh_en.txt";
            StringCleanup sp = new StringCleanup();
        }

        private void TransportBigFolder()
        {
            string outputFolder = @"D:\XiangweiTang\Data\ByWord\Long";
            var list = Directory.EnumerateDirectories(@"D:\XiangweiTang\Data\ByWord\Wav");
            Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 10 }, subFolderPath =>
            {
                if (Directory.EnumerateFiles(subFolderPath).Count() >= 10)
                {
                    string folderName = subFolderPath.Split('\\').Last();
                    string outputSubFolder = Path.Combine(outputFolder, folderName);
                    Directory.CreateDirectory(outputSubFolder);
                    foreach (string filePath in Directory.EnumerateFiles(subFolderPath))
                    {
                        string fileName = filePath.Split('\\').Last();
                        string outputPath = Path.Combine(outputSubFolder, fileName);
                        File.Copy(filePath, outputPath);
                    }
                }
            });
        }

        private IEnumerable<Tuple<string,string>> Intervals(string path, string bigKey, string smallKey)
        {
            TextGrid tg = new TextGrid(path);
            var dict = tg.MatchInterval(bigKey, smallKey);
            var list = dict.Select(x => new { big = (tg.ItemDict[bigKey][x.Key] as TextGridInterval).Text, small = x.Value.Select(y => (tg.ItemDict[smallKey][y] as TextGridInterval).Text).Aggregate((p, q) => p + " " + q) });
            var newList = list.Select(x => new Tuple<string, string>(Common.CleanupTrans(x.big), Common.CleanupSyl(x.small)));
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

        private void CutByWords()
        {
            string path= @"D:\XiangweiTang\Data\ByWord\TC";
            string tmpPath = @"D:\XiangweiTang\Data\Tmp";
            string outputPath = @"D:\XiangweiTang\Data\ByWord\Wav";
            Parallel.ForEach(Directory.EnumerateFiles(path), new ParallelOptions { MaxDegreeOfParallelism = 10 }, filePath =>
             {
                 Console.WriteLine("Processing " + filePath);
                 var list = File.ReadLines(filePath).Select(x => new PhonLine(x));
                 foreach(var line in list)
                 {
                     if (line.Duration >= 1)
                         continue;
                     string tmpOutputPath = Path.Combine(tmpPath, Guid.NewGuid().ToString() + ".wav");
                     string args = $"{line.SrcAudioPath} {tmpOutputPath} trim {line.StartTime} {line.Duration}";
                     Common.RunFile(Cfg.SoxPath, args);
                     string outputFolder = Path.Combine(outputPath, NormPhon(line.Text));
                     Directory.CreateDirectory(outputFolder);
                     string outputFilePath = Path.Combine(outputFolder, $"{ line.SessionId}_{line.FileName}.wav");
                     Wave.ExtendWave(tmpOutputPath, outputFilePath, 8000);
                 }
             });
        }

        private string NormPhon(string s)
        {
            return NumReg.Replace(s, string.Empty).Replace(" ", "_");
        }

        private void ResetTextGrid()
        {
            string inputPath= @"D:\XiangweiTang\在职毕业设计\自然对话-银行";
            string outputPath = @"D:\XiangweiTang\Data\ByWord\Tc";
            Parallel.ForEach(Directory.EnumerateFiles(inputPath, "*.textgrid"), new ParallelOptions { MaxDegreeOfParallelism = 10 }, filePath =>
                 {
                     Console.WriteLine("Processing " + filePath);
                     string fileName = filePath.Split('\\').Last().Split('.')[0];
                     string audioPath = Path.Combine(inputPath, fileName + ".wav");
                     string outputFilePath = Path.Combine(outputPath, fileName + ".txt");
                     TextGrid tg = new TextGrid(filePath);
                     var outputList = tg.MatchWords().Select((x, y) => y.ToString("000000") + "\t" + fileName + "\t" + x + "\t" + audioPath);
                     File.WriteAllLines(outputFilePath, outputList);
                 });
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
                string outputAudioPath = Path.Combine(phonFolder, $"{tcLine.SessionId}_{tcLine.FileName}.wav");
                string args = string.Join(" ", inputPath, outputAudioPath, "trim", tcLine.StartTime, tcLine.Duration);
                Common.RunFile(Cfg.SoxPath, args);
            }
        }
    }
}
