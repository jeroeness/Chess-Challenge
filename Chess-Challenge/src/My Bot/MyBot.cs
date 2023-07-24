using ChessChallenge.API;
using System;
using System.Linq;

struct MoveEval
{
    public Move Move;
    public float Eval;
    private int v;

    public MoveEval(Move move, int v) : this()
    {
        Move = move;
        this.v = v;
    }
}

public class MyBot : IChessBot
{
    static int ThinkingTime = 0;
    static float VariableNodes = 10_00;
    static ulong[] boards = new ulong[20];
    static int boardIndex = 0;

    public Move Think(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining >= 59_900)
        {
            Console.WriteLine("RESET");
            boards = new ulong[20];
            boardIndex = 0;
            ThinkingTime = 0;
            VariableNodes = board.PlyCount <= 1 ? 3000 : 3000;
            //VariableNodes = (board.PlyCount <= 1) ? 4_000 : 5_000;
        }
        
        int nodes = 0;
        VariableNodes += ThinkingTime < 1800 -Math.Min(1600, Math.Max(0, (30_000-timer.MillisecondsRemaining)/8)) ? VariableNodes * .2f : VariableNodes * -.4f;
        nodes = 100 + (int)VariableNodes;
        Move bestMove = board.GetLegalMoves()[0];
        //int nodes = 100_000;
        boards[(boardIndex++) % 20] = board.ZobristKey;
        float score = Negamax(ref board, nodes, 0, float.MinValue, float.MaxValue , true, board.IsWhiteToMove, ref bestMove);
        ThinkingTime = timer.MillisecondsElapsedThisTurn;
        String color = board.IsWhiteToMove ? "WHITE" : "BLACK";
        Console.WriteLine($"{color} think {(float)ThinkingTime/1000.0}s \tnodes {nodes} \tscore {score}");
        return bestMove;
    }

    private static MoveEval[] GetMoveEvals(Move[] moves)
    {
        MoveEval[] moveEvals = new MoveEval[moves.Length];
        int i = 0;
        foreach (Move move in moves)
        {
            
            moveEvals[i++] = (new MoveEval(move, (2*(int)move.CapturePieceType)+((int)move.MovePieceType)));
        }
        return moveEvals;
    }

    private float Negamax(ref Board board, int nodes, int depth, float a, float b, bool isPlayer, bool playAsWhite, ref Move outBestMove)
    {
        Move[] moves = board.GetLegalMoves();
        float best = float.MinValue;

        if (nodes == 0 || moves.Length == 0)
        {
            float h = heuristic(board, playAsWhite, depth, isPlayer);
            return isPlayer ? h : -h;
        }
        if (isPlayer && board.PlyCount <= 2 && depth == 0)
        {
            moves = moves.Select(moves => moves).Where(move => move.MovePieceType == PieceType.Pawn).ToArray();
        }
        MoveEval[] sortedMoveEvals = GetMoveEvals(moves).OrderByDescending(moveEval => moveEval.Eval).ToArray();

        if (depth > 30)
        {
            Console.WriteLine("DEPTH");
        }

        int nextNodes = (int)((float)nodes / (float)(moves.Length + 1)) + ((!isPlayer) ? 0 : 1);
        bool repeat = (depth == 2 && boards.Contains(board.ZobristKey));
        if (depth == 2 && boards.Contains(board.ZobristKey))
        {
            Console.WriteLine($"REPEAT {isPlayer}");
        }
        foreach (MoveEval moveEval in sortedMoveEvals)
        {
            Move move = moveEval.Move;
            board.MakeMove(move);
            Move bestMove = new Move();
            float score = -Negamax(ref board, nextNodes, depth+1, -b, -a, !isPlayer, playAsWhite, ref bestMove);

            if (repeat && score > 10)
            {
                score *= .5f;
            }

            board.UndoMove(move);
            if (score > best)
            {
                outBestMove = move;
                best = score;
            }
            if (best > a)
            {
                a = best;
            }
            if (a >= b)
            {
                break;
            }
        }
        if (depth <= 2)
        {
            //String color = playAsWhite ? "WHITE" : "BLACK";
            //Console.WriteLine($"{color} {depth} {outBestMove} {best} {a} {b} {isPlayer}");
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
        foreach (bool color in colors) {
            float side = playAsWhite == color ? 1 : -1;

            /// Pawn
            foreach (Piece pawn in board.GetPieceList(PieceType.Pawn, color))
            {
                float rankScore = color ? pawn.Square.Rank : 7 - pawn.Square.Rank;
                score += (97 + (pawn.Square.File > 2 && pawn.Square.File < 5 ? 3f : 2f) *
                     rankScore * rankScore) * side;
            }

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

            /// King
            Piece king = board.GetPieceList(PieceType.King, color).First();
            if (specialPieces[Convert.ToInt32(color)] >= 3)
            {
                /// Early game, move to corners
                //score += ((color ? -king.Square.Rank : (-7 + king.Square.Rank))) * side;
                score -= ((4f - (Math.Abs(king.Square.Rank - 3.5f))) + (4f - (Math.Abs(king.Square.File - 3.5f))))*side;
                score += (Math.Abs(king.Square.File - 3.5f)) * side;
                score += (board.HasKingsideCastleRight(color) || board.HasQueensideCastleRight(color) ? 3 : 0)*side;
            } else
            {
                /// Late game, move own king to center
                score += ((4f - (Math.Abs(king.Square.Rank - 3.5f))) + (4f - (Math.Abs(king.Square.File - 3.5f)))) * side * .1f;

                /// Potential improvement: move to the most back ranked pawn
            }
        }

        /// Promote trading pieces when having pieces advantage
        score += (specialPieces[0] - specialPieces[1]) / (specialPieces[0] + specialPieces[1]) * 50;
        return score;
    }
}