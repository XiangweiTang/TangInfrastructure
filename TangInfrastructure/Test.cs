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
            //var list = Directory.EnumerateFiles(@"D:\XiangweiTang\Data\Bank\Cleanup").SelectMany(x => new TextGrid(x).CcList.Select(y => StringCleanup.RemoveTag(y.Text))).SelectMany(x => x.Where(y => y > '龟' || y < '一')).Distinct().ToList();
            string inputPath = @"D:\XiangweiTang\Data\Bank\Raw";
            string outputPath = @"D:\XiangweiTang\Data\Bank\Cleanup";
            //Common.FolderTransport(inputPath, outputPath, RebuildTextGrid, "*.textgrid");
            var list = Directory.EnumerateFiles(@"D:\XiangweiTang\Data\Bank\Cleanup")
                .SelectMany(x => new TextGrid(x).SylList.Select(y => new { text = y.Text, id = x })).SelectMany(x => x.text.Split(' ').Select(y => new { path = x.id, value = y }))
                .Where(x => x.value.Length == 1).ToList();
        }

        private bool RebuildTextGrid(string inputPath, string outputPath)
        {
            try
            {
                TextGrid tg = new TextGrid(inputPath);
                tg.Rebuild(outputPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
