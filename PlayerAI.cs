using System.Drawing;

namespace GeisterAI
{
    public class Search
    {
        public int NumOppnentBlue { get; set; }
        public int NumOppnentRed { get; set; }
        public int NumPly { get; set; }


    }

    public struct SearchParameter
    {
        public int depth;
        public int player;
        public float alpha, beta;
        public readonly bool transposition_cut, store_transposition;

        public SearchParameter(int depth, int player, float alpha, float beta, bool transposition_cut, bool store_transposition)
        {
            this.depth = depth;
            this.player = player;
            this.alpha = alpha;
            this.beta = beta;
            this.transposition_cut = transposition_cut;
            this.store_transposition = store_transposition;
        }

        public static SearchParameter CreateInitParam(int depth, int player, bool transposition_cut, bool store_transposition)
        {
            return new SearchParameter(depth, player, -PlayerAI.INF, PlayerAI.INF, transposition_cut, store_transposition);
        }

        public SearchParameter Deepen()
        {
            return new SearchParameter(depth - 1, -player, -beta, -alpha, transposition_cut, store_transposition);
        }

        public SearchParameter SwapAlphaBeta()
        {
            return new SearchParameter(depth, -player, -beta, -alpha, transposition_cut, store_transposition);
        }

        public SearchParameter CreateNullWindowParam()
        {
            return new SearchParameter(depth - 1, -player, -alpha - 1, -alpha, transposition_cut, store_transposition);
        }
    }

    internal class PlayerAI
    {
        public const int INF = 10000000;
        public bool PrintInfo { get; set; } = true;

        //public float NullWindowSearch(Search search, Move move, SearchParameter p)
        //{
        //    return -Solve(search, move, p.CreateNullWindowParam());
        //}

        //public float Negascout(Search search, Board board, ulong moves, SearchParameter p)
        //{
        //    ulong move = Board.NextMove(moves);
        //    moves = Board.RemoveMove(moves, move);
        //    float max = -Solve(search, new Move(board, move), p.Deepen());

        //    if (p.beta <= max)
        //        return max;

        //    p.alpha = Math.Max(p.alpha, max);

        //    while ((move = Board.NextMove(moves)) != 0)
        //    {
        //        moves = Board.RemoveMove(moves, move);
        //        Move m = new Move(board, move);

        //        float eval = NullWindowSearch(search, m, p);

        //        if (p.beta <= eval)
        //            return eval;

        //        if (p.alpha < eval)
        //        {
        //            p.alpha = eval;
        //            eval = -Solve(search, m, p.Deepen());

        //            if (p.beta <= eval)
        //                return eval;

        //            p.alpha = Math.Max(p.alpha, eval);
        //        }
        //        max = Math.Max(max, eval);
        //    }
        //    return max;
        //}

        static readonly ulong[] DISTANCE_MASKS_P = {
            0b100001_000000_000000_000000_000000_000000UL,
            0b010010_100001_000000_000000_000000_000000UL,
            0b001100_010010_100001_000000_000000_000000UL,
            0b000000_001100_010010_100001_000000_000000UL,
            0b000000_000000_001100_010010_100001_000000UL,
            0b000000_000000_000000_001100_010010_100001UL,
        };

        static readonly ulong[] DISTANCE_MASKS_O = {
            0b100001UL,
            0b100001_010010UL,
            0b100001_010010_001100UL,
            0b100001_010010_001100_000000UL,
            0b100001_010010_001100_000000_000000UL,
            0b100001_010010_001100_000000_000000_000000UL,
        };

        static float EvalDistance(ulong p, ulong[] masks)
        {
            float e = 0;
            for(int i = 0; i < masks.Length; i++)
            {
                e += Bits.BitCount(masks[i] & p) / (1F + i);
            }
            return e;
        }

        public float EvalTest(Search search, Board board, SearchParameter p)
        {
            int n_p = Bits.BitCount(board.pb | board.pr);
            int n_o = Bits.BitCount(board.ob | board.or);

            float d_p = EvalDistance(board.pb | board.pr, DISTANCE_MASKS_P);
            float d_o = EvalDistance(board.ob | board.or, DISTANCE_MASKS_O);

            Console.WriteLine($"{n_p}, {n_o}, {d_p}, {d_o}");

            return p.player * (10 * (n_p - n_o) + d_p - d_o);
        }

        public static float Eval(Board board)
        {
            int n_p = Bits.BitCount(board.pb | board.pr);
            int n_o = Bits.BitCount(board.ob | board.or);

            float d_p = EvalDistance(board.pb, DISTANCE_MASKS_P) * 2;
            float d_o = EvalDistance(board.ob | board.or, DISTANCE_MASKS_O);

            return 10 * (n_p - n_o) + d_p - d_o;
        }

        public bool IsDone(Search search, Board board, SearchParameter p, out int winner)
        {
            if (8 - Bits.BitCount(board.ob | board.or) - search.NumOppnentBlue >= 4)
            {
                winner = -1;
                return true;
            }

            if (board.pb == 0)
            {
                winner = -1;
                return true;
            }

            if (board.pr == 0)
            {
                winner = 1;
                return true;
            }

            if (p.player == 1)
            {
                if ((board.pb & Board.ESCAPE_MASK_P) != 0)
                {
                    winner = 1;
                    return true;
                }
            }
            else
            {
                if (((board.ob | board.or) & Board.ESCAPE_MASK_O) != 0)
                {
                    winner = -1;
                    return true;
                }
            }

            winner = 0;
            return false;
        }

        public (ulong move, int d, float e) SolveRoot(Search search, Board board, SearchParameter p)
        {
            ulong[] moves = board.GetMoves(p.player);

            float max = -10000000;
            ulong max_move = 0;
            int max_d = -1;

            ulong move;
            for (int d = 0; d < moves.Length; d++)
            {
                ulong moves_d = moves[d];

                while ((move = Bits.NextMove(moves_d)) != 0)
                {
                    moves_d = Bits.RemoveMove(moves_d, move);

                    float e = -Solve(search, board.Step(move, d), p.Deepen());
                    p.alpha = Math.Max(p.alpha, e);

                    if(e > max)
                    {
                        max = e;
                        max_move = move;  
                        max_d = d;
                    }

                    if (p.alpha >= p.beta)
                        return (max_move, max_d, max);
                }
            }
            return (max_move, max_d, max);
        }

        public float Negamax(Search search, Board board, ulong[] moves, SearchParameter p)
        {
            float max = -1000000;
            ulong move;
            for (int d = 0; d < moves.Length; d++)
            {
                ulong moves_d = moves[d];

                while ((move = Bits.NextMove(moves_d)) != 0)
                {
                    moves_d = Bits.RemoveMove(moves_d, move);

                    float e = -Solve(search, board.Step(move, d), p.Deepen());
                    max = Math.Max(max, e);
                    p.alpha = Math.Max(p.alpha, e);

                    if (p.alpha >= p.beta)
                        return max;
                }
            }

            return max;
        }

        public virtual float Solve(Search search, Board board, SearchParameter p)
        {
            if (IsDone(search, board, p, out int winner))
            {
                // Console.WriteLine($"Done: {p.player}, {winner}");
                // Console.WriteLine(board);

                return winner * p.player * 1000000;
            }

            if (p.depth <= 0)
                return p.player * Eval(board);

            ulong[] moves = board.GetMoves(p.player);

            float value = 0;
            if (p.depth >= 3)
            {
                value = Negamax(search, board, moves, p);
            }
            else
            {
                value = Negamax(search, board, moves, p);
            }

            return value;
        }
    }
}
