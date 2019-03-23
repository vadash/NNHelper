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
        private readonly ChaoticSmoothManager chaoticSmoothManager = new ChaoticSmoothManager();

        // sync fps
        private readonly Stopwatch syncFpsWatch = new Stopwatch();
        private long syncFramesProcessed;

        //tracking
        private bool trackEnabled;
        private int trackSkippedFrames;
        private const int TRACK_MAX_SKIPPED_FRAMES = 2;

        //debug
        private readonly Stopwatch debugPerformanceStopwatch = new Stopwatch();
        private int debugTotalTimesRun = 0;
        private int debugFoundTarget = 0;

        public Aimbot(Settings settings, NeuralNet neuralNet)
        {
            nn = neuralNet;
            s = settings;
            dh = new DrawHelper(settings);
        }

        public void Start()
        {
            Console.WriteLine("PepeHands please work");
            var gc = new GController(s);
            StartReadKeysThread();
            SynchronizeToGameFps(gc, true);
            mainCycleWatch.Start();
            syncFpsWatch.Start();
            while (true)
            {
                if (mainCycleWatch.ElapsedMilliseconds > 10000)
                {
                    var a = (float)syncFpsWatch.ElapsedMilliseconds / syncFramesProcessed;
                }
                if (aimEnabled)
                {
                    cursorPosition = Cursor.Position;
                    if (IsNewFrameReady()) // update enemy info
                    {
                        syncFramesProcessed++;
                        var bitmap = gc.ScreenCapture();
                        float curDx;
                        float curDy;
                        YoloItem currentEnemy;
                        if (trackEnabled && trackSkippedFrames <= TRACK_MAX_SKIPPED_FRAMES) // do tracking
                        {
                            currentEnemy = nn.Track(bitmap);
                            if (currentEnemy == null)
                            {
                                trackSkippedFrames++;
                                continue;
                            }
                            (curDx, curDy) = GetAimPoint(currentEnemy);
                        }
                        else // using regular search
                        {
                            var enemies = nn.GetItems(bitmap);
                            if (enemies == null || !enemies.Any())
                            {
                                dh.DrawDisabled();
                                continue;
                            }
                            trackEnabled = true;
                            trackSkippedFrames = 0;
                            currentEnemy = GetClosestEnemy(enemies);
                            (curDx, curDy) = GetAimPoint(currentEnemy);
                        }
                        if (IsShooting())
                        {
                            chaoticSmoothManager.AddPoint(curDx, curDy);
                            var chaosSmooth = chaoticSmoothManager.GetSmooth();
                            var distanceSmooth = CalculateDistanceSmoothSimple(curDx, curDy);
                            var (xDelta, yDelta) = ApplySmoothScale(curDx, curDy, Math.Min(chaosSmooth, distanceSmooth));
                            MoveMouse(xDelta, yDelta);
                        }
                        dh.DrawPlaying(s, currentEnemy, IsShooting());
                    }
                    else // no need to update enemy info
                    {
                        SynchronizeToGameFps(gc);
                        SleepTillNextFrame();
                    }
                }
                else
                {
                    dh.DrawDisabled();
                    Thread.Sleep(500);
                }
            }
        }

        private (int, int) ApplySmoothScale(float curDx, float curDy, float smooth)
        {
            if (curDx > -s.SizeX / 2f && curDx < s.SizeX / 2f && curDy > -s.SizeY / 2f && curDy < s.SizeY / 2f)
                return (Convert.ToInt32(curDx * smooth), Convert.ToInt32(curDy * smooth));
            return (0, 0);
        }

        private static float CalculateDistanceSmoothSimple(float curDx, float curDy)
        {
            var dist2 = curDx * curDx + curDy * curDy;
            var dist = Math.Sqrt(dist2);
            float smooth;
            if (dist < 40) smooth = 1f;
            else if (dist < 80) smooth = 0.5f;
            else if (dist < 160) smooth = 0.33f;
            else smooth = 0.25f;
            return smooth;
        }


        #region Next frame math
        private int TimeToNextFrame()
        {
            return (int)Math.Ceiling(syncFramesProcessed * (1000f / 60f) - syncFpsWatch.ElapsedMilliseconds);
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
                    Thread.Sleep(250);
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

        private YoloItem GetClosestEnemy(IEnumerable<YoloItem> enemies)
        {
            var nearestEnemy = enemies.OrderBy(e =>
                DistanceBetweenCross(e.X + e.Width / 2f, e.Y + e.Height / 2f)).First();
            return nearestEnemy;
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

        private static void MoveMouse(int xDelta, int yDelta)
        {
            VirtualMouse.Move(xDelta, yDelta);
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