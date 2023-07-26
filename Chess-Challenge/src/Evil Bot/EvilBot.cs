using ChessChallenge.API;
using System;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;


public class EvilBot : IChessBot
{
    static int ThinkingTime = 0;
    static float VariableNodes = 10_00;
    static ulong[] boards = new ulong[20];
    static int maxDepth = -1;
    static int minDepth = 100;
    static int boardIndex = 0;
    static float bestMoveIndex = 0;
    static Timer timer_;

    public Move Think(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining >= 59_900)
        {
            Console.WriteLine("RESET");
            boards = new ulong[20];
            boardIndex = 0;
            ThinkingTime = 0;
            VariableNodes = board.PlyCount <= 1 ? 120_000 : 120_000;
            //VariableNodes = (board.PlyCount <= 1) ? 4_000 : 5_000;
        }
        timer_ = timer;
        int nodes = 0;
        VariableNodes += ThinkingTime < 800 - Math.Min(600, Math.Max(0, (30_000 - timer.MillisecondsRemaining) / 8)) ? VariableNodes * .2f : VariableNodes * -.4f;
        nodes = 100 + (int)VariableNodes;
        Move bestMove = board.GetLegalMoves()[0];
        minDepth = 1000;
        maxDepth = -1;
        //int nodes = 100_000;
        boards[(boardIndex++) % 20] = board.ZobristKey;
        Move nullMove = new Move();
        float score = Negamax(ref board, nodes, 0, float.MinValue, float.MaxValue, true, board.IsWhiteToMove, ref nullMove, ref bestMove);

