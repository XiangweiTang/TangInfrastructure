using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TangInfrastructure
{
    static class PrepareData
    {
        private static List<string> Head = new List<string> { Constants.UNK, Constants.S_START, Constants.S_END };

        private static double TagRatio = 0.3;
        private static string Tag = Constants.ST_TAG;

        private static Random Rand = new Random();

        public static IEnumerable<Tuple<string,string>> ReadPairs(string inputFolder, string srcExt, string tgtExt)
        {
            return ReadPairs(inputFolder, "dev", srcExt, tgtExt)
                .Concat(ReadPairs(inputFolder, "test", srcExt, tgtExt))
                .Concat(ReadPairs(inputFolder, "train", srcExt, tgtExt));
        }
        
        private static IEnumerable<Tuple<string,string>> ReadPairs(string inputFolder, string type, string srcExt, string tgtExt)
        {
            string srcPath = Path.Combine(inputFolder, type + "." + srcExt);
            string tgtPath = Path.Combine(inputFolder, type + "." + tgtExt);
            return Common.ReadPairs(srcPath, tgtPath);
        }

        public static void SplitPairData(IEnumerable<Tuple<string, string>> list, string outputFolder, string srcExt, string tgtExt, int srcMaxVocab, int tgtMaxVocab, int devCount = 0, int testCount = 0, bool useCount = false)
        {
            Directory.CreateDirectory(outputFolder);
            SplitData<Tuple<string, string>> sd = useCount
                ? new SplitData<Tuple<string, string>>(list, devCount, testCount)
                : new SplitData<Tuple<string, string>>(list);
            WritePair(sd.Dev, outputFolder, "dev", srcExt, tgtExt);
            WritePair(sd.Test, outputFolder, "test", srcExt, tgtExt);
            WritePair(sd.Train, outputFolder, "train", srcExt, tgtExt);

            string srcTrainPath = Path.Combine(outputFolder, "train." + srcExt);
            string srcVocabPath = Path.Combine(outputFolder, "vocab." + srcExt);
            CreateVocab(srcTrainPath, srcVocabPath, srcMaxVocab);
            string tgtTrainPath = Path.Combine(outputFolder, "train." + tgtExt);
            string tgtVocabPath = Path.Combine(outputFolder, "vocab." + tgtExt);
            CreateVocab(tgtTrainPath, tgtVocabPath, tgtMaxVocab);
        }
        
        private static Tuple<string, string> WritePair(IEnumerable<Tuple<string,string>> list, string folder, string type, string srcExt, string tgtExt)
        {
            string srcFilePath = Path.Combine(folder, type + "." + srcExt);
            string tgtFilePath = Path.Combine(folder, type + "." + tgtExt);
            Common.WritePairFiles(srcFilePath, tgtFilePath, list);
            return new Tuple<string, string>(srcFilePath, tgtFilePath);
        }

        public static void CreateVocab(string inputPath, string outputVocabpath, int maxVocab, string pattern="*",IEnumerable<string> extraHead=null)
        {
            IEnumerable<string> list;
            if (File.Exists(inputPath))
            {
                list = File.ReadLines(inputPath).SelectMany(x => x.Split(' '));
            }
            else if (Directory.Exists(inputPath))
            {
                list = Directory.GetFiles(inputPath).SelectMany(x => File.ReadLines(x)).SelectMany(x => x.Split(' '));
            }
            else
            {
                throw new TangInfrastructureException("Trans path doesn't exist, no dictionary will be created: " + inputPath);
            }
            if (extraHead != null)
                Head.AddRange(extraHead);

            var vocab = Head.Concat(
                list.GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Where(x => !string.IsNullOrWhiteSpace(x))).Take(maxVocab);

            File.WriteAllLines(outputVocabpath, vocab);
        }
        
        public static void FromTagToClean(string tagFolder, string cleanFolder, string tagExt, string otherExt)
        {
            Directory.CreateDirectory(tagFolder);
            Directory.CreateDirectory(cleanFolder);
            Console.WriteLine("Transfering tag files...");
            Common.FolderTransport(tagFolder, cleanFolder, Common.RemoveTagsFromFile, "*" + tagExt);
            Console.WriteLine("Transfering non-tag files...");
            Common.FolderTransport(tagFolder, cleanFolder, File.Copy, "*" + otherExt);

            string tagVocabPath = Path.Combine(tagFolder, "vocab." + tagExt);
            string cleanVocabPath = Path.Combine(cleanFolder, "vocab." + tagExt);
            File.Copy(tagVocabPath, cleanVocabPath, true);
        }        

        public static void SetTagRatio(string filePath)
        {
            var list = File.ReadLines(filePath).SelectMany(x => x.Split(' '));
            long total = 0;
            long tag = 0;
            foreach(string word in list)
            {
                total++;
                if (word == Constants.ST_TAG)
                    tag++;
            }

            TagRatio = 1.0 * tag / total;
        }

        public static void SetTag(string tag)
        {
            Tag = tag;
        }

        public static void FromCleanToRandomTag(string cleanFolder, string randomTagFolder, string tagExt, string otherExt)
        {
            Directory.CreateDirectory(randomTagFolder);
            Console.WriteLine("Transfering tag files...");
            Common.FolderTransport(cleanFolder, randomTagFolder, RandomAddTag, "*" + tagExt);
            Console.WriteLine("Transfering non-tag files...");
            Common.FolderTransport(cleanFolder, randomTagFolder, File.Copy, "*" + otherExt);

            string cleanVocabPath = Path.Combine(cleanFolder, "vocab." + tagExt);
            string randomTagVocabPath = Path.Combine(randomTagFolder, "vocab." + tagExt);
            File.Copy(cleanVocabPath, randomTagVocabPath, true);
        }

        public static void FromCleanToChaosTag(string cleanFolder, string chaosTagFolder, string tagExt, string otherExt)
        {
            Directory.CreateDirectory(chaosTagFolder);
            Console.WriteLine("Transfering tag files...");
            Common.FolderTransport(cleanFolder, chaosTagFolder, ChaosAddTag, "*" + tagExt);
            Console.WriteLine("Transfering non-tag files...");
            Common.FolderTransport(cleanFolder, chaosTagFolder, File.Copy, "*" + otherExt);
        }

        private static void RandomAddTag(string inputPath, string outputPath)
        {
            var list = File.ReadLines(inputPath).Select(x => RandomAddTag(x));
            File.WriteAllLines(outputPath, list);
        }

        private static string RandomAddTag(string line)
        {
            return string.Join(" ", RandomTagParts(line));
        }

        private static IEnumerable<string> RandomTagParts(string line)
        {
            var split = line.Split(' ');
            foreach(string word in split)
            {
                yield return word;
                if (Rand.NextDouble() < TagRatio)
                    yield return Tag;
            }
        }

        private static void ChaosAddTag(string inputPath, string outputPath)
        {
            var list = File.ReadLines(inputPath).Select(x => ChaosAddTag(x));
            File.WriteAllLines(outputPath, list);
        }

        private static string ChaosAddTag(string line)
        {
            return string.Join(" ", ChaosTagParts(line));
        }

        private static IEnumerable<object> ChaosTagParts(string line)
        {
            string word = "";
            foreach(char c in line)
            {
                if(c==' ')
                {
                    if (!string.IsNullOrWhiteSpace(word))
                    {
                        yield return word;
                        word = "";
                    }
                }
                else
                {
                    word += c;
                }
                if (Rand.NextDouble() <= TagRatio / 2)
                {
                    if (!string.IsNullOrWhiteSpace(word))
                    {
                        yield return word;
                        word = "";
                    }
                    yield return Tag;
                }
            }
            if (!string.IsNullOrWhiteSpace(word))
                yield return word;
        }

        public static Tuple<string, string> CreateAllFiles(string beforeTagPath, string afterTagPath, string nonTagPath, string outputFolder, string tagExt, string otherExt)
        {
            var list = File.ReadLines(beforeTagPath)
                .Zip(File.ReadLines(afterTagPath), (x, y) => StringProcess.MatchTagToString(y, x))
                .Zip(File.ReadLines(nonTagPath), (x, y) => new Tuple<string, string>(x, y))
                .Where(x => !string.IsNullOrWhiteSpace(x.Item1));
            string tagOutputPath = Path.Combine(outputFolder, "all." + tagExt);
            string otherOutputPath = Path.Combine(outputFolder, "all." + otherExt);
            Common.WritePairFiles(tagOutputPath, otherOutputPath, list);
            return new Tuple<string, string>(tagOutputPath, otherOutputPath);
        }


        public static Tuple<string, string> CreateAllFiles(string beforeTagPath, string afterTagPath, string nonTagPath, string outputFolder, string tagExt, string otherExt, string dictPath)
        {
            var dict = File.ReadLines(dictPath).ToDictionary(x => x.Split('\t')[0], x => x.Split('\t')[1]);
            var list = File.ReadLines(beforeTagPath)
                .Zip(File.ReadLines(afterTagPath), (x, y) => StringProcess.MatchTagToString(y, x, dict))
                .Zip(File.ReadLines(nonTagPath), (x, y) => new Tuple<string, string>(x, y))
                .Where(x => !string.IsNullOrWhiteSpace(x.Item1));
            string tagOutputPath = Path.Combine(outputFolder, "all." + tagExt);
            string otherOutputPath = Path.Combine(outputFolder, "all." + otherExt);
            Common.WritePairFiles(tagOutputPath, otherOutputPath, list);
            return new Tuple<string, string>(tagOutputPath, otherOutputPath);
        }

        public static void CreateBatchCommand(string srcLocale, string tgtLocale, string workFolder, int trainSteps)
        {
            string cmd = "python " + Common.CreateTrainArgs(srcLocale, tgtLocale, workFolder, trainSteps);
            string argPath = Path.Combine(workFolder, "args.txt");
            File.WriteAllText(argPath, cmd);
        }
    }
}
