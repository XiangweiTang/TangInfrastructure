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
        public string ParallelDataFolder { get; private set; } = @"D:\XiangweiTang\Data\OpusPair\";
        public string WorkFolder { get; private set; } = @"D:\tmp\Custom_nmt_2013";
        public string SrcLocale { get; private set; } = "zh";
        public int SrcVocabSize { get; private set; } = 5000;
        public string TgtLocale { get; private set; } = "en";
        public int TgtVocabSize { get; private set; } = 5000;
        public string PythonPath { get; private set; } = @"C:\Users\tangx\AppData\Local\Programs\Python\Python36\python.exe";
        public string CmdPath { get; private set; } = string.Empty;
        public string PowerShellPath { get; private set; } = string.Empty;
        public string TaskName { get; private set; } = string.Empty;
        public string SoxPath { get; private set; } = @"C:\Program Files (x86)\sox-14-4-2\sox.exe";
        public string NmtFolder { get; private set; } = @"D:\XiangweiTang\Python\nmt";
        public int TrainSteps { get; private set; } = 12000;
        public IEnumerable<string> UsedData { get; private set; } = Common.ToCollection("OpenSubtitles2013");


        /*
         * python -m nmt.nmt --src=vi --tgt=en --vocab_prefix=D:\tmp\nmt_model\vocab  
         * --train_prefix=D:\tmp\nmt_model\train 
         * --dev_prefix=D:\tmp\nmt_model\tst2012 
         * --test_prefix=D:\tmp\nmt_model\tst2013 
         * --out_dir=D:\tmp\nmt_model 
         * --num_train_steps=12000 --steps_per_stats=100 --num_layers=2 --num_units=128 --dropout=0.2 --metrics=bleu
         */
        public string NmtCommand => $"-m nmt.nmt --src={SrcLocale} --tgt={TgtLocale} --vocab_prefix={WorkFolder}\\vocab "
            + $"--train_prefix={WorkFolder}\\train --dev_prefix={WorkFolder}\\dev --test_prefix={WorkFolder}\\test "
            + $"--out_dir={WorkFolder} --num_train_steps={TrainSteps} --steps_per_stats=100 --num_layers=2 --num_units=128 --dropout=0.2 --metrics=bleu";

        XmlNode CommonNode;
        XmlNode TaskNode;
        XmlDocument XDoc = new XmlDocument();

        public Config() { }
        public void LoadConfig(string configPath)
        {
            XmlReaderSettings settings = new XmlReaderSettings { IgnoreComments = true };
            using (XmlReader xReader = XmlReader.Create(configPath, settings))
            {
                XDoc.Load(xReader);
                TaskName = XDoc.GetXmlAttribute("Root", "TaskName");
                TaskNode = XDoc["Root"][TaskName];
                CommonNode = XDoc["Root"]["Common"];
                LoadCommonNode();
                LoadTaskNode();
            }            
        }

        private void LoadCommonNode()
        {
            PythonPath = CommonNode.GetXmlAttribute("Python", "Path");
            PowerShellPath = CommonNode.GetXmlAttribute("PowerShell", "Path");
            CmdPath = CommonNode.GetXmlAttribute("Cmd", "Path");
        }

        protected virtual void LoadTaskNode() { }
    }
}
