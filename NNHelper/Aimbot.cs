using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Alturos.Yolo.Model;
using SharpDX.Direct2D1;
using Rectangle = GameOverlay.Drawing.Rectangle;
// ReSharper disable PossibleMultipleEnumeration

namespace NNHelper
{
    public class Aimbot
    {
        private static bool lastMDwnState;
        private static bool Firemode;
        private static long lastTick = DateTime.Now.Ticks;
        private Point coordinates;
        private readonly DrawHelper dh;

        private bool Enabled = true;
        private GameProcess gp;
        private readonly NeuralNet nn;
        private readonly Settings s;
        private int shooting;

        public Aimbot(Settings settings, GameProcess gameProcess, NeuralNet neuralNet)
        {
            gp = gameProcess;
            nn = neuralNet;
            s = settings;
            dh = new DrawHelper(settings);
        }

        public void Start()
        {
            Console.WriteLine("running Aimbot :)");
            var gc = new gController(s);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true) ReadKeys();
            }).Start();

            while (true)
                if (Enabled)
                {
                    coordinates = Cursor.Position;
                    var bitmap = gc.ScreenCapture(true, coordinates);
                    var items = nn.GetItems(bitmap);
                    var yoloItems = items as YoloItem[] ?? items.ToArray();
                    RenderItems(yoloItems);
                    dh.DrawPlaying(coordinates, "", s, yoloItems, Firemode);
                }
                else
                {
                    dh.DrawDisabled();
                }
        }

        public void RenderItems(IEnumerable<YoloItem> items)
        {
            shooting = 0;
            var isKeyDown = User32.GetAsyncKeyState(Keys.RButton) == -32767 ||
                         User32.GetAsyncKeyState(Keys.LButton) == -32767 ||
                         User32.GetAsyncKeyState(Keys.Alt) == -32767;
            if (isKeyDown || DateTime.Now.Ticks > lastTick + 20000000)
            {
                Firemode = isKeyDown || lastMDwnState;
                lastMDwnState = isKeyDown;
                lastTick = DateTime.Now.Ticks;
            }
            if (items.Any() && Firemode) Shooting(ref items);
        }

        private void Shooting(ref IEnumerable<YoloItem> items)
        {
            var nearestEnemy = items.OrderBy(e =>
                DistanceBetweenCross(e.X + e.Width / 2f, e.Y + e.Height / 2f)).First();

            var nearestEnemyBody = Rectangle.Create(
                nearestEnemy.X + Convert.ToInt32(nearestEnemy.Width / 4f),
                nearestEnemy.Y + Convert.ToInt32(nearestEnemy.Height / 4f),
                Convert.ToInt32(nearestEnemy.Width / 2f),
                Convert.ToInt32(nearestEnemy.Height / 2f));

            if ((s.SizeX / 2f < nearestEnemyBody.Left) 
                | (s.SizeX / 2f > nearestEnemyBody.Right)
                | (s.SizeY / 2f < nearestEnemyBody.Top)
                | (s.SizeY / 2f > nearestEnemyBody.Bottom))
            {
                double dx = nearestEnemyBody.Left - s.SizeX / 2f + nearestEnemyBody.Width;
                if (Math.Abs(dx) <= 1f)
                {
                    dx = 0;
                }
                double dy = nearestEnemyBody.Top - s.SizeY / 2f + nearestEnemyBody.Height;
                if (Math.Abs(dy) <= 1f)
                {
                    dy = 0;
                }

                VirtualMouse.Move(Convert.ToInt32(dx * s.SmoothAim), Convert.ToInt32(dy * s.SmoothAim));
            }
        }

        private void ReadKeys()
        {
            if (User32.GetAsyncKeyState(Keys.F7) == -32767)
            {
                Enabled = !Enabled;
                Console.Beep();
            }

            if (User32.GetAsyncKeyState(Keys.Up) == -32767)
                s.SmoothAim = s.SmoothAim >= 1 ? s.SmoothAim : s.SmoothAim + 0.05f;

            if (User32.GetAsyncKeyState(Keys.Down) == -32767)
                s.SmoothAim = s.SmoothAim <= 0 ? s.SmoothAim : s.SmoothAim - 0.05f;

            if (User32.GetAsyncKeyState(Keys.Home) == -32767) s.SimpleRcs = !s.SimpleRcs;

        }

        public float DistanceBetweenCross(float x, float y)
        {
            var yDist = y - (float)s.SizeY / 2;
            var xDist = x - (float)s.SizeX / 2;
            var hypotenuse = (float) Math.Sqrt(Math.Pow(yDist, 2) + Math.Pow(xDist, 2));
            return hypotenuse;
        }
    }
}