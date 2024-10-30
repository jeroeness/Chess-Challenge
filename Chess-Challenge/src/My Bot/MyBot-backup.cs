//using ChessChallenge.API;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http.Headers;
//using static System.Formats.Asn1.AsnWriter;

//struct MoveEval
//{
//    public Move Move;
//    public float Eval;

//}

//struct TableEntry
//{
//    public ulong Zobristkey;
//    public float Score;

//}
//public class MyBot : ChessBot
//{
//    static int ThinkingTime;
//    static float VariableNodes;
//    static int maxDepth;
//    static int minDepth;
//    static int boardIndex;
//    static int cyclesHit;
//    static float bestMoveIndex;
//    static float alpha;
//    static float beta;
//    static Timer timer_;
//    static int nodeCounter;
//    //static Dictionary<ulong, float> boardHeuristics = new Dictionary<ulong, float>();
//    //static TableEntry[] boardHeuristics;
//    static TableEntry[] cycleTable;


//    public Move Think(Board board, Timer timer)
//    {
//        if (timer.MillisecondsRemaining >= 59_900)
//        {
//            //Console.WriteLine("RESET");
//            boardIndex = 0;
//            ThinkingTime = 0;
//            VariableNodes = 2_000;
//            //boardHeuristics = new TableEntry[4_000_000];
//            alpha = -100000;
//            beta = 100000;
//            //VariableNodes = (board.PlyCount <= 1) ? 4_000 : 5_000;
//        }
//        //cycleTable = new TableEntry[1_000_000];
//        cyclesHit = 0;
//        nodeCounter = 0;
//        timer_ = timer;
//        //Console.WriteLine($"Heuristic {heuristic2(board, board.IsWhiteToMove, 0, true)}");
//        VariableNodes += ThinkingTime < 1800 - Math.Min(1600, Math.Max(0, (30_000 - timer.MillisecondsRemaining) / 8)) ? VariableNodes * .2f : VariableNodes * -.4f;
//        int nodes = 100 + (int)VariableNodes;
//        Move bestMove = board.GetLegalMoves()[0];
//        minDepth = 1000;
//        maxDepth = -1;
//        //int nodes = 100_000;
//        Move nullMove = new Move();
//        float score = Negamax(ref board, nodes, 0, float.MinValue, float.MaxValue, true, board.IsWhiteToMove, ref nullMove, ref bestMove);
//        //float score = pvs(ref board, nodes, 0, float.MinValue, float.MaxValue, true, board.IsWhiteToMove, ref bestMove);


//        //board.MakeMove(bestMove);
//        //Console.WriteLine($"Heuristic After move {heuristic2(board, !board.IsWhiteToMove, 0, true)}");
//        ThinkingTime = timer.MillisecondsElapsedThisTurn;
//        String color = board.IsWhiteToMove ? "WHITE" : "BLACK";
//        Console.WriteLine($"{color} think {(float)ThinkingTime / 1000.0}s / {(float)timer.MillisecondsRemaining / 1000.0}s Sort {bestMoveIndex} \tnodes {nodeCounter} \tscore {Math.Round(score)} Depth {minDepth} to {maxDepth} cycles {cyclesHit} {bestMove}");
//        return bestMove;
//    }

//    static MoveEval[] GetMoveEvals(Move[] moves, Board board)
//    {
//        MoveEval[] moveEvals = new MoveEval[moves.Length];
//        int i = 0;
//        foreach (Move move in moves)
//        {
//            //board.MakeMove(move);
//            // query the heuristic table
//            //TableEntry tableEntry = boardHeuristics[board.ZobristKey % (ulong)boardHeuristics.Length];
//            //float tt_score = tableEntry.Zobristkey == board.ZobristKey ? tableEntry.Score : -10_000; // Also for negative gamestate we prioritize TT moves
//            //board.UndoMove(move);
//            //float heuristic = 0;
//            //if (boardHeuristics.TryGetValue(board.ZobristKey, out heuristic))
//            //{

