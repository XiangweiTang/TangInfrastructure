using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Net;
using System.IO.Compression;

namespace TangInfrastructure
{
    static class Common
    {
        static char[] Sep = { ' ', '/' };
        public static Random R = new Random();
        static Common()
        {
            SetDict();
        }
        public static byte[] ReadBytes(string path, int n)
        {
            using(FileStream fs=new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using(BinaryReader br=new BinaryReader(fs))
                {
                    return br.ReadBytes(n);
                }
            }
        }

        public static string GetXmlAttribute(this XmlNode node, params string[] nameList)
        {
            Sanity.Requires(node != null, "The base node is null.");
            Sanity.Requires(nameList.Length > 1, "The name list has to be longer than 1.");
            XmlNode currentNode = node;
            foreach(string name in nameList.Take(nameList.Length - 1))
            {
                currentNode = currentNode[name];
                Sanity.Requires(currentNode != null, $"The node {name} does not exist.");
            }
            return currentNode[nameList.Last()].Value;
        }

        public static IEnumerable<string> ReadEmbed(string path)
        {
            Assembly asmb = Assembly.GetExecutingAssembly();
            using(StreamReader sr=new StreamReader(asmb.GetManifestResourceStream(path)))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    yield return line;
            }
        }

        public static string ReadEmbedAll(string path)
        {
            Assembly asmb = Assembly.GetExecutingAssembly();
            using(StreamReader sr=new StreamReader(asmb.GetManifestResourceStream(path)))
            {
                return sr.ReadToEnd();
            }
        }

