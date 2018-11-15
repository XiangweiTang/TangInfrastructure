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

        public static string CleanupSyl(string s)
        {
            return string.Join(" ", s.Split(Sep, StringSplitOptions.RemoveEmptyEntries).Where(x => !x.Contains("sil") && x != "xx" && x != "x" && x != "xxx"));
        }

        public static string CleanupTrans(string s)
        {
            return new string(s.Where(x => x >= '一' && x <= '龟').ToArray());
        }
        public static IEnumerable<object> SplitWords(string s)
        {
            if (s.Contains('儿'))
            {
                int index = s.IndexOf('儿');
                int pre = index - 1;
                for (int i = 0; i < pre; i++)
                    yield return s[i];
                yield return s.Substring(pre, 2);
                for (int i = index + 1; i < s.Length; i++)
                {
                    yield return s[i];
                }
            }
            else
                foreach (char c in s)
                    yield return c;
        }

        public static void ArrayPlace<T>(this T[] bigArray, T[] smallArray, int index)
        {
            Sanity.Requires(index >= 0, "Index cannot be less than 0.");
            Sanity.Requires(smallArray.Length + index <= bigArray.Length, "Index out of range.");
            Array.Copy(smallArray, 0, bigArray, index, smallArray.Length);
        }

        public static void Decompress(string inputFilepath, string outputFilePath)
        {
            FileInfo inputFile = new FileInfo(inputFilepath);
            using(FileStream inputFs = inputFile.OpenRead())
            {
                using(FileStream outputFs = File.Create(outputFilePath))
                {
                    using(GZipStream decompressStream=new GZipStream(inputFs, CompressionMode.Decompress))
                    {
                        decompressStream.CopyTo(outputFs);                        
                    }
                }
            }
        }
    }
}