        ThinkingTime = timer.MillisecondsElapsedThisTurn;
        //String color = board.IsWhiteToMove ? "WHITE" : "BLACK";
        //Console.WriteLine($"{color} think {(float)ThinkingTime / 1000.0}s / {(float)timer.MillisecondsRemaining / 1000.0}s Sort {bestMoveIndex} \tnodes {nodes} \tscore {score} Depth {minDepth} to {maxDepth}");
        return bestMove;
    }

    private static MoveEval[] GetMoveEvals(Move[] moves)
    {
        MoveEval[] moveEvals = new MoveEval[moves.Length];
        int i = 0;
        foreach (Move move in moves)
        {

            //moveEvals[i++] = (new MoveEval(move, (2*(int)move.CapturePieceType)+((int)move.MovePieceType)));
            moveEvals[i++] = (new MoveEval(move, (int)move.CapturePieceType - ((int)move.MovePieceType % 6) + (int)move.CapturePieceType * 10));
        }
        return moveEvals;
    }

    private float Negamax(ref Board board, int nodes, int depth, float a, float b, bool isPlayer, bool playAsWhite, ref Move prevMove, ref Move outBestMove)
    {
        Move[] moves = board.GetLegalMoves();
        float best = -13370000000;

        if (nodes == 0 || moves.Length == 0)
        {
            minDepth = Math.Min(minDepth, depth);
            maxDepth = Math.Max(maxDepth, depth);
            float h = heuristic(board, playAsWhite, depth, isPlayer);

            return isPlayer ? h : -h;
        }
        if (isPlayer && board.PlyCount <= 2 && depth == 0)
        {
            moves = moves.Select(moves => moves).Where(move => move.MovePieceType == PieceType.Pawn).ToArray();
        }
        MoveEval[] sortedMoveEvals = GetMoveEvals(moves).OrderByDescending(moveEval => moveEval.Eval).ToArray();

        //if (depth > 20)
        //{
        //    Console.WriteLine("DEPTH");
        //}

        int nextNodes = (int)((float)nodes / (float)(moves.Length + 1)) + ((!isPlayer) ? 0 : 1);
        int bonusNodes = 0;
        bool repeat = (depth == 2 && boards.Contains(board.ZobristKey));
        //if (depth == 2 && boards.Contains(board.ZobristKey))
        //{
        //    Console.WriteLine($"REPEAT {isPlayer}");
        //}
        int k = 0;
        foreach (MoveEval moveEval in sortedMoveEvals)
        {
            Move move = moveEval.Move;
            board.MakeMove(move);
            Move bestMove = new Move();
            if (prevMove.IsCapture && move.IsCapture && /*(int)move.CapturePieceType >= (int)move.MovePieceType-1 &&*/ prevMove.TargetSquare == move.TargetSquare && timer_.MillisecondsElapsedThisTurn < 3000)
            {
                bonusNodes = 1;
            }
            else { bonusNodes = 0; }
            //bonusNodes = (move.IsCapture && prevMove.IsCapture && depth < 10) ? 1 : 0;
            //bonusNodes = 0;
            float score = -Negamax(ref board, nextNodes + bonusNodes, depth + 1, -b, -a, !isPlayer, playAsWhite, ref move, ref bestMove);

            score *= (repeat && score > 10) ? .5f : 1;

            board.UndoMove(move);
            k++;
            if (score > best)
            {
                if (depth == 0) bestMoveIndex = k;
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
        if (depth == 0)
        {
            bestMoveIndex = 1f - (float)bestMoveIndex / (float)sortedMoveEvals.Length;
        }
        return best;
    }

    private float heuristic(Board board, bool playAsWhite, int depth, bool isPlayer)
    {
        if (!isPlayer)
        {
            // must be check mate for enemy player
            //Console.WriteLine("NOT PLAYER!!!!!!!!!!!!!!!!!!!!!!");
        }

        float score = 0;
        if (board.IsInCheckmate())
        {

            if (!playAsWhite)
            {
                score += (100 - depth) * 100_000;
                if (isPlayer) // fix for: zwart verliezend ziet geen mate
                {
                    score *= -1;
                }
            }
            else
            {
                score += (100 - depth) * 100_000;
                if (playAsWhite == isPlayer)
                {
                    score *= -1;
                }
            }
        }
        bool[] colors = new bool[2] { playAsWhite, !playAsWhite };
        float[] specialPieces = new float[2];


        //int lateGame = specialPieces[true] + specialPieces[false] <= 6 ? 1 : 0;
        foreach (bool color in colors)
        {
            float side = playAsWhite == color ? 1 : -1;

            /// Knight
            PieceList knights = board.GetPieceList(PieceType.Knight, color);
            foreach (Piece knight in knights)
            {
                score += (316 + ((4f - (Math.Abs(knight.Square.Rank - 3.5f))) + (4f - (Math.Abs(knight.Square.File - 3.5f))))) * side;
            }

            /// Bischop
            PieceList bischops = board.GetPieceList(PieceType.Bishop, color);
            foreach (Piece bischop in bischops)
            {
                score += (328 + ((4f - (Math.Abs(bischop.Square.Rank - 3.5f))) + (4f - (Math.Abs(bischop.Square.File - 3.5f))))) * side;
            }

            /// Rook
            int rooksCount = board.GetPieceList(PieceType.Rook, color).Count;
            score += rooksCount * 493 * side;

            /// Queen
            int queenCount = board.GetPieceList(PieceType.Queen, color).Count;
            score += queenCount * 982 * side;

            specialPieces[Convert.ToInt32(color)] = knights.Count + bischops.Count + rooksCount + queenCount;
            int lateGame = specialPieces[Convert.ToInt32(color)] >= 3 ? 1 : 0;

            /// King
            Piece king = board.GetPieceList(PieceType.King, color).First();
            if (lateGame == 0)
            {
                /// Early game, move to corners
                //score += ((color ? -king.Square.Rank : (-7 + king.Square.Rank))) * side;
                score -= ((4f - (Math.Abs(king.Square.Rank - 3.5f))) + (4f - (Math.Abs(king.Square.File - 3.5f)))) * side;
                score += (Math.Abs(king.Square.File - 3.5f)) * side;
                score += (board.HasKingsideCastleRight(color) || board.HasQueensideCastleRight(color) ? 3 : 0) * side;
            }
            else
            {
                /// Late game, move own king to center
                score += ((4f - (Math.Abs(king.Square.Rank - 3.5f))) + (4f - (Math.Abs(king.Square.File - 3.5f)))) * side * .1f;

                /// Potential improvement: move to the most back ranked pawn
            }


            /// Pawn
            foreach (Piece pawn in board.GetPieceList(PieceType.Pawn, color))
            {
                float rankScore = color ? pawn.Square.Rank : 7 - pawn.Square.Rank;
                //score += (95 + ((pawn.Square.File > 2 && pawn.Square.File < 5) ? 1f : 0f) * rankScore * 5f) * side;
                score += (98 + rankScore * rankScore * 2f) * side;
            }

        }

        /// Promote trading pieces when having pieces advantage
        score += (specialPieces[0] - specialPieces[1]) / (specialPieces[0] + specialPieces[1] + 1) * 50;

        return score;
    }
}