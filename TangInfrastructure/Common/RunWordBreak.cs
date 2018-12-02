using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TangInfrastructure
{
    class RunWordBreak
    {
        static Config Cfg = new Config();
        public RunWordBreak(Config cfg)
        {
            Cfg = cfg;
        }

        public void WordBreak(string inputPath, string outputPath)
        {
            string intermediaPath = Path.Combine(Cfg.TmpFolder, Guid.NewGuid().ToString() + ".txt");
            ToWbr(inputPath, intermediaPath);
            CleanupWbr(intermediaPath, outputPath);
        }

        private void ToWbr(string inputPath, string wbrPath)
        {
            string args = Cfg.WordBreakPython + " " + inputPath + " " + wbrPath;
            Common.RunFile(Cfg.PythonPath, args);
        }

        private void CleanupWbr(string wbrPath, string outputPath)
        {
            var list = File.ReadLines(wbrPath).Select(x => StringProcess.CleanupSpace(x));
            File.WriteAllLines(outputPath, list);
        }        
    }
}
