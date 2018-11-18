using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    class RunNmt
    {
        Config Cfg = new Config();
        public RunNmt(Config cfg)
        {
            Cfg = cfg;
        }

        public void RunDemo()
        {
            string fileName = Cfg.PythonPath;
            string args = Cfg.NmtCommand;
            Common.RunFile(fileName, args,Cfg.NmtFolder);
        }
    }
}
