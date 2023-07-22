using ChessChallenge.API;
using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;

public class MyBot : IChessBot
{

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = new Move();
        float score = negamax(ref board, 100_000, 0, new float[2] { -float.MaxValue, -float.MaxValue }, true, board.IsWhiteToMove, ref bestMove);
        return bestMove;
    }


    private float negamax(ref Board board, int nodes, int depth, float[] ab, bool isPlayer, bool playAsWhite, ref Move outBestMove)
    {
        Move[] moves = board.GetLegalMoves();
        float best = float.MinValue;

        if (nodes == 0 || moves.Length == 0)
        {
            float h = heuristic(board, playAsWhite);
            return isPlayer ? h : -h;
        }

        int nextNodes =(int)((float)nodes / (float)moves.Length);
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            Move bestMove = new Move();
            float score = -negamax(ref board, nextNodes, depth+1, ab, !isPlayer, playAsWhite, ref bestMove);
            board.UndoMove(move);
            if (score > best)
            {
                outBestMove = move;
                best = score;
            }
            if (best > ab[isPlayer ? 1 : 0])
            {
                ab[isPlayer ? 1: 0] = best;
            }
        }
        return best;
    }

    private float heuristic(Board board, bool playAsWhite)
    {
        float score =
        board.GetPieceList(PieceType.Pawn, playAsWhite).Count +
        board.GetPieceList(PieceType.Knight, playAsWhite).Count * 300 +
        board.GetPieceList(PieceType.Bishop, playAsWhite).Count * 300 +
        board.GetPieceList(PieceType.Rook, playAsWhite).Count * 500 +
        board.GetPieceList(PieceType.Queen, playAsWhite).Count * 900 +
        board.GetPieceList(PieceType.King, playAsWhite).Count * 90000
        - (
        board.GetPieceList(PieceType.Pawn, !playAsWhite).Count +
        board.GetPieceList(PieceType.Knight, !playAsWhite).Count * 300 +
        board.GetPieceList(PieceType.Bishop, !playAsWhite).Count * 300 +
        board.GetPieceList(PieceType.Rook, !playAsWhite).Count * 500 +
        board.GetPieceList(PieceType.Queen, !playAsWhite).Count * 900 +
        board.GetPieceList(PieceType.King, !playAsWhite).Count * 90000);
        return score;
    }
}