//            //moveEvals[i++] = (new MoveEval(move, (2*(int)move.CapturePieceType)+((int)move.MovePieceType)));
//            moveEvals[i++] = new MoveEval { Move = move, Eval = /*tt_score*/ +(int)move.CapturePieceType - ((int)move.MovePieceType % 6) + (int)move.CapturePieceType * 10 };
//        }
//        return moveEvals;
//    }

//    float pvs(ref Board board, int nodes, int depth, float a, float b, bool isPlayer, bool playAsWhite, ref Move outBestMove)
//    {
//        Move[] moves = board.GetLegalMoves();
//        float best = a;

//        if (nodes == 0 || moves.Length == 0)
//        {
//            minDepth = Math.Min(minDepth, depth);
//            maxDepth = Math.Max(maxDepth, depth);
//            float h = heuristic2(board, playAsWhite, depth, isPlayer);
//            nodeCounter++;
//            return isPlayer ? h : -h;
//        }
//        MoveEval[] sortedMoveEvals = GetMoveEvals(moves, board).OrderByDescending(moveEval => moveEval.Eval).ToArray();
//        int nextNodes = (int)((float)nodes / (float)(moves.Length + 1));
//        bool repeat = depth == 2 && board.GameRepetitionHistory.Contains(board.ZobristKey);

//        Move bestMove = new Move();

//        float score = 0;
//        for (int i = 0; i < sortedMoveEvals.Length;)
//        {

//            Move move = sortedMoveEvals[i++].Move;
//            board.MakeMove(move);
//            if (i == 1)
//            {
//                score = -pvs(ref board, nextNodes, depth + 1, -b, -a, !isPlayer, playAsWhite, ref bestMove);
//            }
//            else
//            {

//                score = -pvs(ref board, nextNodes, depth + 1, -a - 1, -a, !isPlayer, playAsWhite, ref bestMove);
//                if (a < score && score < b)
//                {
//                    if (depth == 0)
//                    {
//                        Console.WriteLine($"a < score < b => {a} < {score} < {b}");
//                    }
//                    score = -pvs(ref board, nextNodes, depth + 1, -b, -score, !isPlayer, playAsWhite, ref bestMove);
//                }

//            }
//            if (depth == 0)
//            {
//                Console.WriteLine($"Move {move} score {score}");
//            }
//            board.UndoMove(move);
//            if (score > best)
//            {
//                best = score;
//                outBestMove = move;
//            }
//            if (best >= b)
//            {
//                break;
//            }

//        }
//        return best;
//    }

//    float Negamax(ref Board board, int nodes, int depth, float a, float b, bool isPlayer, bool playAsWhite, ref Move prevMove, ref Move outBestMove)
//    {
//        //TableEntry tableEntry = cycleTable[board.ZobristKey % (ulong)cycleTable.Length];
//        //if (tableEntry.Zobristkey == board.ZobristKey)
//        //{
//        //    cyclesHit++;
//        //    return tableEntry.Score;
//        //}
//        Move[] moves = board.GetLegalMoves(/*depth > 5*/);
//        float best = -13370000000;

//        if (nodes == 0 || moves.Length == 0 || depth >= 7)
//        {
//            minDepth = Math.Min(minDepth, depth);
//            maxDepth = Math.Max(maxDepth, depth);
//            float h = heuristic_jacques(board);
//            nodeCounter++;
//            return h;
//            //return isPlayer ? h : -h;
//        }
//        //if (isPlayer && board.PlyCount <= 2 && depth == 0)
//        //{
//        //    moves = moves.Select(moves => moves).Where(move => move.MovePieceType == PieceType.Pawn).ToArray();
//        //}
//        MoveEval[] sortedMoveEvals = GetMoveEvals(moves, board).OrderByDescending(moveEval => moveEval.Eval).ToArray();

//        //if (depth > 30)
//        //{
//        //    Console.WriteLine("DEPTH");
//        //}

//        int nextNodes = (int)((float)nodes / (float)(moves.Length + 1)) /*+ ((!isPlayer) ? 0 : 1)*/;

//        bool repeat = depth == 2 && board.GameRepetitionHistory.Contains(board.ZobristKey);

