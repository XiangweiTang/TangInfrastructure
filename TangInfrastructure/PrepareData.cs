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
            SplitData<Tuple<string, string>> sd = new SplitData<Tuple<string, string>>(parallelData);
            var dev = sd.Dev;
            var test = sd.Test;
            var train = sd.Train;
            PrintData("dev", dev);
            PrintData("test", test);
            PrintData("train", train);
            PrepareDict(Cfg.SrcLocale, Cfg.SrcVocabSize);
            PrepareDict(Cfg.TgtLocale, Cfg.TgtVocabSize);
        }

        private void PrepareDict(string ext, int maxVocab)
        {
            List<string> head = new List<string> { "<unk>", "<s>", "</s>" };
            string inputPath = Path.Combine(Cfg.NmtModelWorkFolder, "train." + ext);
            string outputPath = Path.Combine(Cfg.NmtModelWorkFolder, "vocab." + ext);
            Common.BuildVocab(inputPath, maxVocab, outputPath, head);
        }

        private void PrintData(string type, IEnumerable<Tuple<string,string>> list)
        {
            string srcPath = Path.Combine(Cfg.NmtModelWorkFolder, type + "." + Cfg.SrcLocale);
            string tgtPath = Path.Combine(Cfg.NmtModelWorkFolder, type + "." + Cfg.TgtLocale);
            Common.WritePairFiles(srcPath, tgtPath, list.Select(x => CleanupPairs(x)));
        }

        private Tuple<string,string> CleanupPairs(Tuple<string,string> pair)
        {
            return new Tuple<string, string>(StringProcess.CleanupChsString(pair.Item1), StringProcess.CleanupEnuString(pair.Item2));
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
    }
}
