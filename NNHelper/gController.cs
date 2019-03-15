using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace NNHelper
{
    public class gController : IDisposable
    {
        private readonly IntPtr hBitmap;
        private readonly IntPtr hdcDest;
        private readonly IntPtr hdcSrc;
        private readonly int height;
        private IntPtr hOld;
        private readonly IntPtr phnd;
        private int screen_height;

        private int screen_width;

        private readonly int width;
        private readonly User32.RECT windowRect;

        public gController(Settings s)
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

        public void Dispose()
        {
            //GDI32.SelectObject(hdcDest, hOld);
            //GDI32.DeleteDC(hdcDest);
            //User32.ReleaseDC(phnd, hdcSrc);
            //GDI32.DeleteObject(hBitmap);
        }


        //added fix by caching capture object references
        public Image ScreenCapture(bool followMouse, Point coordinates)
        {
            var size = new Point(width, height);

            //IntPtr handle = phnd;
            //IntPtr hdcSrc = User32.GetWindowDC(handle);
            //User32.RECT windowRect = new User32.RECT();
            //User32.GetWindowRect(handle, ref windowRect);
            //screen_width = windowRect.right - windowRect.left;
            //screen_height = windowRect.bottom - windowRect.top;
            //IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            //IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, size.X, size.Y);
            //IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            //if (followMouse)
            //{
            //    GDI32.BitBlt(hdcDest, 0, 0, size.X, size.Y, hdcSrc, coordinates.X - size.X / 2, coordinates.Y - size.Y / 2, GDI32.SRCCOPY);
            //}
            //else GDI32.BitBlt(hdcDest, 0, 0, size.X, size.Y, hdcSrc, screen_width / 2 - size.X / 2, screen_height / 2 - size.Y / 2, GDI32.SRCCOPY);
            //GDI32.SelectObject(hdcDest, hOld);
            //GDI32.DeleteDC(hdcDest);
            //User32.ReleaseDC(handle, hdcSrc);
            //System.Drawing.Image img = System.Drawing.Image.FromHbitmap(hBitmap);
            //GDI32.DeleteObject(hBitmap);
            //return img;


            //User32.GetWindowRect(phnd, ref windowRect);
            //hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            //hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            //hOld = GDI32.SelectObject(hdcDest, hBitmap);

            if (followMouse)
                GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, coordinates.X - width / 2,
                    coordinates.Y - height / 2, GDI32.SRCCOPY);
            else
                GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, screen_width / 2 - width / 2,
                    screen_height / 2 - height / 2, GDI32.SRCCOPY);

            //GDI32.SelectObject(hdcDest, hOld);
            //GDI32.DeleteDC(hdcDest);
            Image img = Image.FromHbitmap(hBitmap);
            return img;
        }

        public Image CaptureWindow(string name, bool followMouse, Point coordinates)
        {
            var size = new Point(width, height);

            if (Process.GetProcessesByName(name).Count() == 0)
            {
                MessageBox.Show($"Looks like you closed {name}...");
                Process.GetCurrentProcess().Kill();
            }

            var handle = Process.GetProcessesByName(name)[0].MainWindowHandle;
            var hdcSrc = User32.GetWindowDC(handle);
            var windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            screen_width = windowRect.right - windowRect.left;
            screen_height = windowRect.bottom - windowRect.top;
            var hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            var hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, size.X, size.Y);
            var hOld = GDI32.SelectObject(hdcDest, hBitmap);
            if (followMouse)
                GDI32.BitBlt(hdcDest, 0, 0, size.X, size.Y, hdcSrc, coordinates.X - size.X / 2,
                    coordinates.Y - size.Y / 2, GDI32.SRCCOPY);
            else
                GDI32.BitBlt(hdcDest, 0, 0, size.X, size.Y, hdcSrc, screen_width / 2 - size.X / 2,
                    screen_height / 2 - size.Y / 2, GDI32.SRCCOPY);
            GDI32.SelectObject(hdcDest, hOld);
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            Image img = Image.FromHbitmap(hBitmap);
            GDI32.DeleteObject(hBitmap);
            return img;
        }

        public void saveCapture(bool screenshotmode, string path)
        {
            ScreenCapture(screenshotmode, Cursor.Position).Save(path, ImageFormat.Png);
        }
    }
}