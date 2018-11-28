using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TangInfrastructure
{
    class PrepareTagData
    {
        static Config Cfg = new Config();

        static Random Rand = new Random();

        public static double R = 0;
        
        public PrepareTagData(Config cfg)
        {
            Cfg = cfg;
        }

        public static bool CreateTagData(string inputPath, string outputPath)
        {
            var list = File.ReadLines(inputPath).Select(x => RandomAddTag(x));
            File.WriteAllLines(outputPath, list);
            return true;
        }

        private static string RandomAddTag(string line)
        {
            return string.Join(" ", RandomTag(line));
        }

        private static IEnumerable<string> RandomTag(string line)
        {
            var split = line.Split(' ');
            int n = Math.Max(Convert.ToInt32(R * split.Length), 1);
            var list = split.Select((x, y) => y).Shuffle().Take(n).OrderBy(x => x);
            for(int i = 0; i < split.Length; i++)
            {
                yield return split[i];
                if (list.Contains(i))
                    yield return "<bi>";
            }
        }

        public static void ReorgData(string inputSrcPath, string inputTgtPath, string noTagFolder, string tagFolder, int devCount = -1, int testCount = -1)
        {
            var srcList = File.ReadLines(inputSrcPath);
            var tgtList = File.ReadLines(inputTgtPath);

            var pairList = srcList.Zip(tgtList, (x, y) => new Tuple<string, string>(x, y));

            SplitData<Tuple<string, string>> sd = (devCount < 0 && testCount < 0) ? new SplitData<Tuple<string, string>>(pairList) : new SplitData<Tuple<string, string>>(pairList, devCount, testCount);

            PrintData("dev", tagFolder, sd.Dev);
            PrintData("test", tagFolder, sd.Test);
            var trainPathPair = PrintData("train", tagFolder, sd.Train);
            string srcTrainPath = trainPathPair.Item1;
            string tgtTrainPath = trainPathPair.Item2;

            List<string> head = new List<string> { "<unk>", "<s>", "</s>" };
            string srcVocabPath = Path.Combine(tagFolder, "vocab." + Cfg.SrcLocale);
            string tgtVocabPath = Path.Combine(tagFolder, "vocab." + Cfg.TgtLocale);

            Common.FolderTransport(tagFolder, noTagFolder, ClearTags, "*.zh");

            Common.BuildVocab(srcTrainPath, Cfg.SrcVocabSize, srcVocabPath, head);
            Common.BuildVocab(tgtTrainPath, Cfg.TgtVocabSize, tgtVocabPath, head);            
        }

        private static bool ClearTags(string inputPath, string outputPath)
        {
            var list = File.ReadLines(inputPath).Select(x => StringProcess.CleanupTag(x));
            File.WriteAllLines(outputPath, list);
            return true;
        }

        private static Tuple<string,string> PrintData(string type,string folder,IEnumerable<Tuple<string,string>> list)
        {
            string srcPath = Path.Combine(folder, type + "." + Cfg.SrcLocale);
            string tgtPath = Path.Combine(folder, type + "." + Cfg.TgtLocale);
            Common.WritePairFiles(srcPath, tgtPath, list);
            return new Tuple<string, string>(srcPath, tgtPath);
        }
    }
}
