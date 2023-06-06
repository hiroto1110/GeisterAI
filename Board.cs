using System.Runtime.Intrinsics.X86;

namespace GeisterAI
{
    public struct Board
    {
        const ulong BOARD_MASK = (1UL << 36) - 1;
        static readonly ulong[] MASKS = {
            BOARD_MASK,
            BOARD_MASK & 0x7df7df7df,
            BOARD_MASK & 0xfbefbefbe,
            BOARD_MASK };

        static readonly int[] DIRECTIONS = { -6, -1, 1, 6 };

        public const ulong ESCAPE_MASK_P = 0b100001_000000_000000_000000_000000_000000UL;
        public const ulong ESCAPE_MASK_O = 0b100001UL;

        public const ulong INIT_MASK_P = 0b011110_011110UL;
        public const ulong INIT_MASK_O = 0b011110_011110_000000_000000_000000_000000UL;

        public ulong pb, pr, ob, or;

        public Board(ulong pb, ulong pr, ulong ob, ulong or)
        {
            this.pb = pb;
            this.pr = pr;
            this.ob = ob;
            this.or = or;
        }

        public static Board InitRandom(Random rand)
        {
            ulong NextBits(int n, int length)
            {
                ulong result = 0;
                for (int i = 0; i < n; i++)
                {
                    ulong b;
                    do
                        b = 1UL << rand.Next(length);
                    while ((b & result) != 0);
                    result |= b;
                }
                return result;
            }

            ulong p_bits = NextBits(4, 8);
            ulong o_bits = NextBits(4, 8);

            ulong pb = Bmi2.X64.ParallelBitDeposit(p_bits, INIT_MASK_P);
            ulong pr = INIT_MASK_P ^ pb;

            ulong ob = Bmi2.X64.ParallelBitDeposit(o_bits, INIT_MASK_O);
            ulong or = INIT_MASK_O ^ ob;

            return new Board(pb, pr, ob, or);
        }

        public bool IsDone(int player, out int winner)
        {
            if (pr == 0 || ob == 0)
            {
                winner = 1;
                return true;
            }

            if (pb == 0 || or == 0)
            {
                winner = -1;
                return true;
            }

            if (player == 1)
            {
                if ((pb & ESCAPE_MASK_P) != 0)
                {
                    winner = 1;
                    return true;
                }
            }
            else
            {
                if ((ob & ESCAPE_MASK_O) != 0)
                {
                    winner = -1;
                    return true;
                }
            }

            winner = 0;
            return false;
        }

        public ulong[] GetMoves(int player) => player switch
        {
            1 => GetMovesP(),
            -1 => GetMovesO(),
            _ => throw new Exception()
        };

        public ulong[] GetMovesP() => GetMoves(pb | pr);
        public ulong[] GetMovesO() => GetMoves(ob | or);

        public static ulong[] GetMoves(ulong p)
        {
            ulong move0 = GetMoveShiftR(p, MASKS[0] & ~p, 6);
            ulong move1 = GetMoveShiftR(p, MASKS[1] & ~p, 1);
            ulong move2 = GetMoveShiftL(p, MASKS[2] & ~p, 1);
            ulong move3 = GetMoveShiftL(p, MASKS[3] & ~p, 6);

            return new[] { move0, move1, move2, move3 };
        }

        public static (ulong move, int d) ChoiceRandomMove(ulong[] moves, Random rand)
        {
            int n = moves.Sum(Bits.BitCount);
            int index = rand.Next(n);

            for(int i = 0; i < moves.Length; i++)
            {
                int n_i = Bits.BitCount(moves[i]);

                if (n_i <= index)
                {
                    index -= n_i;
                    continue;
                }
                ulong move = Bmi2.X64.ParallelBitDeposit(1UL << index, moves[i]);
                return (move, i);
            }
            return (0, -1);
        }

        static ulong GetMoveShiftL(ulong p, ulong mask, int shift)
        {
            return ((p << shift) & mask) >> shift;
        }

        static ulong GetMoveShiftR(ulong p, ulong mask, int shift)
        {
            return ((p >> shift) & mask) << shift;
        }

        static ulong ShiftLeft(ulong b, int shift)
        {
            if (shift > 0)
                return b << shift;
            else
                return b >> -shift;
        }

        public Board Step(ulong move, int d)
        {
            ulong next = ShiftLeft(move, DIRECTIONS[d]);
            ulong diff = move | next;

            if ((pb & move) != 0)
                return new Board(pb ^ diff, pr, ob & ~next, or & ~next);

            else if ((pr & move) != 0)
                return new Board(pb, pr ^ diff, ob & ~next, or & ~next);

            else if ((ob & move) != 0)
                return new Board(pb & ~next, pr & ~next, ob ^ diff, or);

            else if ((or & move) != 0)
                return new Board(pb & ~next, pr & ~next, ob, or ^ diff);

            throw new ArgumentException($"Invalid Move {move}, {d}{Environment.NewLine}{this}");
        }

        public ulong FlipVertical(ulong b)
        {
            ulong result = 0;
            result |= (b & 0b111111UL) << 30;
            result |= (b & (0b111111UL << 6)) << 18;
            result |= (b & (0b111111UL << 12)) << 6;
            result |= (b & (0b111111UL << 18)) >> 6;
            result |= (b & (0b111111UL << 24)) >> 18;
            result |= (b & (0b111111UL << 30)) >> 30;
            return result;
        }

        public Board FlipPO()
        {
            return new Board(FlipVertical(ob), FlipVertical(or), FlipVertical(pb), FlipVertical(pr));
        }

        public override string ToString()
        {
            var b = this;

            string Disc(int x, int y)
            {
                ulong p = 1UL << (x + y * 6);

                if ((b.pb & p) != 0)
                    return "B";

                if ((b.pr & p) != 0)
                    return "R";

                if ((b.ob & p) != 0)
                    return "b";

                if ((b.or & p) != 0)
                    return "r";

                return " ";
            }

            string Line(int y)
            {
                return $"| {Disc(0, y)} | {Disc(1, y)} | {Disc(2, y)} | {Disc(3, y)} | {Disc(4, y)} | {Disc(5, y)} |";
            }

            return string.Join(Environment.NewLine,
                $"+---+---+---+---+---+---+", Line(0),
                $"+---+---+---+---+---+---+", Line(1),
                $"+---+---+---+---+---+---+", Line(2),
                $"+---+---+---+---+---+---+", Line(3),
                $"+---+---+---+---+---+---+", Line(4),
                $"+---+---+---+---+---+---+", Line(5),
                $"+---+---+---+---+---+---+");
        }
    }
}
