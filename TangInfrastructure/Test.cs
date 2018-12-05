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
        Regex Tags = new Regex("<[^>]*>", RegexOptions.Compiled);
        public Test(string[] args)
        {
            string zhWbrPath = @"";
            string enPath = @"";
            string biInferFolder = @"";
            string stInferFolder = @"";
            string biAllFolder = @"";
            string stAllFolder = @"";
            string biExpRootPath = @"";
            string stExpRootPath = @"";
            RunSchedule(zhWbrPath, enPath, biInferFolder, biAllFolder, biExpRootPath, Constants.BI_TAG);
            RunSchedule(zhWbrPath, enPath, stInferFolder, stAllFolder, stExpRootPath, Constants.ST_TAG);
        }

        private void Dedupe(string folder)
        {
            string trainEnPath = Path.Combine(folder, "train.en");
            string trainZhPath = Path.Combine(folder, "train.zh");
            var trainList = Common.ReadPairs(trainZhPath, trainEnPath).ToList();
            string testEnPath = Path.Combine(folder, "test.en");
            string testZhPath = Path.Combine(folder, "test.zh");
            var testList = Common.ReadPairs(testZhPath, testEnPath);
            string devEnPath = Path.Combine(folder, "dev.en");
            string devZhPath = Path.Combine(folder, "dev.zh");
            var devList = Common.ReadPairs(devZhPath, devEnPath);

            int tt = trainList.Intersect(testList).Count();
            int td = trainList.Intersect(devList).Count();
            Console.WriteLine(tt);
            Console.WriteLine(td);
        }

        private void RunInfer(string inputPath, string outputPath, string workFolder)
        {
            Console.WriteLine("Run infer.");
            RunNmt rn = new RunNmt(Cfg);
            Cfg.WorkFolder = workFolder;
            Cfg.TestInputPath = inputPath;
            Cfg.TestOutputPath = outputPath;
            rn.RunDemoTest();
        }
        
        private void RunSchedule(string zhWbrPath, string enPath, string inferWorkFolder, string allFolder,string expRootPath,string tag)
        {
            string inferPath = Path.Combine(allFolder, "all.tag");
            RunInfer(zhWbrPath, inferPath, inferWorkFolder);
            var list = PrepareExpSetFromRawTags(zhWbrPath, inferPath, enPath, allFolder, expRootPath, tag);
            RunNmt rn = new RunNmt(Cfg);
            foreach(string folderPath in list)
            {
                Cfg.WorkFolder = folderPath;
                new RunNmt(Cfg).RunDemoTrain();
            }
        }
        

        private void RunSchedule()
        {
            string srcPath = @"D:\tmp\RawData\all.zh";
            string tgtPath = @"D:\tmp\RawData\all.wbr";
            string enuPath = @"D:\tmp\RawData\all.en";
            string allFolder = @"D:\tmp\StWbrAll";
            string expRootFolder = @"D:\tmp\StWbr";
            //Console.WriteLine("Word break.");
            //RunWordBreak rwb = new RunWordBreak(Cfg);
            //rwb.WordBreak(srcPath, tgtPath);

            Console.WriteLine("Run infer.");
            RunNmt rn = new RunNmt(Cfg);
            Cfg.TestInputPath = @"";
            Cfg.TestOutputPath = @"";
            rn.RunDemoTest();

            Console.WriteLine("Split");
            PrepareExpSetFromRawTags(tgtPath, Cfg.TestOutputPath, enuPath, allFolder, expRootFolder, Constants.BI_TAG);

            Console.WriteLine("Run train");
            Cfg.WorkFolder = Path.Combine(expRootFolder, "Clean");
            rn = new RunNmt(Cfg);
            rn.RunDemoTrain();

            Cfg.WorkFolder = Path.Combine(expRootFolder, "Tag");
            rn = new RunNmt(Cfg);
            rn.RunDemoTrain();

            Cfg.WorkFolder = Path.Combine(expRootFolder, "Random");
            rn = new RunNmt(Cfg);
            rn.RunDemoTrain();
        }

        private void Init()
        {
            Directory.CreateDirectory(Cfg.TmpFolder);
        }

        private void RefreshTextGridWbr(string inputFolder, string outputFolder, string tag)
        {
            foreach(string cleanPath in Directory.EnumerateFiles(inputFolder, " *.sr"))
            {
                string tagPath = cleanPath.Replace(".sr", ".tg");
                string name = cleanPath.Split('\\').Last().Split('.')[0];
                string outputCleanPath = Path.Combine(outputFolder, name + ".sr");
                string outputTagPath = Path.Combine(outputFolder, name + ".tg");
                RefreshTextGridWbr(cleanPath, tagPath, outputTagPath,outputCleanPath, tag);                
            }
        }

        private void RefreshTextGridWbr(string cleanDatapath, string tagDataPath,string outputPath, string wbrPath, string tag)
        {
            string tmpName = Guid.NewGuid().ToString();
            string noEmptyPath = Path.Combine(Cfg.TmpFolder, tmpName + ".noEmpty");

            var noEmptyList = File.ReadLines(cleanDatapath).Select(x => x.Replace(" ", string.Empty));
            File.WriteAllLines(noEmptyPath, noEmptyList);

            RunWordBreak rwb = new RunWordBreak(Cfg);
            rwb.WordBreak(noEmptyPath, wbrPath);

            var tagList = File.ReadLines(tagDataPath).Select(x => StringProcess.GetTagPrefixIndices(x));
            var wbrList = File.ReadLines(wbrPath);
            var outputList = wbrList.Zip(tagList, (x, y) => StringProcess.InsertTagToWords(x, " " + tag + " ", y)).Select(x => StringProcess.CleanupSpace(x));
            File.WriteAllLines(outputPath, outputList);            
        }

        private void CreateNewData(string cleanDataPath, string tagDataPath, string noEmptyPath,string wbrPath,string outputPath)
        {
            var noEmptyList = File.ReadLines(cleanDataPath).Select(x => x.Replace(" ", string.Empty));
            File.WriteAllLines(noEmptyPath, noEmptyList);
            var tagList = File.ReadLines(tagDataPath).Select(x => StringProcess.GetTagPrefixIndices(x));



            //RunWordBreak rwb = new RunWordBreak(Cfg);
            //rwb.WordBreak(noEmptyPath, wbrPath);

            var wbrList = File.ReadLines(wbrPath);
            var outputList = wbrList.Zip(tagList, (x, y) => StringProcess.InsertTagToWords(x, " <bi> ", y)).Select(x => StringProcess.CleanupSpace(x));
            File.WriteAllLines(outputPath, outputList);
        }        


        private IEnumerable<string> PrepareExpSetFromRawTags(string beforeTagPath, string afterTagPath, string enuPath, string allFolder, string expRootFolder, string tag)
        {
            // Suppose we've already had the TAGGed files
            string tagFolder = Path.Combine(expRootFolder, "Tag");
            string cleanFolder = Path.Combine(expRootFolder, "Clean");
            string randomFolder = Path.Combine(expRootFolder, "Random");
            string chaosFolder = Path.Combine(expRootFolder, "Chaos");
            // Create all.zh and all.en, where all files are all valid files.
            var pairs = PrepareData.CreateAllFiles(beforeTagPath, afterTagPath, enuPath, allFolder, "zh", "en");
            var list = Common.ReadPairs(pairs.Item1, pairs.Item2);
            PrepareData.SplitPairData(list, tagFolder, Cfg.SrcLocale, Cfg.TgtLocale, Cfg.SrcVocabSize, Cfg.TgtVocabSize, 5000, 5000, true);
            PrepareData.FromTagToClean(tagFolder, cleanFolder, Cfg.SrcLocale, Cfg.TgtLocale);
            PrepareData.SetTagRatio(pairs.Item1);
            PrepareData.SetTag(tag);
            PrepareData.FromCleanToRandomTag(cleanFolder, randomFolder, Cfg.SrcLocale, Cfg.TgtLocale);
            PrepareData.FromCleanToChaosTag(cleanFolder, chaosFolder, Cfg.SrcLocale, Cfg.TgtLocale);
            //PrepareData.CreateBatchCommand(Cfg.SrcLocale, Cfg.TgtLocale, tagFolder, Cfg.TrainSteps);
            //PrepareData.CreateBatchCommand(Cfg.SrcLocale, Cfg.TgtLocale, cleanFolder, Cfg.TrainSteps);
            //PrepareData.CreateBatchCommand(Cfg.SrcLocale, Cfg.TgtLocale, randomFolder, Cfg.TrainSteps);
            yield return chaosFolder;
            yield return cleanFolder;
            yield return randomFolder;
            yield return tagFolder;
        }

        private void PrepareOpusData()
        {
            Opus opus = new Opus(Cfg);
            Opus.DecompressXmls();
            Opus.XmlToTc();
            Opus.MatchPairFiles();
        }
    }
}