//        int k = 0;
//        foreach (MoveEval moveEval in sortedMoveEvals)
//        {
//            Move move = moveEval.Move;
//            board.MakeMove(move);
//            Move bestMove = new Move();
//            int bonusNodes = 0;
//            //if (prevMove.IsCapture && move.IsCapture && prevMove.TargetSquare == move.TargetSquare && timer_.MillisecondsElapsedThisTurn < 3000)
//            //{
//            //    bonusNodes = 1;
//            //}
//            bonusNodes = (depth % 2 == 1) ? 2 : 0;
//            if (/* timer_.MillisecondsRemaining > 20_000 &&*/ (move.IsCapture || board.IsInCheck()) /* && (int)move.CapturePieceType + 1 >= (int)move.MovePieceType % 6 */ /* && timer_.MillisecondsElapsedThisTurn < 3000*/)
//            {
//                bonusNodes = 1;
//            }
//            //if (depth < 3)
//            //{
//            //    bonusNodes = 2;
//            //}
//            //if (depth >=5)
//            //{
//            //    bonusNodes = 0;
//            //}
//            //bonusNodes = (move.IsCapture && prevMove.IsCapture && depth < 10) ? 1 : 0;
//            //bonusNodes = 0;
//            float score = -Negamax(ref board, nextNodes + bonusNodes, depth + 1, -b, -a, !isPlayer, playAsWhite, ref move, ref bestMove);

//            score *= (repeat && score > 100) ? .5f : 1;

//            k++;
//            if (score > best)
//            {
//                if (depth == 0) bestMoveIndex = k;

//                //boardHeuristics[board.ZobristKey % (ulong)boardHeuristics.Length].Zobristkey = board.ZobristKey;
//                //boardHeuristics[board.ZobristKey % (ulong)boardHeuristics.Length].Score = score;
//                outBestMove = move;
//                best = score;
//            }
//            board.UndoMove(move);
//            if (best > a)
//            {
//                a = best;
//            }
//            //if (depth <= 0)
//            //{
//            //    String color = playAsWhite ? "WHITE" : "BLACK";
//            //    Console.WriteLine($"{color} {move}:{score} {depth} {outBestMove}:{best} {a}~{b} {isPlayer}");
//            //}
//            if (a >= b)
//            {
//                break;
//            }
//        }
//        if (depth == 0)
//        {
//            //bestMoveIndex = 1f - (float)bestMoveIndex / (float)sortedMoveEvals.Length;
//            alpha = a;
//        }
//        if (depth == 1)
//        {
//            beta = Math.Max(a, beta);

//        }
//        //cycleTable[board.ZobristKey % (ulong)cycleTable.Length] = new TableEntry { Zobristkey = board.ZobristKey, Score = best };
//        return best * .99f;
//    }

//    static float distance(Square a, float rank, float file)
//    {
//        return Math.Min(6, (Math.Abs(a.Rank - rank) + Math.Abs(a.File - file)) - 3.5f) * 5;//1 to 6 -> -2.5 to 2.5
//    }

//    //float heuristic3(Board board, bool playAsWhite, int depth, bool isPlayer)
//    //{
//    //    float score = 0;
//    //    if (board.IsInCheckmate())
//    //    {
//    //        score += (100 - depth) * 100_000;
//    //        if (isPlayer) // fix for: zwart verliezend ziet geen mate
//    //        {
//    //            score *= -1;
//    //        }
//    //        return score;
//    //    }

//    //}

//    int[] pieceVal = { 0, 100, 310, 330, 500, 1000, 10000 };
//    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
//    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };


//    public int getPstVal(int psq)
//    {
//        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
//    }

//    public int heuristic_jacques(Board board)
//    {
//        int mg = 0, eg = 0, phase = 0;

//        foreach (bool stm in new[] { true, false })
//        {
//            for (var p = PieceType.Pawn; p <= PieceType.King; p++)
//            {
//                int piece = (int)p, ind;
//                ulong mask = board.GetPieceBitboard(p, stm);
//                while (mask != 0)
//                {
//                    phase += piecePhase[piece];
//                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
//                    mg += getPstVal(ind) + pieceVal[piece];
//                    eg += getPstVal(ind + 64) + pieceVal[piece];
//                }
//            }

//            mg = -mg;
//            eg = -eg;
//        }

