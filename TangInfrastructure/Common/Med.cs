using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    class Med<T>
    {
        public int INS { get; private set; } = 0;
        public int DEL { get; private set; } = 0;
        public int SUB { get; private set; } = 0;
        public int ERR { get => INS + DEL + SUB; }
        public int REF { get; private set; }
        public int HYP { get; private set; }
        public double ErrorRate { get; private set; } = 0;

        public IEnumerable<Tuple<T, T>> MatchedPairs
        {
            get
            {
                return _MathcedPairs.AsEnumerable().Reverse();
            }
        }
        private List<Tuple<T, T>> _MathcedPairs = new List<Tuple<T, T>>();

        private int[,] Dp;
        public Med() { }
        public void RunMed(IEnumerable<T> refs,IEnumerable<T> hyps)
        {
            T[] refArray = refs.ToArray();
            T[] hypArray = hyps.ToArray();
            REF = refArray.Length;
            HYP = hypArray.Length;
            if (REF == 0 && HYP == 0)
                return;
            if (REF == 0)
            {
                INS = HYP;
                ErrorRate = 100.0;
                return;
            }
            if (HYP == 0)
            {
                DEL = HYP;
                ErrorRate = 100.0;
                return;
            }
            RunDp(refArray, hypArray);
            _MathcedPairs.AddRange(BackTrack(refArray, hypArray));
            ErrorRate = 100.0 * ERR / REF;
        }

        private void RunDp(T[] refArray, T[] hypArray)
        {
            int refN = refArray.Length + 1;
            int hypN = hypArray.Length + 1;
            Dp = new int[refN, hypN];
            for(int i = 0; i < refN; i++)
            {
                Dp[i, 0] = i;
            }
            for(int j = 0; j < hypN; j++)
            {
                Dp[0, j] = j;
            }
            for(int i = 1; i < refN; i++)
            {
                for(int j = 1; j < hypN; j++)
                {
                    int left = Dp[i, j - 1] + 1;
                    int down = Dp[i - 1, j] + 1;
                    int diag = Dp[i - 1, j - 1] + (refArray[i - 1].Equals(hypArray[j - 1]) ? 0 : 1);
                    Dp[i, j] = Math.Min(diag, Math.Min(left, down));
                }
            }
        }

        private IEnumerable<Tuple< T,T>> BackTrack(T[] refArray, T[] hypArray)
        {
            int r = refArray.Length;
            int h = hypArray.Length;
            while (r >= 0 && h >= 0)
            {
                if (r == 0 && h == 0)
                    break;
                if (r == 0)
                {
                    h--;
                    INS++;
                    continue;
                }
                if (h == 0)
                {
                    r--;
                    DEL++;
                    continue;
                }
                if (refArray[r - 1].Equals(hypArray[h - 1]))
                {
                    yield return new Tuple<T, T>(refArray[r - 1], hypArray[h - 1]);
                    r--;
                    h--;                    
                    continue;
                }
                if (Dp[r - 1, h] + 1 == Dp[r, h])
                {
                    r--;
                    DEL++;
                    continue;
                }
                if (Dp[r, h - 1] + 1 == Dp[r, h])
                {
                    h--;
                    INS++;
                    continue;
                }
                r--;
                h--;
                SUB++;
            }
        }

        public string Output=> string.Join("\t", OutputPart());

        private IEnumerable<object> OutputPart()
        {
            yield return REF;
            yield return HYP;
            yield return DEL;
            yield return INS;
            yield return SUB;
            yield return ErrorRate;
        }
    }
}
