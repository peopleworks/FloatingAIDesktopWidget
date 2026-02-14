namespace FloatingAIDesktopWidget;

// Intermediate class for deserializing both legacy (Target) and new (Targets[]) formats
internal sealed class AppSettingsRaw
{
    public TargetSettings? Target { get; init; }       // Legacy format (single target)
    public TargetSettings[]? Targets { get; init; }    // New format (multiple targets)
    public UiSettings? UI { get; init; }
    public HotkeySettings? Hotkey { get; init; }
}

internal sealed class AppSettingsProvider : IDisposable
{
    private readonly object _gate = new();
    private readonly FileSystemWatcher _watcher;
    private readonly string _settingsPath;
    private readonly System.Threading.Timer _debounceTimer;

    private AppSettings _current = new();

    public event EventHandler? SettingsChanged;

    public AppSettingsProvider(string settingsPath)
    {
        _settingsPath = settingsPath;
        Reload();

        var directory = Path.GetDirectoryName(_settingsPath);
        var fileName = Path.GetFileName(_settingsPath);

        _watcher = new FileSystemWatcher(directory ?? AppContext.BaseDirectory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime
        };

        _watcher.Changed += (_, _) => DebounceReload();
        _watcher.Created += (_, _) => DebounceReload();
        _watcher.Renamed += (_, _) => DebounceReload();
        _watcher.Deleted += (_, _) => DebounceReload();
        _watcher.EnableRaisingEvents = true;

        _debounceTimer = new System.Threading.Timer(_ => ReloadFromTimer(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public AppSettings Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    private void DebounceReload()
    {
        _debounceTimer.Change(250, Timeout.Infinite);
    }

    private void ReloadFromTimer()
    {
        Reload();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Reload()
    {
        var loaded = LoadAndMigrate(_settingsPath);

        lock (_gate)
        {
            _current = loaded;
        }
    }

    private static AppSettings LoadAndMigrate(string settingsPath)
    {
        // Try to load as raw format (supports both legacy and new)
        if (!JsonFile.TryRead<AppSettingsRaw>(settingsPath, out var raw) || raw is null)
        {
            return new AppSettings();
        }

        // Determine targets array
        TargetSettings[] targets;

        if (raw.Targets is not null && raw.Targets.Length > 0)
        {
            // New format: use Targets array directly
            targets = raw.Targets;
        }
        else if (raw.Target is not null)
        {
            // Legacy format: convert single Target to array
            targets = [raw.Target];
        }
        else
        {
            // No targets configured
            targets = [];
        }

        // Ensure each target has a Name
        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            if (string.IsNullOrWhiteSpace(t.Name))
            {
                // Generate default name based on index
                var defaultName = Strings.Language == AppLanguage.Es
                    ? $"Asistente {i + 1}"
                    : $"Assistant {i + 1}";

                targets[i] = new TargetSettings
                {
                    Name = defaultName,
                    IconPath = t.IconPath,
                    FileName = t.FileName,
                    Arguments = t.Arguments,
                    WorkingDirectory = t.WorkingDirectory,
                    RunAsAdministrator = t.RunAsAdministrator,
                    SingleInstance = t.SingleInstance,
                    FocusExistingIfRunning = t.FocusExistingIfRunning,
                    AllowCloseFromMenu = t.AllowCloseFromMenu
                };
            }
        }

        return new AppSettings
        {
            Targets = targets,
            UI = raw.UI ?? new UiSettings(),
            Hotkey = raw.Hotkey ?? new HotkeySettings()
        };
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _debounceTimer.Dispose();
    }
}

