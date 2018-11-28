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
