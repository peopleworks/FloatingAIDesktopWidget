namespace FloatingAIDesktopWidget;

public sealed class AppSettings
{
    public TargetSettings[] Targets { get; init; } = [];
    public UiSettings UI { get; init; } = new();
    public HotkeySettings Hotkey { get; init; } = new();
}

public sealed class TargetSettings
{
    public string? Name { get; init; }
    public string? IconPath { get; init; }
    public string? FileName { get; init; }
    public string? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public bool RunAsAdministrator { get; init; }
    public bool SingleInstance { get; init; } = true;
    public bool FocusExistingIfRunning { get; init; } = true;
    public bool AllowCloseFromMenu { get; init; } = true;
}

public sealed class UiSettings
{
    public int Size { get; init; } = 64;
    public int Margin { get; init; } = 16;
    public bool AlwaysOnTop { get; init; } = true;
    public bool ShowInTaskbar { get; init; } = false;
    public bool SnapToEdge { get; init; } = true;
    public double Opacity { get; init; } = 1.0;
    public string? IconPath { get; init; }
    public string? Language { get; init; } = "auto";

    // Button background style
    public string BackgroundStyle { get; init; } = "auto"; // "auto", "light", "dark", "transparent", "custom"
    public string? BackgroundColorTop { get; init; }       // Hex color for custom gradient top (e.g., "#1E90FF")
    public string? BackgroundColorBottom { get; init; }    // Hex color for custom gradient bottom (e.g., "#1C86EE")

    // Multi-target mode
    public string MultiTargetMode { get; init; } = "ContextMenu"; // "ContextMenu", "RadialCustom", "Satellites"
}

public sealed class HotkeySettings
{
    public bool Enabled { get; init; } = false;
    public string? Gesture { get; init; } = "Ctrl+Shift+Space";
}
