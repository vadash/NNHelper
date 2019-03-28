using Alturos.Yolo.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private long lastTick = DateTime.Now.Ticks;
        private readonly DrawHelper dh;
        private bool aimEnabled = true;
        private readonly NeuralNet nn;
        private readonly Settings s;
        private readonly Stopwatch mainCycleWatch = new Stopwatch();
        private readonly ChaoticSmoothManager chaoticSmoothManager = new ChaoticSmoothManager();
        private const int fps = 60;
        private const float scale = 2.5f;

        // sync fps
        private readonly Stopwatch syncFpsWatch = new Stopwatch();
        private long syncFramesProcessed;

        //tracking
        private bool trackEnabled;
        private int trackSkippedFrames;
        private const int TRACK_MAX_SKIPPED_FRAMES = 3;

        private static readonly Mutex TargetMutex = new Mutex();
        private YoloItem currentTarget;

        //debug
        //private readonly Stopwatch debugPerformanceStopwatch = new Stopwatch();
        //private int debugTotalTimesRun = 0;
        //private int debugFoundTarget = 0;
        //private static List<float> SyncList = new List<float>();

        public Aimbot(Settings settings, NeuralNet neuralNet)
        {
            nn = neuralNet;
            s = settings;
            dh = new DrawHelper(settings);
        }

        public void Start()
        {
            Console.WriteLine("PepeHands please work");
            StartReadKeysThread();
            StartRenderThread();
            StartMouseMoveThread();
            StartDetectorThread();
            mainCycleWatch.Start();
            syncFpsWatch.Start();
            Application.Run();
        }

        private void StartDetectorThread()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                var gc = new GController(s);
                SynchronizeToGameFps(gc, true);
                while (true)
                {
                    if (aimEnabled)
                    {
                        if (IsNewFrameReady()) // update enemy info
                                               //if (true)
                        {
                            syncFramesProcessed++;
                            var newFrame = gc.ScreenCapture();
                            if (trackEnabled && trackSkippedFrames <= TRACK_MAX_SKIPPED_FRAMES) // do tracking
                            {
                                TargetMutex.WaitOne();
                                currentTarget = nn.Track(newFrame);
                                TargetMutex.ReleaseMutex();
                                if (currentTarget == null)
                                {
                                    trackSkippedFrames++;
                                }
                            }
                            else // using regular search
                            {
                                var confidence = IsAiming() ? 0.25f : 0.4f;
                                var enemies = nn.GetItems(newFrame, confidence);
                                if (enemies == null || !enemies.Any())
                                {
                                    if (trackSkippedFrames > TRACK_MAX_SKIPPED_FRAMES)
                                    {
                                        TargetMutex.WaitOne();
                                        currentTarget = null;
                                        TargetMutex.ReleaseMutex();
                                    }
                                }
                                else
                                {
                                    trackEnabled = true;
                                    trackSkippedFrames = 0;
                                    var tmp = GetClosestEnemy(enemies);
                                    TargetMutex.WaitOne();
                                    currentTarget = tmp;
                                    TargetMutex.ReleaseMutex();
                                    nn.SetTrackingPoint(currentTarget);
                                }
                            }
                        }
                        else // no need to update enemy info
                        {
                            SynchronizeToGameFps(gc);
                            SleepTillNextFrame();
                        }
                    }
                    else
                    {
                        Thread.Sleep(250);
                    }
                }
            }).Start();
        }

        private void StartMouseMoveThread()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    Thread.Sleep((int)(1000f/fps/4f));
                    if (currentTarget == null || !IsAiming()) continue;
                    TargetMutex.WaitOne();
                    if (currentTarget == null)
                    {
                        TargetMutex.ReleaseMutex();
                        continue;
                    }
                    var (curDx, curDy) = GetAimPoint(currentTarget);
                    var distanceSmooth = CalculateDistanceSmoothSimple(curDx, curDy);
                    var (xDelta, yDelta) = ApplySmoothScale(curDx, curDy, distanceSmooth);
                    currentTarget.X -= (int)(xDelta / scale);
                    currentTarget.Y -= (int)(yDelta / scale);
                    TargetMutex.ReleaseMutex();
                    MoveMouse(xDelta, yDelta);
                }
            }).Start();
        }

        private (int, int) ApplySmoothScale(float curDx, float curDy, float smooth)
        {
            if (curDx > -s.SizeX / 2f && curDx < s.SizeX / 2f && curDy > -s.SizeY / 2f && curDy < s.SizeY / 2f)
                return (Convert.ToInt32(scale * curDx * smooth), Convert.ToInt32(scale * curDy * smooth));
            return (0, 0);
        }

        private static float CalculateDistanceSmoothSimple(float curDx, float curDy)
        {
            var dist2 = curDx * curDx + curDy * curDy;
            var dist = Math.Sqrt(dist2);
            float smooth;
            if (dist < 40) smooth = 0.99f/3f;
            else if (dist < 80) smooth = 0.99f/4f;
            else if (dist < 160f) smooth = 0.99f/6f;
            else smooth = 0.99f/10f;
            return smooth;
        }


        #region Next frame math
        private int TimeToNextFrame()
        {
            return (int)Math.Ceiling(syncFramesProcessed * (1000f / fps) - syncFpsWatch.ElapsedMilliseconds);
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

        private void StartReadKeysThread()
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
                        lastTick = DateTime.Now.Ticks;
                    }
                    Thread.Sleep(250);
                }
            }).Start();
        }

        private void StartRenderThread()
        {
            Thread.Sleep(1000);
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    if (currentTarget == null)
                        dh.DrawDisabled();
                    else
                        dh.DrawPlaying(currentTarget, IsAiming());
                    Thread.Sleep((int)(1000f/fps));
                }
            }).Start();
        }

        private (float curDx, float curDy) GetAimPoint(YoloItem enemy)
        {
            if (enemy == null)
            {
                return (0, 0);
            }
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
                while (Util.Equals(bitmap, gc.ScreenCapture(), 1000))
                {
                }
                syncFramesProcessed = 0;
                syncFpsWatch.Restart();
            }
        }

        public bool IsAiming()
        {
            return DateTime.Now.Ticks < lastTick + 20000000;
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