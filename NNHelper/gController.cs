using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace NNHelper
{
    public class GController : IDisposable
    {
        private readonly IntPtr hBitmap;
        private readonly IntPtr hdcDest;
        private readonly IntPtr hdcSrc;
        private readonly int height;
        private IntPtr hOld;
        private readonly IntPtr phnd;
        private readonly int screen_height;
        private readonly int screen_width;
        private readonly int width;
        private readonly User32.RECT windowRect;

        public GController(Settings s)
        {
            width = s.SizeX;
            height = s.SizeY;
            phnd = Process.GetProcessesByName(s.Game).FirstOrDefault().MainWindowHandle;
            ////set screencapture handles
            hdcSrc = User32.GetWindowDC(phnd);
            windowRect = new User32.RECT();
            User32.GetWindowRect(phnd, ref windowRect);
            screen_width = windowRect.right - windowRect.left;
            screen_height = windowRect.bottom - windowRect.top;
            hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            hOld = GDI32.SelectObject(hdcDest, hBitmap);
        }
        
        public Image ScreenCapture()
        {
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, screen_width / 2 - width / 2,
                screen_height / 2 - height / 2, GDI32.SRCCOPY);
            Image img = Image.FromHbitmap(hBitmap);
            return img;
        }

        public void Dispose()
        {
            
        }
    }
}