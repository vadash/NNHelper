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

        private Font defaultFont;
        public SolidBrush hcb;
        public SolidBrush tfb;
        public static Color AreaColor { get; set; } = new Color(0, 255, 0, 10);
        public static Color TextColor { get; set; } = new Color(120, 255, 255);
        public static Color BodyColor { get; set; } = new Color(0, 255, 0, 80);
        public static Color HeadColor { get; set; } = new Color(255, 0, 0, 80);

        public static string DefaultFontStr { get; set; } = "Arial";
        public static int DefaultFontSize { get; set; } = 12;


        public new void Setup()
        {
            base.Setup();

            defaultFont = CreateFont(DefaultFontStr, DefaultFontSize);
            acb = AreaColor.GetSolidBrush(this);
            tfb = TextColor.GetSolidBrush(this);
            csb = CreateSolidBrush(Color.Blue);
            csfmb = CreateSolidBrush(Color.Red);
            hcb = HeadColor.GetSolidBrush(this);
            bcb = BodyColor.GetSolidBrush(this);
        }

        public void WriteText(string text)
        {
            DrawText(defaultFont, tfb, StartPoint, text);
        }
    }

    public static class ColorHelper
    {
        public static SolidBrush GetSolidBrush(this Color c, Graphics graphics)
        {
            return graphics.CreateSolidBrush(c);
        }
    }
}