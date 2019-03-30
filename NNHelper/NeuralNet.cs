using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Alturos.Yolo;
using Alturos.Yolo.Model;

namespace NNHelper
{
    public class NeuralNet
    {
        public string[] trainingNames;
        private YoloWrapper yoloWrapper;
        private YoloTracking yoloTracking;

        private NeuralNet()
        {
        }

        public static YoloWrapper GetYolo(string Game)
        {
            if (File.Exists($"trainfiles/{Game}.cfg") && File.Exists($"trainfiles/{Game}.weights") &&
                File.Exists($"trainfiles/{Game}.names"))
            {
                var yoloWrapper = new YoloWrapper($"trainfiles/{Game}.cfg", $"trainfiles/{Game}.weights",
                    $"trainfiles/{Game}.names");
                Console.Clear();
                if (yoloWrapper.EnvironmentReport.CudaExists == false)
                {
                    Console.WriteLine("Install CUDA 10");
                    Console.ReadLine();
                    Process.GetCurrentProcess().Kill();
                }

                if (yoloWrapper.EnvironmentReport.CudnnExists == false)
                {
                    Console.WriteLine("Cudnn doesn't exist");
                    Console.ReadLine();
                    Process.GetCurrentProcess().Kill();
                }

                if (yoloWrapper.EnvironmentReport.MicrosoftVisualCPlusPlus2017RedistributableExists == false)
                {
                    Console.WriteLine("Install Microsoft Visual C++ 2017 Redistributable");
                    Console.ReadLine();
                    Process.GetCurrentProcess().Kill();
                }

                if (yoloWrapper.DetectionSystem.ToString() != "GPU")
                {
                    MessageBox.Show("No GPU card detected. Exiting...");
                    Console.ReadLine();
                    Process.GetCurrentProcess().Kill();
                }

                return yoloWrapper;
            }

            return null;
        }

        public static NeuralNet Create(string game)
        {
            var nn = new NeuralNet {trainingNames = null, yoloWrapper = GetYolo(game), yoloTracking = new YoloTracking(GetYolo(game), 80) };
            return nn.yoloWrapper == null ? null : nn;
        }
        
        public IEnumerable<YoloItem> GetItems(Image img, double confidence = 0.2)
        {
            using (var ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Bmp);
                return yoloWrapper.Detect(ms.ToArray()).Where(x => x.Confidence > confidence);
            }
        }

        public YoloItem Track(Image img)
        {
            using (var ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Bmp);
                return yoloTracking.Analyse(ms.ToArray());
            }
        }
    }
}