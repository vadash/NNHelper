using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;

namespace NNHelper
{
    [DataContract]
    public class Settings
    {
        [DataMember] public int SizeX { get; set; }

        [DataMember] public int SizeY { get; set; }

        [DataMember] public string Game { get; set; }

        [DataMember] public bool SimpleRcs { get; set; }

        [DataMember] public Keys ShootKey { get; set; }

        [DataMember] public float SmoothAim { get; set; }

        [DataMember] public bool Information { get; set; }

        [DataMember] public bool DrawAreaRectangle { get; set; }

        [DataMember] public bool DrawText { get; set; }


        public static Settings ReadSettings()
        {
            // Read settings
            var jsonSerializer = new DataContractJsonSerializer(typeof(Settings[]));
            var autoConfig = new Settings
            {
                SizeX = 416,
                SizeY = 416,
                Game = "r5apex",
                SimpleRcs = true,
                ShootKey = Keys.Alt,
                SmoothAim = 0.1f,
                Information = true,
            };
            using (var fs = new FileStream("config.json", FileMode.OpenOrCreate))
            {
                if (fs.Length == 0)
                {
                    jsonSerializer.WriteObject(fs, new[] {autoConfig});
                    MessageBox.Show("Created auto-config, change whatever settings you want and restart.");
                    Process.GetCurrentProcess().Kill();
                    return null;
                }
                var settings = (Settings[]) jsonSerializer.ReadObject(fs);
                return settings?[0];
            }
        }
    }
}