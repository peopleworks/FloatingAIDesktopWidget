using System.Reflection;

namespace FloatingAIDesktopWidget;

internal static class AppPaths
{
    public static string AppBaseDirectory => AppContext.BaseDirectory;

    public static string SettingsPath => Path.Combine(AppBaseDirectory, "appsettings.json");

    public static string AppDataDirectory
    {
        get
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var product = Assembly.GetExecutingAssembly().GetName().Name ?? "FloatingAIDesktopWidget";
            return Path.Combine(baseDir, product);
        }
    }

    public static string WidgetStatePath => Path.Combine(AppDataDirectory, "widget.state.json");

    public static string PreferencesPath => Path.Combine(AppDataDirectory, "widget.prefs.json");

    public static void EnsureAppDataDirectory()
    {
        Directory.CreateDirectory(AppDataDirectory);
    }
}
