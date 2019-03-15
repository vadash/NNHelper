using GameOverlay.Windows;

namespace NNHelper
{
    public class GraphicWindow
    {
        public GraphicsEx graphics;
        public OverlayWindow window;
        
        public GraphicWindow(int w, int h)
        {
            window = new OverlayWindow(0, 0, w, h)
            {
                IsTopmost = true,
                IsVisible = true
            };
            window.CreateWindow();

            graphics = new GraphicsEx
            {
                MeasureFPS = true,
                Height = window.Height,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true,
                UseMultiThreadedFactories = false,
                VSync = true,
                Width = window.Width,
                WindowHandle = window.Handle
            };
            graphics.Setup();
        }
    }
}