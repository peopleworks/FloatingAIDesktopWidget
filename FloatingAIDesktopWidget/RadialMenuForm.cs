using System.Drawing.Drawing2D;

namespace FloatingAIDesktopWidget;

internal sealed class RadialMenuForm : Form
{
    private const float ItemSpacing = 12f; // Space between items
    private const float ItemRadius = 36f;
    private const int IconSize = 24;
    private const int MenuPadding = 20; // Padding around the menu

    private readonly List<RadialMenuItem> _menuItems = new();
    private RadialMenuItem? _hoveredItem;
    private PointF _centerPoint;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private bool _fadingIn;
    private bool _fadingOut;
    private bool _openDownward; // True = open downward, False = open upward

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

        // Create menu items first
        foreach (var target in targets)
        {
            var icon = TryLoadIcon(target.IconPath);
            _menuItems.Add(new RadialMenuItem(target, icon));
        }

        // Size: width for one item, height for all items stacked vertically
        int width = (int)((ItemRadius * 2) + MenuPadding * 2);
        int height = (int)((_menuItems.Count * (ItemRadius * 2 + ItemSpacing)) - ItemSpacing + MenuPadding * 2);
        ClientSize = new Size(width, Math.Max(height, 100)); // Minimum height of 100
        _centerPoint = new PointF(width / 2f, height / 2f);

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

        float x = _centerPoint.X; // All items centered horizontally
        float startY = MenuPadding + ItemRadius;

        for (int i = 0; i < _menuItems.Count; i++)
        {
            float y = startY + (i * (ItemRadius * 2 + ItemSpacing));

            _menuItems[i].Angle = 0; // Not used in vertical layout
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
        // Get the working area of the screen where the cursor is
        var screen = Screen.FromPoint(screenPosition);
        var workingArea = screen.WorkingArea;

        // Determine if we should open downward or upward
        int screenMidY = workingArea.Top + workingArea.Height / 2;
        _openDownward = screenPosition.Y < screenMidY;

        // Calculate position - horizontally centered on cursor
        int x = screenPosition.X - ClientSize.Width / 2;
        int y;

        const int OffsetFromWidget = 45; // Space between widget and menu

        if (_openDownward)
        {
            // Open downward - menu starts below the widget with spacing
            y = screenPosition.Y + OffsetFromWidget;
        }
        else
        {
            // Open upward - menu starts above the widget with spacing
            y = screenPosition.Y - ClientSize.Height - OffsetFromWidget;
        }

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

        // Draw background panel
        DrawBackgroundPanel(g);

        // Draw menu items
        foreach (var item in _menuItems)
        {
            DrawMenuItem(g, item, item == _hoveredItem);
        }
    }

    private void DrawBackgroundPanel(Graphics g)
    {
        // No background panel - completely transparent for floating effect
        // Items will float independently
    }

    private static GraphicsPath GetRoundedRectPath(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    private void DrawMenuItem(Graphics g, RadialMenuItem item, bool isHovered)
    {
        // Clean, modern look - white/light background with subtle shadow
        var bgColor = isHovered
            ? Color.FromArgb(255, 240, 240, 245)
            : Color.FromArgb(250, 250, 250, 252);

        // Draw subtle shadow first
        if (isHovered)
        {
            var shadowBounds = new RectangleF(
                item.Bounds.X + 2,
                item.Bounds.Y + 2,
                item.Bounds.Width,
                item.Bounds.Height
            );
            using var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            g.FillEllipse(shadowBrush, shadowBounds);
        }

        // Background circle
        using var bgBrush = new SolidBrush(bgColor);
        g.FillEllipse(bgBrush, item.Bounds);

        // Border - subtle on normal, accent on hover
        var borderColor = isHovered
            ? Color.FromArgb(200, 100, 150, 255)
            : Color.FromArgb(180, 200, 200, 210);
        using var borderPen = new Pen(borderColor, isHovered ? 2f : 1f);
        g.DrawEllipse(borderPen, item.Bounds);

        // Icon or display initial letter
        if (item.Icon != null)
        {
            var iconRect = new RectangleF(
                item.Position.X - IconSize / 2f,
                item.Position.Y - IconSize / 2f,
                IconSize,
                IconSize
            );
            g.DrawImage(item.Icon, iconRect);
        }
        else
        {
            // Draw first letter of name if no icon
            DrawInitial(g, item);
        }
    }

    private void DrawInitial(Graphics g, RadialMenuItem item)
    {
        var displayName = string.IsNullOrWhiteSpace(item.DisplayName) ? "?" : item.DisplayName;
        var initial = displayName[0].ToString().ToUpper();

        using var font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Bold, GraphicsUnit.Point);
        var textSize = g.MeasureString(initial, font);

        using var textBrush = new SolidBrush(Color.FromArgb(255, 80, 80, 90));
        g.DrawString(initial, font, textBrush,
            item.Position.X - textSize.Width / 2,
            item.Position.Y - textSize.Height / 2);
    }

    // Removed DrawItemLabel - in vertical menu style, we only show icons
    // Tooltip support could be added in the future if needed

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
