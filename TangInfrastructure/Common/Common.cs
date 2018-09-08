using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Net;

namespace TangInfrastructure
{
    static class Common
    {
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
    }
}
