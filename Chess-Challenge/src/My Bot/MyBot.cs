using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using static System.Formats.Asn1.AsnWriter;

struct MoveEval
{
    public Move Move;
    public float Eval;

}

struct TableEntry
{
    public ulong Zobristkey;
    public float Score;

}
public class MyBot : IChessBot
{
    static int ThinkingTime;
    static float VariableNodes;
    static int maxDepth;
    static int minDepth;
    static int boardIndex;
    static int cyclesHit;
    static float bestMoveIndex;
    static float alpha;
    static float beta;
    static Timer timer_;
    static int nodeCounter;
    //static Dictionary<ulong, float> boardHeuristics = new Dictionary<ulong, float>();
    static TableEntry[] boardHeuristics;
    static TableEntry[] cycleTable;


    public Move Think(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining >= 59_900)
        {
            //Console.WriteLine("RESET");
            boardIndex = 0;
            ThinkingTime = 0;
            VariableNodes = 5_000;
            boardHeuristics = new TableEntry[4_000_000];
            alpha = -100000;
            beta = 100000;
            //VariableNodes = (board.PlyCount <= 1) ? 4_000 : 5_000;
        }
        //cycleTable = new TableEntry[1_000_000];
        cyclesHit = 0;
        nodeCounter = 0;
        timer_ = timer;
        //Console.WriteLine($"Heuristic {heuristic2(board, board.IsWhiteToMove, 0, true)}");
        VariableNodes += ThinkingTime < 1800 -Math.Min(1600, Math.Max(0, (30_000-timer.MillisecondsRemaining)/8)) ? VariableNodes * .2f : VariableNodes * -.4f;
        int nodes = 100 + (int)VariableNodes;
        Move bestMove = board.GetLegalMoves()[0];
        minDepth = 1000;
        maxDepth = -1;
        //int nodes = 100_000;
        Move nullMove = new Move();
        float score = Negamax(ref board, nodes, 0, alpha - 400, beta + 400, true, board.IsWhiteToMove, ref nullMove, ref bestMove);
       
        
        //board.MakeMove(bestMove);
        //Console.WriteLine($"Heuristic After move {heuristic2(board, !board.IsWhiteToMove, 0, true)}");
        ThinkingTime = timer.MillisecondsElapsedThisTurn;
        String color = board.IsWhiteToMove ? "WHITE" : "BLACK";
        Console.WriteLine($"{color} think {(float)ThinkingTime/1000.0}s / {(float)timer.MillisecondsRemaining/ 1000.0}s Sort {bestMoveIndex} \tnodes {nodeCounter} \tscore {Math.Round(score)} Depth {minDepth} to {maxDepth} cycles {cyclesHit} {bestMove}"); 
        return bestMove;
    }

    static MoveEval[] GetMoveEvals(Move[] moves, Board board)
    {
        MoveEval[] moveEvals = new MoveEval[moves.Length];
        int i = 0;
        foreach (Move move in moves)
        {  
            board.MakeMove(move);
            // query the heuristic table
            TableEntry tableEntry = boardHeuristics[board.ZobristKey % (ulong)boardHeuristics.Length];
            float tt_score = tableEntry.Zobristkey == board.ZobristKey ? tableEntry.Score : -10_000; // Also for negative gamestate we prioritize TT moves
            board.UndoMove(move);
            //float heuristic = 0;
            //if (boardHeuristics.TryGetValue(board.ZobristKey, out heuristic))
            //{
            
            //moveEvals[i++] = (new MoveEval(move, (2*(int)move.CapturePieceType)+((int)move.MovePieceType)));
            moveEvals[i++] = new MoveEval { Move = move, Eval = tt_score + (int)move.CapturePieceType - ((int)move.MovePieceType % 6) + (int)move.CapturePieceType * 10 };
        }
        return moveEvals;
    }

    float Negamax(ref Board board, int nodes, int depth, float a, float b, bool isPlayer, bool playAsWhite, ref Move prevMove, ref Move outBestMove)
    {
        //TableEntry tableEntry = cycleTable[board.ZobristKey % (ulong)cycleTable.Length];
        //if (tableEntry.Zobristkey == board.ZobristKey)
        //{
        //    cyclesHit++;
        //    return tableEntry.Score;
        //}
        Move[] moves = board.GetLegalMoves(/*depth > 5*/);
        float best = -13370000000;

        if (nodes == 0 || moves.Length == 0 || depth >=6)
        {
            minDepth = Math.Min(minDepth, depth);
            maxDepth = Math.Max(maxDepth, depth);
            float h = heuristic2(board, playAsWhite, depth, isPlayer);
            nodeCounter++;
            //return h;
            return isPlayer ? h : -h;
        }
        if (isPlayer && board.PlyCount <= 2 && depth == 0)
        {
            moves = moves.Select(moves => moves).Where(move => move.MovePieceType == PieceType.Pawn).ToArray();
        }
        MoveEval[] sortedMoveEvals = GetMoveEvals(moves, board).OrderByDescending(moveEval => moveEval.Eval).ToArray();

        if (depth > 30)
        {
            Console.WriteLine("DEPTH");
        }

        int nextNodes = (int)((float)nodes / (float)(moves.Length + 1)) /*+ ((!isPlayer) ? 0 : 1)*/;
        
        bool repeat = depth == 2 && board.GameRepetitionHistory.Contains(board.ZobristKey);
        
        int k = 0;
        foreach (MoveEval moveEval in sortedMoveEvals)
        {
            Move move = moveEval.Move;
            board.MakeMove(move);
            Move bestMove = new Move();
            int bonusNodes = 0;
            //if (prevMove.IsCapture && move.IsCapture && prevMove.TargetSquare == move.TargetSquare && timer_.MillisecondsElapsedThisTurn < 3000)
            //{
            //    bonusNodes = 1;
            //}
            //bonusNodes = (depth % 2 == 0) ? 2 : 0;
            if (/* timer_.MillisecondsRemaining > 20_000 &&*/ (move.IsCapture || board.IsInCheck()) /* && (int)move.CapturePieceType + 1 >= (int)move.MovePieceType % 6 */ /* && timer_.MillisecondsElapsedThisTurn < 3000*/)
            {
                bonusNodes = 1;
            }
            //if (depth < 3)
            //{
            //    bonusNodes = 2;
            //}
            //if (depth >=5)
            //{
            //    bonusNodes = 0;
            //}
            //bonusNodes = (move.IsCapture && prevMove.IsCapture && depth < 10) ? 1 : 0;
            //bonusNodes = 0;
            float score = -Negamax(ref board, nextNodes + bonusNodes, depth+1, -b, -a, !isPlayer, playAsWhite, ref move, ref bestMove);
            
            score *= (repeat && score > 50) ? .5f : 1;

            k++;
            if (score > best)
            {
                if (depth == 0)bestMoveIndex = k;

                boardHeuristics[board.ZobristKey % (ulong)boardHeuristics.Length] = new TableEntry { Zobristkey = board.ZobristKey, Score = score };
                outBestMove = move;
                best = score;
            }
            board.UndoMove(move);
            if (best > a)
            {
                a = best;
            }
            //if (depth <= 0)
            //{
            //    String color = playAsWhite ? "WHITE" : "BLACK";
            //    Console.WriteLine($"{color} {move}:{score} {depth} {outBestMove}:{best} {a}~{b} {isPlayer}");
            //}
            if (a >= b)
            {
                break;
            }
        }
        if (depth == 0)
        {
            //bestMoveIndex = 1f - (float)bestMoveIndex / (float)sortedMoveEvals.Length;
            alpha = a;
        }
        if (depth == 1)
        {
            beta = Math.Max(a, beta);

        }
        //cycleTable[board.ZobristKey % (ulong)cycleTable.Length] = new TableEntry { Zobristkey = board.ZobristKey, Score = best };
        return best*.99f;
    }

    static float distance(Square a, float rank, float file)
    {
        return Math.Min(6, (Math.Abs(a.Rank - rank) + Math.Abs(a.File - file)) - 3.5f) *5;//1 to 6 -> -2.5 to 2.5
    }
    
    float heuristic2(Board board, bool playAsWhite, int depth, bool isPlayer)
    {
        float score = 0;
        if (board.IsInCheckmate())
        {
            score -= (100 - depth) * 100_000;
            if (!isPlayer) // fix for: zwart verliezend ziet geen mate
            {
                score *= -1;
            }
            //else
            //{
            //    Console.WriteLine("CHECKMATE");
            //}
            return score;
            // return when enemy in checkmate
        }
        // TODO if in stalemate, return 0;

        int captures = board.GameMoveHistory.Where(move => move.IsCapture).ToArray().Length;
        float lategame = Math.Min(1,Math.Max(captures - 16f,0) / 10f);


        int[] material = new int[2]; 
        foreach (PieceList pl in board.GetAllPieceLists())
        {
            float pscore = 0;
            float side = playAsWhite == pl.IsWhitePieceList ? 1 : -1;
            Square enKingSq = board.GetKingSquare(!pl.IsWhitePieceList);
            //Square myKingSq = board.GetKingSquare(pl.IsWhitePieceList);

            foreach (Piece p in pl)
            {
                float distToMiddle = distance(p.Square, 3.5f, 3.5f);
                float onEnKingFileOrRank = (p.Square.File == enKingSq.File || p.Square.Rank == enKingSq.Rank) ? 1 : 0;
                float onEnKingDiagonal = (Math.Abs(p.Square.File - enKingSq.File) == Math.Abs(p.Square.Rank - enKingSq.Rank)) ? 1 : 0;
                float rankScore = (pl.IsWhitePieceList) ? p.Square.Rank : 7 - p.Square.Rank;
                float distanceToEnemyKing = distance(p.Square, enKingSq.Rank, enKingSq.File);
                switch (p.PieceType)
                {
                    case PieceType.Pawn:
                        pscore += 99 + rankScore *(1+rankScore*.5f) * (4-Math.Abs(p.Square.File -3.5f))*3 + rankScore * 5 * lategame;
                        break;
                    case PieceType.Knight:
                        pscore += 316 - distToMiddle;
                        break;
                    case PieceType.Bishop:
                        pscore += 328 /*- distToMiddle + onEnKingDiagonal*/;
                        break;
                    case PieceType.Rook:
                        pscore += 493 + onEnKingFileOrRank - distToMiddle;
                        break;
                    case PieceType.Queen:
                        pscore += 982 + onEnKingFileOrRank*5 + onEnKingDiagonal - rankScore*2;
                        break;
                    case PieceType.King:
                        pscore += (distToMiddle*2 - 2*rankScore * ((1-lategame)*2-1)) + Convert.ToUInt32(board.HasKingsideCastleRight(p.IsWhite) || board.HasQueensideCastleRight(p.IsWhite))*4;
                        break;
                }
                material[Convert.ToUInt32(pl.IsWhitePieceList)]++;
                pscore += (4 * lategame * -distanceToEnemyKing);
            }
            score += pscore * side;
        }

        //Console.WriteLine($"{score}");
        score += (material[0] - material[1]) / (material[0] + material[1]);

        // Count moves available
        board.ForceSkipTurn();
        float testscore = 0;
        if (playAsWhite)
        {
            testscore = heuristic2(board, !playAsWhite, depth, isPlayer);
        }
        score -= board.GetLegalMoves().Length*.1f; // Todo this is slow, use non alloc
        //score += board.IsInCheck() ? 50 : 0;
        board.UndoSkipTurn();
        score += board.GetLegalMoves().Length*.1f; // Todo this is slow, use non alloc
        //score -= board.IsInCheck() ? 50 : 0;
        if (playAsWhite && -testscore - score > .001)
        {
            Console.WriteLine($"{score} {testscore}");
        }

        return score;
    }

    //private float heuristic(Board board, bool playAsWhite, int depth, bool isPlayer)
    //{
    //    float score = 0;
    //    if (board.IsInCheckmate())
    //    {
    //        score += (100 - depth) * 100_000;
    //        if (isPlayer) // fix for: zwart verliezend ziet geen mate
    //        {
    //            score *= -1;
    //        }
    //    }
    //    bool[] colors = new bool[2] { playAsWhite, !playAsWhite };
    //    float[] specialPieces = new float[2];


    //    //int lateGame = specialPieces[true] + specialPieces[false] <= 6 ? 1 : 0;
    //    foreach (bool color in colors)
    //    {
    //        float side = playAsWhite == color ? 1 : -1;

    //        /// Knight
    //        PieceList knights = board.GetPieceList(PieceType.Knight, color);
    //        foreach (Piece knight in knights)
    //        {
    //            //score += (316 + ((4f - (Math.Abs(knight.Square.Rank - 3.5f))) + (4f - (Math.Abs(knight.Square.File - 3.5f))))) * side;
    //            score += (310 - distance(knight.Square, 3.5f, 3.5f)) * side;
    //        }

    //        /// Bischop
    //        PieceList bischops = board.GetPieceList(PieceType.Bishop, color);
    //        foreach (Piece bischop in bischops)
    //        {
    //            score += (328 - distance(bischop.Square, 3.5f, 3.5f) * .1f) * side;
    //        }

    //        /// Rook
    //        int rooksCount = board.GetPieceList(PieceType.Rook, color).Count;
    //        score += rooksCount * 493 * side;

    //        /// Queen
    //        int queenCount = board.GetPieceList(PieceType.Queen, color).Count;
    //        score += queenCount * 982 * side;

    //        specialPieces[Convert.ToInt32(color)] = knights.Count + bischops.Count + rooksCount + queenCount;
    //        int lateGame = specialPieces[Convert.ToInt32(color)] >= 3 ? 1 : 0;

    //        /// King
    //        Piece king = board.GetPieceList(PieceType.King, color).First();
    //        if (lateGame == 0)
    //        {
    //            /// Early game, move to corners
    //            //score += ((color ? -king.Square.Rank : (-7 + king.Square.Rank))) * side;
    //            //score -= ((4f - (Math.Abs(king.Square.Rank - 3.5f))) + (4f - (Math.Abs(king.Square.File - 3.5f))))*side;
    //            //score += (Math.Abs(king.Square.File - 3.5f)) * side;
    //            score += distance(king.Square, 3.5f, 3.5f) * side;
    //            score += (board.HasKingsideCastleRight(color) || board.HasQueensideCastleRight(color) ? 2 : 0) * side;
    //        }
    //        else
    //        {
    //            /// Late game, move own king to center
    //            score += ((4f - (Math.Abs(king.Square.Rank - 3.5f))) + (4f - (Math.Abs(king.Square.File - 3.5f)))) * side * .1f;

    //            /// Potential improvement: move to the most back ranked pawn
    //        }


    //        /// Pawn
    //        foreach (Piece pawn in board.GetPieceList(PieceType.Pawn, color))
    //        {
    //            float rankScore = color ? pawn.Square.Rank : 7 - pawn.Square.Rank;
    //            score += (95 + ((pawn.Square.File > 1 && pawn.Square.File < 6) ? 2f : -.1f) * rankScore * (1 + rankScore * .1f)) * side;
    //            //score += (98 + rankScore * 2f) * side;
    //            //float mid = distance(pawn.Square, 3.5f, 3.5f)*5;
    //            //score += (100 + mid*mid) * side;
    //        }

    //    }

    //    /// Promote trading pieces when having pieces advantage
    //    score += (specialPieces[0] - specialPieces[1]) / (specialPieces[0] + specialPieces[1] + 1) * 20;

    //    return score;
    //}
}
