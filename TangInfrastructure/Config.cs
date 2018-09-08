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
        public string PythonPath { get; private set; } = string.Empty;
        public string CmdPath { get; private set; } = string.Empty;
        public string PowerShellPath { get; private set; } = string.Empty;
        public string TaskName { get; private set; } = string.Empty;

        XmlNode CommonNode;
        XmlNode TaskNode;
        XmlDocument XDoc = new XmlDocument();
        public XmlDocument XSimplifiedDoc = new XmlDocument();

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

        public void GetSimplifiedDoc(string taskNode)
        {
            
        }
    }
}
