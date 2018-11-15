using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TangInfrastructure
{
    class SubtitleProcess
    {
        public IEnumerable<char> CleanSentencePair(string path)
        {
            return File.ReadLines(path)
                .Select(x => x.Split('\t'))
                .Where(x => x.Length == 2)
                .SelectMany(x => GetInvalid(InvalidChs,x[0]).Concat(GetInvalid(InvalidEnu, x[1])))
                .Distinct();
        }

        private IEnumerable<char> GetInvalid(Func<char,bool> invalid, string s)
        {
            return s.Where(invalid);
        }


        private bool InvalidChs(char x)
        {
            return !ValidChs(x);
        }

        private bool InvalidEnu(char x)
        {
            return !ValidEnu(x);
        }

        private bool ValidChs(char x)
        {
            return (x >= '一' && x <= '龟') || (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9');
        }

        private bool ValidEnu(char x)
        {
            return (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9') || x == '\'';
        }

    }
}