//        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
//    }

//    float heuristic2(Board board, bool playAsWhite, int depth, bool isPlayer)
//    {
//        float score = 0;
//        if (board.IsInCheckmate())
//        {
//            score += (100 - depth) * 100_000;
//            if (isPlayer) // fix for: zwart verliezend ziet geen mate
//            {
//                score *= -1;
//            }
//            //else
//            //{
//            //    Console.WriteLine("CHECKMATE");
//            //}
//            return score;
//            // return when enemy in checkmate
//        }
//        // TODO if in stalemate, return 0;

//        int captures = board.GameMoveHistory.Where(move => move.IsCapture).ToArray().Length;
//        float lategame = Math.Min(1, Math.Max(captures - 16f, 0) / 10f);


//        int[] material = new int[2];
//        foreach (PieceList pl in board.GetAllPieceLists())
//        {
//            foreach (Piece p in pl)
//            {
//                material[Convert.ToUInt32(pl.IsWhitePieceList)]++;
//            }
//        }
//        float materialAdvantage = (material[0] - material[1]) / (material[0] + material[1]);
//        foreach (PieceList pl in board.GetAllPieceLists())
//        {
//            float pscore = 0;
//            float side = playAsWhite == pl.IsWhitePieceList ? 1 : -1;
//            Square enKingSq = board.GetKingSquare(!pl.IsWhitePieceList);
//            //Square myKingSq = board.GetKingSquare(pl.IsWhitePieceList);

//            foreach (Piece p in pl)
//            {
//                float distToMiddle = distance(p.Square, 3.5f, 3.5f);
//                float onEnKingFileOrRank = (p.Square.File == enKingSq.File || p.Square.Rank == enKingSq.Rank) ? 1 : 0;
//                float onEnKingDiagonal = (Math.Abs(p.Square.File - enKingSq.File) == Math.Abs(p.Square.Rank - enKingSq.Rank)) ? 1 : 0;
//                float rankScore = (pl.IsWhitePieceList) ? p.Square.Rank : 7 - p.Square.Rank;
//                float distanceToEnemyKing = distance(p.Square, enKingSq.Rank, enKingSq.File);
//                switch (p.PieceType)
//                {
//                    case PieceType.Pawn:
//                        pscore += 99 + rankScore * (1 + rankScore * .5f) * (4 - Math.Abs(p.Square.File - 3.5f)) * 3 + rankScore * 5 * lategame;
//                        break;
//                    case PieceType.Knight:
//                        pscore += 316 - distToMiddle;
//                        break;
//                    case PieceType.Bishop:
//                        pscore += 328 /*- distToMiddle + onEnKingDiagonal*/;
//                        break;
//                    case PieceType.Rook:
//                        pscore += 493 + onEnKingFileOrRank - distToMiddle;
//                        break;
//                    case PieceType.Queen:
//                        pscore += 982 + onEnKingFileOrRank * 5 + onEnKingDiagonal - rankScore * 2;
//                        break;
//                    case PieceType.King:
//                        pscore += (distToMiddle * 2 - rankScore * ((1 - lategame) * 2 - 1)) + Convert.ToUInt32(board.HasKingsideCastleRight(p.IsWhite) || board.HasQueensideCastleRight(p.IsWhite)) * 4;
//                        break;
//                }
//                pscore += (5 * lategame * -distanceToEnemyKing * materialAdvantage * side);
//            }
//            score += pscore * side;
//        }

//        //Console.WriteLine($"{score}");
//        score += materialAdvantage;

//        // Count moves available
//        board.ForceSkipTurn();
//        float testscore = 0;
//        if (playAsWhite)
//        {
//            testscore = heuristic2(board, !playAsWhite, depth, isPlayer);
//        }
//        score -= board.GetLegalMoves().Length * .1f; // Todo this is slow, use non alloc
//        //score += board.IsInCheck() ? 50 : 0;
//        board.UndoSkipTurn();
//        score += board.GetLegalMoves().Length * .1f; // Todo this is slow, use non alloc
//        //score -= board.IsInCheck() ? 50 : 0;
//        if (playAsWhite && -testscore - score > .001)
//        {
//            Console.WriteLine($"{score} {testscore}");
//        }

