using System;
using System.Collections.Generic;
using Alturos.Yolo.Model;
using GameOverlay.Drawing;
using Point = System.Drawing.Point;

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


        public void DrawPlaying(Point curMousePos, string selectedObject, Settings settings, IEnumerable<YoloItem> items,
            bool firemode)
        {
            mainWnd.window.X = curMousePos.X - s.SizeX / 2;
            mainWnd.window.Y = curMousePos.Y - s.SizeY / 2;
            mainWnd.graphics.BeginScene();
            mainWnd.graphics.ClearScene();

            if (s.DrawAreaRectangle)
                mainWnd.graphics.DrawRectangle(mainWnd.graphics.csb, 0, 0, s.SizeX, s.SizeY, 2);

            mainWnd.graphics.FillRectangle(firemode ? mainWnd.graphics.csfmb : mainWnd.graphics.csb,
                Rectangle.Create(s.SizeX / 2, s.SizeY / 2, 4, 4));

            //draw main text
            if (s.DrawText)
                mainWnd.graphics.WriteText(
                    $"FPS {mainWnd.graphics.FPS}");

            foreach (var item in items) DrawItem(item);

            mainWnd.graphics.EndScene();
        }

        private void DrawItem(YoloItem item)
        {
            var body = Rectangle.Create(
                item.X + Convert.ToInt32(item.Width / 6f),
                item.Y + Convert.ToInt32(item.Height / 6f),
                Convert.ToInt32(item.Width / 1.5f),
                Convert.ToInt32(item.Height / 2f));
            mainWnd.graphics.DrawRectangle(mainWnd.graphics.bcb, body, 2);

            //mainWnd.graphics.DrawRectangle(mainWnd.graphics.hcb, GameOverlay.Drawing.Rectangle.Create(item.X, item.Y, item.Width, item.Height), 2);
            //mainWnd.graphics.DrawCrosshair(mainWnd.graphics.bcb, body.Left + body.Width / 2,
            //body.Top + body.Height / 2 + Convert.ToInt32(1 * shooting), 2, 2, CrosshairStyle.Cross);
            //mainWnd.graphics.DrawLine(mainWnd.graphics.bcb, s.SizeX / 2, s.SizeY / 2, body.Left + body.Width / 2,
            //body.Top + body.Height / 2 + Convert.ToInt32(1 * shooting), 2);
        }

        public void DrawDisabled()
        {
            mainWnd.window.X = 0;
            mainWnd.window.Y = 0;
            mainWnd.graphics.BeginScene();
            mainWnd.graphics.ClearScene();
            mainWnd.graphics.EndScene();
        }
    }
}