using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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
        private Point cursorPosition;
        private Point lastCursorPosition;
        private readonly DrawHelper dh;
        private bool aimEnabled = true;
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
            Console.WriteLine("PepeHands please work");
            var mainCycleWatch = new Stopwatch();
            mainCycleWatch.Start();
            var lastSeenEnemyWatch = new Stopwatch();
            lastSeenEnemyWatch.Start();
            var gc = new GController(s);
            IEnumerable<YoloItem> items = null;

            while (true)
                if (aimEnabled)
                {
                    cursorPosition = Cursor.Position;
                    var sleep = Convert.ToInt32(1000f / 60f - mainCycleWatch.ElapsedMilliseconds);
                    if (sleep > 0)
                    {
                        if (lastSeenEnemyWatch.ElapsedMilliseconds < 3000)
                        {
                            if (items == null || !items.Any()) continue;
                            RecalculateItemsPosition(ref items);
                        }
                        else
                            dh.DrawDisabled();
                        dh.DrawPlaying(cursorPosition, "", s, items, _firemode);
                    }
                    else
                    {
                        mainCycleWatch.Restart();
                        var bitmap = gc.ScreenCapture();
                        items = nn.GetItems(bitmap);
                        ShootItems(items);
                        if (items != null && items.Any()) lastSeenEnemyWatch.Restart();
                    }
                    lastCursorPosition = cursorPosition;
                }
                else
                {
                    dh.DrawDisabled();
                }
        }

        private void RecalculateItemsPosition(ref IEnumerable<YoloItem> items)
        {
            var dx = cursorPosition.X - lastCursorPosition.X;
            var dy = cursorPosition.Y - lastCursorPosition.Y;
            if (!items.Any())
                return;
            if (dx == 0 && dy == 0)
                return;
            foreach (var item in items)
            {
                item.X -= dx;
                item.Y -= dy;
            }
        }

        public void ShootItems(IEnumerable<YoloItem> items)
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
            var nearestEnemyHead = Util.GetEnemyHead(nearestEnemy);
            var nearestEnemyBody = Util.GetEnemyBody(nearestEnemy);
            var (curDx, curDy) = DetermineMove(nearestEnemyHead);
            SlowlyMoveToHead(ref curDx, ref curDy, nearestEnemyBody);
            DontMoveInHead(nearestEnemyHead, ref curDx, ref curDy);
            var smooth = CalculateSmooth(curDx, curDy);
            MoveMouse(curDx, curDy, smooth);
        }

        private void DontMoveInHead(Rectangle nearestEnemyHead, ref float curDx, ref float curDy)
        {
            if (nearestEnemyHead.Left <= s.SizeX / 2f && s.SizeX / 2f <= nearestEnemyHead.Right) curDx = 0;
            if (nearestEnemyHead.Top <= s.SizeY / 2f && s.SizeY / 2f <= nearestEnemyHead.Bottom) curDy = 0;
        }

        private void SlowlyMoveToHead(ref float curDx, ref float curDy, Rectangle nearestEnemyBody)
        {
            if (nearestEnemyBody.Left <= s.SizeX / 2f && s.SizeX / 2f <= nearestEnemyBody.Right)
            {
                var minWidth = Math.Min(1f, nearestEnemyBody.Width / 30f);
                curDx = Math.Sign(curDx) * Math.Min(Math.Abs(curDx), minWidth);
            }
            if (nearestEnemyBody.Top <= s.SizeY / 2f && s.SizeY / 2f <= nearestEnemyBody.Bottom)
            {
                var minHeight = Math.Min(1f, nearestEnemyBody.Height / 30f);
                curDy = Math.Sign(curDy) * Math.Min(Math.Abs(curDy), minHeight);
            }
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