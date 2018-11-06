using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TangInfrastructure
{
    class Test
    {
        public Test(string[] args)
        {
            string path = @"D:\XiangweiTang\在职毕业设计\自然对话-银行\NDYH0002.TextGrid";
            TextGrid tg = new TextGrid(path);
            var list = tg.ItemList;
            var groups = list.GroupBy(x => x.TierIndex).ToDictionary(x => x.Key, x => x.Select(y => y));
            var newList = groups.Select(x => x.Key + "\t" + x.Value.Count());
        }
    }
}
