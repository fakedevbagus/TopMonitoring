using System;
using System.Linq;
using System.Windows.Input;

namespace TopMonitoring.App
{
    internal readonly record struct HotkeyDefinition(int Modifiers, int VirtualKey);

    internal static class HotkeyParser
    {
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;

        public static bool TryParse(string? gesture, out HotkeyDefinition hotkey)
        {
            hotkey = default;
            if (string.IsNullOrWhiteSpace(gesture)) return false;

            var tokens = gesture
                .Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();

            if (tokens.Length == 0) return false;

            var mods = 0;
            string? keyToken = null;

            foreach (var token in tokens)
            {
                var upper = token.ToUpperInvariant();
                switch (upper)
                {
                    case "CTRL":
                    case "CONTROL":
                        mods |= MOD_CONTROL;
                        break;
                    case "ALT":
                        mods |= MOD_ALT;
                        break;
                    case "SHIFT":
                        mods |= MOD_SHIFT;
                        break;
                    case "WIN":
                    case "WINDOWS":
                        mods |= MOD_WIN;
                        break;
                    default:
                        keyToken = token;
                        break;
                }
            }

            if (keyToken == null) return false;

            try
            {
                var converter = new KeyConverter();
                var keyObj = converter.ConvertFromString(keyToken);
                if (keyObj is not Key key || key == Key.None) return false;

                var vk = KeyInterop.VirtualKeyFromKey(key);
                if (vk == 0) return false;

                hotkey = new HotkeyDefinition(mods, vk);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
