using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TopMonitoring.App
{
    /// <summary>
    /// Minimal AppBar docking to top of the primary screen.
    /// Reserves a fixed height (24px) so other windows only shift down 24px.
    /// </summary>
    public sealed class AppBar : IDisposable
    {
        private const int FixedHeight = 24;
        private readonly Window _window;
        private bool _registered;

        public AppBar(Window window) => _window = window;

        public void Register()
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
            SetPosition();
        }

        public void SetPosition()
        {
            if (!_registered) return;

            var hwnd = new WindowInteropHelper(_window).Handle;
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var height = FixedHeight;

            var abd = new NativeMethods.APPBARDATA
            {
                cbSize = Marshal.SizeOf<NativeMethods.APPBARDATA>(),
                hWnd = hwnd,
                uEdge = NativeMethods.ABE_TOP,
                rc = new NativeMethods.RECT { left = 0, top = 0, right = screenWidth, bottom = height }
            };

            // Query then set final position
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_QUERYPOS, ref abd);
            abd.rc.top = 0;
            abd.rc.bottom = abd.rc.top + height;
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_SETPOS, ref abd);

            _window.Left = abd.rc.left;
            _window.Top = abd.rc.top;
            _window.Width = abd.rc.right - abd.rc.left;
            _window.Height = abd.rc.bottom - abd.rc.top;
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
