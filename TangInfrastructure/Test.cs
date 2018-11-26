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
            string inputPath = @"D:\private\Bank\bank\NDYH0002.TextGrid";
            string path = @"D:\private\Bank\bank\NDYH0002_new.TextGrid";
            var tg = new TextGrid(inputPath);
            //tg.Rebuild(path);
            LoadTextGrid(path);
        }

        private void LoadTextGrid(string path)
        {
            TextGrid tg = new TextGrid(path);
            var list = tg.InsertBiToCc().ToList();
        }
    }
}
