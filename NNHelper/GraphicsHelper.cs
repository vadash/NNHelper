using GameOverlay.Drawing;

namespace NNHelper
{
    public class GraphicsEx : Graphics
    {
        public static readonly Point StartPoint = new Point(0, 0);
        public SolidBrush acb;
        public SolidBrush bcb;
        public SolidBrush csb;
        public SolidBrush csfmb;

        private Font DefaultFont;
        public SolidBrush hcb;
        public SolidBrush tfb;
        public static Color AreaColor { get; set; } = new Color(0, 255, 0, 10);
        public static Color TextColor { get; set; } = new Color(120, 255, 255, 255);
        public static Color BodyColor { get; set; } = new Color(0, 255, 0, 80);
        public static Color HeadColor { get; set; } = new Color(255, 0, 0, 80);

        public static string DefaultFontstr { get; set; } = "Arial";
        public static int DefaultFontSize { get; set; } = 10;


        public new void Setup()
        {
            base.Setup();

            DefaultFont = CreateFont(DefaultFontstr, DefaultFontSize);
            acb = AreaColor.getSolidBrush(this);
            tfb = TextColor.getSolidBrush(this);
            csb = CreateSolidBrush(Color.Blue);
            csfmb = CreateSolidBrush(Color.Red);
            hcb = HeadColor.getSolidBrush(this);
            bcb = BodyColor.getSolidBrush(this);
        }

        public void WriteText(string text, Font f = null)
        {
            f = f ?? DefaultFont;
            DrawText(DefaultFont, tfb, StartPoint, text);
        }
    }

    public static class ColorHelper
    {
        public static SolidBrush getSolidBrush(this Color c, Graphics graphics)
        {
            return graphics.CreateSolidBrush(c);
        }
    }
}