namespace FloatingAIDesktopWidget;

internal sealed class SatelliteManager : IDisposable
{
    private const int OrbitRadius = 100;
    private const int AnimationInterval = 16; // ~60 FPS
    private const int EnterDurationMs = 300;
    private const int ExitDurationMs = 200;

    private readonly List<SatelliteWidget> _satellites = new();
    private readonly List<PointF> _targetPositions = new();
    private readonly System.Windows.Forms.Timer _animationTimer;
    private Point _centerPosition;
    private DateTime _animationStartTime;
    private bool _isAnimatingIn;
    private bool _isAnimatingOut;
    private bool _isDisposed;

    public event EventHandler<TargetSettings>? SatelliteClicked;
    public event EventHandler? Closed;

    public SatelliteManager()
    {
        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = AnimationInterval
        };
        _animationTimer.Tick += OnAnimationTick;
    }

    public void Show(Point centerPosition, TargetSettings[] targets)
    {
        _centerPosition = centerPosition;

        // Create satellites
        foreach (var target in targets)
        {
            var icon = TryLoadIcon(target.IconPath);
            var satellite = new SatelliteWidget(target, icon);
            satellite.SatelliteClicked += (_, t) =>
            {
                SatelliteClicked?.Invoke(this, t);
            };

            _satellites.Add(satellite);
        }

        // Calculate target positions
        CalculateTargetPositions();

        // Start all satellites at center
        foreach (var satellite in _satellites)
        {
            satellite.Location = new Point(
                _centerPosition.X - satellite.Width / 2,
                _centerPosition.Y - satellite.Height / 2
            );
            satellite.Opacity = 0;
            satellite.Show();
        }

        // Start expand animation
        AnimateIn();
    }

    public void Hide()
    {
        if (_isAnimatingOut || _isDisposed) return;

        AnimateOut();
    }

    private void CalculateTargetPositions()
    {
        if (_satellites.Count == 0) return;

        float angleStep = (float)(2 * Math.PI / _satellites.Count);
        float startAngle = -(float)Math.PI / 2; // Start at top

        for (int i = 0; i < _satellites.Count; i++)
        {
            float angle = startAngle + (i * angleStep);
            float x = _centerPosition.X + OrbitRadius * (float)Math.Cos(angle);
            float y = _centerPosition.Y + OrbitRadius * (float)Math.Sin(angle);

            _targetPositions.Add(new PointF(
                x - _satellites[i].Width / 2,
                y - _satellites[i].Height / 2
            ));
        }

        // Adjust for screen bounds
        AdjustForScreenBounds();
    }

    private void AdjustForScreenBounds()
    {
        var screen = Screen.FromPoint(_centerPosition);
        var workingArea = screen.WorkingArea;

        for (int i = 0; i < _targetPositions.Count; i++)
        {
            var pos = _targetPositions[i];
            var size = _satellites[i].Size;

            float x = Math.Max(workingArea.Left, Math.Min(pos.X, workingArea.Right - size.Width));
            float y = Math.Max(workingArea.Top, Math.Min(pos.Y, workingArea.Bottom - size.Height));

            _targetPositions[i] = new PointF(x, y);
        }
    }

    private void AnimateIn()
    {
        _isAnimatingIn = true;
        _isAnimatingOut = false;
        _animationStartTime = DateTime.Now;
        _animationTimer.Start();
    }

    private void AnimateOut()
    {
        _isAnimatingIn = false;
        _isAnimatingOut = true;
        _animationStartTime = DateTime.Now;
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        var elapsed = (DateTime.Now - _animationStartTime).TotalMilliseconds;

        if (_isAnimatingIn)
        {
            var progress = Math.Min(1.0, elapsed / EnterDurationMs);
            var easedProgress = EaseOutCubic((float)progress);

            UpdateSatellitePositions(easedProgress);

            if (progress >= 1.0)
            {
                _isAnimatingIn = false;
                _animationTimer.Stop();
            }
        }
        else if (_isAnimatingOut)
        {
            var progress = Math.Min(1.0, elapsed / ExitDurationMs);
            var easedProgress = EaseInCubic((float)progress);

            UpdateSatellitePositions(1.0f - easedProgress);

            if (progress >= 1.0)
            {
                _isAnimatingOut = false;
                _animationTimer.Stop();
                CloseSatellites();
            }
        }
    }

    private void UpdateSatellitePositions(float progress)
    {
        var centerX = _centerPosition.X;
        var centerY = _centerPosition.Y;

        for (int i = 0; i < _satellites.Count; i++)
        {
            var satellite = _satellites[i];
            var target = _targetPositions[i];

            // Interpolate position
            var currentX = centerX - satellite.Width / 2 + (target.X - (centerX - satellite.Width / 2)) * progress;
            var currentY = centerY - satellite.Height / 2 + (target.Y - (centerY - satellite.Height / 2)) * progress;

            satellite.Location = new Point((int)currentX, (int)currentY);
            satellite.Opacity = Math.Min(1.0, progress * 1.5); // Fade in faster
        }
    }

    private void CloseSatellites()
    {
        foreach (var satellite in _satellites)
        {
            satellite.Close();
            satellite.Dispose();
        }

        _satellites.Clear();
        _targetPositions.Clear();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private static float EaseOutCubic(float t)
    {
        return 1 - (float)Math.Pow(1 - t, 3);
    }

    private static float EaseInCubic(float t)
    {
        return (float)Math.Pow(t, 3);
    }

    private static Image? TryLoadIcon(string? iconPath)
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
                expanded = Path.Combine(AppContext.BaseDirectory, expanded);
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

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _animationTimer.Stop();
        _animationTimer.Dispose();

        foreach (var satellite in _satellites)
        {
            try
            {
                satellite.Close();
                satellite.Dispose();
            }
            catch
            {
                // ignore
            }
        }

        _satellites.Clear();
        _targetPositions.Clear();
    }
}
