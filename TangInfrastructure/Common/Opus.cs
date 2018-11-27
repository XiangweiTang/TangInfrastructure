using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace TangInfrastructure
{
    class Opus
    {
        static Config Cfg = new Config();
        public Opus(Config cfg)
        {
            Cfg = cfg;
        }        


        #region Match pairs and split.


        public static void MatchPairFiles()
        {
            var list = Cfg.UsedCorpora.SelectMany(x => MatchPairFilesByCorpus(x));
            var split = new SplitData<Tuple<string, string>>(list, 5000, 5000);

            PrintData("dev", split.Dev);
            PrintData("test", split.Test);
            PrintData("train", split.Train, true);
        }

        private static void PrintData(string type, IEnumerable<Tuple<string,string>> list, bool createDict = false)
        {
            string srcPath = Path.Combine(Cfg.NmtModelWorkFolder, type + "." + Cfg.SrcLocale);
            string tgtPath = Path.Combine(Cfg.NmtModelWorkFolder, type + "." + Cfg.TgtLocale);
            Common.WritePairFiles(srcPath, tgtPath, list);
            if (createDict)
            {
                string srcDictPath = Path.Combine(Cfg.NmtModelWorkFolder, "vocab." + Cfg.SrcLocale);
                string tgtDictPath = Path.Combine(Cfg.NmtModelWorkFolder, "vocab." + Cfg.TgtLocale);
                PrepareDict(srcPath, Cfg.SrcVocabSize, srcDictPath);
                PrepareDict(tgtPath, Cfg.TgtVocabSize, tgtDictPath);
            }
        }

        public static void PrepareDict(string inputPath, int vocabSize, string outputPath)
        {
            var head = Common.ToCollection("<unk>", "<s>", "</s>");
            var tail = File.ReadLines(inputPath).SelectMany(x => x.Split(' '))
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Where(Common.ValidEmpty);

            var list = head.Concat(tail).Take(vocabSize);
            File.WriteAllLines(outputPath, list);
        }

        private static IEnumerable<Tuple<string,string>> MatchPairFilesByCorpus(string corpus)
        {
            string matchPath = Path.Combine(Cfg.OpusDataRootFolder, corpus, Cfg.MatchFileName);
            return File.ReadLines(matchPath).SelectMany(x => MatchPairFiles(x));
        }
        private static IEnumerable<Tuple<string,string>> MatchPairFiles(string pathLine)
        {
            string srcFilePath = pathLine.Split('\t')[1];
            string tgtFilePath = pathLine.Split('\t')[3];
            Console.WriteLine("Processing " + srcFilePath);
            var srcList = File.ReadLines(srcFilePath).Select(x => new TcLine(x).Transcription).Select(CleanupEnuString);            
            var tgtList = File.ReadLines(tgtFilePath).Select(x => new TcLine(x).Transcription).Select(CleanupChsString);
            return srcList.Zip(tgtList, (x, y) => new Tuple<string, string>(x, y)).Where(x => ValidChsString(x.Item2)).Where(ValidPair);
        }
        #endregion

        #region Clean string.
        private static Func<string, string> CleanupChsString = x =>
         {
             string charValid = new string(x.ToLower().Where(ValidChs).ToArray());
             //string queExValid = StringCleanup.CleanupQueEx(charValid);
             string spaceValid = StringProcess.CleanupSpace(charValid);
             string gbk = StringProcess.BigToGbk(spaceValid);
             return gbk;
         };

        private static Func<char, bool> ValidChs = x =>
         {
             return StringProcess.ValidChsOnly(x) || StringProcess.ValidLowerEnuOnly(x) || StringProcess.ValidNumOnly(x) || x == ' ';
         };

        private static Func<string, string> CleanupEnuString = x =>
           {
               string aposValid = StringProcess.CleanupApos(x.ToLower());
               string charValid = new string(aposValid.Where(ValidEnu).ToArray());
               //string queExValid = StringCleanup.CleanupQueEx(charValid);
               string spaceValid = StringProcess.CleanupSpace(charValid);               
               return spaceValid;
           };

        private static Func<char, bool> ValidEnu = x =>
         {
             return StringProcess.ValidLowerEnuOnly(x) || StringProcess.ValidNumOnly(x) || x == '\'' || x == ' ';
         };

        private static Func<string, bool> ValidChsString = x =>
         {
             return x.All(y => StringProcess.ValidChsOnly(y) || y == ' ');
         };

        private static Func<Tuple<string, string>, bool> ValidPair = x =>
          {
              int srcLength = x.Item1.Split(' ').Length;
              int tgtLength = x.Item2.Split(' ').Length;
              bool r= Constants.CHS_ENU_RATIO * tgtLength * Constants.LENGTH_RATIO >= srcLength
              && Constants.CHS_ENU_RATIO * tgtLength <= srcLength * Constants.LENGTH_RATIO;
              return r;
          };
        #endregion

        #region Turn to Tc
        public static void XmlToTc()
        {
            foreach(string corpus in Cfg.UsedCorpora)
            {
                string matchingPath = Path.Combine(Cfg.OpusDataRootFolder, corpus, Cfg.MatchFileName);
                string matchXmlPath = Directory.EnumerateFiles(Path.Combine(Cfg.OpusDataRootFolder, corpus, "xml"), "*.xml").Single();
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(matchXmlPath);
                var nodes = xDoc.SelectNodes("cesAlign/linkGrp").Cast<XmlNode>();
                List<string> list = new List<string>();
                Parallel.ForEach(nodes, new ParallelOptions { MaxDegreeOfParallelism = 10 }, grpNode =>
                 {
                     string s = MatchSingleGrp(corpus, grpNode);
                     if (!string.IsNullOrWhiteSpace(s))
                         list.Add(s);
                 });

                File.WriteAllLines(matchingPath, list);
            }
        }

        private static string MatchSingleGrp(string corpus, XmlNode grpNode)
        {
            string fromDocSubPath = grpNode.Attributes["fromDoc"].Value;
            string fromXmlPath = Path.Combine(Cfg.OpusDataRootFolder, corpus, "xml", fromDocSubPath).ToLower().Replace(".gz", string.Empty);
            string fromDocFileName = fromDocSubPath.Split('/').Last().Split('.')[0];

            int startIndex = fromDocSubPath.IndexOf('/');
            int endIndex = fromDocSubPath.LastIndexOf('/');
            string sessionId = fromDocSubPath.Substring(startIndex, endIndex - startIndex).Trim('/');

            string toDocSubPath = grpNode.Attributes["toDoc"].Value;
            string toXmlPath = Path.Combine(Cfg.OpusDataRootFolder, corpus, "xml", toDocSubPath).ToLower().Replace(".gz", string.Empty);
            string toDocFileName = toDocSubPath.Split('/').Last().Split('.')[0];

            string fromTcFolder = Path.Combine(Cfg.OpusDataRootFolder, corpus, "Tc", Cfg.SrcLocale, sessionId);
            string toTcFolder = Path.Combine(Cfg.OpusDataRootFolder, corpus, "Tc", Cfg.TgtLocale, sessionId);
            Directory.CreateDirectory(fromTcFolder);
            Directory.CreateDirectory(toTcFolder);

            Console.WriteLine("Processing " + corpus + " " + sessionId);

            
            string fromTcPath = Path.Combine(fromTcFolder, fromDocFileName + ".txt");
            string toTcPath = Path.Combine(toTcFolder, toDocFileName + ".txt");
            try
            {
                var nodes = grpNode.SelectNodes("link");
                XmlDocument fromDoc = new XmlDocument();
                fromDoc.Load(fromXmlPath);
                XmlDocument toDoc = new XmlDocument();
                toDoc.Load(toXmlPath);

                var list = MatchSingleGrp(nodes, fromDoc, toDoc, corpus, sessionId);
                Common.WritePairFiles(fromTcPath, toTcPath, list);

                return string.Join("\t", fromXmlPath, fromTcPath, toXmlPath, toTcPath);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static IEnumerable<Tuple<string,string>> MatchSingleGrp(XmlNodeList nodes, XmlDocument fromDoc, XmlDocument toDoc, string corpus, string sessionId)
        {
            var fromDict = fromDoc.SelectNodes("document/s").Cast<XmlNode>().ToDictionary(x => x.Attributes["id"].Value, x => x);
            var toDict = toDoc.SelectNodes("document/s").Cast<XmlNode>().ToDictionary(x => x.Attributes["id"].Value, x => x);

            for (int i = 0; i < nodes.Count; i++)
            {
                string pair = nodes[i].Attributes["xtargets"].Value;
                TcLine fromLine = new TcLine();
                TcLine toLine = new TcLine();
                try
                {
                    var fromIds = pair.Split(';')[0].Split(' ');
                    var toIds = pair.Split(';')[1].Split(' ');
                    if (fromIds.Length > 0 && toIds.Length > 0)
                    {
                        var fromSegNodes = fromIds.Select(x => fromDict[x]);
                        var toSegNodes = toIds.Select(x => toDict[x]);
                        fromLine = CreateTcFromXml(fromSegNodes, Cfg.SrcLocale, corpus, sessionId, i.ToString("000000"));
                        toLine = CreateTcFromXml(toSegNodes, Cfg.TgtLocale, corpus, sessionId, i.ToString("000000"));
                    }
                }
                catch { }
                if (!string.IsNullOrWhiteSpace(fromLine.Transcription) && !string.IsNullOrWhiteSpace(toLine.Transcription))
                    yield return new Tuple<string, string>(fromLine.Output, toLine.Output);
            }
        }

        private static TcLine CreateTcFromXml(IEnumerable< XmlNode> segNodes, string locale, string corpus, string sessionId, string internalId)
        {
            var list = segNodes.SelectMany(x => x.SelectNodes("w").Cast<XmlNode>().Select(y => y.InnerText));
            string trans = StringProcess.CleanupSpace(string.Join(" ", list));
            double startTime = 0;
            double endTime = 0;
            if (segNodes.Where(x => x["time"] != null).Count()>1)
            {
                string startStr = string.Empty;
                string endStr = string.Empty;
                try
                {
                    var startNode = segNodes.First(x => x["time"] != null);
                    startStr = startNode["time"].Attributes["value"].Value;
                    startTime = Common.TimeStrToSec(startStr);
                }
                catch { }

                try
                {
                    var endNode = segNodes.Last(x => x["time"] != null);
                    endStr = endNode["time"].Attributes["value"].Value;
                    endTime = Common.TimeStrToSec(endStr);
                }
                catch { }
            }
            return new TcLine(corpus, "U", sessionId, internalId, startTime, endTime, trans, "<NA/>");
        }
        #endregion

        #region Decompress        
        public static void DecompressXmls(params string[] userSetUsedCorpora)
        {
            var usedCorpora = userSetUsedCorpora.Length == 0 ? Cfg.UsedCorpora : userSetUsedCorpora;
            foreach(string usedCorpus in usedCorpora)
            {
                string corpusPath = Path.Combine(Cfg.OpusDataRootFolder, usedCorpus, "xml");
                Parallel.ForEach(Directory.EnumerateFiles(corpusPath, "*.gz", SearchOption.AllDirectories), new ParallelOptions { MaxDegreeOfParallelism = 10 }, inputGzPath =>
                   {
                       DecompressXml(inputGzPath);
                   });
            }
        }
        private static void DecompressXml(string inputGzPath)
        {
            string outputXmlPath = inputGzPath.ToLower().Replace(".gz", string.Empty);
            Common.Decompress(inputGzPath, outputXmlPath);
        }
        #endregion
    }
}
