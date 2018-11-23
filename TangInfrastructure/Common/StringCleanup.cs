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
        static Regex XReg = new Regex("[x]{1,3}([^a-z]+|$)", RegexOptions.Compiled);
        static Regex SilReg = new Regex("(sil[a-z_]*)|([*]+)|(si_[a-z]*)", RegexOptions.Compiled);
        static Regex TagReg = new Regex("<[^>]*>", RegexOptions.Compiled);
        static Regex SylCleanReg = new Regex("\\?|:|？|：|\\/|\\(|\\)", RegexOptions.Compiled);
        static Regex ExCleanReg = new Regex("[!]+", RegexOptions.Compiled);
        static Regex QueCleanReg = new Regex("[?]+", RegexOptions.Compiled);

        public static string RemoveTag(string s)
        {
            return TagReg.Replace(s, string.Empty);
        }

        public static string TaggingXSil(string inputString)
        {
            string xNorm = XReg.Replace(inputString.ToLower(), "<xx>");
            string silNorm = SilReg.Replace(xNorm, "<sil>");
            return silNorm;
        }

        public static string CleanupSyl(string inputString)
        {
            string sepTag = inputString.Replace(">", "> ").Replace("<", " <");
            string charClean = SylCleanReg.Replace(sepTag, " ");
            string spaceClean = SpaceReg.Replace(charClean, " ").Trim();
            return spaceClean;
        }

        public static string CleanupChsString(string chsString, bool keepTag = false)
        {
            string charClean = keepTag ? CleanupChsCharKeepTag(chsString.ToLower(),ValidChs) : CleanupChsChar(chsString.ToLower());
            string gbk = BigToGbk(charClean);
            string spaceClean = CleanupSpace(gbk);
            return spaceClean;
        }

        public static string CleanupEnuString(string enuString, bool keepTag=false)
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

        public static string CleanupChsCharKeepTag(string inputString, Func<char,bool> validFunc)
        {
            string tagString = TaggingXSil(inputString);
            return new string(FilterCharKeepTag(tagString, validFunc).ToArray());
        }

        private static IEnumerable<char> FilterCharKeepTag(string inputString, Func<char, bool> valid)
        {
            bool inTag = false;
            foreach(char c in inputString)
            {
                if (c == '<')
                    inTag = true;
                if (inTag)
                {
                    yield return c;
                    if (c == '>')
                        inTag = false;
                }
                else
                {
                    if (valid(c))
                        yield return c;
                }
            }
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
              return (x >= '一' && x <= '龟');// || (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9') || x == ' ';
          };
        private static Func<char, bool> ValidEnu = x =>
           {
               return (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9') || x == '\'' || x == ' ';
           };

        public static Func<string, string> CleanupQueEx = x =>
         {
             string queClean = QueCleanReg.Replace(x, " ? ");
             string exClean = ExCleanReg.Replace(queClean, " ! ");
             return exClean;
         };
        public static Func<char, bool> ValidChsOnly = x =>
         {
             return x >= '一' && x <= '龟';
         };
        public static Func<char, bool> ValidLowerEnuOnly = x =>
         {
             return x >= 'a' && x <= 'z';
         };
        public static Func<char, bool> ValidUpperEnuOnly = x =>
         {
             return x >= 'A' && x <= 'Z';
         };
        public static Func<char, bool> ValidNumOnly = x =>
         {
             return x >= '0' && x <= '9';
         };
    }
}
