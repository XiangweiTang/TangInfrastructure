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
            string name = "59dcb025-8c06-44ad-8d92-087e8219237a";
            string noSpacePath = Path.Combine(Cfg.TmpFolder, name + ".noSpace");
            string wbrPath = Path.Combine(Cfg.TmpFolder, name + ".wbr");
            //RemoveSpac(inputPath, noSpacePath);
            //ToWbr(noSpacePath, wbrPath);
            CleanupWbr(wbrPath, outputPath);
        }

        private void RemoveSpac(string spacePath, string noSpacePath)
        {
            var list = File.ReadLines(spacePath).Select(x => x.Replace(" ", string.Empty));
            File.WriteAllLines(noSpacePath, list);
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
