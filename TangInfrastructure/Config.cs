using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace TangInfrastructure
{
    class Config
    {
        public string OpusDataRootFolder { get; private set; } = @"D:\XiangweiTang\Data\Opus";
        public string ParallelDataFolder { get; private set; } = @"D:\XiangweiTang\Data\OpusPair\";
        public string NmtModelWorkFolder { get; private set; } = @"D:\tmp\Model_NoEnu";
        public string SrcLocale { get; private set; } = "sr";
        public int SrcVocabSize { get; private set; } = 1000;
        public string TgtLocale { get; private set; } = "tg";
        public int TgtVocabSize { get; private set; } = 1000;
        public bool ReverseOnTrain { get; private set; } = false;
        public string PythonPath { get; private set; } = @"C:\Users\tangx\AppData\Local\Programs\Python\Python36\python.exe";
        public string CmdPath { get; private set; } = string.Empty;
        public string PowerShellPath { get; private set; } = string.Empty;        
        public string SoxPath { get; private set; } = @"C:\Program Files (x86)\sox-14-4-2\sox.exe";
        public string NmtFolder { get; private set; } = @"D:\XiangweiTang\Python\nmt";
        public int TrainSteps { get; private set; } = 10000;
        public IEnumerable<string> UsedCorpora { get; private set; } = Common.ToCollection("OpenSubtitles2018", "OpenSubtitles", "OpenSubtitles2011", "OpenSubtitles2013", "OpenSubtitles2016");
        public string TestInputPath { get; private set; } = @"D:\tmp\Custom_nmt_2013\test.zh";
        public string TestOutputPath { get; private set; } = @"D:\tmp\Custom_nmt_2013\test_result.en";

        /*
         * python -m nmt.nmt --src=vi --tgt=en --vocab_prefix=D:\tmp\nmt_model\vocab  
         * --train_prefix=D:\tmp\nmt_model\train 
         * --dev_prefix=D:\tmp\nmt_model\tst2012 
         * --test_prefix=D:\tmp\nmt_model\tst2013 
         * --out_dir=D:\tmp\nmt_model 
         * --num_train_steps=12000 --steps_per_stats=100 --num_layers=2 --num_units=128 --dropout=0.2 --metrics=bleu
         */
        private string TrainSrc => ReverseOnTrain ? TgtLocale : SrcLocale;
        private string TrainTgt => ReverseOnTrain ? SrcLocale : TgtLocale;
        public string TrainNmtCommand => $"-m nmt.nmt --src={TrainSrc} --tgt={TrainTgt} --vocab_prefix={NmtModelWorkFolder}\\vocab "
            + $"--train_prefix={NmtModelWorkFolder}\\train --dev_prefix={NmtModelWorkFolder}\\dev --test_prefix={NmtModelWorkFolder}\\test "
            + $"--out_dir={NmtModelWorkFolder} --num_train_steps={TrainSteps} --steps_per_stats=100 --num_layers=2 --num_units=128 --dropout=0.2 --metrics=bleu";
        public string TestNmtCommand => $"-m nmt.nmt --out_dir={NmtModelWorkFolder} --inference_input_file={TestInputPath} --inference_output_file={TestOutputPath}";
        public string MatchFileName => "matching.txt";

        XmlNode CommonNode;
        XmlDocument XDoc = new XmlDocument();

        public Config() { }
        public void LoadConfig(string configPath)
        {
            XmlReaderSettings settings = new XmlReaderSettings { IgnoreComments = true };
            using (XmlReader xReader = XmlReader.Create(configPath, settings))
            {
                XDoc.Load(xReader);
                CommonNode = XDoc["Root"]["Common"];
                //LoadCommonNode();
                NmtModelWorkFolder = XDoc["Root"]["Nmt"]["WorkFolder"].Attributes["Path"].Value;
            }            
        }

        private void LoadCommonNode()
        {
            PythonPath = CommonNode.GetXmlAttribute("Python", "Path");
            PowerShellPath = CommonNode.GetXmlAttribute("PowerShell", "Path");
            CmdPath = CommonNode.GetXmlAttribute("Cmd", "Path");
        }
    }
}
