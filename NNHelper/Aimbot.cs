using Alturos.Yolo.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
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
        private readonly Stopwatch mainCycleWatch = new Stopwatch();
        private readonly Stopwatch syncFpsWatch = new Stopwatch();

        public Aimbot(Settings settings, NeuralNet neuralNet)
        {
            nn = neuralNet;
            s = settings;
            dh = new DrawHelper(settings);
            mainCycleWatch.Start();
            syncFpsWatch.Start();
        }

        public void Start()
        {
            Console.WriteLine("PepeHands please work");
            var lastSeenEnemyWatch = new Stopwatch();
            lastSeenEnemyWatch.Start();
            var gc = new GController(s);
            IEnumerable<YoloItem> items = null;
            var ticksInFrame = (long)(Math.Pow(10, 7) / 60f); // 10 MHZ for my PC, 59 fps

            SynchronizeToGameFps(gc, true);
            while (true)
                if (aimEnabled)
                {
                    cursorPosition = Cursor.Position;
                    if (mainCycleWatch.ElapsedTicks < ticksInFrame) // no need to update enemy info, just recalculate box position
                        if (lastSeenEnemyWatch.ElapsedMilliseconds < 2000)
                        {
                            if (items == null || !items.Any()) continue;
                        }
                        else
                        {
                            SynchronizeToGameFps(gc);
                            dh.DrawDisabled();
                        }
                    else // update enemy info
                    {
                        mainCycleWatch.Restart();
                        var bitmap = gc.ScreenCapture();
                        items = nn.GetItems(bitmap);
                        ShootItems(items);
                        if (items == null || !items.Any()) continue;
                        lastSeenEnemyWatch.Restart();
                    }
                    dh.DrawPlaying(cursorPosition, "", s, items, _firemode);
                    lastCursorPosition = cursorPosition;
                }
                else
                    dh.DrawDisabled();
        }

        private void SynchronizeToGameFps(GController gc, bool bForce = false)
        {
            if (bForce || syncFpsWatch.ElapsedMilliseconds >= 30000)
            {
                var bitmap = gc.ScreenCapture();
                while (Util.Equals(bitmap, gc.ScreenCapture()))
                {
                }
                syncFpsWatch.Restart();
            }
        }

        public void ShootItems(IEnumerable<YoloItem> items)
        {
            var isKeyDown = User32.GetAsyncKeyState(Keys.RButton) == -32767 ||
                            User32.GetAsyncKeyState(Keys.LButton) == -32767 ||
                            User32.GetAsyncKeyState(Keys.XButton1) == -32767 ||
                            User32.GetAsyncKeyState(Keys.XButton2) == -32767;
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
            float curDx, curDy;
            if (nearestEnemyHead.Height > 10)
            {
                (curDx, curDy) = CalculateMoveToHead(nearestEnemyHead);
                DontMoveInZone(nearestEnemyHead, ref curDx, ref curDy);
                IfInsideBodySlowlyMoveToHead(ref curDx, ref curDy, nearestEnemyBody, nearestEnemyHead);
            }
            else
            {
                (curDx, curDy) = CalculateMoveToBody(nearestEnemyBody);
                DontMoveInZone(nearestEnemyBody, ref curDx, ref curDy);
            }
            MoveMouse(curDx, curDy);
        }

        private (float curDx, float curDy) CalculateMoveToHead(Rectangle nearestEnemyHead)
        {
            var curDx = nearestEnemyHead.Left + nearestEnemyHead.Width / 2f - s.SizeX / 2f;
            var curDy = nearestEnemyHead.Top + nearestEnemyHead.Height - s.SizeY / 2f;
            return (curDx, curDy);
        }

        private (float curDx, float curDy) CalculateMoveToBody(Rectangle nearestEnemyBody)
        {
            var curDx = nearestEnemyBody.Left + nearestEnemyBody.Width / 2f - s.SizeX / 2f;
            var curDy = nearestEnemyBody.Top + nearestEnemyBody.Height / 2f - s.SizeY / 2f;
            return (curDx, curDy);
        }

        private void DontMoveInZone(Rectangle zone, ref float curDx, ref float curDy)
        {
            if (zone.Left <= s.SizeX / 2f && s.SizeX / 2f <= zone.Right) curDx = 0;
            if (zone.Top <= s.SizeY / 2f && s.SizeY / 2f <= zone.Bottom) curDy = 0;
        }

        private void IfInsideBodySlowlyMoveToHead(ref float curDx, ref float curDy, Rectangle body, Rectangle head)
        {
            if (body.Left <= s.SizeX / 2f && s.SizeX / 2f <= body.Right &&
                body.Top <= s.SizeY / 2f && s.SizeY / 2f <= body.Bottom)
            {
                var minWidth = Math.Max(1f, head.Width / 10f);
                curDx = Math.Sign(curDx) * Math.Min(Math.Abs(curDx), minWidth);
                var minHeight = Math.Max(1f, head.Height / 10f);
                curDy = Math.Sign(curDy) * Math.Min(Math.Abs(curDy), minHeight);
            }
        }

        private void MoveMouse(float curDx, float curDy)
        {
            if (Math.Abs(curDx) < 0.5f && Math.Abs(curDy) < 0.5f)
                return;
            if (curDx > -s.SizeX / 2f && curDx < s.SizeX / 2f && curDy > -s.SizeY / 2f && curDy < s.SizeY / 2f)
            {
                VirtualMouse.Move(Convert.ToInt32(curDx), Convert.ToInt32(curDy));
                //Thread.Sleep(2000);
            }
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