using System;
using System.Collections.Generic;
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
        private double shooting = 0;

        private readonly ExponentialMovingAverageIndicator lastX = new ExponentialMovingAverageIndicator(5);
        private readonly ExponentialMovingAverageIndicator lastY = new ExponentialMovingAverageIndicator(5);
        private readonly ExponentialMovingAverageIndicator lastHeight = new ExponentialMovingAverageIndicator(5);
        private readonly ExponentialMovingAverageIndicator lastWidth = new ExponentialMovingAverageIndicator(5);

        public Aimbot(Settings settings, NeuralNet neuralNet)
        {
            nn = neuralNet;
            s = settings;
            dh = new DrawHelper(settings);
        }

        public void Start()
        {
            Console.WriteLine("running Aimbot :)");
            var gc = new GController(s);

            while (true)
                if (enabled)
                {
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
            if (s.SimpleRcs)
                if (User32.GetAsyncKeyState(Keys.LButton) == 0) shooting = 0;

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

            // Smoothing variables
            lastX.AddDataPoint(nearestEnemy.X);
            lastY.AddDataPoint(nearestEnemy.Y);
            lastHeight.AddDataPoint(nearestEnemy.Height);
            lastWidth.AddDataPoint(nearestEnemy.Width);

            var smoothAim = InterpolateSmoothCoeff(out var dist);

            var nearestEnemyBody = Rectangle.Create(
                (float)(lastX.Average + lastWidth.Average / 4f),
                (float)(lastY.Average + lastHeight.Average / 4f),
                (float)(lastWidth.Average / 2f),
                (float)(lastHeight.Average / 2f));

            if ((s.SizeX / 2f < nearestEnemyBody.Left) 
                | (s.SizeX / 2f > nearestEnemyBody.Right)
                | (s.SizeY / 2f < nearestEnemyBody.Top)
                | (s.SizeY / 2f > nearestEnemyBody.Bottom))
            {
                var dx = nearestEnemyBody.Left - s.SizeX / 2f + nearestEnemyBody.Width / 2f;
                if (Math.Abs(dx) <= 2f)
                {
                    dx = 0;
                }
                var dy = nearestEnemyBody.Top - s.SizeY / 2f + nearestEnemyBody.Height / 2f + shooting;
                if (Math.Abs(dy) <= 2f)
                {
                    dy = 0;
                }

                if (Math.Abs(dx) > 2f && Math.Abs(dy) > 2f)
                {
                    VirtualMouse.MoveTo(Convert.ToInt32(dx * smoothAim), Convert.ToInt32(dy * smoothAim));
                }
                if (s.SimpleRcs) shooting += 2;
            }
            else
            {
                if (s.SimpleRcs) shooting = 0;
            }
        }

        private double InterpolateSmoothCoeff(out float dist)
        {
            dist = DistanceBetweenCross(lastX.Average + lastWidth.Average / 2f, lastY.Average + lastHeight.Average / 2f);
            var tmp = -0.0000109091 * dist * dist + 0.00414545 * dist + 0.105455;
            if (tmp < 0.1) tmp = 0.1;
            if (tmp > 0.5) tmp = 0.5;
            return tmp;
        }

        private void ReadKeys()
        {



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