using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TangInfrastructure
{
    static class StringCleanup
    {
        static Regex SpaceReg = new Regex("[\\s]{2,}", RegexOptions.Compiled);
        static Regex IsoAps = new Regex("(^| )'( |$)", RegexOptions.Compiled);
        static Regex MvAps = new Regex("([a-z]+)' ([a-z]+)", RegexOptions.Compiled);

        public static string CleanupChsString(string chsString)
        {
            string charClean = CleanupChsChar(chsString.ToLower());
            string gbk = BigToGbk(charClean);
            string spaceClean = CleanupSpace(gbk);
            return spaceClean;
        }

        public static string CleanupEnuString(string enuString)
        {
            string charClean = CleanupEnuChar(enuString.ToLower());
            string apoClean = CleanupApos(charClean);
            string spaceClean = CleanupSpace(apoClean);            
            return spaceClean;
        }

        public static string CleanupApos(string inputString)
        {            
            string move = MvAps.Replace(inputString, "$1 '$2");
            string removeIso = IsoAps.Replace(move, " ");
            return removeIso;
        }

        public static string CleanupSpace(string inputString)
        {
            return SpaceReg.Replace(inputString, " ").Trim();
        }

        public static string CleanupChsChar(string chsString)
        {
            return new string(chsString.Where(ValidChs).ToArray());
        }
        
        public static string BigToGbk(string bigString)
        {
            return new string(bigString.Select(x =>Common. BigToGbkDict.ContainsKey(x) ?Common. BigToGbkDict[x] : x).ToArray());
        }

        public static string CleanupEnuChar(string enuString)
        {
            return new string(enuString.Where(ValidEnu).ToArray());
        }

        private static Func<char, bool> ValidChs = x =>
          {
              return (x >= '一' && x <= '龟') || (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9') || x == ' ';
          };

        private static Func<char, bool> ValidEnu = x =>
           {
               return (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9') || x == '\'' || x == ' ';
           };

    }
}
