using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    class SplitData<T>
    {
        public T[] Train { get; private set; } = new T[0];
        public T[] Dev { get; private set; } = new T[0];
        public T[] Test { get; private set; } = new T[0];
        public SplitData(IEnumerable<T> inputCollection, int devCount, int testCount)
        {
            var shuffle = inputCollection.Shuffle();
            Split(shuffle, devCount, testCount);
        }

        public SplitData(IEnumerable<T> inputCollection)
        {
            var shuffle = inputCollection.Shuffle();
            int n = shuffle.Length;
            int devCount = Convert.ToInt32(n * 0.1);
            int testCount = Convert.ToInt32(n * 0.1);
            Split(shuffle, devCount, testCount);
        }

        private void Split(T[] shuffle, int devCount, int testCount)
        {
            Dev = shuffle.ArrayTake(devCount);
            Test = shuffle.ArrayRange(devCount, testCount);
            Train = shuffle.ArraySkip(devCount + testCount);
        }
    }
}
