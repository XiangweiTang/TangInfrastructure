using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].ToLower() == "magictest")
                TestMod(args);
            else
                ConfigMod(args);
        }

        static void TestMod(string[] args)
        {
            var newArgs = args.Skip(1).ToArray();
            Test t = new Test(newArgs);
        }

        static void ConfigMod(string[] args)
        {

        }
    }
}
