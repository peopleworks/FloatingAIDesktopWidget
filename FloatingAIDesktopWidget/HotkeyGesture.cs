using System.Runtime.InteropServices;

namespace FloatingAIDesktopWidget;

internal readonly record struct HotkeyGesture(uint Modifiers, uint VirtualKey, string Display);

internal static class HotkeyGestureParser
{
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    public static bool TryParse(string? gesture, out HotkeyGesture hotkey, out string error)
    {
        hotkey = default;
        error = "";

        if (string.IsNullOrWhiteSpace(gesture))
        {
            error = Strings.ErrorHotkeyEmpty;
            return false;
        }

        var parts = gesture
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToArray();

        if (parts.Length == 0)
        {
            error = Strings.ErrorHotkeyEmpty;
            return false;
        }

        uint modifiers = 0;
        string? keyPart = null;

        foreach (var part in parts)
        {
            if (IsModifier(part, out var mod))
            {
                modifiers |= mod;
                continue;
            }

            if (keyPart is not null)
            {
                error = Strings.ErrorHotkeyMultipleKeys(keyPart, part);
                return false;
            }

            keyPart = part;
        }

        if (keyPart is null)
        {
            error = Strings.ErrorHotkeyMissingKey;
            return false;
        }

        if (modifiers == 0)
        {
            error = Strings.ErrorHotkeyNeedsModifier;
            return false;
        }

        if (!TryParseKey(keyPart, out var key, out var keyError))
        {
            error = keyError;
            return false;
        }

        var normalized = Normalize(modifiers, key);
        hotkey = new HotkeyGesture(modifiers | MOD_NOREPEAT, key, normalized);
        return true;
    }

    private static bool IsModifier(string part, out uint modifier)
    {
        modifier = 0;
        var p = part.Trim();

        if (p.Equals("CTRL", StringComparison.OrdinalIgnoreCase) || p.Equals("CONTROL", StringComparison.OrdinalIgnoreCase))
        {
            modifier = MOD_CONTROL;
            return true;
        }

        if (p.Equals("SHIFT", StringComparison.OrdinalIgnoreCase))
        {
            modifier = MOD_SHIFT;
            return true;
        }

        if (p.Equals("ALT", StringComparison.OrdinalIgnoreCase))
        {
            modifier = MOD_ALT;
            return true;
        }

        if (p.Equals("WIN", StringComparison.OrdinalIgnoreCase) || p.Equals("WINDOWS", StringComparison.OrdinalIgnoreCase) || p.Equals("SUPER", StringComparison.OrdinalIgnoreCase))
        {
            modifier = MOD_WIN;
            return true;
        }

        return false;
    }

    private static bool TryParseKey(string part, out uint virtualKey, out string error)
    {
        virtualKey = 0;
        error = "";

        var p = part.Trim();

        if (p.Length == 1 && char.IsDigit(p[0]))
        {
            virtualKey = (uint)(Keys.D0 + (p[0] - '0'));
            return true;
        }

        if (p.Equals("ESC", StringComparison.OrdinalIgnoreCase))
        {
            virtualKey = (uint)Keys.Escape;
            return true;
        }

        if (p.Equals("PGUP", StringComparison.OrdinalIgnoreCase))
        {
            virtualKey = (uint)Keys.PageUp;
            return true;
        }

        if (p.Equals("PGDN", StringComparison.OrdinalIgnoreCase))
        {
            virtualKey = (uint)Keys.PageDown;
            return true;
        }

        if (!Enum.TryParse<Keys>(p, ignoreCase: true, out var keys))
        {
            error = Strings.ErrorHotkeyUnknownKey(part);
            return false;
        }

        var keyCode = keys & Keys.KeyCode;
        if (keyCode == Keys.None)
        {
            error = Strings.ErrorHotkeyInvalidKey(part);
            return false;
        }

        virtualKey = (uint)keyCode;
        return true;
    }

    private static string Normalize(uint modifiers, uint key)
    {
        var tokens = new List<string>(5);

        if ((modifiers & MOD_CONTROL) != 0) tokens.Add("Ctrl");
        if ((modifiers & MOD_ALT) != 0) tokens.Add("Alt");
        if ((modifiers & MOD_SHIFT) != 0) tokens.Add("Shift");
        if ((modifiers & MOD_WIN) != 0) tokens.Add("Win");

        tokens.Add(((Keys)key).ToString());
        return string.Join('+', tokens);
    }
}

internal static class GlobalHotkey
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    public static bool TryRegister(nint hwnd, int id, HotkeyGesture gesture, out string error)
    {
        error = "";

        if (hwnd == 0)
        {
            error = "No hay handle de ventana para registrar el hotkey.";
            return false;
        }

        _ = UnregisterHotKey(hwnd, id);

        if (RegisterHotKey(hwnd, id, gesture.Modifiers, gesture.VirtualKey))
        {
            return true;
        }

        var win32 = Marshal.GetLastWin32Error();
        error = Strings.ErrorHotkeyRegisterFailed(gesture.Display, win32);
        return false;
    }

    public static void Unregister(nint hwnd, int id)
    {
        if (hwnd == 0)
        {
            return;
        }

        _ = UnregisterHotKey(hwnd, id);
    }
}
