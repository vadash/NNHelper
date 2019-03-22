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
        private static long _lastTick = DateTime.Now.Ticks;
        private Point cursorPosition;
        private readonly DrawHelper dh;
        private bool aimEnabled = true;
        private readonly NeuralNet nn;
        private readonly Settings s;
        private readonly Stopwatch mainCycleWatch = new Stopwatch();

        // sync fps
        private readonly Stopwatch syncFpsWatch = new Stopwatch();
        private long syncFramesProcessed;

        //tracking
        private bool trackEnabled;
        private int trackSkippedFrames;
        private const int TRACK_MAX_SKIPPED_FRAMES = 3;

        //debug
        private readonly Stopwatch debugPerformanceStopwatch = new Stopwatch();
        private int debugTotalTimesRun = 0;
        private int debugFoundTarget = 0;

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
            var gc = new GController(s);
            StartReadKeysThread();
            SynchronizeToGameFps(gc, true);
            while (true)
            {
                if (aimEnabled)
                {
                    cursorPosition = Cursor.Position;
                    if (IsNewFrameReady()) // update enemy info
                    {
                        syncFramesProcessed++;
                        var bitmap = gc.ScreenCapture();
                        if (trackEnabled && trackSkippedFrames < TRACK_MAX_SKIPPED_FRAMES) // do tracking
                        {
                            var item = nn.Track(bitmap);
                            if (item == null)
                            {
                                trackSkippedFrames++;
                                continue;
                            }
                            var (curDx, curDy) = GetAimPoint(item);
                            if (IsShooting())
                            {
                                MoveMouse(curDx, curDy);
                            }
                        }
                        else // using regular search
                        {
                            var items = nn.GetItems(bitmap);
                            if (items == null || !items.Any()) continue;
                            trackEnabled = true;
                            trackSkippedFrames = 0;
                            var (curDx, curDy) = GetAimPoint(items);
                            if (IsShooting())
                            {
                                MoveMouse(curDx, curDy);
                            }
                        }
                    }
                    else // no need to update enemy info
                    {
                        SynchronizeToGameFps(gc);
                        SleepTillNextFrame();
                    }
                    //dh.DrawPlaying(cursorPosition, "", s, items, _firemode);
                }
                else
                {
                    dh.DrawDisabled();
                    Thread.Sleep(500);
                }
            }
        }

        #region Next frame math
        private int TimeToNextFrame()
        {
            return (int)Math.Ceiling(syncFramesProcessed * (1000f / 60f) - mainCycleWatch.ElapsedMilliseconds);
        }

        private bool IsNewFrameReady()
        {
            return TimeToNextFrame() <= 0;
        }

        private void SleepTillNextFrame()
        {
            var time = TimeToNextFrame();
            if (time > 0)
            {
                Thread.Sleep(time);
            }
        } 
        #endregion

        private static void StartReadKeysThread()
        {
            Thread.Sleep(1000);
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    var isKeyDown = User32.IsKeyPushedDown(Keys.RButton) ||
                                    User32.IsKeyPushedDown(Keys.LButton) ||
                                    User32.IsKeyPushedDown(Keys.XButton1);
                    if (isKeyDown)
                    {
                        _lastTick = DateTime.Now.Ticks;
                    }
                    Thread.Sleep(500);
                }
            }).Start();
        }

        private (float curDx, float curDy) GetAimPoint(YoloItem enemy)
        {
            var nearestEnemyHead = Util.GetEnemyHead(enemy);
            var nearestEnemyBody = Util.GetEnemyBody(enemy);
            if (nearestEnemyHead.Height > Settings.MinHeadSize) // aim to neck
            {
                var curDx = nearestEnemyHead.Left + nearestEnemyHead.Width / 2f - s.SizeX / 2f;
                var curDy = nearestEnemyHead.Top + nearestEnemyHead.Height - s.SizeY / 2f;
                IfInsideZone1SlowlyMoveToZone2(ref curDx, ref curDy, nearestEnemyBody, nearestEnemyHead);
                DontMoveInZone(nearestEnemyHead, ref curDx, ref curDy);
                return (curDx, curDy);
            }
            else // aim to middle body
            {
                var curDx = nearestEnemyBody.Left + nearestEnemyBody.Width / 2f - s.SizeX / 2f;
                var curDy = nearestEnemyBody.Top + nearestEnemyBody.Height / 2f - s.SizeY / 2f;
                IfInsideZone1SlowlyMoveToZone2(ref curDx, ref curDy, nearestEnemyBody, nearestEnemyHead);
                DontMoveInZone(nearestEnemyBody, ref curDx, ref curDy);
                return (curDx, curDy);
            }
        }
        
        private (float curDx, float curDy) GetAimPoint(IEnumerable<YoloItem> enemies)
        {
            var nearestEnemy = enemies.OrderBy(e =>
                DistanceBetweenCross(e.X + e.Width / 2f, e.Y + e.Height / 2f)).First();
            return GetAimPoint(nearestEnemy);
        }

        private void SynchronizeToGameFps(GController gc, bool bForce = false)
        {
            if (bForce || syncFpsWatch.ElapsedMilliseconds >= 30000)
            {
                var bitmap = gc.ScreenCapture();
                while (Util.Equals(bitmap, gc.ScreenCapture()))
                {
                }
                syncFramesProcessed = 0;
                syncFpsWatch.Restart();
            }
        }

        public bool IsShooting()
        {
            return DateTime.Now.Ticks < _lastTick + 20000000;
        }

        private void DontMoveInZone(Rectangle zone, ref float curDx, ref float curDy)
        {
            if (zone.Left <= s.SizeX / 2f && s.SizeX / 2f <= zone.Right &&
                zone.Top <= s.SizeY / 2f && s.SizeY / 2f <= zone.Bottom)
            {
                curDx = 0;
                curDy = 0;
            }
        }

        private void IfInsideZone1SlowlyMoveToZone2(ref float curDx, ref float curDy, Rectangle zone1, Rectangle zone2)
        {
            if (zone1.Left <= s.SizeX / 2f && s.SizeX / 2f <= zone1.Right &&
                zone1.Top <= s.SizeY / 2f && s.SizeY / 2f <= zone1.Bottom)
            {
                var destX = zone2.Left + zone2.Width / 2f;
                curDx = destX - s.SizeX / 2f;
                var minWidth = Math.Max(1f, zone2.Width / 10f);
                curDx = Math.Sign(curDx) * Math.Min(Math.Abs(curDx), minWidth);

                var destY = zone2.Top + zone2.Height / 2f;
                curDy = destY - s.SizeY / 2f;
                var minHeight = Math.Max(1f, zone2.Height / 10f);
                curDy = Math.Sign(curDy) * Math.Min(Math.Abs(curDy), minHeight);
            }
        }

        private void MoveMouse(float curDx, float curDy)
        {
            if (Math.Abs(curDx) < 0.5f && Math.Abs(curDy) < 0.5f)
                return;
            VirtualMouse.Move(Convert.ToInt32(curDx), Convert.ToInt32(curDy));
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