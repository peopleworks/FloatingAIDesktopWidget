using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FloatingAIDesktopWidget;

internal static class WindowActivator
{
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    public static bool TryActivate(nint hwnd)
    {
        if (hwnd == 0)
        {
            return false;
        }

        try
        {
            _ = ShowWindowAsync(hwnd, SW_RESTORE);

            var foreground = GetForegroundWindow();
            var foregroundThread = foreground != 0 ? GetWindowThreadProcessId(foreground, out _) : 0;

            var targetThread = GetWindowThreadProcessId(hwnd, out _);
            var currentThread = GetCurrentThreadId();

            var attached = false;
            try
            {
                if (foregroundThread != 0 && foregroundThread != currentThread)
                {
                    attached |= AttachThreadInput(currentThread, foregroundThread, true);
                }

                if (targetThread != 0 && targetThread != currentThread)
                {
                    attached |= AttachThreadInput(currentThread, targetThread, true);
                }

                return SetForegroundWindow(hwnd);
            }
            finally
            {
                try
                {
                    if (targetThread != 0 && targetThread != currentThread)
                    {
                        _ = AttachThreadInput(currentThread, targetThread, false);
                    }

                    if (foregroundThread != 0 && foregroundThread != currentThread)
                    {
                        _ = AttachThreadInput(currentThread, foregroundThread, false);
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch
        {
            return false;
        }
    }

    public static bool TryCloseMainWindow(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return true;
            }

            if (process.MainWindowHandle == 0)
            {
                process.Refresh();
            }

            if (process.MainWindowHandle == 0)
            {
                return false;
            }

            return process.CloseMainWindow();
        }
        catch
        {
            return false;
        }
    }
}

