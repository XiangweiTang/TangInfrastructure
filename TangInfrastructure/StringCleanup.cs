using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TangInfrastructure
{
    class StringCleanup
    {
        public static string CleanupChsString(string chsString)
        {
            return new string(chsString.Where(ValidChs).ToArray());
        }

        public static string CleanupEnuString(string enuString)
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
