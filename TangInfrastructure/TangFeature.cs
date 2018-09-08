using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    abstract class TangFeature
    {
        private Config Cfg = new Config();
        public void LoadConfigAndRun(string configPath)
        {
            Cfg = LoadConfig(configPath);
            Run();
        }


        abstract protected Config LoadConfig(string configPath);
        abstract protected void Run();
    }
}
