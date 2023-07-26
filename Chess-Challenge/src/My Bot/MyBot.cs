using ChessChallenge.API;
using System;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;

struct MoveEval
{
    public Move Move;
    public float Eval;

}

public class MyBot : IChessBot
{
    static int ThinkingTime;
    static float VariableNodes;
    static int maxDepth;
    static int minDepth;
    static int boardIndex;
    static float bestMoveIndex;
    static int captures;
    static Timer timer_;

    public Move Think(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining >= 59_900)
        {
            Console.WriteLine("RESET");
            boardIndex = 0;
            ThinkingTime = 0;
            captures = 0;
            VariableNodes = 120_000;
            //VariableNodes = (board.PlyCount <= 1) ? 4_000 : 5_000;
        }
        timer_ = timer;
        Console.WriteLine($"Heuristic {heuristic2(board, board.IsWhiteToMove, 0, true)}");
        VariableNodes += ThinkingTime < 1800 -Math.Min(1600, Math.Max(0, (30_000-timer.MillisecondsRemaining)/8)) ? VariableNodes * .2f : VariableNodes * -.4f;
        int nodes = 100 + (int)VariableNodes;
        Move bestMove = board.GetLegalMoves()[0];
        minDepth = 1000;
        maxDepth = -1;
        //int nodes = 100_000;
        Move nullMove = new Move();
        float score = Negamax(ref board, nodes, 0, float.MinValue, float.MaxValue , true, board.IsWhiteToMove, ref nullMove, ref bestMove);
        if (bestMove.IsCapture)
        {
            captures++;
        }
        if (board.GameMoveHistory.Length > 0 &&  board.GameMoveHistory.Last().IsCapture)
        {
            captures++;
        }
        
        board.MakeMove(bestMove);
        Console.WriteLine($"Heuristic After move {heuristic2(board, !board.IsWhiteToMove, 0, true)}");
        ThinkingTime = timer.MillisecondsElapsedThisTurn;
        String color = board.IsWhiteToMove ? "WHITE" : "BLACK";
        Console.WriteLine($"{color} think {(float)ThinkingTime/1000.0}s / {(float)timer.MillisecondsRemaining/ 1000.0}s Sort {bestMoveIndex} \tnodes {nodes} \tscore {score} Depth {minDepth} to {maxDepth} Captures {captures}"); 
        return bestMove;
    }

    static MoveEval[] GetMoveEvals(Move[] moves)
    {
        MoveEval[] moveEvals = new MoveEval[moves.Length];
        int i = 0;
        foreach (Move move in moves)
        {
            
            //moveEvals[i++] = (new MoveEval(move, (2*(int)move.CapturePieceType)+((int)move.MovePieceType)));
            moveEvals[i++] = new MoveEval { Move = move, Eval = (int)move.CapturePieceType - ((int)move.MovePieceType % 6) + (int)move.CapturePieceType * 10 };
        }
        return moveEvals;
    }

    float Negamax(ref Board board, int nodes, int depth, float a, float b, bool isPlayer, bool playAsWhite, ref Move prevMove, ref Move outBestMove)
    {
        Move[] moves = board.GetLegalMoves();
        float best = -13370000000;

        if (nodes == 0 || moves.Length == 0)
        {
            minDepth = Math.Min(minDepth, depth);
            maxDepth = Math.Max(maxDepth, depth);
            float h = heuristic2(board, playAsWhite, depth, isPlayer);
            
            return isPlayer ? h : -h;
        }
        //if (isPlayer && board.PlyCount <= 2 && depth == 0)
        //{
        //    moves = moves.Select(moves => moves).Where(move => move.MovePieceType == PieceType.Pawn).ToArray();
        //}
        MoveEval[] sortedMoveEvals = GetMoveEvals(moves).OrderByDescending(moveEval => moveEval.Eval).ToArray();

        //if (depth > 20)
        //{
        //    Console.WriteLine("DEPTH");
        //}

        int nextNodes = (int)((float)nodes / (float)(moves.Length + 1)) + ((!isPlayer) ? 0 : 1);
        
        //bool repeat = (depth == 2 && boards.Contains(board.ZobristKey));
        bool repeat = (depth == 2 && board.GameRepetitionHistory.Contains(board.ZobristKey));
        if (repeat)
        {
            Console.WriteLine($"REPEAT {isPlayer}");
        }
        int k = 0;
        foreach (MoveEval moveEval in sortedMoveEvals)
        {
            Move move = moveEval.Move;
            board.MakeMove(move);
            Move bestMove = new Move();
            int bonusNodes = 0;
            if (prevMove.IsCapture && move.IsCapture && prevMove.TargetSquare == move.TargetSquare&& timer_.MillisecondsElapsedThisTurn < 3000)
            {
                bonusNodes = 1;
            }
            //bonusNodes = (move.IsCapture && prevMove.IsCapture && depth < 10) ? 1 : 0;
            //bonusNodes = 0;
            float score = -Negamax(ref board, nextNodes + bonusNodes, depth+1, -b, -a, !isPlayer, playAsWhite, ref move, ref bestMove);
            
            score *= (repeat && score > 50) ? .5f : 1;

            board.UndoMove(move);
            k++;
            if (score > best)
            {
                if (depth == 0)bestMoveIndex = k;
                outBestMove = move;
                best = score;
            }
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
        //if (depth == 0)
        //{
        //    bestMoveIndex = 1f - (float)bestMoveIndex / (float)sortedMoveEvals.Length;
        //}
        return best;
    }

    static float distance(Square a, float rank, float file)
    {
        return Math.Abs(a.Rank - rank) + Math.Abs(a.File - file);
    }
    
    float heuristic2(Board board, bool playAsWhite, int depth, bool isPlayer)
    {
        float score = 0;
        if (board.IsInCheckmate())
        {
            score += (100 - depth) * 100_000;
            if (isPlayer) // fix for: zwart verliezend ziet geen mate
            {
                score *= -1;
            }
            // return when enemy in checkmate
        }
        int[] material = new int[2]; 
        float lategame = (captures > 16) ? 1 : 0;
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
                        pscore += 99 + rankScore  *(4-Math.Abs(p.Square.File -3.5f));
                        break;
                    case PieceType.Knight:
                        pscore += 316 - distToMiddle *.3f;
                        break;
                    case PieceType.Bishop:
                        pscore += 328 - distToMiddle + onEnKingDiagonal;
                        break;
                    case PieceType.Rook:
                        pscore += 493 + onEnKingFileOrRank;
                        break;
                    case PieceType.Queen:
                        pscore += 982 + onEnKingFileOrRank*10 + onEnKingDiagonal - rankScore;
                        break;
                    case PieceType.King:
                        pscore += distToMiddle*2 + Convert.ToUInt32(board.HasKingsideCastleRight(p.IsWhite) || board.HasQueensideCastleRight(p.IsWhite))*4;
                        break;
                }
                material[Convert.ToUInt32(pl.IsWhitePieceList)]++;
                pscore += (10 * lategame * -distanceToEnemyKing);
            }
            score += pscore * side;
        }
        if (float.IsNaN(score))
        {
            Console.WriteLine("NAN");
        }
        //Console.WriteLine($"{score}");
        score += (material[0] - material[1]) / (material[0] + material[1]);
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
