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

        public static void ExtractTcLine(string rootPath, string outputRootPath)
        {
            foreach(string corpusFolder in Directory.EnumerateDirectories(rootPath))
            {
                string corpusName = corpusFolder.Split('\\').Last();
                string xmlPath = Path.Combine(corpusFolder, "xml");
                foreach(string localeFolder in Directory.EnumerateDirectories(xmlPath))
                {
                    string locale = localeFolder.Split('\\').Last();
                    var list = new DirectoryInfo(localeFolder).EnumerateFiles("*.xml", SearchOption.AllDirectories);
                    string outputFolderPath = Path.Combine(outputRootPath, corpusName);
                    Directory.CreateDirectory(outputFolderPath);
                    Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 10 }, file =>
                      {
                          Console.WriteLine("Processing " + file.FullName);
                          string sessionId = file.FullName.Replace(localeFolder, string.Empty).Replace(file.Extension, string.Empty).Trim('\\').Replace("\\", "_");
                          string outputFilePath = Path.Combine(outputFolderPath, sessionId + "." + locale);
                          if (!File.Exists(outputFilePath))
                          {
                              try
                              {
                                  var oList = ExtractTcLine(file.FullName, locale, corpusName, sessionId).Select(x => x.Output);
                                  File.WriteAllLines(outputFilePath, oList);
                              }
                              catch { }
                          }
                      });
                }
            }
        }        

        private static IEnumerable<OpusLine> ExtractTcLine(string xmlPath, string locale, string corpusName, string sessionId)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(xmlPath);
            var nodes = xdoc.SelectNodes("document/s");
            for(int i = 0; i < nodes.Count; i++)
            {
                yield return ExtractTcLine(nodes[i], locale, corpusName, sessionId);
            }
        }

        private static OpusLine ExtractTcLine(XmlNode sentenceNode, string locale, string corpusName, string sessionId)
        {
            string internalId = sentenceNode.Attributes["id"].Value;
            OpusLine line = new OpusLine(locale, corpusName, "U", sessionId, internalId, 0, 0, string.Empty);
            try
            {
                var timeNodes = sentenceNode.SelectNodes("time");
                double startTime = Common.TimeStrToSec(timeNodes[0].Attributes["value"].Value);
                double endTime = Common.TimeStrToSec(timeNodes[1].Attributes["value"].Value);
                line.SetStartTime(startTime);
                line.SetEndTime(endTime);
            }
            catch
            {
            }
            line.UpdateTranscript(Merge(sentenceNode));
            return line;
        }


    }
}
