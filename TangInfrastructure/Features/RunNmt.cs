﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TangInfrastructure
{
    class RunNmt
    {
        Config Cfg = new Config();
        public RunNmt(Config cfg)
        {
            Cfg = cfg;
        }

        public void RunDemoTrain()
        {
            string fileName = Cfg.PythonPath;
            string args = Cfg.TrainNmtCommand;
            Common.RunFile(fileName, args,Cfg.NmtFolder);
            //string path = Path.Combine(Cfg.WorkFolder, "args.txt");
            //File.WriteAllText(path, "python " + args);
        }

        public void RunDemoTest()
        {
            string fileName = Cfg.PythonPath;
            string args = Cfg.TestNmtCommand;
            Common.RunFile(fileName, args, Cfg.NmtFolder);
            //System.IO.File.WriteAllText("args.txt", args);
        }
    }
}
