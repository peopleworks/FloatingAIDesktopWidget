using System.Drawing.Drawing2D;
using System.Linq;

namespace FloatingAIDesktopWidget;

internal sealed class WidgetForm : Form
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0xFAD1;

    private readonly AppSettingsProvider _settingsProvider;
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _menu;
    private readonly EventHandler _settingsChangedHandler;

    private Image? _iconImage;
    private Icon? _trayIconImage;
    private bool _hover;
    private bool _pressed;

    private UserPreferences _prefs;

    private readonly ToolStripMenuItem _miOpen;
    private readonly ToolStripMenuItem _miClose;
    private readonly ToolStripMenuItem _miEditSettings;
    private readonly ToolStripMenuItem _miReload;
    private readonly ToolStripMenuItem _miResetPos;
    private readonly ToolStripMenuItem _miExit;
    private readonly ToolStripMenuItem _miLanguage;
    private readonly ToolStripMenuItem _miLangAuto;
    private readonly ToolStripMenuItem _miLangEs;
    private readonly ToolStripMenuItem _miLangEn;

    private bool _trackingDrag;
    private bool _movedDuringDrag;
    private Point _mouseDownScreen;
    private Point _windowDownLocation;

    private HotkeySettings _hotkeySettings = new();
    private SatelliteManager? _satelliteManager;
    private RadialMenuForm? _radialMenuForm;

    public WidgetForm(AppSettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
        _prefs = UserPreferencesStore.Load();

        _settingsChangedHandler = (_, _) =>
        {
            try
            {
                BeginInvoke(ApplySettingsSafe);
            }
            catch
            {
                // ignore
            }
        };
        _settingsProvider.SettingsChanged += _settingsChangedHandler;

        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        DoubleBuffered = true;

        _menu = new ContextMenuStrip();

        _miOpen = new ToolStripMenuItem("", null, (_, _) => LaunchTarget());
        _miClose = new ToolStripMenuItem("", null, (_, _) => CloseTarget());
        _miEditSettings = new ToolStripMenuItem("", null, (_, _) => OpenSettingsFile());
        _miReload = new ToolStripMenuItem("", null, (_, _) => ApplySettingsSafe());
        _miResetPos = new ToolStripMenuItem("", null, (_, _) =>
        {
            WidgetStateStore.Clear();
            MoveToInitialLocation();
            WidgetStateStore.Save(Location);
        });
        _miExit = new ToolStripMenuItem("", null, (_, _) => Application.Exit());

        _miLangAuto = new ToolStripMenuItem("", null, (_, _) => SetLanguageOverride(null));
        _miLangEs = new ToolStripMenuItem("", null, (_, _) => SetLanguageOverride("es"));
        _miLangEn = new ToolStripMenuItem("", null, (_, _) => SetLanguageOverride("en"));
        _miLanguage = new ToolStripMenuItem("", null, (_, _) => { })
        {
            DropDownItems = { _miLangAuto, _miLangEs, _miLangEn }
        };

        _menu.Items.AddRange(
        [
            _miOpen,
            _miClose,
            new ToolStripSeparator(),
            _miEditSettings,
            _miReload,
            _miResetPos,
            new ToolStripSeparator(),
            _miLanguage,
            new ToolStripSeparator(),
            _miExit
        ]);

        _trayIconImage = IconFactory.CreateTrayIcon(32);
        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Icon = _trayIconImage,
            Text = Strings.UiName,
            ContextMenuStrip = _menu
        };
        _trayIcon.MouseDoubleClick += (_, _) => LaunchTarget();

        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; _pressed = false; Invalidate(); };

        ApplySettingsSafe();
    }

    protected override bool ShowWithoutActivation => true;

    public void Nudge()
    {
        try
        {
            Show();
            var wasTopMost = TopMost;
            TopMost = true;
            TopMost = wasTopMost;
            _trayIcon.ShowBalloonTip(1000, Strings.UiName, Strings.BalloonAlreadyRunning, ToolTipIcon.Info);
        }
        catch
        {
            // ignore
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyHotkey(_hotkeySettings, showErrorBalloon: false);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        GlobalHotkey.Unregister(Handle, HotkeyId);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && (int)m.WParam == HotkeyId)
        {
            LaunchTarget();
            return;
        }

        base.WndProc(ref m);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;

            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        MoveToInitialLocation();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            _trackingDrag = true;
            _movedDuringDrag = false;
            _mouseDownScreen = PointToScreen(e.Location);
            _windowDownLocation = Location;
            Invalidate();
        }
        else if (e.Button == MouseButtons.Right)
        {
            _menu.Show(Cursor.Position);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_trackingDrag || e.Button != MouseButtons.Left)
        {
            return;
        }

        var currentScreen = PointToScreen(e.Location);
        var dx = currentScreen.X - _mouseDownScreen.X;
        var dy = currentScreen.Y - _mouseDownScreen.Y;

        if (!_movedDuringDrag && (Math.Abs(dx) > 3 || Math.Abs(dy) > 3))
        {
            _movedDuringDrag = true;
        }

        var next = new Point(_windowDownLocation.X + dx, _windowDownLocation.Y + dy);
        Location = ClampToWorkingArea(next, Size);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _trackingDrag = false;
        _pressed = false;
        Invalidate();

        if (_movedDuringDrag)
        {
            var ui = _settingsProvider.Current.UI;
            if (ui.SnapToEdge)
            {
                SnapToEdge(ui.Margin);
            }

            WidgetStateStore.Save(Location);
            return;
        }

        LaunchTarget();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = ClientRectangle;
        rect.Inflate(-1, -1);

        // Get background colors based on configuration
        var (top, bottom) = GetBackgroundColors();
        using var fill = new LinearGradientBrush(rect, top, bottom, LinearGradientMode.Vertical);
        e.Graphics.FillEllipse(fill, rect);

        using var border = new Pen(Color.FromArgb(180, 255, 255, 255), 1f);
        e.Graphics.DrawEllipse(border, rect);

        DrawIcon(e.Graphics, rect);
    }

    private (Color top, Color bottom) GetBackgroundColors()
    {
        var ui = _settingsProvider.Current.UI;
        var style = ui.BackgroundStyle?.ToLowerInvariant() ?? "auto";

        // State-based color adjustments (pressed/hover)
        var stateFactor = _pressed ? 0.7 : _hover ? 1.15 : 1.0;

        switch (style)
        {
            case "light":
                // Light background - good for dark icons with transparency
                return (
                    AdjustBrightness(Color.FromArgb(255, 240, 240, 245), stateFactor),
                    AdjustBrightness(Color.FromArgb(255, 220, 220, 230), stateFactor)
                );

            case "dark":
                // Dark background - good for light icons
                return (
                    AdjustBrightness(Color.FromArgb(255, 60, 60, 75), stateFactor),
                    AdjustBrightness(Color.FromArgb(255, 35, 35, 45), stateFactor)
                );

            case "transparent":
                // Minimal background - let icon transparency show
                return (
                    AdjustBrightness(Color.FromArgb(100, 255, 255, 255), stateFactor),
                    AdjustBrightness(Color.FromArgb(80, 240, 240, 250), stateFactor)
                );

            case "custom":
                // User-defined custom colors
                var customTop = TryParseHexColor(ui.BackgroundColorTop) ?? Color.FromArgb(255, 0, 102, 204);
                var customBottom = TryParseHexColor(ui.BackgroundColorBottom) ?? Color.FromArgb(255, 0, 89, 179);
                return (
                    AdjustBrightness(customTop, stateFactor),
                    AdjustBrightness(customBottom, stateFactor)
                );

            case "auto":
            default:
                // Auto mode: detect if using custom icon with transparency
                if (_iconImage != null && HasTransparency(_iconImage))
                {
                    // Icon has transparency - use light background
                    return (
                        AdjustBrightness(Color.FromArgb(255, 245, 245, 250), stateFactor),
                        AdjustBrightness(Color.FromArgb(255, 225, 225, 240), stateFactor)
                    );
                }
                else
                {
                    // Default dark background (original behavior)
                    return (
                        AdjustBrightness(Color.FromArgb(255, 60, 60, 75), stateFactor),
                        AdjustBrightness(Color.FromArgb(255, 35, 35, 45), stateFactor)
                    );
                }
        }
    }

    private static Color AdjustBrightness(Color color, double factor)
    {
        if (factor == 1.0) return color;

        var r = Math.Clamp((int)(color.R * factor), 0, 255);
        var g = Math.Clamp((int)(color.G * factor), 0, 255);
        var b = Math.Clamp((int)(color.B * factor), 0, 255);
        return Color.FromArgb(color.A, r, g, b);
    }

    private static bool HasTransparency(Image image)
    {
        if (image is not Bitmap bitmap) return false;

        // Quick check: sample some pixels to detect transparency
        try
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var samples = Math.Min(100, width * height / 10); // Sample ~10% of pixels, max 100

            for (int i = 0; i < samples; i++)
            {
                var x = (i * 7) % width;  // Pseudo-random sampling
                var y = (i * 11) % height;
                var pixel = bitmap.GetPixel(x, y);

                if (pixel.A < 255) return true; // Found transparency
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static Color? TryParseHexColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;

        try
        {
            hex = hex.Trim();
            if (hex.StartsWith("#")) hex = hex[1..];

            if (hex.Length == 6)
            {
                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);
                return Color.FromArgb(255, r, g, b);
            }
            else if (hex.Length == 8)
            {
                var a = Convert.ToByte(hex.Substring(0, 2), 16);
                var r = Convert.ToByte(hex.Substring(2, 2), 16);
                var g = Convert.ToByte(hex.Substring(4, 2), 16);
                var b = Convert.ToByte(hex.Substring(6, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _settingsProvider.SettingsChanged -= _settingsChangedHandler;
            GlobalHotkey.Unregister(Handle, HotkeyId);
            _satelliteManager?.Dispose();
            _radialMenuForm?.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _menu.Dispose();
            _iconImage?.Dispose();
            _trayIconImage?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ApplySettingsSafe()
    {
        try
        {
            _settingsProvider.Reload();
            ApplySettings(_settingsProvider.Current);
        }
        catch
        {
            // ignore
        }
    }

    private void ApplySettings(AppSettings settings)
    {
        var ui = settings.UI;

        ApplyLanguage(ui.Language);
        ApplyLocalization();

        var size = Math.Clamp(ui.Size, 40, 200);
        ClientSize = new Size(size, size);

        TopMost = ui.AlwaysOnTop;
        ShowInTaskbar = ui.ShowInTaskbar;

        var opacity = ui.Opacity;
        if (double.IsNaN(opacity) || opacity < 0.2 || opacity > 1.0)
        {
            opacity = 1.0;
        }
        Opacity = opacity;

        _iconImage?.Dispose();
        _iconImage = TryLoadIconImage(ui.IconPath) ?? IconFactory.CreateDefaultButtonImage(size);
        RefreshTrayIcon();

        _hotkeySettings = settings.Hotkey ?? new HotkeySettings();
        ApplyHotkey(_hotkeySettings, showErrorBalloon: true);

        UpdateRegion();
        Location = ClampToWorkingArea(Location, Size);
        Invalidate();
    }

    private void ApplyHotkey(HotkeySettings settings, bool showErrorBalloon)
    {
        try
        {
            if (!IsHandleCreated)
            {
                return;
            }

            if (!settings.Enabled)
            {
                GlobalHotkey.Unregister(Handle, HotkeyId);
                return;
            }

            if (!HotkeyGestureParser.TryParse(settings.Gesture, out var gesture, out var parseError))
            {
                GlobalHotkey.Unregister(Handle, HotkeyId);

                if (showErrorBalloon)
                {
                    _trayIcon.ShowBalloonTip(4000, "Hotkey", parseError, ToolTipIcon.Warning);
                }

                return;
            }

            if (!GlobalHotkey.TryRegister(Handle, HotkeyId, gesture, out var registerError))
            {
                if (showErrorBalloon)
                {
                    _trayIcon.ShowBalloonTip(5000, "Hotkey", registerError, ToolTipIcon.Warning);
                }

                return;
            }
        }
        catch
        {
            // ignore
        }
    }

    private void UpdateRegion()
    {
        using var path = new GraphicsPath();
        path.AddEllipse(new Rectangle(0, 0, Width, Height));
        Region = new Region(path);
    }

    private void MoveToInitialLocation()
    {
        if (WidgetStateStore.TryLoad(out var state) && state is not null)
        {
            var loaded = new Point(state.X, state.Y);
            var clamped = ClampToWorkingArea(loaded, Size);
            Location = clamped;
            return;
        }

        var ui = _settingsProvider.Current.UI;
        var margin = Math.Max(0, ui.Margin);

        var working = Screen.PrimaryScreen?.WorkingArea ?? Screen.GetWorkingArea(Cursor.Position);
        Location = new Point(working.Right - Width - margin, working.Bottom - Height - margin);
    }

    private static Point ClampToWorkingArea(Point location, Size windowSize)
    {
        var center = new Point(location.X + (windowSize.Width / 2), location.Y + (windowSize.Height / 2));
        var screen = Screen.FromPoint(center);
        var working = screen.WorkingArea;

        var x = Math.Clamp(location.X, working.Left, working.Right - windowSize.Width);
        var y = Math.Clamp(location.Y, working.Top, working.Bottom - windowSize.Height);
        return new Point(x, y);
    }

    private void SnapToEdge(int margin)
    {
        margin = Math.Max(0, margin);

        var center = new Point(Left + (Width / 2), Top + (Height / 2));
        var screen = Screen.FromPoint(center);
        var working = screen.WorkingArea;

        var distLeft = Math.Abs(Left - working.Left);
        var distRight = Math.Abs((working.Right - Width) - Left);
        var x = distLeft <= distRight ? working.Left + margin : (working.Right - Width - margin);

        var y = Math.Clamp(Top, working.Top + margin, working.Bottom - Height - margin);
        Location = new Point(x, y);
    }

    private void LaunchTarget()
    {
        // If satellites are open, close them instead of opening new ones
        if (_satelliteManager != null)
        {
            _satelliteManager.Hide();
            return;
        }

        // If radial menu is open, close it
        if (_radialMenuForm != null && !_radialMenuForm.IsDisposed)
        {
            _radialMenuForm.Close();
            _radialMenuForm = null;
            return;
        }

        var targets = _settingsProvider.Current.Targets;

        if (targets.Length == 0)
        {
            _trayIcon.ShowBalloonTip(3000, Strings.UiName, Strings.ErrorNoTargetsConfigured(AppPaths.SettingsPath), ToolTipIcon.Warning);
            return;
        }

        if (targets.Length == 1)
        {
            // Single target: launch directly
            LaunchSpecificTarget(targets[0]);
            return;
        }

        // Multiple targets: show selection menu
        ShowTargetSelectionMenu();
    }

    private void ShowTargetSelectionMenu()
    {
        var targets = _settingsProvider.Current.Targets;
        var mode = _settingsProvider.Current.UI.MultiTargetMode ?? "ContextMenu";

        if (mode.Equals("Satellites", StringComparison.OrdinalIgnoreCase))
        {
            ShowSatellites(targets);
        }
        else if (mode.Equals("RadialCustom", StringComparison.OrdinalIgnoreCase))
        {
            ShowRadialMenu(targets);
        }
        else
        {
            ShowContextMenu(targets);
        }
    }

    private void ShowSatellites(TargetSettings[] targets)
    {
        try
        {
            // Close previous satellites if any
            _satelliteManager?.Dispose();

            // Calculate center position of the widget
            var centerPos = new Point(
                Location.X + Width / 2,
                Location.Y + Height / 2
            );

            _satelliteManager = new SatelliteManager();
            _satelliteManager.SatelliteClicked += (_, target) =>
            {
                _satelliteManager.Hide();
                LaunchSpecificTarget(target);
            };
            _satelliteManager.Closed += (_, _) =>
            {
                _satelliteManager?.Dispose();
                _satelliteManager = null;
            };

            _satelliteManager.Show(centerPos, targets);
        }
        catch
        {
            // Fallback to context menu if satellites fail
            ShowContextMenu(targets);
        }
    }

    private void ShowRadialMenu(TargetSettings[] targets)
    {
        try
        {
            _radialMenuForm?.Dispose();
            _radialMenuForm = new RadialMenuForm(targets);
            _radialMenuForm.ItemSelected += (_, target) =>
            {
                LaunchSpecificTarget(target);
                _radialMenuForm = null;
            };
            _radialMenuForm.FormClosed += (_, _) =>
            {
                _radialMenuForm = null;
            };
            _radialMenuForm.ShowAt(Cursor.Position);
        }
        catch
        {
            // Fallback to context menu if radial fails
            ShowContextMenu(targets);
        }
    }

    private void ShowContextMenu(TargetSettings[] targets)
    {
        var menu = new ContextMenuStrip();

        foreach (var target in targets)
        {
            var name = string.IsNullOrWhiteSpace(target.Name) ? Strings.MenuOpen : target.Name;
            var menuItem = new ToolStripMenuItem(name, null, (_, _) => LaunchSpecificTarget(target));

            // Try to load target-specific icon
            var icon = TryLoadIconImage(target.IconPath);
            if (icon is not null)
            {
                try
                {
                    menuItem.Image = icon;
                    menuItem.ImageScaling = ToolStripItemImageScaling.SizeToFit;
                }
                catch
                {
                    icon.Dispose();
                }
            }

            menu.Items.Add(menuItem);
        }

        menu.Show(Cursor.Position);
    }

    private void LaunchSpecificTarget(TargetSettings target)
    {
        if (TargetLauncher.TryLaunch(target, out var error))
        {
            return;
        }

        _trayIcon.BalloonTipTitle = Strings.UiName;
        _trayIcon.BalloonTipText = error;
        _trayIcon.ShowBalloonTip(4000);
    }

    private void CloseTarget()
    {
        var targets = _settingsProvider.Current.Targets;

        if (targets.Length == 0)
        {
            _trayIcon.ShowBalloonTip(3000, Strings.UiName, Strings.ErrorNoTargetsConfigured(AppPaths.SettingsPath), ToolTipIcon.Warning);
            return;
        }

        if (targets.Length == 1)
        {
            // Single target: close directly
            CloseSpecificTarget(targets[0]);
            return;
        }

        // Multiple targets: show submenu to select which one to close
        ShowCloseTargetMenu();
    }

    private void ShowCloseTargetMenu()
    {
        var targets = _settingsProvider.Current.Targets;
        var mode = _settingsProvider.Current.UI.MultiTargetMode ?? "ContextMenu";

        // Filter to only closeable targets
        var closeableTargets = targets.Where(t => t.AllowCloseFromMenu).ToArray();

        if (closeableTargets.Length == 0)
        {
            _trayIcon.ShowBalloonTip(3000, Strings.UiName, Strings.BalloonOptionDisabled, ToolTipIcon.Info);
            return;
        }

        if (mode.Equals("Satellites", StringComparison.OrdinalIgnoreCase))
        {
            ShowSatellitesClose(closeableTargets);
        }
        else if (mode.Equals("RadialCustom", StringComparison.OrdinalIgnoreCase))
        {
            ShowRadialCloseMenu(closeableTargets);
        }
        else
        {
            ShowContextCloseMenu(closeableTargets);
        }
    }

    private void ShowSatellitesClose(TargetSettings[] targets)
    {
        try
        {
            // Close previous satellites if any
            _satelliteManager?.Dispose();

            // Calculate center position of the widget
            var centerPos = new Point(
                Location.X + Width / 2,
                Location.Y + Height / 2
            );

            _satelliteManager = new SatelliteManager();
            _satelliteManager.SatelliteClicked += (_, target) =>
            {
                _satelliteManager.Hide();
                CloseSpecificTarget(target);
            };
            _satelliteManager.Closed += (_, _) =>
            {
                _satelliteManager?.Dispose();
                _satelliteManager = null;
            };

            _satelliteManager.Show(centerPos, targets);
        }
        catch
        {
            // Fallback to context menu if satellites fail
            ShowContextCloseMenu(targets);
        }
    }

    private void ShowRadialCloseMenu(TargetSettings[] targets)
    {
        try
        {
            _radialMenuForm?.Dispose();
            _radialMenuForm = new RadialMenuForm(targets);
            _radialMenuForm.ItemSelected += (_, target) =>
            {
                CloseSpecificTarget(target);
                _radialMenuForm = null;
            };
            _radialMenuForm.FormClosed += (_, _) =>
            {
                _radialMenuForm = null;
            };
            _radialMenuForm.ShowAt(Cursor.Position);
        }
        catch
        {
            // Fallback to context menu if radial fails
            ShowContextCloseMenu(targets);
        }
    }

    private void ShowContextCloseMenu(TargetSettings[] targets)
    {
        var menu = new ContextMenuStrip();

        // Add option to close each target individually
        foreach (var target in targets)
        {
            var name = string.IsNullOrWhiteSpace(target.Name) ? Strings.MenuClose : target.Name;
            var menuItem = new ToolStripMenuItem(name, null, (_, _) => CloseSpecificTarget(target));

            // Try to load target-specific icon
            var icon = TryLoadIconImage(target.IconPath);
            if (icon is not null)
            {
                try
                {
                    menuItem.Image = icon;
                    menuItem.ImageScaling = ToolStripItemImageScaling.SizeToFit;
                }
                catch
                {
                    icon.Dispose();
                }
            }

            menu.Items.Add(menuItem);
        }

        // Add separator and "Close all" option
        if (menu.Items.Count > 1)
        {
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(Strings.MenuCloseAll, null, (_, _) => CloseAllTargets()));
        }

        menu.Show(Cursor.Position);
    }

    private void CloseSpecificTarget(TargetSettings target)
    {
        if (!target.AllowCloseFromMenu)
        {
            _trayIcon.ShowBalloonTip(3000, Strings.UiName, Strings.BalloonOptionDisabled, ToolTipIcon.Info);
            return;
        }

        if (TargetLauncher.TryCloseExisting(target, out var error))
        {
            _trayIcon.ShowBalloonTip(2000, Strings.UiName, Strings.BalloonClosingAssistant, ToolTipIcon.Info);
            return;
        }

        _trayIcon.ShowBalloonTip(5000, Strings.UiName, error, ToolTipIcon.Warning);
    }

    private void CloseAllTargets()
    {
        var targets = _settingsProvider.Current.Targets;
        var closedCount = 0;

        foreach (var target in targets)
        {
            if (!target.AllowCloseFromMenu)
            {
                continue;
            }

            if (TargetLauncher.TryCloseExisting(target, out _))
            {
                closedCount++;
            }
        }

        if (closedCount > 0)
        {
            var message = Strings.Language == AppLanguage.Es
                ? $"Cerrando {closedCount} asistente(s)..."
                : $"Closing {closedCount} assistant(s)...";
            _trayIcon.ShowBalloonTip(2000, Strings.UiName, message, ToolTipIcon.Info);
        }
        else
        {
            _trayIcon.ShowBalloonTip(3000, Strings.UiName, Strings.ErrorAssistantNotRunning, ToolTipIcon.Warning);
        }
    }

    private void SetLanguageOverride(string? language)
    {
        try
        {
            var next = new UserPreferences { LanguageOverride = language };
            UserPreferencesStore.Save(next);
            _prefs = next;
            ApplySettingsSafe();
        }
        catch
        {
            // ignore
        }
    }

    private void ApplyLanguage(string? configuredLanguage)
    {
        var language = string.IsNullOrWhiteSpace(_prefs.LanguageOverride) ? configuredLanguage : _prefs.LanguageOverride;
        Strings.SetLanguage(language);
    }

    private void ApplyLocalization()
    {
        _trayIcon.Text = Strings.UiName;

        var targets = _settingsProvider.Current.Targets;

        // Update menu text based on number of targets
        if (targets.Length > 1)
        {
            _miOpen.Text = Strings.MenuSelectTarget;
            _miClose.Text = Strings.MenuSelectTarget;
        }
        else
        {
            _miOpen.Text = Strings.MenuOpen;
            _miClose.Text = Strings.MenuClose;
        }

        _miEditSettings.Text = Strings.MenuEditSettings;
        _miReload.Text = Strings.MenuReload;
        _miResetPos.Text = Strings.MenuResetPosition;
        _miExit.Text = Strings.MenuExit;

        _miLanguage.Text = Strings.MenuLanguage;
        _miLangAuto.Text = Strings.MenuLangAuto;
        _miLangEs.Text = Strings.MenuLangEs;
        _miLangEn.Text = Strings.MenuLangEn;

        var langOverride = _prefs.LanguageOverride;
        _miLangAuto.Checked = string.IsNullOrWhiteSpace(langOverride);
        _miLangEs.Checked = string.Equals(langOverride, "es", StringComparison.OrdinalIgnoreCase);
        _miLangEn.Checked = string.Equals(langOverride, "en", StringComparison.OrdinalIgnoreCase);
    }

    private void OpenSettingsFile()
    {
        try
        {
            _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppPaths.SettingsPath,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignore
        }
    }

    private void RefreshTrayIcon()
    {
        try
        {
            _trayIconImage?.Dispose();

            // For multi-target: use generic icon or first target's icon
            var targets = _settingsProvider.Current.Targets;
            if (targets.Length > 1)
            {
                // Multiple targets: use generic icon or widget icon
                _trayIconImage = _iconImage is null ? IconFactory.CreateTrayIcon(32) : IconFactory.CreateIconFromImage(_iconImage, 32);
            }
            else
            {
                // Single or no target: use widget icon
                _trayIconImage = _iconImage is null ? IconFactory.CreateTrayIcon(32) : IconFactory.CreateIconFromImage(_iconImage, 32);
            }

            _trayIcon.Icon = _trayIconImage;
        }
        catch
        {
            // ignore
        }
    }

    private void DrawIcon(Graphics graphics, Rectangle rect)
    {
        var padding = Math.Max(6, rect.Width / 6);
        var inner = Rectangle.Inflate(rect, -padding, -padding);

        if (_iconImage is not null)
        {
            graphics.DrawImage(_iconImage, inner);
            return;
        }

        using var font = new Font(FontFamily.GenericSansSerif, inner.Width / 3f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.White);
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString("AI", font, brush, inner, format);
    }

    private static Image? TryLoadIconImage(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        try
        {
            var expanded = Environment.ExpandEnvironmentVariables(iconPath.Trim());
            if (!Path.IsPathRooted(expanded))
            {
                expanded = Path.Combine(AppPaths.AppBaseDirectory, expanded);
            }

            if (!File.Exists(expanded))
            {
                return null;
            }

            return Image.FromFile(expanded);
        }
        catch
        {
            return null;
        }
    }
}
