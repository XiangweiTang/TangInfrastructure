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

        public void PrepareOpusData()
        {
            var parallelData = Cfg.UsedCorpora
                .SelectMany(x => GetPairFile(Path.Combine(Cfg.ParallelDataFolder, x))).SelectMany(x => x);
            SplitData<Tuple<string, string>> sd = new SplitData<Tuple<string, string>>(parallelData, new PairEquality());
            var dev = sd.Dev;
            var test = sd.Test;
            var train = sd.Train;
            PrintData("dev", dev);
            PrintData("test", test);
            PrintData("train", train, true);
        }

        private void PrintData(string type, IEnumerable<Tuple<string,string>> list, bool createDict=false)
        {
            string srcPath = Path.Combine(Cfg.NmtModelWorkFolder, type + "." + Cfg.SrcLocale);
            string tgtPath = Path.Combine(Cfg.NmtModelWorkFolder, type + "." + Cfg.TgtLocale);
            Common.WritePairFiles(srcPath, tgtPath, list.Select(x => CleanupPairs(x)));
            if (createDict)
            {
                string srcDictPath = Path.Combine(Cfg.NmtModelWorkFolder, "vocab." + Cfg.SrcLocale);
                string tgtDictPath = Path.Combine(Cfg.NmtModelWorkFolder, "vocab." + Cfg.TgtLocale);
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
            string srcFolder = Path.Combine(folderPath, Cfg.SrcLocale);
            string tgtFolder = Path.Combine(folderPath, Cfg.TgtLocale);
            foreach(string srcFilePath in Directory.EnumerateFiles(srcFolder))
            {
                string fileName = srcFilePath.Split('\\').Last();
                string tgtFilePath = Path.Combine(tgtFolder, fileName);
                var srcList = File.ReadLines(srcFilePath);
                var tgtList = File.ReadLines(tgtFilePath);
                yield return srcList.Zip(tgtList, (x, y) => new Tuple<string, string>(x, y));
            }
        }

        private static void PrepareDict(string filePath, int vocabSize, string outputPath)
        {
            var head = Common.ToCollection("<unk>", "<s>", "</s>");
            var tail = File.ReadLines(filePath).SelectMany(x => x.Split(' '))
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count()).ToList();
                //.Select(x => x.Key)
                //.Where(x => !string.IsNullOrWhiteSpace(x));

            //var list = head.Concat(tail).Take(vocabSize).ToList();
            //File.WriteAllLines(outputPath, list);
        }

        
    }
    class PairEquality : IEqualityComparer<Tuple<string, string>>
    {
        public bool Equals(Tuple<string, string> x, Tuple<string, string> y)
        {
            return x.Item1 == y.Item1 && x.Item2 == y.Item2;
        }

        public int GetHashCode(Tuple<string, string> t)
        {
            return t.Item1.GetHashCode() ^ t.Item2.GetHashCode();
        }
    }
}
