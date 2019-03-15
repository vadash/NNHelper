using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace NNHelper
{
    public class GameProcess
    {
        public Settings s;

        private GameProcess(Settings settings)
        {
            s = settings;
        }

        public string ProcessName => s.Game;

        public static GameProcess Create(Settings settings)
        {
            var gp = new GameProcess(settings);
            var p = Process.GetProcessesByName(settings.Game).FirstOrDefault();
            if (p == null)
            {
                MessageBox.Show($"You have not launched {gp.s.Game}...");
                //Process.GetCurrentProcess().Kill();
                while (gp.isRunning() == false)
                {
                    Console.Clear();
                    Console.WriteLine($"Waiting for {gp.s.Game} to open. Press any key to retry!");
                    Console.ReadLine();
                }
            }

            return gp;
        }


        public bool isRunning()
        {
            return Process.GetProcessesByName(s.Game).FirstOrDefault() != null;
        }
    }
}