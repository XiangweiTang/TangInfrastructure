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

        private IEnumerable<Tuple<string,string>> Merge(string folderPath, string type)
        {
            string zhPath = Path.Combine(folderPath, type + ".zh");
            string enPath = Path.Combine(folderPath, type + ".en");
            return File.ReadLines(zhPath).Zip(File.ReadLines(enPath), (x, y) => new Tuple<string, string>(x, y));
        }

        private bool Replace(string tagPath, string noTagPath)
        {
            var list = File.ReadAllLines(tagPath).Select(x => StringProcess.CleanupTag(x));
            File.WriteAllLines(noTagPath, list);
            return true;
        }

        private void ReprintTripleFiles(string zhSrcPath, string zhTgtPath, string enuPath)
        {
            var list = File.ReadLines(zhSrcPath).Zip(File.ReadLines(zhTgtPath), (x, y) => StringProcess.MatchString(y, x)).Zip(File.ReadLines(enuPath), (x, y) => new Tuple<string, string>(x, y))
                .Where(x => !string.IsNullOrWhiteSpace(x.Item1));
            Common.WritePairFiles(@"D:\tmp\DeepMatch\ch.ch", @"D:\tmp\DeepMatch\en.en", list);
        }

        private void CleanupTextGrids()
        {
            var list = File.ReadLines(@"D:\XiangweiTang\Data\Bank\WithBi\FullTags.txt")
                .Select(x => Tags.Replace(x, "<bi>"))
                .Shuffle();
            SplitData<string> split = new SplitData<string>(list, 500, 500);
            Print(split.Dev, "dev");
            Print(split.Test, "test");
            Print(split.Train, "train");
        }

        private void Print(IEnumerable<string> tgt, string type)
        {
            string srcPath = Path.Combine(@"D:\XiangweiTang\Data\Bank\ForTrain", type + ".sr");
            string tgtPath = Path.Combine(@"D:\XiangweiTang\Data\Bank\ForTrain", type + ".tg");
            var src = tgt.Select(x => StringProcess.CleanupSpace(Tags.Replace(x, string.Empty)));
            File.WriteAllLines(srcPath, src);
            File.WriteAllLines(tgtPath, tgt);
        }

        private bool Cleanup(string inputTextGridPath, string outputTextGridPath)
        {
            TextGrid tg = new TextGrid(inputTextGridPath);
            tg.Rebuild(outputTextGridPath);
            return true;
        }

        private IEnumerable<string> Insert(string inputTextGridPath)
        {
            TextGrid tg = new TextGrid(inputTextGridPath);
            return tg.InsertBiToCc();
        }

        private void LoadTextGrid(string path)
        {
            TextGrid tg = new TextGrid(path);
            var list = tg.InsertBiToCc().ToList();
        }
    }
}