        public static IEnumerable<string> ReadPage(string uri)
        {
            HttpWebRequest req = WebRequest.CreateHttp(uri);
            using(HttpWebResponse resp=(HttpWebResponse)req.GetResponse())
            {
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        yield return line;
                }
            }
        }

        public static string ReadPageAll(string uri)
        {
            WebClient client = new WebClient();
            return client.DownloadString(uri);
        }

        public static void WritePairFiles(string outputPath1, string outputPath2, IEnumerable<Tuple<string, string>> outputPairList)
        {
            StreamWriter sw1 = new StreamWriter(outputPath1);
            StreamWriter sw2 = new StreamWriter(outputPath2);
            foreach(var pair in outputPairList)
            {
                sw1.WriteLine(pair.Item1);
                sw2.WriteLine(pair.Item2);
            }
            sw1.Close();
            sw2.Close();
        }

        public static void DownloadFile(string uri, string path)
        {
            WebClient client = new WebClient();
            client.DownloadFile(uri, path);
        }

        public static T[] Shuffle<T>(this IEnumerable<T> collection)
        {
            var array = collection.ToArray();
            for(int i = 0; i < array.Length; i++)
            {
                int j = R.Next(array.Length);
                T t = array[i];
                array[i] = array[j];
                array[j] = t;
            }
            return array;
        }                

        public static T[] ArrayTake<T>(this T[] inputArray, int take)
        {
            take = Math.Min(take, inputArray.Length);
            T[] array = new T[take];
            Array.Copy(inputArray, array, take);
            return array;
        }

        public static T[] ArraySkip<T>(this T[] inputArray, int skip)
        {
            skip = Math.Min(inputArray.Length, skip);
            T[] array = new T[inputArray.Length - skip];
            Array.Copy(inputArray, skip, array, 0, inputArray.Length - skip);
            return array;
        }

        public static T[] ArrayRange<T>(this T[] inputArray, int skip, int take)
        {
            skip = Math.Min(inputArray.Length, skip);
            take = Math.Min(take, Math.Max(inputArray.Length - skip, 0));
            T[] array = new T[take];
            Array.Copy(inputArray, skip, array, 0, take);
            return array;
        }

        public static T[] ArrayConcat<T>(this T[] head, T[] tail)
        {
            T[] array = new T[head.Length + tail.Length];
            array.ArrayPlace(head, 0);
            array.ArrayPlace(tail, head.Length);
            return array;
        }

        public static string CleanUp(this string s)
        {
            return new string(s.Where(x => x <= '龟' && x >= '一').ToArray());
        }

        public static void RunFile(string filePath, string args, string workDir = "", bool newFolder = false)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = filePath;
            proc.StartInfo.Arguments = args;
            if (!string.IsNullOrWhiteSpace(workDir))
                proc.StartInfo.WorkingDirectory = workDir;
            proc.StartInfo.UseShellExecute = newFolder;
            proc.Start();
            proc.WaitForExit();
        }
        
        public static void ArrayPlace<T>(this T[] bigArray, T[] smallArray, int index)
        {
            Sanity.Requires(index >= 0, "Index cannot be less than 0.");
            Sanity.Requires(smallArray.Length + index <= bigArray.Length, "Index out of range.");
            Array.Copy(smallArray, 0, bigArray, index, smallArray.Length);
        }

        public static void Decompress(string inputFilepath, string outputFilePath, bool overwrite=true)
        {
            if (File.Exists(outputFilePath))
            {
                if (overwrite)
                    File.Delete(outputFilePath);
                else
                    return;
            }
            bool exceptionFlag = false;
            try
            {
                FileInfo inputFile = new FileInfo(inputFilepath);
                using (FileStream inputFs = inputFile.OpenRead())
                {
                    using (FileStream outputFs = File.Create(outputFilePath))
                    {
                        using (GZipStream decompressStream = new GZipStream(inputFs, CompressionMode.Decompress))
                        {
                            decompressStream.CopyTo(outputFs);
                        }
                    }
                }
            }
            catch
            {
                exceptionFlag = true;
            }
            finally
            {
                Console.WriteLine(outputFilePath + "\tDecompressed.");
                if (File.Exists(outputFilePath) && exceptionFlag)
                    File.Delete(outputFilePath);
            }
        }

        public static double TimeStrToSec(string timeStr)
        {
            var split = timeStr.Split(':');
            string secStr = split.Last().Replace(',', '.');
            double second = double.Parse(secStr);
            if(split.Length>1)
            {
                string minStr = split[split.Length - 2];
                second += 60 * int.Parse(minStr);
            }
            if (split.Length > 2)
            {
                string hrStr = split[split.Length - 3];
                second += 3600 * int.Parse(hrStr);
            }
            return second;
        }

        public static IEnumerable<TcLine> ResetTimeStamp(IEnumerable<TcLine> list)
        {
            double lastEnd = 0;           
            foreach(var line in list)
            {
                if (line.StartTime < lastEnd)
                {
                    line.SetStartTime(lastEnd);
                    lastEnd = line.EndTime;
                }
                yield return line;
            }
        }
        
        public static IEnumerable<Line> GetLines(string path, string type)
        {
            var list = File.ReadLines(path);
            switch (type.ToLower())
            {
                case "tc":
                    return list.Select(x => new TcLine(x));
                case "opus":
                    return list.Select(x => new OpusLine(x));
                default:
                    throw new TangInfrastructureException("Invalid line type " + type);
            }
        }

        public static T[] ToCollection<T>(params T[] items)
        {
            return items;
        }

        public static bool SequentialContains<T>(IEnumerable<T> bigCollection, IEnumerable<T> smallCollection)
        {
            var smallArray = smallCollection.ToArray();
            int smallIndex = 0;
            foreach(T bigT in bigCollection)
            {
                if (smallIndex >= smallArray.Length)
                    return true;
                for(int i = smallIndex; i < smallArray.Length; i++)
                {
                    if (bigT.Equals(smallArray[smallIndex]))
                    {
                        smallIndex = i + 1;
                        break;
                    }
                }
            }
            return smallIndex >= smallArray.Length;
        }

        public static IEnumerable<int> SequentialMatch<T>(IEnumerable<T> bigCollection, IEnumerable<T> smallCollection)
        {
            var smallArray = smallCollection.Reverse().ToArray();
            int smallIndex = 0;
            int bigIndex = 0;
            foreach (T bigT in bigCollection.Reverse())
            {                
                if (smallIndex >= smallArray.Length)
                    break;
                for (int i = smallIndex; i < smallArray.Length; i++)
                {
                    if (bigT.Equals(smallArray[smallIndex]))
                    {
                        yield return bigIndex;
                        smallIndex = i + 1;
                        break;
                    }
                }
                bigIndex++;
            }
        }

        public delegate void FileTransport(string inputPath, string outputPath);
        public static void FolderTransport(string inputFolderPath, string outputFolderPath, FileTransport fileTransport, string pattern="*")
        {
            Parallel.ForEach(Directory.EnumerateFiles(inputFolderPath, pattern), new ParallelOptions { MaxDegreeOfParallelism = 10 }, inputFilePath =>
            {
                Console.WriteLine("Processing " + inputFilePath);
                string fileName = inputFilePath.Split('\\').Last();
                string outputFilePath = Path.Combine(outputFolderPath, fileName);
                fileTransport(inputFilePath, outputFilePath);
            });
        }

        public static Dictionary<char, char> BigToGbkDict = new Dictionary<char, char>();
        private static void SetDict()
        {
            BigToGbkDict = ReadEmbed($"{Constants.PROJECT_NAME}.Data.GBK_BIG.txt")
                 .ToDictionary(x => x[2], x => x[0]);
        }


        public static void PrintData(IEnumerable<Tuple<string,string>> sntPair, string folder, string type, string srcPattern, string tgtPattern)
        {
            string srcPath = Path.Combine(folder, type + srcPattern);
            string tgtPath = Path.Combine(folder, type + tgtPattern);
            WritePairFiles(srcPath, tgtPath, sntPair);
        }
        
        public static void BuildVocab(string inputPath, int maxVocab, string outputPath, IEnumerable<string> head, string pattern="*")
        {
            IEnumerable<string> list;
            if (File.Exists(inputPath))
            {
                list = File.ReadLines(inputPath).SelectMany(x => x.Split(' '));
            }
            else if (Directory.Exists(inputPath))
            {
                list = Directory.EnumerateFiles(inputPath, pattern).SelectMany(x => File.ReadLines(x)).SelectMany(x => x.Split(' '));
            }
            else
            {
                throw new TangInfrastructureException($"The path {inputPath} doesn't exist!");
            }
            var vocab = head.Concat(list.GroupBy(x => x).OrderByDescending(x => x.Count()).Select(x => x.Key)).Where(x=>!string.IsNullOrWhiteSpace(x)).Take(maxVocab);
            File.WriteAllLines(outputPath, vocab);
        }

        public static IEnumerable<Tuple<string,string>> ReadPairs(string srcPath, string tgtPath)
        {
            var srcList = File.ReadLines(srcPath);
            var tgtList = File.ReadLines(tgtPath);
            return srcList.Zip(tgtList, (x, y) => new Tuple<string, string>(x, y));
        }

        public static void RemoveTagsFromFile(string tagPath, string cleanPath)
        {
            var list = File.ReadLines(tagPath).Select(x => StringProcess.CleanupTag(x));
            File.WriteAllLines(cleanPath, list);
        }

        public static string CreateTrainArgs(string srcLocale, string tgtLocale, string workFolder, int trainSteps)
        {
            return $"-m nmt.nmt --src={srcLocale} --tgt={tgtLocale} --vocab_prefix={workFolder}\\vocab "
             + $"--train_prefix={workFolder}\\train --dev_prefix={workFolder}\\dev --test_prefix={workFolder}\\test "
             + $"--out_dir={workFolder} --num_train_steps={trainSteps} --steps_per_stats=100 --num_layers=2 --num_units=128 --dropout=0.2 --metrics=bleu";
        }

        public static string CreateTestArgs(string workFolder, string inputPath, string outputPath)
        {
            return $"-m nmt.nmt --out_dir={workFolder} --inference_input_file={inputPath} --inference_output_file={outputPath}";
        }
    }
}
