using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NNHelper
{
    public class User32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int vKey);

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);

        public static bool IsKeyPushedDown(Keys vKey)
        {
            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
        }

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        /// <summary>
        ///     MOUSEEVENTF_MOVE 0x0001
        ///     MOUSEEVENTF_LEFTDOWN 0x0002
        ///     MOUSEEVENTF_LEFTUP 0x0004
        /// </summary>
        /// <param name="dwFlags"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dwData"></param>
        /// <param name="dwExtraInfo"></param>
        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("kernel32.dll")]
        public static extern void ExitProcess([In] uint uExitCode);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}