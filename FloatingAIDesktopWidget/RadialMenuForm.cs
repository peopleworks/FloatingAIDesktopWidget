using System.Drawing.Drawing2D;

namespace FloatingAIDesktopWidget;

internal sealed class RadialMenuForm : Form
{
    private const float MenuRadius = 110f;
    private const float ItemRadius = 36f;
    private const int IconSize = 24;

    private readonly List<RadialMenuItem> _menuItems = new();
    private RadialMenuItem? _hoveredItem;
    private PointF _centerPoint;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private bool _fadingIn;
    private bool _fadingOut;

    public event EventHandler<TargetSettings>? ItemSelected;

    public RadialMenuForm(TargetSettings[] targets)
    {
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        BackColor = Color.Magenta; // Will be transparent
        TransparencyKey = Color.Magenta;

        // Size: enough to contain the menu circle + items
        int size = (int)((MenuRadius + ItemRadius + 20) * 2);
        ClientSize = new Size(size, size);
        _centerPoint = new PointF(size / 2f, size / 2f);

        // Create menu items
        foreach (var target in targets)
        {
            var icon = TryLoadIcon(target.IconPath);
            _menuItems.Add(new RadialMenuItem(target, icon));
        }

        CalculateItemPositions();

        MouseMove += OnMouseMoveHandler;
        MouseClick += OnMouseClickHandler;
        Deactivate += (_, _) => FadeOut();

        // Fade animation timer
        _fadeTimer = new System.Windows.Forms.Timer
        {
            Interval = 20 // ~50 FPS
        };
        _fadeTimer.Tick += OnFadeTimerTick;
    }

    protected override bool ShowWithoutActivation => true;

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

    private void CalculateItemPositions()
    {
        if (_menuItems.Count == 0) return;

        float angleStep = (float)(2 * Math.PI / _menuItems.Count);
        float startAngle = -(float)Math.PI / 2; // Start at top

        for (int i = 0; i < _menuItems.Count; i++)
        {
            float angle = startAngle + (i * angleStep);
            float x = _centerPoint.X + MenuRadius * (float)Math.Cos(angle);
            float y = _centerPoint.Y + MenuRadius * (float)Math.Sin(angle);

            _menuItems[i].Angle = angle;
            _menuItems[i].Position = new PointF(x, y);
            _menuItems[i].Bounds = new RectangleF(
                x - ItemRadius,
                y - ItemRadius,
                ItemRadius * 2,
                ItemRadius * 2
            );
        }
    }

    private RadialMenuItem? HitTest(Point mousePos)
    {
        foreach (var item in _menuItems)
        {
            float dx = mousePos.X - item.Position.X;
            float dy = mousePos.Y - item.Position.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance <= ItemRadius)
            {
                return item;
            }
        }