//        return score;
//    }

//    //private float heuristic(Board board, bool playAsWhite, int depth, bool isPlayer)
//    //{
//    //    float score = 0;
//    //    if (board.IsInCheckmate())
//    //    {
//    //        score += (100 - depth) * 100_000;
//    //        if (isPlayer) // fix for: zwart verliezend ziet geen mate
//    //        {
//    //            score *= -1;
//    //        }
//    //    }
//    //    bool[] colors = new bool[2] { playAsWhite, !playAsWhite };
//    //    float[] specialPieces = new float[2];


//    //    //int lateGame = specialPieces[true] + specialPieces[false] <= 6 ? 1 : 0;
//    //    foreach (bool color in colors)
//    //    {
//    //        float side = playAsWhite == color ? 1 : -1;

//    //        /// Knight
//    //        PieceList knights = board.GetPieceList(PieceType.Knight, color);
//    //        foreach (Piece knight in knights)
//    //        {
//    //            //score += (316 + ((4f - (Math.Abs(knight.Square.Rank - 3.5f))) + (4f - (Math.Abs(knight.Square.File - 3.5f))))) * side;
//    //            score += (310 - distance(knight.Square, 3.5f, 3.5f)) * side;
//    //        }

//    //        /// Bischop
//    //        PieceList bischops = board.GetPieceList(PieceType.Bishop, color);
//    //        foreach (Piece bischop in bischops)
//    //        {
//    //            score += (328 - distance(bischop.Square, 3.5f, 3.5f) * .1f) * side;
//    //        }

//    //        /// Rook
//    //        int rooksCount = board.GetPieceList(PieceType.Rook, color).Count;
//    //        score += rooksCount * 493 * side;

//    //        /// Queen
//    //        int queenCount = board.GetPieceList(PieceType.Queen, color).Count;
//    //        score += queenCount * 982 * side;

//    //        specialPieces[Convert.ToInt32(color)] = knights.Count + bischops.Count + rooksCount + queenCount;
//    //        int lateGame = specialPieces[Convert.ToInt32(color)] >= 3 ? 1 : 0;

//    //        /// King
//    //        Piece king = board.GetPieceList(PieceType.King, color).First();
//    //        if (lateGame == 0)
//    //        {
//    //            /// Early game, move to corners
//    //            //score += ((color ? -king.Square.Rank : (-7 + king.Square.Rank))) * side;
//    //            //score -= ((4f - (Math.Abs(king.Square.Rank - 3.5f))) + (4f - (Math.Abs(king.Square.File - 3.5f))))*side;
//    //            //score += (Math.Abs(king.Square.File - 3.5f)) * side;
//    //            score += distance(king.Square, 3.5f, 3.5f) * side;
//    //            score += (board.HasKingsideCastleRight(color) || board.HasQueensideCastleRight(color) ? 2 : 0) * side;
//    //        }
//    //        else
//    //        {
//    //            /// Late game, move own king to center
//    //            score += ((4f - (Math.Abs(king.Square.Rank - 3.5f))) + (4f - (Math.Abs(king.Square.File - 3.5f)))) * side * .1f;

//    //            /// Potential improvement: move to the most back ranked pawn
//    //        }


//    //        /// Pawn
//    //        foreach (Piece pawn in board.GetPieceList(PieceType.Pawn, color))
//    //        {
//    //            float rankScore = color ? pawn.Square.Rank : 7 - pawn.Square.Rank;
//    //            score += (95 + ((pawn.Square.File > 1 && pawn.Square.File < 6) ? 2f : -.1f) * rankScore * (1 + rankScore * .1f)) * side;
//    //            //score += (98 + rankScore * 2f) * side;
//    //            //float mid = distance(pawn.Square, 3.5f, 3.5f)*5;
//    //            //score += (100 + mid*mid) * side;
//    //        }

//    //    }

//    //    /// Promote trading pieces when having pieces advantage
//    //    score += (specialPieces[0] - specialPieces[1]) / (specialPieces[0] + specialPieces[1] + 1) * 20;

//    //    return score;
//    //}
//}
