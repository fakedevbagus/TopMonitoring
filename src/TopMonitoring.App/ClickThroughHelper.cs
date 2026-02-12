using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TopMonitoring.App
{
    internal static class ClickThroughHelper
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        public static void Apply(Window window, bool enabled)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (enabled)
            {
                exStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
                window.IsHitTestVisible = false;
            }
            else
            {
                exStyle &= ~WS_EX_TRANSPARENT;
                window.IsHitTestVisible = true;
            }

            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
