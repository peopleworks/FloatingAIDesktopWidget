namespace FloatingAIDesktopWidget;

internal sealed class WidgetState
{
    public int X { get; init; }
    public int Y { get; init; }
}

internal static class WidgetStateStore
{
    public static bool TryLoad(out WidgetState? state)
    {
        return JsonFile.TryRead<WidgetState>(AppPaths.WidgetStatePath, out state) && state is not null;
    }

    public static void Save(Point location)
    {
        AppPaths.EnsureAppDataDirectory();
        _ = JsonFile.TryWrite(AppPaths.WidgetStatePath, new WidgetState { X = location.X, Y = location.Y });
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(AppPaths.WidgetStatePath))
            {
                File.Delete(AppPaths.WidgetStatePath);
            }
        }
        catch
        {
            // ignore
        }
    }
}
