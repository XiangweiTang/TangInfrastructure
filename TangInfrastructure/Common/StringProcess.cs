using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TangInfrastructure
{
    static class StringProcess
    {
        static Regex OverlapReg = new Regex("\\[[^]]*OV[^]]*]", RegexOptions.Compiled);
        static Regex SpaceReg = new Regex("[\\s]{2,}", RegexOptions.Compiled);
        static Regex IsoAps = new Regex("(^| )'( |$)", RegexOptions.Compiled);
        static Regex MoveAps = new Regex("([a-z]+)' ([a-z]+)", RegexOptions.Compiled);
        static Regex XReg = new Regex("[x]{2,3}|\\bx\\b", RegexOptions.Compiled);
        static Regex SilReg = new Regex("(sil[a-z_]*)|([*]+)|(si_[a-z]*)", RegexOptions.Compiled);
        static Regex ContainsTagReg = new Regex("<[^>]*>", RegexOptions.Compiled);
        static Regex OnlyTagReg = new Regex("^\\s*<[^>]*>\\s*$", RegexOptions.Compiled);
        static char[] Sep = { ' ' };

        public static string CleanupTag(string s)
        {
            string noTag = ContainsTagReg.Replace(s, string.Empty);
            string noSpace = CleanupSpace(noTag);
            return noSpace;
        }

        public static string NormXSil(string inputString)
        {
            string xNorm = XReg.Replace(inputString.ToLower(), " <xx> ");
            string silNorm = SilReg.Replace(xNorm, " <sil> ");
            return silNorm;
        }

        public static string NormOverlap(string inputString)
        {
            return OverlapReg.Replace(inputString, " <overlap> ");
        }

        public static string CleanupChsString(string chsString, bool keepTag = false)
        {
            string charClean = string.Join(" ", CleanupChsStringParts(chsString, keepTag));
            string gbk = BigToGbk(charClean);
            string spaceClean = CleanupSpace(gbk);
            return spaceClean;
        }

        private static IEnumerable<string> CleanupChsStringParts(string chsString,bool keepTag = false)
        {
            var list = chsString.Split(Sep, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in list)
            {
                if (keepTag && IsTag(word))
                    yield return word;
                else
                {
                    yield return CleanupChsChar(word);
                }
            }
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
            string move = MoveAps.Replace(inputString, "$1 '$2");
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
            return new string(bigString.Select(x => Common.BigToGbkDict.ContainsKey(x) ? Common.BigToGbkDict[x] : x).ToArray());
        }        

        public static string CleanupEnuChar(string enuString)
        {
            return new string(enuString.Where(ValidEnu).ToArray());
        }
        
        private static Func<char, bool> ValidChs = x =>
          {
              return (x >= '一' && x <= '龟') || (x == ' ');
          };
        private static Func<char, bool> ValidEnu = x =>
           {
               return (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9') || x == '\'' || x == ' ';
           };
        public static Func<string, bool> IsTag = x =>
         {
             return OnlyTagReg.IsMatch(x);
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
        public static string MatchTagToString(string withTagString, string noTagString)
        {
            var withTagList = withTagString.Split(' ');
            var noTagList = noTagString.Split(' ');
            // length mismatch.
            //if (noTagList.Length * 2 < withTagList.Length || withTagList.Length * 2 < noTagList.Length)
            //    return "";
            var tagIndices = withTagList.Select((x, y) => new { isTag = x != Constants.UNK && OnlyTagReg.IsMatch(x), index = y })
                .Where(x => x.isTag && x.index > 0).ToArray();
            var preTagWords = tagIndices.Select(x => withTagList[x.index - 1]).ToArray();
            // Words mismatch.
            if (!Common.SequentialContains(noTagList, preTagWords))
                return "";
            var list = Common.SequentialMatch(noTagList, preTagWords).Reverse().ToList();
            if (list.Count == 0)
                return "";
            for (int i = 0; i < list.Count; i++)
            {
                noTagList[list[i]] = noTagList[list[i]] + " " + withTagList[tagIndices[i].index];
            }

            return string.Join(" ", noTagList);
        }
        public static string MatchTagToString(string withTagString, string noTagString, Dictionary<string,string> dict)
        {
            var withTagList = withTagString.Split(' ');
            var noTagList = noTagString.Split(' ');
            // length mismatch.
            //if (noTagList.Length * 2 < withTagList.Length || withTagList.Length * 2 < noTagList.Length)
            //    return "";
            var tagIndices = withTagList.Select((x, y) => new { isTag = x != Constants.UNK && OnlyTagReg.IsMatch(x), index = y })
                .Where(x => x.isTag && x.index > 0).ToArray();
            var preTagWords = tagIndices.Select(x => withTagList[x.index - 1]).ToArray();
            // Words mismatch.
            if (!Common.SequentialContains(noTagList, preTagWords,dict))
                return "";
            var list = Common.SequentialMatch(noTagList, preTagWords,dict).Reverse().ToList();
            if (list.Count == 0)
                return "";
            for (int i = 0; i < list.Count; i++)
            {
                noTagList[list[i]] = noTagList[list[i]] + " " + withTagList[tagIndices[i].index];
            }

            return string.Join(" ", noTagList);
        }

        public static string SplitWord(string s)
        {
            return string.Join(" ", SplitWordParts(s));
        }

        public static IEnumerable<int> GetTagPrefixIndices(string s)
        {
            var list = SplitWordParts(s);
            int charIndex = 0;
            foreach(object word in list)
            {
                if (OnlyTagReg.IsMatch(word.ToString()))
                    yield return charIndex;
                else
                    charIndex++;
            }            
        }

        private static IEnumerable<object> SplitWordParts(string s)
        {
            foreach(string word in s.Split(' '))
            {
                if (OnlyTagReg.IsMatch(word))
                    yield return word;
                else
                {
                    foreach (char c in word)
                        yield return c;
                }
            }
        }

        public static string InsertTagToWords(string lineWithSpace, string tag, IEnumerable<int> list)
        {
            return string.Join("", InsertTagToWordsParts(lineWithSpace, tag, list.ToArray()));
        }

        private static IEnumerable<object> InsertTagToWordsParts(string lineWithSpace, string tag, int[] array)
        {
            var list = lineWithSpace.Split(' ');
            int index = 0;
            int charIndex = 0;
            foreach(char c in lineWithSpace)
            {
                if (index<array.Length&& charIndex == array[index])
                {
                    yield return tag;
                    index++;
                }
                yield return c;
                if (c != ' ')
                    charIndex++;
            }
        }

        public static string NormBi(string inputString)
        {
            return ContainsTagReg.Replace(inputString, "<bi>");
        }
    }
}
