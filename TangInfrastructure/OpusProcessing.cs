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

        public static void ProcessMatchGroups(string matchXmlPath,string rootInputFolder, string rootOutputFolder, string corpus, string fromLocale, string toLocale)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(matchXmlPath);
            var grpNodes = xdoc.SelectNodes("cesAlign/linkGrp");
            string fromFolder = Path.Combine(rootOutputFolder, corpus, fromLocale);
            Directory.CreateDirectory(fromFolder);
            string toFolder = Path.Combine(rootOutputFolder, corpus, toLocale);
            Directory.CreateDirectory(toFolder);
            for(int i = 0; i < grpNodes.Count; i++)
            {
                var list = ProcessMatchGroup(grpNodes[i], rootInputFolder);
                string fileName = Guid.NewGuid().ToString();
                string fromPath = Path.Combine(fromFolder, fileName + ".txt");
                string toPath = Path.Combine(toFolder, fileName + ".txt");
                Common.WritePairFiles(fromPath, toPath, list);
            }
        }

        private static IEnumerable<Tuple<string,string>> ProcessMatchGroup(XmlNode grpNode, string rootInputFolder)
        {
            string fromPath = RecoverPath(grpNode.Attributes["fromDoc"].Value, rootInputFolder);            
            string toPath = RecoverPath(grpNode.Attributes["toDoc"].Value, rootInputFolder);
            if (File.Exists(fromPath) && File.Exists(toPath))
            {
                Console.WriteLine("Processing " + fromPath);
                var fromDict = Common.GetLines(fromPath, "opus").ToDictionary(x => x.InternalId, x => x.Transcription);
                var toDict = Common.GetLines(toPath, "opus").ToDictionary(x => x.InternalId, x => x.Transcription);

                var linkNodes = grpNode.SelectNodes("link");
                for (int i = 0; i < linkNodes.Count; i++)
                {
                    var pair = GetMatchedPair(linkNodes[i], fromDict, toDict);
                    if (!string.IsNullOrWhiteSpace(pair.Item1) && !string.IsNullOrWhiteSpace(pair.Item2))
                        yield return pair;
                }
            }
        }
        
        private static Tuple<string,string> GetMatchedPair(XmlNode linkNode,Dictionary<string,string> fromDict, Dictionary<string,string> toDict)
        {
            string pairStr = linkNode.Attributes["xtargets"].Value;
            try
            {
                var fromIndices = pairStr.Split(';')[0].Split(' ');
                string fromStr = string.Join(" ", fromIndices.Select(x => fromDict[x]));

                var toIndices = pairStr.Split(';')[1].Split(' ');
                string toStr = string.Join(" ", toIndices.Select(x => toDict[x]));

                return new Tuple<string, string>(fromStr, toStr);
            }
            catch
            {
                return new Tuple<string, string>(string.Empty, string.Empty);
            }
        }

        private static string RecoverPath(string xmlValue, string rootFolder)
        {
            var split = xmlValue.ToLower().Replace(".xml.gz", ".txt").Split('/');
            string locale = split[0];
            string subPath = string.Join("_", split.Skip(1));
            return Path.Combine(rootFolder, locale, subPath);
        }

        public static void ExtractOpusToTc(string rootPath, string outputRootPath, bool overwrite)
        {
            foreach(string corpusFolder in Directory.EnumerateDirectories(rootPath))
            {
                string corpusName = corpusFolder.Split('\\').Last();
                string xmlPath = Path.Combine(corpusFolder, "xml");
                foreach(string localeFolder in Directory.EnumerateDirectories(xmlPath))
                {
                    string locale = localeFolder.Split('\\').Last();
                    var list = new DirectoryInfo(localeFolder).EnumerateFiles("*.xml", SearchOption.AllDirectories);
                    string outputFolderPath = Path.Combine(outputRootPath, corpusName, locale);
                    Directory.CreateDirectory(outputFolderPath);
                    Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = 10 }, file =>
                      {
                          Console.WriteLine("Processing " + file.FullName);
                          string sessionId = file.FullName.Replace(localeFolder, string.Empty).Replace(file.Extension, string.Empty).Trim('\\').Replace("\\", "_");
                          string outputFilePath = Path.Combine(outputFolderPath, $"{sessionId}.txt");
                          if (!File.Exists(outputFilePath) || overwrite)
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
            var timeNodes = sentenceNode.SelectNodes("time");
            try
            {
                double startTime = Common.TimeStrToSec(timeNodes[0].Attributes["value"].Value);
                line.SetStartTime(startTime);
            }
            catch { }

            try
            {
                double endTime = Common.TimeStrToSec(timeNodes[1].Attributes["value"].Value);
                line.SetEndTime(endTime);
            }
            catch { }
            line.UpdateTranscript(Merge(sentenceNode));
            return line;
        }

        private static string Merge(XmlNode node)
        {
            return string.Join(" ", node.SelectNodes("w").Cast<XmlNode>().Select(x => x.InnerText));
        }

        public static void Decompress(string rootFolder)
        {
            Parallel.ForEach(Directory.EnumerateFiles(rootFolder, "*.gz", SearchOption.AllDirectories), new ParallelOptions { MaxDegreeOfParallelism = 10 }, gzPath =>
               {
                   string xmlPath = gzPath.Replace(".gz", "");
                   Common.Decompress(gzPath, xmlPath);
               });
        }
    }
}
