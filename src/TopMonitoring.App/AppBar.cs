using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TopMonitoring.App
{
    /// <summary>
    /// Minimal AppBar docking to top of a target screen.
    /// Reserves a fixed height so other windows only shift down that height.
    /// </summary>
    public sealed class AppBar : IDisposable
    {
        private readonly Window _window;
        private bool _registered;

        public AppBar(Window window) => _window = window;

        public void Register(System.Windows.Forms.Screen targetScreen, int height)
        {
            if (_registered) return;

            var hwnd = new WindowInteropHelper(_window).Handle;

            var abd = new NativeMethods.APPBARDATA
            {
                cbSize = Marshal.SizeOf<NativeMethods.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = NativeMethods.ABE_TOP
            };

            NativeMethods.SHAppBarMessage(NativeMethods.ABM_NEW, ref abd);
            _registered = true;
            SetPosition(targetScreen, height);
        }

        public void SetPosition(System.Windows.Forms.Screen targetScreen, int height)
        {
            if (!_registered) return;

            var hwnd = new WindowInteropHelper(_window).Handle;
            var bounds = targetScreen.Bounds;

            var abd = new NativeMethods.APPBARDATA
            {
                cbSize = Marshal.SizeOf<NativeMethods.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = NativeMethods.ABE_TOP,
                rc = new NativeMethods.RECT
                {
                    left = bounds.Left,
                    top = bounds.Top,
                    right = bounds.Right,
                    bottom = bounds.Top + height
                }
            };

            // Query then set final position
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_QUERYPOS, ref abd);
            abd.rc.left = bounds.Left;
            abd.rc.right = bounds.Right;
            abd.rc.top = bounds.Top;
            abd.rc.bottom = bounds.Top + height;
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_SETPOS, ref abd);

            var topLeft = DpiHelper.FromPixelsPoint(_window, abd.rc.left, abd.rc.top);
            var size = DpiHelper.FromPixelsSize(_window, abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top);

            _window.Left = topLeft.X;
            _window.Top = topLeft.Y;
            _window.Width = size.Width;
            _window.Height = size.Height;
        }

        public void Dispose()
        {
            if (!_registered) return;

            var hwnd = new WindowInteropHelper(_window).Handle;
            var abd = new NativeMethods.APPBARDATA
            {
                cbSize = Marshal.SizeOf<NativeMethods.APPBARDATA>(),
                hWnd = hwnd
            };

            NativeMethods.SHAppBarMessage(NativeMethods.ABM_REMOVE, ref abd);
            _registered = false;
        }

        private static class NativeMethods
        {
            public const int ABM_NEW = 0x00000000;
            public const int ABM_REMOVE = 0x00000001;
            public const int ABM_QUERYPOS = 0x00000002;
            public const int ABM_SETPOS = 0x00000003;
            public const int ABE_TOP = 1;

            [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left, top, right, bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct APPBARDATA
            {
                public int cbSize;
                public IntPtr hWnd;
                public uint uCallbackMessage;
                public int uEdge;
                public RECT rc;
                public int lParam;
            }
        }
    }
}
