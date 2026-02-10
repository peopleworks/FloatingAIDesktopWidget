using System.Diagnostics;
using System.Linq;

namespace FloatingAIDesktopWidget;

internal static class TargetLauncher
{
    public static bool TryLaunch(TargetSettings target, out string error)
    {
        error = "";

        var fileName = Normalize(target.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            error = Strings.ErrorInvalidConfigEmptyFileName(AppPaths.SettingsPath);
            return false;
        }

        if (target.FocusExistingIfRunning)
        {
            if (TryFindExistingProcess(fileName, out var existing))
            {
                var hwnd = existing.MainWindowHandle;
                if (hwnd == 0)
                {
                    try
                    {
                        existing.Refresh();
                        hwnd = existing.MainWindowHandle;
                    }
                    catch
                    {
                        // ignore
                    }
                }

                if (hwnd != 0)
                {
                    _ = WindowActivator.TryActivate(hwnd);
                }

                if (target.SingleInstance)
                {
                    return true;
                }
            }
        }

        var args = target.Arguments ?? "";
        var workingDirectory = Normalize(target.WorkingDirectory);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            UseShellExecute = true
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            psi.WorkingDirectory = workingDirectory;
        }
        else
        {
            try
            {
                var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(fileName));
                var dir = Path.GetDirectoryName(full);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    psi.WorkingDirectory = dir;
                }
            }
            catch
            {
                // ignore
            }
        }

        if (target.RunAsAdministrator)
        {
            psi.Verb = "runas";
        }

        try
        {
            _ = Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            error = Strings.ErrorCouldNotStart(fileName, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    public static bool TryCloseExisting(TargetSettings target, out string error)
    {
        error = "";

        var fileName = Normalize(target.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            error = Strings.ErrorInvalidConfigEmptyFileName(AppPaths.SettingsPath);
            return false;
        }

        if (!TryFindExistingProcess(fileName, out var existing))
        {
            error = Strings.ErrorAssistantNotRunning;
            return false;
        }

        if (!WindowActivator.TryCloseMainWindow(existing))
        {
            error = Strings.ErrorCouldNotCloseAssistant;
            return false;
        }

        return true;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var expanded = Environment.ExpandEnvironmentVariables(value.Trim());
        if (expanded.Length >= 2 && expanded[0] == '"' && expanded[^1] == '"')
        {
            expanded = expanded[1..^1];
        }

        return expanded.Trim();
    }

    private static bool TryFindExistingProcess(string configuredFileName, out Process existing)
    {
        existing = default!;

        string? expectedFullPath = null;
        string? expectedProcessName = null;

        try
        {
            var expanded = Environment.ExpandEnvironmentVariables(configuredFileName);
            expectedFullPath = Path.GetFullPath(expanded);
            expectedProcessName = Path.GetFileNameWithoutExtension(expectedFullPath);
        }
        catch
        {
            try
            {
                expectedProcessName = Path.GetFileNameWithoutExtension(configuredFileName);
            }
            catch
            {
                expectedProcessName = null;
            }
        }

        if (string.IsNullOrWhiteSpace(expectedProcessName))
        {
            return false;
        }

        var candidates = Process.GetProcessesByName(expectedProcessName);
        if (candidates.Length == 0)
        {
            return false;
        }

        Process? best = null;
        foreach (var p in candidates)
        {
            try
            {
                if (p.HasExited)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(expectedFullPath))
                {
                    try
                    {
                        var modulePath = p.MainModule?.FileName;
                        if (!string.IsNullOrWhiteSpace(modulePath))
                        {
                            var full = Path.GetFullPath(modulePath);
                            if (string.Equals(full, expectedFullPath, StringComparison.OrdinalIgnoreCase))
                            {
                                best = p;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // AccessDenied on some processes/UIPI; fall back to window handle heuristic.
                    }
                }

                if (best is null && p.MainWindowHandle != 0)
                {
                    best = p;
                }
            }
            catch
            {
                // ignore
            }
        }

        best ??= candidates.FirstOrDefault(p =>
        {
            try { return !p.HasExited; } catch { return false; }
        });

        if (best is null)
        {
            return false;
        }

        existing = best;
        return true;
    }
}
