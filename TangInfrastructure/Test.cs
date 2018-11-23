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
            PrepareData pd = new PrepareData(Cfg);
            //OpusProcessing.Decompress(@"D:\XiangweiTang\Data\OpusXml\OpenSubtitles2016\xml\en");
            OpusProcessing.ExtractOpusToTc(@"D:\XiangweiTang\Data\OpusXml", @"D:\XiangweiTang\Data\OpusTxt", false);
            
            //pd.PrepareOpusData();
            //RunNmt rn = new RunNmt(Cfg);
            //rn.RunDemoTrain();
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
