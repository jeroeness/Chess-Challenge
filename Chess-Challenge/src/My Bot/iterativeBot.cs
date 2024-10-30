using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using static System.Formats.Asn1.AsnWriter;

public class MyBot2 : ChessBot
{
    //https://github.com/JacquesRW/Chess-Challenge/blob/main/Chess-Challenge/src/My%20Bot/MyBot.cs
    Move bestmoveRoot = Move.NullMove;

    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    int[] pieceVal = { 0, 100, 310, 330, 500, 1000, 10000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };
    int max_depth;
    int min_depth;

    //public MyBot2()
    //{
    //    mg_pesto_table = {
    //        mg_pawn_table,
    //        mg_knight_table,
    //        mg_bishop_table,
    //        mg_rook_table,
    //        mg_queen_table,
    //        mg_king_table
    //    };
    //}

    public override Move Think(Board board, Timer timer)
    {
        bestmoveRoot = Move.NullMove;
        // https://www.chessprogramming.org/Iterative_Deepening
        for (int depth = 1; depth <= 50; depth++)
        {
            max_depth = 0;
            min_depth = 100;

            int score = Search(board, timer, -30000, 30000, depth, 0);

            Console.WriteLine($"{board.PlyCount} {depth} {bestmoveRoot} {score}\t {min_depth} {max_depth}");


            // Out of time
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
        }
        Console.WriteLine($"----");
        return bestmoveRoot.IsNull ? board.GetLegalMoves()[0] : bestmoveRoot;
    }

    // https://www.chessprogramming.org/Transposition_Table
    struct TTEntry
    {
        public ulong key;
        public Move move;
        public int depth, score, bound;
        public TTEntry(ulong _key, Move _move, int _depth, int _score, int _bound)
        {
            key = _key; move = _move; depth = _depth; score = _score; bound = _bound;
        }
    }

    const int entries = (1 << 22);
    TTEntry[] tt = new TTEntry[entries];

    public int getPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }

    int[] mg_value = { 82, 337, 365, 477, 1025, 0 };
    int[] eg_value = { 94, 281, 297, 512, 936, 0 };


//    int[] mg_pawn_table = {
//      0,   0,   0,   0,   0,   0,  0,   0,
//     98, 134,  61,  95,  68, 126, 34, -11,
//     -6,   7,  26,  31,  65,  56, 25, -20,
//    -14,  13,   6,  21,  23,  12, 17, -23,
//    -27,  -2,  -5,  12,  17,   6, 10, -25,
//    -26,  -4,  -4, -10,   3,   3, 33, -12,
//    -35,  -1, -20, -23, -15,  24, 38, -22,
//      0,   0,   0,   0,   0,   0,  0,   0,
//};

//    int[] eg_pawn_table = {
//      0,   0,   0,   0,   0,   0,   0,   0,
//    178, 173, 158, 134, 147, 132, 165, 187,
//     94, 100,  85,  67,  56,  53,  82,  84,
//     32,  24,  13,   5,  -2,   4,  17,  17,
//     13,   9,  -3,  -7,  -7,  -8,   3,  -1,
//      4,   7,  -6,   1,   0,  -5,  -1,  -8,
//     13,   8,   8,  10,  13,   0,   2,  -7,
//      0,   0,   0,   0,   0,   0,   0,   0,
//};

//    int[] mg_knight_table = {
//    -167, -89, -34, -49,  61, -97, -15, -107,
//     -73, -41,  72,  36,  23,  62,   7,  -17,
//     -47,  60,  37,  65,  84, 129,  73,   44,
//      -9,  17,  19,  53,  37,  69,  18,   22,
//     -13,   4,  16,  13,  28,  19,  21,   -8,
//     -23,  -9,  12,  10,  19,  17,  25,  -16,
//     -29, -53, -12,  -3,  -1,  18, -14,  -19,
//    -105, -21, -58, -33, -17, -28, -19,  -23,
//};

//    int[] eg_knight_table = {
//    -58, -38, -13, -28, -31, -27, -63, -99,
//    -25,  -8, -25,  -2,  -9, -25, -24, -52,
//    -24, -20,  10,   9,  -1,  -9, -19, -41,
//    -17,   3,  22,  22,  22,  11,   8, -18,
//    -18,  -6,  16,  25,  16,  17,   4, -18,
//    -23,  -3,  -1,  15,  10,  -3, -20, -22,
//    -42, -20, -10,  -5,  -2, -20, -23, -44,
//    -29, -51, -23, -15, -22, -18, -50, -64,
//};