        return null;
    }

    public void ShowAt(Point screenPosition)
    {
        // Calculate initial centered position
        int x = screenPosition.X - ClientSize.Width / 2;
        int y = screenPosition.Y - ClientSize.Height / 2;

        // Get the working area of the screen where the cursor is
        var screen = Screen.FromPoint(screenPosition);
        var workingArea = screen.WorkingArea;

        // Clamp position to keep menu fully visible on screen
        x = Math.Max(workingArea.Left, Math.Min(x, workingArea.Right - ClientSize.Width));
        y = Math.Max(workingArea.Top, Math.Min(y, workingArea.Bottom - ClientSize.Height));

        Location = new Point(x, y);
        FadeIn();
    }

    private void FadeIn()
    {
        Opacity = 0;
        Show();
        _fadingIn = true;
        _fadingOut = false;
        _fadeTimer.Start();
    }

    private void FadeOut()
    {
        if (_fadingOut) return; // Already fading out

        _fadingIn = false;
        _fadingOut = true;
        _fadeTimer.Start();
    }

    private void OnFadeTimerTick(object? sender, EventArgs e)
    {
        if (_fadingIn)
        {
            Opacity = Math.Min(1.0, Opacity + 0.15);
            if (Opacity >= 1.0)
            {
                _fadingIn = false;
                _fadeTimer.Stop();
            }
        }
        else if (_fadingOut)
        {
            Opacity = Math.Max(0.0, Opacity - 0.2);
            if (Opacity <= 0.0)
            {
                _fadingOut = false;
                _fadeTimer.Stop();
                Close();
            }
        }
    }

    private void OnMouseMoveHandler(object? sender, MouseEventArgs e)
    {
        var hitItem = HitTest(e.Location);

        if (hitItem != _hoveredItem)
        {
            _hoveredItem = hitItem;
            Cursor = _hoveredItem != null ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    private void OnMouseClickHandler(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var hitItem = HitTest(e.Location);

        if (hitItem != null)
        {
            ItemSelected?.Invoke(this, hitItem.Target);
            FadeOut();
        }
        else
        {
            // Click outside menu - close without selection
            FadeOut();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Draw central background circle
        DrawCentralCircle(g);

        // Draw connecting lines (optional, subtle)
        DrawConnectingLines(g);

        // Draw menu items
        foreach (var item in _menuItems)
        {
            DrawMenuItem(g, item, item == _hoveredItem);
        }
    }

    private void DrawCentralCircle(Graphics g)
    {
        const float centralRadius = 50f;

        var rect = new RectangleF(
            _centerPoint.X - centralRadius,
            _centerPoint.Y - centralRadius,
            centralRadius * 2,
            centralRadius * 2
        );

        using var brush = new SolidBrush(Color.FromArgb(200, 60, 60, 75));
        g.FillEllipse(brush, rect);

        using var pen = new Pen(Color.FromArgb(100, 255, 255, 255), 1f);
        g.DrawEllipse(pen, rect);
    }

    private void DrawConnectingLines(Graphics g)
    {
        using var pen = new Pen(Color.FromArgb(50, 255, 255, 255), 1f);

        foreach (var item in _menuItems)
        {
            g.DrawLine(pen, _centerPoint, item.Position);
        }
    }

    private void DrawMenuItem(Graphics g, RadialMenuItem item, bool isHovered)
    {
        // Background circle
        var bgColor = isHovered
            ? Color.FromArgb(255, 100, 120, 140)
            : Color.FromArgb(255, 80, 80, 95);

        using var bgBrush = new SolidBrush(bgColor);
        g.FillEllipse(bgBrush, item.Bounds);

        // Border
        using var borderPen = new Pen(Color.FromArgb(180, 255, 255, 255), isHovered ? 2f : 1f);
        g.DrawEllipse(borderPen, item.Bounds);

        // Icon
        if (item.Icon != null)
        {
            var iconRect = new RectangleF(
                item.Position.X - IconSize / 2f,
                item.Position.Y - IconSize / 2f - 6, // Offset up for text
                IconSize,
                IconSize
            );

            g.DrawImage(item.Icon, iconRect);
        }

        // Text label
        DrawItemLabel(g, item);
    }

    private void DrawItemLabel(Graphics g, RadialMenuItem item)
    {
        using var font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular, GraphicsUnit.Point);
        var textSize = g.MeasureString(item.DisplayName, font);

        var textPos = new PointF(
            item.Position.X - textSize.Width / 2,
            item.Position.Y + (item.Icon != null ? 8 : -textSize.Height / 2)
        );

        // Text shadow for legibility
        using var shadowBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        g.DrawString(item.DisplayName, font, shadowBrush, textPos.X + 1, textPos.Y + 1);

        // Text
        using var textBrush = new SolidBrush(Color.White);
        g.DrawString(item.DisplayName, font, textBrush, textPos);
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

            var originalImage = Image.FromFile(expanded);

            // Scale to icon size
            var scaled = new Bitmap(IconSize, IconSize);
            using (var gfx = Graphics.FromImage(scaled))
            {
                gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                gfx.DrawImage(originalImage, 0, 0, IconSize, IconSize);
            }

            originalImage.Dispose();
            return scaled;
        }
        catch
        {
            return null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fadeTimer.Stop();
            _fadeTimer.Dispose();

            foreach (var item in _menuItems)
            {
                item.Icon?.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
