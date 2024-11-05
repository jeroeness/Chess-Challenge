using ChessChallenge.API;
using ChessChallenge.Application;
using Raylib_cs;
using System.Numerics;
using ChessChallenge.Chess;

namespace Chess_Challenge_Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        static Camera2D cam;
        static void UpdateCamera(int screenWidth, int screenHeight)
        {
            cam = new Camera2D();
            cam.target = new Vector2(0, 15);
            cam.offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
            cam.zoom = screenWidth / 1280f * 0.7f;
        }

        [Test]
        public void Test1()
        {
            Vector2 loadedWindowSize = Settings.ScreenSizeSmall;
            int screenWidth = (int)loadedWindowSize.X;
            int screenHeight = (int)loadedWindowSize.Y;

            Raylib.InitWindow(screenWidth, screenHeight, "Chess Coding Challenge");
            Raylib.SetTargetFPS(60);

            UpdateCamera(screenWidth, screenHeight);

            ChallengeController controller = new();
            controller.StartNewGame(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.MyBot, "8/8/8/8/8/4k3/q7/nn2K3 b - - 0 1");

            while (!Raylib.WindowShouldClose() && !controller.board.IsWhiteToMove)
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(22, 22, 22, 255));
                Raylib.BeginMode2D(cam);

                controller.Update();
                controller.Draw();

                Raylib.EndMode2D();

                controller.DrawOverlay();

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();

            Assert.IsTrue(ChessChallenge.Chess.Move.SameMove(new ChessChallenge.Chess.Move(8,12), controller.board.AllGameMoves.Last()));
        }
    }
}