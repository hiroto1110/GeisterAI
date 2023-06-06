using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeisterAI
{
    public struct Move
    {
        public readonly ulong move;
        public readonly int d;

        public Move(ulong move, int d)
        {
            this.move = move;
            this.d = d;
        }
    }
}
