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
        private static IEnumerable<IEnumerable<Tuple<string,string>>> ParseMatchingXml(string path, string enPath, string zhPath)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(path);
            var pairNodes = xDoc.SelectNodes("cesAlign /linkGrp");
            for (int i = 0; i < pairNodes.Count; i++)
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
            foreach (string match in matches)
            {
                string enIndices = match.Split(';')[0].Trim();
                if (string.IsNullOrEmpty(enIndices))
                    continue;
                string chIndices = match.Split(';')[1].Trim();
                if (string.IsNullOrEmpty(chIndices))
                    continue;
                string zh = string.Join(" ", chIndices.Split(' ').Select(x => zhDict[x]));
                string en = string.Join(" ", enIndices.Split(' ').Select(x => enDict[x]));
                yield return new Tuple<string, string>(zh, en);
            }
        }

        private static Dictionary<string, string> XmlToDict(string xmlPath)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(xmlPath);
            return xDoc.SelectNodes("document/s")
                .Cast<XmlNode>()
                .ToDictionary(x => x.Attributes["id"].Value, x => Merge(x));
        }

        private static string Merge(XmlNode node)
        {
            return string.Join(" ", node.SelectNodes("w").Cast<XmlNode>().Select(x => x.InnerText));
        }

        public static void Decompress(string rootPath)
        {
            Parallel.ForEach(Directory.EnumerateFiles(rootPath, "*.gz", SearchOption.AllDirectories), new ParallelOptions { MaxDegreeOfParallelism = 10 }, gzPath =>
               {
                   Console.WriteLine("Processing " + gzPath);
                   string xmlPath = gzPath.ToLower().Replace(".gz", "");
                   Common.Decompress(gzPath, xmlPath);
               });
        }
    }
}
