﻿using System;
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
            Init();
            RefreshTextGridWbr(@"D:\tmp\RedoTextGridData\Raw", @"D:\tmp\RedoTextGridData\BiWbr");
        }

        private void Init()
        {
            Directory.CreateDirectory(Cfg.TmpFolder);
        }

        private void RefreshTextGridWbr(string inputFolder, string outputFolder)
        {
            foreach(string cleanPath in Directory.EnumerateFiles(inputFolder, "*.sr"))
            {
                string tagPath = cleanPath.Replace(".sr", ".tg");
                string name = cleanPath.Split('\\').Last().Split('.')[0];
                string outputCleanPath = Path.Combine(outputFolder, name + ".sr");
                string outputTagPath = Path.Combine(outputFolder, name + ".tg");
                RefreshTextGridWbr(cleanPath, tagPath, outputTagPath);
                var list = File.ReadLines(outputTagPath).Select(x => StringProcess.CleanupTag(x));
                File.WriteAllLines(outputCleanPath, list);
            }
        }

        private void RefreshTextGridWbr(string cleanDatapath, string tagDataPath,string outputPath)
        {
            string tmpName = Guid.NewGuid().ToString();
            string noEmptyPath = Path.Combine(Cfg.TmpFolder, tmpName + ".noEmpty");
            string wbrPath = Path.Combine(Cfg.TmpFolder, tmpName + ".wbr");

            var noEmptyList = File.ReadLines(cleanDatapath).Select(x => x.Replace(" ", string.Empty));
            File.WriteAllLines(noEmptyPath, noEmptyList);

            RunWordBreak rwb = new RunWordBreak(Cfg);
            rwb.WordBreak(noEmptyPath, wbrPath);

            var tagList = File.ReadLines(tagDataPath).Select(x => StringProcess.GetTagPrefixIndices(x));
            var wbrList = File.ReadLines(wbrPath);
            var outputList = wbrList.Zip(tagList, (x, y) => StringProcess.InsertTagToWords(x, " <bi> ", y)).Select(x => StringProcess.CleanupSpace(x));
            File.WriteAllLines(outputPath, outputList);
        }

        private void CreateNewData(string cleanDataPath, string tagDataPath, string noEmptyPath,string wbrPath,string outputPath)
        {
            //var noEmptyList = File.ReadLines(cleanDataPath).Select(x => x.Replace(" ", string.Empty));
            //File.WriteAllLines(noEmptyPath, noEmptyList);
            var tagList = File.ReadLines(tagDataPath).Select(x => StringProcess.GetTagPrefixIndices(x));



            //RunWordBreak rwb = new RunWordBreak(Cfg);
            //rwb.WordBreak(noEmptyPath, wbrPath);

            var wbrList = File.ReadLines(wbrPath);
            var outputList = wbrList.Zip(tagList, (x, y) => StringProcess.InsertTagToWords(x, " <bi> ", y)).Select(x => StringProcess.CleanupSpace(x));
            File.WriteAllLines(outputPath, outputList);
        }        


        private void PrepareExpSetFromRawTags(string beforeTagPath, string afterTagPath, string enuPath, string allFolder, string expRootFolder)
        {
            // Suppose we've already had the TAGGed files
            string tagFolder = Path.Combine(expRootFolder, "Tag");
            string cleanFolder = Path.Combine(expRootFolder, "Clean");
            string randomFolder = Path.Combine(expRootFolder, "Random");
            // Create all.zh and all.en, where all files are all valid files.
            var pairs = PrepareData.CreateAllFiles(beforeTagPath, afterTagPath, enuPath, allFolder, "zh", "en");
            var list = Common.ReadPairs(pairs.Item1, pairs.Item2);
            PrepareData.SplitPairData(list, tagFolder, Cfg.SrcLocale, Cfg.TgtLocale, Cfg.SrcVocabSize, Cfg.TgtVocabSize, 5000, 5000, true);
            PrepareData.FromTagToClean(tagFolder, cleanFolder, Cfg.SrcLocale, Cfg.TgtLocale);
            PrepareData.SetTagRatio(pairs.Item1);
            PrepareData.SetTag(Constants.BI_TAG);
            PrepareData.FromCleanToRandomTag(cleanFolder, randomFolder, Cfg.SrcLocale, Cfg.TgtLocale);
            PrepareData.CreateBatchCommand(Cfg.SrcLocale, Cfg.TgtLocale, tagFolder, Cfg.TrainSteps);
            PrepareData.CreateBatchCommand(Cfg.SrcLocale, Cfg.TgtLocale, cleanFolder, Cfg.TrainSteps);
            PrepareData.CreateBatchCommand(Cfg.SrcLocale, Cfg.TgtLocale, randomFolder, Cfg.TrainSteps);
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
