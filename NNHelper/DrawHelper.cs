using Alturos.Yolo.Model;
using Rectangle = GameOverlay.Drawing.Rectangle;

namespace NNHelper
{
    public class DrawHelper
    {
        private readonly GraphicWindow mainWnd;
        private readonly Settings s;

        public DrawHelper(Settings settings)
        {
            s = settings;
            mainWnd = new GraphicWindow(settings.SizeX, settings.SizeY);
        }

        private void DrawItem(YoloItem enemy)
        {
            var head = Util.GetEnemyHead(enemy);
            var body = Util.GetEnemyBody(enemy);
            mainWnd.graphics.DrawRectangle(mainWnd.graphics.redBrush, body, 2);
            if (head.Height >= Settings.MinHeadSize)
            {
                mainWnd.graphics.DrawRectangle(mainWnd.graphics.redBrush, head, 2);
            }
        }

        public void DrawDisabled()
        {
            mainWnd.window.X = 0;
            mainWnd.window.Y = 0;
            mainWnd.graphics.BeginScene();
            mainWnd.graphics.ClearScene();
            mainWnd.graphics.EndScene();
        }

        public void DrawPlaying(YoloItem enemy, bool isAiming)
        {
            mainWnd.window.X = 1920 / 2 - s.SizeX / 2;
            mainWnd.window.Y = 1080 / 2 - s.SizeY / 2;
            mainWnd.graphics.BeginScene();
            mainWnd.graphics.ClearScene();
            // draw area
            //mainWnd.graphics.DrawRectangle(mainWnd.graphics.blueBrush, 0, 0, s.SizeX, s.SizeY, 2);
            // draw crossfire
            mainWnd.graphics.FillRectangle(isAiming ? mainWnd.graphics.redBrush : mainWnd.graphics.blueBrush,
                Rectangle.Create(s.SizeX / 2 - 2, s.SizeY / 2 - 2, 4, 4));
            //mainWnd.graphics.WriteText(
            //    $"FPS {mainWnd.graphics.FPS}");
            // draw targets
            DrawItem(enemy);
            mainWnd.graphics.EndScene();
        }
    }
}