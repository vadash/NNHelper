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
            var nearestEnemy = items.OrderBy(e =>
                DistanceBetweenCross(e.X + e.Width / 2f, e.Y + e.Height / 2f)).First();
            var nearestEnemyHead = GetEnemyHead(nearestEnemy);
            var nearestEnemyBody = GetEnemyBody(nearestEnemy);
            var (curDx, curDy) = DetermineMove(nearestEnemyHead);
            SlowlyMoveToHead(ref curDx, ref curDy, nearestEnemyBody);
            DontMoveInHead(nearestEnemyHead, ref curDx, ref curDy);
            var smooth = CalculateSmooth(curDx, curDy);
            MoveMouse(curDx, curDy, smooth);
        }

        private static Rectangle GetEnemyBody(YoloItem nearestEnemy)
        {
            var nearestEnemyBody = Rectangle.Create(
                nearestEnemy.X + Convert.ToInt32(nearestEnemy.Width * (1f - GraphicsEx.BodyWidth) / 2f),
                y: nearestEnemy.Y + Convert.ToInt32(nearestEnemy.Height * (1f - GraphicsEx.BodyHeight) / 2f),
                Convert.ToInt32(GraphicsEx.BodyWidth * nearestEnemy.Width),
                Convert.ToInt32(GraphicsEx.BodyHeight * nearestEnemy.Height));
            return nearestEnemyBody;
        }

        private static Rectangle GetEnemyHead(YoloItem nearestEnemy)
        {
            var nearestEnemyHead = Rectangle.Create(
                nearestEnemy.X + Convert.ToInt32(nearestEnemy.Width * (1f - GraphicsEx.HeadWidth) / 2f),
                y: Convert.ToInt32(nearestEnemy.Y),
                Convert.ToInt32(GraphicsEx.HeadWidth * nearestEnemy.Width),
                Convert.ToInt32(GraphicsEx.HeadHeight * nearestEnemy.Height));
            return nearestEnemyHead;
        }

        private void DontMoveInHead(Rectangle nearestEnemyHead, ref float curDx, ref float curDy)
        {
            if (nearestEnemyHead.Left <= s.SizeX / 2f && s.SizeX / 2f <= nearestEnemyHead.Right) curDx = 0;
            if (nearestEnemyHead.Top <= s.SizeY / 2f && s.SizeY / 2f <= nearestEnemyHead.Bottom) curDy = 0;
        }

        private void SlowlyMoveToHead(ref float curDx, ref float curDy, Rectangle nearestEnemyBody)
        {
            if (nearestEnemyBody.Left <= s.SizeX / 2f && s.SizeX / 2f <= nearestEnemyBody.Right)
                curDx = Math.Sign(curDx) * Math.Min(Math.Abs(curDx), nearestEnemyBody.Height / 30f);
            if (nearestEnemyBody.Top <= s.SizeY / 2f && s.SizeY / 2f <= nearestEnemyBody.Bottom)
                curDy = Math.Sign(curDy) * Math.Min(Math.Abs(curDy), nearestEnemyBody.Width / 30f);
        }

        private (float curDx, float curDy) DetermineMove(Rectangle nearestEnemyHead)
        {
            var curDx = nearestEnemyHead.Left + nearestEnemyHead.Width / 2f - s.SizeX / 2f;
            var curDy = nearestEnemyHead.Top + nearestEnemyHead.Height / 3f - s.SizeY / 2f;
            return (curDx, curDy);
        }

        private void MoveMouse(float curDx, float curDy, float smooth)
        {
            if (Math.Abs(curDx) < 1f && Math.Abs(curDy) < 1f)
                return;
            if (curDx > -s.SizeX / 2f && curDx < s.SizeX / 2f && curDy > -s.SizeY / 2f && curDy < s.SizeY / 2f)
                VirtualMouse.MoveTo(Convert.ToInt32(curDx * smooth), Convert.ToInt32(curDy * smooth));
        }

        private static float CalculateSmooth(float curDx, float curDy)
        {
            float smooth;
            var squareDist = curDx * curDx + curDy * curDy;
            if (squareDist <= 40 * 40)
                smooth = 1.0f;
            else if (squareDist <= 80 * 80)
                smooth = 0.5f;
            else if (squareDist <= 160 * 160)
                smooth = 0.25f;
            else
                smooth = 0.125f;
            return smooth;
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