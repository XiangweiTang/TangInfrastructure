using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TangInfrastructure
{
    class PrepareData
    {
        Config Cfg = new Config();
        public PrepareData(Config cfg)
        {
            Cfg = cfg;
        }

        public void PrintData()
        {
            var parallelData = Cfg.UsedData
                .SelectMany(x => GetPairFile(Path.Combine(Cfg.ParallelDataFolder, x))).SelectMany(x => x);
            SplitData<Tuple<string, string>> sd = new SplitData<Tuple<string, string>>(parallelData);
            var dev = sd.Dev;
            var test = sd.Test;
            var train = sd.Train;
            PrintData("dev", dev);
            PrintData("test", test);
            PrintData("train", train, true);
        }

        private void PrintData(string type, IEnumerable<Tuple<string,string>> list, bool createDict=false)
        {
            string srcPath = Path.Combine(Cfg.WorkFolder, type + "." + Cfg.SrcLocale);
            string tgtPath = Path.Combine(Cfg.WorkFolder, type + "." + Cfg.TgtLocale);
            Common.WritePairFiles(srcPath, tgtPath, list.Select(x => CleanupPairs(x)));
            if (createDict)
            {
                string srcDictPath = Path.Combine(Cfg.WorkFolder, "vocab." + Cfg.SrcLocale);
                string tgtDictPath = Path.Combine(Cfg.WorkFolder, "vocab." + Cfg.TgtLocale);
                PrepareDict(srcPath, Cfg.SrcVocabSize, srcDictPath);
                PrepareDict(tgtPath, Cfg.TgtVocabSize, tgtDictPath);
            }
        }

        private Tuple<string,string> CleanupPairs(Tuple<string,string> pair)
        {
            return new Tuple<string, string>(StringCleanup.CleanupChsString(pair.Item1), StringCleanup.CleanupEnuString(pair.Item2));
        }

        private IEnumerable<IEnumerable<Tuple<string, string>>> GetPairFile(string folderPath)
        {
            string pattern = "*." + Cfg.SrcLocale;
            foreach (string srcPath in Directory.EnumerateFiles(folderPath, pattern))
            {
                string tgtPath = srcPath.ToLower().Replace("." + Cfg.SrcLocale.ToLower(), "") + "." + Cfg.TgtLocale;
                if (File.Exists(tgtPath))
                {
                    var srcList = File.ReadLines(srcPath);
                    var tgtList = File.ReadLines(tgtPath);
                    yield return srcList.Zip(tgtList, (x, y) => new Tuple<string, string>(x, y));
                }
            }
        }

        private static void PrepareDict(string filePath, int vocabSize, string outputPath)
        {
            var head = Common.ToCollection("<unk>", "<s>", "</s>");
            var tail = File.ReadLines(filePath).SelectMany(x => x.Split(' '))
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Where(x => !string.IsNullOrWhiteSpace(x));
            
            var list = head.Concat(tail).Take(vocabSize);
            File.WriteAllLines(outputPath, list);
        }
    }
}