//    int[] mg_bishop_table = {
//    -29,   4, -82, -37, -25, -42,   7,  -8,
//    -26,  16, -18, -13,  30,  59,  18, -47,
//    -16,  37,  43,  40,  35,  50,  37,  -2,
//     -4,   5,  19,  50,  37,  37,   7,  -2,
//     -6,  13,  13,  26,  34,  12,  10,   4,
//      0,  15,  15,  15,  14,  27,  18,  10,
//      4,  15,  16,   0,   7,  21,  33,   1,
//    -33,  -3, -14, -21, -13, -12, -39, -21,
//};

//    int[] eg_bishop_table = {
//    -14, -21, -11,  -8, -7,  -9, -17, -24,
//     -8,  -4,   7, -12, -3, -13,  -4, -14,
//      2,  -8,   0,  -1, -2,   6,   0,   4,
//     -3,   9,  12,   9, 14,  10,   3,   2,
//     -6,   3,  13,  19,  7,  10,  -3,  -9,
//    -12,  -3,   8,  10, 13,   3,  -7, -15,
//    -14, -18,  -7,  -1,  4,  -9, -15, -27,
//    -23,  -9, -23,  -5, -9, -16,  -5, -17,
//};

//    int[] mg_rook_table = {
//     32,  42,  32,  51, 63,  9,  31,  43,
//     27,  32,  58,  62, 80, 67,  26,  44,
//     -5,  19,  26,  36, 17, 45,  61,  16,
//    -24, -11,   7,  26, 24, 35,  -8, -20,
//    -36, -26, -12,  -1,  9, -7,   6, -23,
//    -45, -25, -16, -17,  3,  0,  -5, -33,
//    -44, -16, -20,  -9, -1, 11,  -6, -71,
//    -19, -13,   1,  17, 16,  7, -37, -26,
//};

//    int[] eg_rook_table = {
//    13, 10, 18, 15, 12,  12,   8,   5,
//    11, 13, 13, 11, -3,   3,   8,   3,
//     7,  7,  7,  5,  4,  -3,  -5,  -3,
//     4,  3, 13,  1,  2,   1,  -1,   2,
//     3,  5,  8,  4, -5,  -6,  -8, -11,
//    -4,  0, -5, -1, -7, -12,  -8, -16,
//    -6, -6,  0,  2, -9,  -9, -11,  -3,
//    -9,  2,  3, -1, -5, -13,   4, -20,
//};

//    int[] mg_queen_table = {
//    -28,   0,  29,  12,  59,  44,  43,  45,
//    -24, -39,  -5,   1, -16,  57,  28,  54,
//    -13, -17,   7,   8,  29,  56,  47,  57,
//    -27, -27, -16, -16,  -1,  17,  -2,   1,
//     -9, -26,  -9, -10,  -2,  -4,   3,  -3,
//    -14,   2, -11,  -2,  -5,   2,  14,   5,
//    -35,  -8,  11,   2,   8,  15,  -3,   1,
//     -1, -18,  -9,  10, -15, -25, -31, -50,
//};

//    int[] eg_queen_table = {
//     -9,  22,  22,  27,  27,  19,  10,  20,
//    -17,  20,  32,  41,  58,  25,  30,   0,
//    -20,   6,   9,  49,  47,  35,  19,   9,
//      3,  22,  24,  45,  57,  40,  57,  36,
//    -18,  28,  19,  47,  31,  34,  39,  23,
//    -16, -27,  15,   6,   9,  17,  10,   5,
//    -22, -23, -30, -16, -16, -23, -36, -32,
//    -33, -28, -22, -43,  -5, -32, -20, -41,
//};

//    int[] mg_king_table = {
//    -65,  23,  16, -15, -56, -34,   2,  13,
//     29,  -1, -20,  -7,  -8,  -4, -38, -29,
//     -9,  24,   2, -16, -20,   6,  22, -22,
//    -17, -20, -12, -27, -30, -25, -14, -36,
//    -49,  -1, -27, -39, -46, -44, -33, -51,
//    -14, -14, -22, -46, -44, -30, -15, -27,
//      1,   7,  -8, -64, -43, -16,   9,   8,
//    -15,  36,  12, -54,   8, -28,  24,  14,
//};

//    int[] eg_king_table = {
//    -74, -35, -18, -18, -11,  15,   4, -17,
//    -12,  17,  14,  17,  17,  38,  23,  11,
//     10,  17,  23,  15,  20,  45,  44,  13,
//     -8,  22,  24,  27,  26,  33,  26,   3,
//    -18,  -4,  21,  24,  27,  23,   9, -11,
//    -19,  -3,  11,  21,  23,  16,   7,  -9,
//    -27, -11,   4,  13,  14,   4,  -5, -17,
//    -53, -34, -21, -11, -28, -14, -24, -43
//};

