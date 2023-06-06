using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GeisterAI
{
    public static class Bits
    {
        public static int BitCount(ulong v)
        {
            return BitOperations.PopCount(v);
        }

        public static ulong LowestOneBit(ulong i)
        {
            return i & (~i + 1);
        }

        public static int LeadingZeroCount(ulong b)
        {
            return BitOperations.LeadingZeroCount(b);
        }

        public static ulong NextMove(ulong moves)
        {
            return LowestOneBit(moves);
        }

        public static ulong RemoveMove(ulong moves, ulong move)
        {
            return moves ^ move;
        }
    }
}
