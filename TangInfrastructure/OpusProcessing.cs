using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace TangInfrastructure
{
    class OpusProcessing
    {
        public static void MatchFiles(string matchPath, string inputEnPath, string inputZhPath, string outputFolder, string corpusName)
        {
            string outputEnPath = Path.Combine(outputFolder, corpusName + ".en");
            string outputZhPath = Path.Combine(outputFolder, corpusName + ".zh");
            StreamWriter enSw = new StreamWriter(outputEnPath);
            StreamWriter zhSw = new StreamWriter(outputZhPath);
            foreach(var pair in ParseMatchingXml(matchPath, inputEnPath, inputZhPath).SelectMany(x => x))
            {
                enSw.WriteLine(pair.Item1);
                zhSw.WriteLine(pair.Item2);
            }
            enSw.Close();
            zhSw.Close();
        }

        private static IEnumerable<IEnumerable<Tuple<string,string>>> ParseMatchingXml(string matchPath, string enPath, string zhPath)
        {
            XmlDocument matchXDoc = new XmlDocument();
            matchXDoc.Load(matchPath);
            var pairNodes = matchXDoc.SelectNodes("cesAlign/linkGrp");
            for(int i = 0; i < pairNodes.Count; i++)
            {
                var node = pairNodes[i];
                string enGz = Path.Combine(enPath, node.Attributes["fromDoc"].Value);
                string zhGz = Path.Combine(zhPath, node.Attributes["toDoc"].Value);
                string enXml = enGz.Replace(".gz", ".xml");
                string zhXml = zhGz.Replace(".gz", ".xml");
                Common.Decompress(enGz, enXml);
                Common.Decompress(zhGz, zhXml);
                var matches = node.SelectNodes("link").Cast<XmlNode>().Select(x => x.Attributes["xtargets"].Value);
                yield return ExtractTrans(enXml, zhXml, matches);
            }
        }

        private static IEnumerable<Tuple<string,string>> ExtractTrans(string enXml, string zhXml, IEnumerable<string> matches)
        {
            var enDict = XmlToDict(enXml);
            var zhDict = XmlToDict(zhXml);
            foreach(string match in matches)
            {
                string enIndices = match.Split(';')[0].Trim();
                if (string.IsNullOrEmpty(enIndices))
                    continue;
                string zhIndices = match.Split(';')[1].Trim();
                if (string.IsNullOrEmpty(zhIndices))
                    continue;
                string en = string.Join(" ", enIndices.Split(' ').Select(x => enDict[x]));
                string zh = string.Join(" ", zhIndices.Split(' ').Select(x => zhDict[x]));
                yield return new Tuple<string, string>(en, zh);
            }
        }

        private static Dictionary<string,string> XmlToDict(string xmlPath)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(xmlPath);
            return xDoc.SelectNodes("document/s")
                .Cast<XmlNode>()
                .ToDictionary(x => x.Attributes["id"].Value, x => Merge(x));
        }

        private static string Merge(XmlNode sentNode)
        {
            return string.Join(" ", sentNode.SelectNodes("w").Cast<XmlNode>().Select(x => x.InnerText));
        }
    }
}
