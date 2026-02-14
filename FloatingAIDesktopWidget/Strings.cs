using System.Globalization;

namespace FloatingAIDesktopWidget;

internal enum AppLanguage
{
    Auto,
    Es,
    En
}

internal static class Strings
{
    private static readonly object Gate = new();
    private static AppLanguage _lang = AppLanguage.Auto;

    public static AppLanguage Language
    {
        get
        {
            lock (Gate)
            {
                return _lang;
            }
        }
    }

    public static void SetLanguage(string? language)
    {
        var parsed = ParseLanguage(language);
        lock (Gate)
        {
            _lang = parsed;
        }
    }

    public static string UiName => "FloatingAIDesktopWidget";

    public static string MenuOpen => IsEn ? "Open assistant" : "Abrir asistente";
    public static string MenuClose => IsEn ? "Close assistant" : "Cerrar asistente";
    public static string MenuSelectTarget => IsEn ? "Select application..." : "Seleccionar aplicacion...";
    public static string MenuCloseAll => IsEn ? "Close all assistants" : "Cerrar todos los asistentes";
    public static string MenuEditSettings => IsEn ? "Edit appsettings.json" : "Editar appsettings.json";
    public static string MenuReload => IsEn ? "Reload configuration" : "Recargar configuracion";
    public static string MenuResetPosition => IsEn ? "Reset position" : "Restablecer posicion";
    public static string MenuExit => IsEn ? "Exit" : "Salir";

    public static string MenuLanguage => IsEn ? "Language" : "Idioma";
    public static string MenuLangAuto => IsEn ? "From settings" : "Desde configuracion";
    public static string MenuLangEs => IsEn ? "Espanol" : "Espanol";
    public static string MenuLangEn => IsEn ? "English" : "English";

    public static string BalloonAlreadyRunning => IsEn ? "Already running." : "Ya estaba en ejecucion.";
    public static string BalloonClosingAssistant => IsEn ? "Closing assistant..." : "Cerrando asistente...";
    public static string BalloonOptionDisabled => IsEn ? "Option disabled in appsettings.json." : "Opcion deshabilitada en appsettings.json.";

    public static string HotkeyTitle => IsEn ? "Hotkey" : "Hotkey";

    public static string ErrorNoTargetsConfigured(string settingsPath) =>
        IsEn
            ? $"No targets configured in {settingsPath}."
            : $"No hay targets configurados en {settingsPath}.";

    public static string ErrorInvalidConfigEmptyFileName(string settingsPath) =>
        IsEn
            ? $"Invalid config: 'Target:FileName' is empty. Edit {settingsPath}."
            : $"Config invalida: 'Target:FileName' esta vacio. Edita {settingsPath}.";

    public static string ErrorCouldNotStart(string fileName, string typeName, string message) =>
        IsEn
            ? $"Could not start '{fileName}'. {typeName}: {message}"
            : $"No se pudo iniciar '{fileName}'. {typeName}: {message}";

    public static string ErrorAssistantNotRunning =>
        IsEn ? "Assistant doesn't seem to be running." : "El asistente no parece estar ejecutandose.";

    public static string ErrorCouldNotCloseAssistant =>
        IsEn
            ? "Could not close assistant (no main window or requires higher privileges)."
            : "No se pudo cerrar el asistente (no tiene ventana principal o requiere mas privilegios).";

    public static string ErrorHotkeyEmpty =>
        IsEn ? "Hotkey.Gesture is empty." : "Hotkey.Gesture esta vacio.";

    public static string ErrorHotkeyMissingKey =>
        IsEn ? "Invalid Hotkey.Gesture: missing key (ex: Ctrl+Shift+Space)." : "Hotkey.Gesture invalido: falta la tecla (ej: Ctrl+Shift+Space).";

    public static string ErrorHotkeyNeedsModifier =>
        IsEn ? "Invalid Hotkey.Gesture: include at least one modifier (Ctrl/Alt/Shift/Win)." : "Hotkey.Gesture invalido: debe incluir al menos un modificador (Ctrl/Alt/Shift/Win).";

    public static string ErrorHotkeyMultipleKeys(string a, string b) =>
        IsEn
            ? $"Invalid Hotkey.Gesture: more than one key detected ('{a}' and '{b}')."
            : $"Hotkey.Gesture invalido: mas de una tecla detectada ('{a}' y '{b}').";

    public static string ErrorHotkeyUnknownKey(string part) =>
        IsEn
            ? $"Key '{part}' not recognized. Use something like 'Space', 'F8', 'A', 'Oemtilde'."
            : $"Tecla '{part}' no reconocida. Usa algo como 'Space', 'F8', 'A', 'Oemtilde'.";

    public static string ErrorHotkeyInvalidKey(string part) =>
        IsEn ? $"Key '{part}' is not valid." : $"Tecla '{part}' no valida.";

    public static string ErrorHotkeyRegisterFailed(string display, int win32) =>
        IsEn
            ? $"Could not register hotkey '{display}' (Win32={win32}). It's likely already used by another app."
            : $"No se pudo registrar el hotkey '{display}' (Win32={win32}). Probablemente ya lo usa otra aplicacion.";

    private static bool IsEn
    {
        get
        {
            var lang = Language;
            if (lang == AppLanguage.En) return true;
            if (lang == AppLanguage.Es) return false;

            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return culture.Equals("en", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static AppLanguage ParseLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return AppLanguage.Auto;
        }

        var v = language.Trim().ToLowerInvariant();
        return v switch
        {
            "auto" => AppLanguage.Auto,
            "es" or "spa" or "spanish" or "espanol" => AppLanguage.Es,
            "en" or "eng" or "english" => AppLanguage.En,
            _ => AppLanguage.Auto
        };
    }
}
