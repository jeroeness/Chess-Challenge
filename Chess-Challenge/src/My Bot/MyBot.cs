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
    static float VariableNodes = 10_000;

    public Move Think(Board board, Timer timer)
    {
        Console.WriteLine(timer.MillisecondsRemaining);
        if (timer.MillisecondsRemaining >= 59_900)
        {
            ThinkingTime = 0;
            VariableNodes = (board.PlyCount <= 1) ? 400_000 : 500_000;
        }
        
        int nodes = 0;
        VariableNodes += ThinkingTime < 2200 -Math.Min(2000, Math.Max(0, (30_000-timer.MillisecondsRemaining)/8)) ? VariableNodes * .08f : VariableNodes * -.2f;
        nodes = 1_000 + (int)VariableNodes;
        Move bestMove = board.GetLegalMoves()[0];
        //int nodes = 100_000;
        float score = Negamax(ref board, nodes, 0, float.MinValue, float.MaxValue , true, board.IsWhiteToMove, ref bestMove);
        ThinkingTime = timer.MillisecondsElapsedThisTurn;
        Console.WriteLine($"thinktime {(float)ThinkingTime/1000.0} nodes {nodes} score {score} Target think time {2200 - Math.Min(2000, Math.Max(0, (30_000 - timer.MillisecondsRemaining) / 8))}");
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
            float h = heuristic(board, playAsWhite, depth);
            return isPlayer ? h : -h;
        }
        MoveEval[] sortedMoveEvals = GetMoveEvals(moves).OrderByDescending(moveEval => moveEval.Eval).ToArray();


        int nextNodes =(int)((float)nodes / (float)moves.Length);
        foreach (MoveEval moveEval in sortedMoveEvals)
        {
            Move move = moveEval.Move;
            board.MakeMove(move);
            Move bestMove = new Move();
            float score = -Negamax(ref board, nextNodes, depth+1, -b, -a, !isPlayer, playAsWhite, ref bestMove);
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
        if (depth == 1)
        {
            //Console.WriteLine($"{outBestMove} {best} {isPlayer}");
        }
        return best;
    }

    private float heuristic(Board board, bool playAsWhite, int depth)
    {
       
        float score = 0; 
        if (board.IsInCheckmate())
        {
            score += (100-depth)*100_000;
        }
        bool[] colors = new bool[2] { playAsWhite, !playAsWhite };
        foreach (bool color in colors) {
            float side = playAsWhite == color ? 1 : -1;

            /// Pawn
            foreach (Piece pawn in board.GetPieceList(PieceType.Pawn, color))
            {
                score += (100 + (pawn.Square.File > 2 && pawn.Square.File < 5 ? 2f : 1f) *
                    (color ? pawn.Square.Rank : 7 - pawn.Square.Rank)) * side;
            }

            /// Knight
            foreach (Piece knight in board.GetPieceList(PieceType.Knight, color))
            {
                score += (316 + ((4f - (Math.Abs(knight.Square.Rank - 3.5f))) + (4f - (Math.Abs(knight.Square.File - 3.5f))))) * side;
            }

            /// King
            Piece king = board.GetPieceList(PieceType.King, color).First();
            
            score += ((color ? -king.Square.Rank : (-7 + king.Square.Rank))) * side;
            score += (Math.Abs(king.Square.File - 3.5f)) * side;
            score += board.HasKingsideCastleRight(color) || board.HasQueensideCastleRight(color) ? 3 : 0;
            

            /// Bischop
            foreach (Piece bischop in board.GetPieceList(PieceType.Bishop, color))
            {
                score += (328 + ((4f - (Math.Abs(bischop.Square.Rank - 3.5f)))*.5f + .5f*(4f - (Math.Abs(bischop.Square.File - 3.5f))))) * side;
            }

            /// Rook
            score += board.GetPieceList(PieceType.Rook, color).Count * 493 * side;

            /// Queen
            score += board.GetPieceList(PieceType.Queen, color).Count * 982 * side;



        }
        //float score =
        //board.GetPieceList(PieceType.Pawn, playAsWhite).Count +
        //board.GetPieceList(PieceType.Knight, playAsWhite).Count * 300 +
        //board.GetPieceList(PieceType.Bishop, playAsWhite).Count * 300 +
        //board.GetPieceList(PieceType.Rook, playAsWhite).Count * 500 +
        //board.GetPieceList(PieceType.Queen, playAsWhite).Count * 900 +
        //board.GetPieceList(PieceType.King, playAsWhite).Count * 90000
        //- (
        //board.GetPieceList(PieceType.Pawn, !playAsWhite).Count +
        //board.GetPieceList(PieceType.Knight, !playAsWhite).Count * 300 +
        //board.GetPieceList(PieceType.Bishop, !playAsWhite).Count * 300 +
        //board.GetPieceList(PieceType.Rook, !playAsWhite).Count * 500 +
        //board.GetPieceList(PieceType.Queen, !playAsWhite).Count * 900 +
        //board.GetPieceList(PieceType.King, !playAsWhite).Count * 90000);
        return score;
    }
}