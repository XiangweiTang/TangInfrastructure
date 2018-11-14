using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    class MED
    {
        public int INS { get; private set; } = 0;
        public int DEL { get; private set; } = 0;
        public int SUB { get; private set; } = 0;
        public int REF { get; private set; } = 0;
        public int ERR { get; private set; } = 0;
        public int HYP { get; private set; } = 0;

        public double ErrorRate { get; private set; } = 0.0;

        public MED() { }

        public void Run<T>(IEnumerable<T> refList, IEnumerable<T> hypList)
        {
            var refArray = refList.ToArray();
            var hypArray = hypList.ToArray();
            REF = refArray.Length;
            HYP = hypArray.Length;

            if (REF == 0)
            {
                ERR = INS = HYP;
                SetOverall();
                return;
            }

            if (HYP == 0)
            {
                ERR = DEL = REF;
                SetOverall();
                return;
            }

            var matrix = DP(refArray, hypArray);
            BackTrack(matrix, refArray, hypArray);
            SetOverall();
        }        

        private void SetOverall()
        {
            //ERR = INS + DEL + SUB;
            ErrorRate = 1.0 * ERR / REF;
        }

        private void BackTrack<T>(int[,] matrix, T[] refArray, T[] hypArray)
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
                    r--;
                    h--;
                    continue;
                }

                int current = matrix[r, h];
                int preIns = matrix[r, h - 1];
                int preDel = matrix[r - 1, h];
                int preSub = matrix[r - 1, h - 1];

                if (current == preIns + 1)
                {
                    h--;
                    INS++;
                    continue;
                }
                if (current == preDel + 1)
                {
                    r--;
                    DEL++;
                    continue;
                }
                r--;
                h--;
                SUB++;
            }
        }
                
        private int[,] DP<T>(T[] refArray, T[] hypArray)
        {
            int refLength = refArray.Length;
            int hypLength = hypArray.Length;
            int[,] matrix = new int[refLength + 1, hypLength + 1];

            for (int i = 1; i <= refLength; i++)
            {
                matrix[i, 0] = i;
            }

            for (int j = 1; j <= hypLength; j++)
            {
                matrix[0, j] = j;
            }

            for (int i = 1; i < refLength + 1; i++)
            {
                for (int j = 1; j < hypLength + 1; j++)
                {
                    int left = matrix[i, j - 1] + 1;
                    int down = matrix[i - 1, j] + 1;
                    int diag = matrix[i - 1, j - 1] + (refArray[i - 1].Equals(hypArray[j - 1]) ? 0 : 1);
                    matrix[i, j] = Math.Min(Math.Min(left, down), diag);
                }
            }
            ERR = matrix[refLength, hypLength];
            return matrix;
        }
    }
}
