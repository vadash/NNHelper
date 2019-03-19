using GameOverlay.Drawing;

namespace NNHelper
{
    public class GraphicsEx : Graphics
    {
        public static readonly Point StartPoint = new Point(0, 0);
        public SolidBrush areaBrush;
        public SolidBrush bodyBrush;
        public SolidBrush blueBrush;
        public SolidBrush redBrush;

        private Font defaultFont;
        public SolidBrush headBrush;
        public SolidBrush textBrush;
        public static Color AreaColor { get; set; } = new Color(0, 255, 0, 10);
        public static Color TextColor { get; set; } = new Color(120, 255, 255);
        public static Color BodyColor { get; set; } = new Color(0, 255, 0, 80);
        public static Color HeadColor { get; set; } = new Color(255, 0, 0, 80);

        public static string DefaultFontStr { get; set; } = "Arial";
        public static int DefaultFontSize { get; set; } = 14;


        public new void Setup()
        {
            base.Setup();
            defaultFont = CreateFont(DefaultFontStr, DefaultFontSize);
            areaBrush = AreaColor.GetSolidBrush(this);
            textBrush = TextColor.GetSolidBrush(this);
            blueBrush = CreateSolidBrush(Color.Blue);
            redBrush = CreateSolidBrush(Color.Red);
            headBrush = HeadColor.GetSolidBrush(this);
            bodyBrush = BodyColor.GetSolidBrush(this);
        }

        public void WriteText(string text)
        {
            DrawText(defaultFont, textBrush, StartPoint, text);
        }
    }

    public static class ColorHelper
    {
        public static SolidBrush GetSolidBrush(this Color color, Graphics graphics)
        {
            return graphics.CreateSolidBrush(color);
        }
    }
}