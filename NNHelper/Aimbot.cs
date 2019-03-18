using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Alturos.Yolo.Model;
using Rectangle = GameOverlay.Drawing.Rectangle;
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable FunctionNeverReturns

namespace NNHelper
{
    public class Aimbot
    {
        private static bool _lastMDwnState;
        private static bool _firemode;
        private static long _lastTick = DateTime.Now.Ticks;
        private Point coordinates;
        private readonly DrawHelper dh;
        private bool enabled = true;
        private readonly NeuralNet nn;
        private readonly Settings s;

        public Aimbot(Settings settings, NeuralNet neuralNet)
        {
            nn = neuralNet;
            s = settings;
            dh = new DrawHelper(settings);
        }

        public void Start()
        {
            Console.WriteLine("running Aimbot :)");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var gc = new GController(s);

            while (true)
                if (enabled)
                {
                    var sleep = 1000f / 60f - stopwatch.ElapsedMilliseconds;
                    if (sleep > 0)
                    {
                        Thread.Sleep((int)sleep);
                    }
                    stopwatch.Restart();

                    coordinates = Cursor.Position;
                    var bitmap = gc.ScreenCapture();
                    var items = nn.GetItems(bitmap);
                    RenderItems(items);
                    dh.DrawPlaying(coordinates, "", s, items, _firemode);
                }
                else
                {
                    dh.DrawDisabled();
                }
        }


        public void RenderItems(IEnumerable<YoloItem> items)
        {
            var isKeyDown = User32.GetAsyncKeyState(Keys.RButton) == -32767 ||
                            User32.GetAsyncKeyState(Keys.LButton) == -32767;
            if (isKeyDown || DateTime.Now.Ticks > _lastTick + 20000000)
            {
                _firemode = isKeyDown || _lastMDwnState;
                _lastMDwnState = isKeyDown;
                _lastTick = DateTime.Now.Ticks;
            }
            if (items.Any() && _firemode) Shooting(ref items);
        }

        private void Shooting(ref IEnumerable<YoloItem> items)
        {
            s.SmoothAim = 0.5f;

            var nearestEnemy = items.OrderBy(e =>
                DistanceBetweenCross(e.X + e.Width / 2f, e.Y + e.Height / 2f)).First();

            var nearestEnemyBody = Rectangle.Create(
                nearestEnemy.X + nearestEnemy.Width / 4f,
                nearestEnemy.Y + nearestEnemy.Height / 4f,
                nearestEnemy.Width / 2f,
                nearestEnemy.Height / 2f);

            var nearestEnemyHead = Rectangle.Create(
                nearestEnemy.X + nearestEnemy.Width / 3f,
                nearestEnemy.Y + nearestEnemy.Height / 12f,
                nearestEnemy.Width / 3f,
                nearestEnemy.Height / 3f);

            var curDx = nearestEnemyHead.Left + nearestEnemyHead.Width / 2f - s.SizeX / 2f;
            var curDy = nearestEnemyHead.Top + nearestEnemyHead.Height / 3f - s.SizeY / 2f;
            // slowly move cursor to head if we targeting body but dont move 1px distance
            if (s.SizeX / 2f > nearestEnemy.X + nearestEnemy.Width * 0.2f / 4f && s.SizeX / 2f < nearestEnemy.X + nearestEnemy.Width * 0.8f)
                curDx = Math.Sign(curDx) * Math.Min(Math.Abs(curDx), nearestEnemy.Height / 30f);
            if (s.SizeY / 2f > nearestEnemy.Y + nearestEnemy.Height / 12f && s.SizeY / 2f < nearestEnemy.Y + nearestEnemy.Height * 0.8f)
                curDy = Math.Sign(curDy) * Math.Min(Math.Abs(curDy), nearestEnemy.Width / 30f);
            // do we really need to move ? double checking range +-160, +-160
            if (curDx > -s.SizeX / 2f && curDx < s.SizeX / 2f && curDy > -s.SizeY / 2f && curDy < s.SizeY / 2f)
            VirtualMouse.MoveTo((int)curDx, (int)curDy);
        }

        public float DistanceBetweenCross(double x, double y)
        {
            var yDist = y - s.SizeY / 2f;
            var xDist = x - s.SizeX / 2f;
            var hypotenuse = Math.Sqrt(Math.Pow(yDist, 2) + Math.Pow(xDist, 2));
            return (float)hypotenuse;
        }
    }
}