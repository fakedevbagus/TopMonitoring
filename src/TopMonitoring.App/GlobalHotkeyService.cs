using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TopMonitoring.App
{
    public sealed class GlobalHotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;

        private readonly IntPtr _hwnd;
        private readonly HwndSource _source;
        private readonly Dictionary<int, Action> _callbacks = new();
        private int _nextId = 3000;

        public GlobalHotkeyService(Window window)
        {
            _hwnd = new WindowInteropHelper(window).Handle;
            _source = HwndSource.FromHwnd(_hwnd);
            _source.AddHook(WndProc);
        }

        public void Register(string? gesture, Action action)
        {
            if (!HotkeyParser.TryParse(gesture, out var hotkey)) return;

            var id = ++_nextId;
            if (RegisterHotKey(_hwnd, id, hotkey.Modifiers, hotkey.VirtualKey))
            {
                _callbacks[id] = action;
            }
        }

        public void Clear()
        {
            foreach (var id in _callbacks.Keys)
            {
                UnregisterHotKey(_hwnd, id);
            }
            _callbacks.Clear();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();
                if (_callbacks.TryGetValue(id, out var action))
                {
                    action();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Clear();
            _source.RemoveHook(WndProc);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