//    int[] mg_pesto_table; =
//    {
//        mg_pawn_table,
//        mg_knight_table,
//        mg_bishop_table,
//        mg_rook_table,
//        mg_queen_table,
//        mg_king_table
//};

//int[] eg_pesto_table; =
//    {
//    eg_pawn_table,
//        eg_knight_table,
//        eg_bishop_table,
//        eg_rook_table,
//        eg_queen_table,
//        eg_king_table
//    };

//int gamephaseInc[12] = { 0, 0, 1, 1, 1, 1, 2, 2, 4, 4, 0, 0 };
//int mg_table[12][64] ;
//int eg_table[12][64] ;

public int Evaluate(Board board)
    {
        int mg = 0, eg = 0, phase = 0;

        foreach (bool stm in new[] { true, false })
        {
            for (var p = PieceType.Pawn; p <= PieceType.King; p++)
            {
                int piece = (int)p, ind;
                ulong mask = board.GetPieceBitboard(p, stm);
                while (mask != 0)
                {
                    phase += piecePhase[piece];
                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                    mg += getPstVal(ind) + pieceVal[piece];
                    eg += getPstVal(ind + 64) + pieceVal[piece];
                }
            }

            mg = -mg;
            eg = -eg;
        }

        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

    bool MoveIsCheck(Move move, Board board)
{
    board.MakeMove(move);
    bool check = board.IsInCheck();
    board.UndoMove(move);
    return check;
}

// https://www.chessprogramming.org/Negamax
// https://www.chessprogramming.org/Quiescence_Search
public int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply)
{
    ulong key = board.ZobristKey;
    bool qsearch = depth <= 0;
    bool notRoot = ply > 0;
    int best = -30000;

    // Check for repetition (this is much more important than material and 50 move rule draws)
    if (notRoot && board.IsRepeatedPosition())
        return 0;

    TTEntry entry = tt[key % entries];

    // TT cutoffs
    if (notRoot && entry.key == key && entry.depth >= depth && (
        entry.bound == 3 // exact score
            || entry.bound == 2 && entry.score >= beta // lower bound, fail high
            || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
    ))
    {
        min_depth = Math.Min(min_depth, ply);
        return entry.score;
    }

    int eval = Evaluate(board);

    // Quiescence search is in the same function as negamax to save tokens
    if (qsearch)
    {
        best = eval;
        if (best >= beta)
        {
            max_depth = Math.Max(max_depth, ply);
            return best;
        }
        alpha = Math.Max(alpha, best);
    }

    // Generate moves, only captures in qsearch
    Move[] moves = board.GetLegalMoves(false);
    if (qsearch)
    {

        moves = moves.Where(m => m.IsCapture/* || (MoveIsCheck(m, board))*/).ToArray();
    }
    int[] scores = new int[moves.Length];

    // Score moves
    for (int i = 0; i < moves.Length; i++)
    {
        Move move = moves[i];
        // TT move
        if (move == entry.move) scores[i] = 1000000;
        // https://www.chessprogramming.org/MVV-LVA
        else if (move.IsCapture) scores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
    }

    Move bestMove = Move.NullMove;
    int origAlpha = alpha;

    // Search moves
    for (int i = 0; i < moves.Length; i++)
    {

        // Incrementally sort moves
        for (int j = i + 1; j < moves.Length; j++)
        {
            if (scores[j] > scores[i])
                (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
        }

        Move move = moves[i];
        board.MakeMove(move);
        int score = -Search(board, timer, -beta, -alpha, depth - 1, ply + 1);
        board.UndoMove(move);

        // New best move
        if (score > best)
        {
            best = score;
            bestMove = move;
            if (ply == 0) bestmoveRoot = move;

            // Improve alpha
            alpha = Math.Max(alpha, score);

            // Fail-high
            if (alpha >= beta) break;

        }
        if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return best;
    }

    // (Check/Stale)mate
    if (!qsearch && moves.Length == 0) return board.IsInCheck() ? -30000 + ply : 0;

    // Did we fail high/low or get an exact score?
    int bound = best >= beta ? 2 : best > origAlpha ? 3 : 1;

    // Push to TT
    tt[key % entries] = new TTEntry(key, bestMove, depth, best, bound);

    return best;
}

}