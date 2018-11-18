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
        public static string CleanupEnuString(string s)
        {
            return new string(s.Where(ValidEnu).ToArray());
        }

        public static string CleanupChsString(string s)
        {
            return new string(s.Where(ValidChs).ToArray());
        }

        private static bool ValidChs(char x)
        {
            return (x >= '一' && x <= '龟') || (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9') || x == ' ';
        }

        private static bool ValidEnu(char x)
        {
            return (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z') || (x >= '0' && x <= '9') || x == '\'' || x == ' ';
        }
    }
}
