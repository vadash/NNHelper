using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Alturos.Yolo.Model;
using Rectangle = GameOverlay.Drawing.Rectangle;

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

            var isMdwn = User32.GetAsyncKeyState(Keys.RButton) == -32767 ||
                         User32.GetAsyncKeyState(Keys.LButton) == -32767;
            if (isMdwn || DateTime.Now.Ticks > lastTick + 20000000)
            {
                Firemode = isMdwn || lastMDwnState;
                lastMDwnState = isMdwn;
                lastTick = DateTime.Now.Ticks;
            }

            if (items.Count() > 0 && Firemode) Shooting(ref items);
        }

        private void Shooting(ref IEnumerable<YoloItem> items)
        {
            var nearestEnemy = items.OrderBy(x =>
                DistanceBetweenCross(x.X + Convert.ToInt32(x.Width / 6) + x.Width / 1.5f / 2,
                    x.Y + x.Height / 6 + x.Height / 3 / 2)).First();

            var nearestEnemyBody = Rectangle.Create(nearestEnemy.X + Convert.ToInt32(nearestEnemy.Width / 6),
                nearestEnemy.Y + nearestEnemy.Height / 6 + (float)2 * shooting,
                Convert.ToInt32(nearestEnemy.Width / 1.5f), nearestEnemy.Height / 3 + (float)2 * shooting);
            if (s.SmoothAim <= 0)
            {
                VirtualMouse.Move(Convert.ToInt32(nearestEnemyBody.Left - s.SizeX / 2 + nearestEnemyBody.Width / 2),
                    Convert.ToInt32(nearestEnemyBody.Top - s.SizeY / 2 + nearestEnemyBody.Height / 7 +
                                    1 * shooting));

                if (s.SimpleRcs) shooting += 2;
            }
            else
            {
                if ((s.SizeX / 2 < nearestEnemyBody.Left) | (s.SizeX / 2 > nearestEnemyBody.Right)
                                                          | (s.SizeY / 2 < nearestEnemyBody.Top) |
                                                          (s.SizeY / 2 > nearestEnemyBody.Bottom))
                {
                    VirtualMouse.Move(
                        Convert.ToInt32((nearestEnemyBody.Left - s.SizeX / 2 + nearestEnemyBody.Width / 2) *
                                        s.SmoothAim),
                        Convert.ToInt32((nearestEnemyBody.Top - s.SizeY / 2 + nearestEnemyBody.Height / 7 +
                                         1 * shooting) * s.SmoothAim));
                }
                else
                {
                    if (s.SimpleRcs) shooting += 2;
                }
            }

            if (s.SimpleRcs)
                VirtualMouse.Move(0, shooting);
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

            if (User32.GetAsyncKeyState(Keys.Home) == -32767) s.SimpleRcs = s.SimpleRcs ? false : true;

        }

        public float DistanceBetweenCross(float X, float Y)
        {
            var ydist = Y - s.SizeY / 2;
            var xdist = X - s.SizeX / 2;
            var Hypotenuse = (float) Math.Sqrt(Math.Pow(ydist, 2) + Math.Pow(xdist, 2));
            return Hypotenuse;
        }
    }
}