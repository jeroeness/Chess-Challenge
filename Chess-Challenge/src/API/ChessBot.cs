
namespace ChessChallenge.API
{
    public abstract class ChessBot
    {
        public int estimatedScore;
        public Move suggestedMove;
        public abstract Move Think(Board board, Timer timer);
    }
}