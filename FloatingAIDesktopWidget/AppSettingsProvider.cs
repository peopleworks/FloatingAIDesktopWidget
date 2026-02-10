namespace FloatingAIDesktopWidget;

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
        if (!JsonFile.TryRead<AppSettings>(_settingsPath, out var loaded) || loaded is null)
        {
            loaded = new AppSettings();
        }

        lock (_gate)
        {
            _current = loaded;
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _debounceTimer.Dispose();
    }
}

