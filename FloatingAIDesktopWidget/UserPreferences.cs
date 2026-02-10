namespace FloatingAIDesktopWidget;

internal sealed class UserPreferences
{
    public string? LanguageOverride { get; init; } = null; // "es" | "en" | null
}

internal static class UserPreferencesStore
{
    public static UserPreferences Load()
    {
        if (JsonFile.TryRead<UserPreferences>(AppPaths.PreferencesPath, out var prefs) && prefs is not null)
        {
            return prefs;
        }

        return new UserPreferences();
    }

    public static void Save(UserPreferences prefs)
    {
        AppPaths.EnsureAppDataDirectory();
        _ = JsonFile.TryWrite(AppPaths.PreferencesPath, prefs);
    }
}

