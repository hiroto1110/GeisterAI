using GeisterAI;
using System.Data;

int w1 = 0;
int w2 = 0;

for(int i = 0; i < 100; i++)
{
    int win = RunGame(4, 0.2F, 0.8F);

    if (win == 1)
        w1++;
    if (win == -1)
        w2++;

    Console.WriteLine($"{(float)w1/(w1+w2)}:  {w1}/{w1 + w2}");
}

int RunGame(int depth, float e1, float e2)
{
    var player = new PlayerAI();
    var rand = new Random();

    var search = new Search();

    var b = Board.InitRandom(rand);
    int p = 1;

    for (int i = 0; i < 200; i++)
    {
        //Console.WriteLine(b);
        //player.EvalTest(search, b, SearchParameter.CreateInitParam(depth, 1, true, true));
        //Console.WriteLine();

        ulong move;
        int d;

        if (p == -1)
            b = b.FlipPO();

        if(rand.NextDouble() > (p == 1 ? e1 : e2))
        {
            search.NumOppnentBlue = 4 - Bits.BitCount(b.ob);
            search.NumOppnentRed = 4 - Bits.BitCount(b.or);

            (move, d, _) = player.SolveRoot(new Search(), b, SearchParameter.CreateInitParam(depth, 1, true, true));
        }
        else
        {
            (move, d) = Board.ChoiceRandomMove(b.GetMoves(1), rand);
        }

       b = b.Step(move, d);

        if (p == -1)
            b = b.FlipPO();

        p = -p;

        if (b.IsDone(p, out int winner))
            return winner;
    }

    return 0;
}

void RunGameManual()
{
    var rand = new Random();
    var b = Board.InitRandom(rand);

    int player = 1;
    while (true)
    {
        Console.WriteLine(b);
        Console.WriteLine();

        ulong[] moves;
        if (player == 1)
            moves = b.GetMovesP();
        else
            moves = b.GetMovesO();

        ulong moves_all = 0;
        foreach (var m in moves)
        {
            moves_all |= m;
        }

        Console.WriteLine("moves");
        Console.WriteLine(new Board(0, 0, 0, moves_all));
        Console.WriteLine();

        ulong move;
        int d;

        do
        {
            int[] xyd = Console.ReadLine().Split(" ").Select(int.Parse).ToArray();

            move = 1UL << (xyd[0] + xyd[1] * 6);
            d = xyd[2];
        }
        while ((moves[d] & move) == 0);

        b = b.Step(move, d);

        player *= -1;
    }
}


