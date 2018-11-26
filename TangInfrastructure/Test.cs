using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace TangInfrastructure
{
    class Test
    {
        char[] Sep = { ' ', '/' };
        Config Cfg = new Config();
        Regex NumReg = new Regex("[0-9]+", RegexOptions.Compiled);
        Regex ValidReg = new Regex("^[a-zA-Z_]*$", RegexOptions.Compiled);
        public Test(string[] args)
        {
            string noTagString = "今天 很 高兴 见到 大家";
            string tagString = "明天 很 <tag> 愉快 遇见 你";
            string s = StringProcess.MatchString(tagString, noTagString);
        }

        private void LoadTextGrid(string path)
        {
            TextGrid tg = new TextGrid(path);
            var list = tg.InsertBiToCc().ToList();
        }
    }
}
