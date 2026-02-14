using System.Drawing.Drawing2D;

namespace FloatingAIDesktopWidget;

internal sealed class SatelliteWidget : Form
{
    private const int SatelliteSize = 48;
    private const int SatelliteIconSize = 24;

    private bool _isHovered;
    private float _scale = 1.0f;
    private readonly Label _tooltipLabel;

    public TargetSettings Target { get; }
    public Image? TargetIcon { get; }
    public string DisplayName { get; }

    public event EventHandler<TargetSettings>? SatelliteClicked;

    public SatelliteWidget(TargetSettings target, Image? icon)
    {
        Target = target;
        TargetIcon = icon;
        DisplayName = string.IsNullOrWhiteSpace(target.Name) ? "App" : target.Name;

        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        ClientSize = new Size(SatelliteSize, SatelliteSize);

        // Create tooltip label
        _tooltipLabel = new Label
        {
            AutoSize = true,
            BackColor = Color.FromArgb(230, 40, 40, 50),
            ForeColor = Color.White,
            Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular, GraphicsUnit.Point),
            Padding = new Padding(4, 2, 4, 2),
            Text = DisplayName,
            Visible = false
        };
        Controls.Add(_tooltipLabel);

        MouseEnter += (_, _) =>
        {
            _isHovered = true;
            _scale = 1.15f;
            Cursor = Cursors.Hand;
            ShowTooltip();
            Invalidate();
        };

        MouseLeave += (_, _) =>
        {
            _isHovered = false;
            _scale = 1.0f;
            Cursor = Cursors.Default;
            HideTooltip();
            Invalidate();
        };

        Click += (_, _) =>
        {
            SatelliteClicked?.Invoke(this, Target);
        };

        UpdateRegion();
    }

    private void ShowTooltip()
    {
        var textSize = _tooltipLabel.PreferredSize;
        _tooltipLabel.Location = new Point(
            (Width - textSize.Width) / 2,
            Height + 5
        );
        _tooltipLabel.Visible = true;
        _tooltipLabel.BringToFront();
    }

    private void HideTooltip()
    {
        _tooltipLabel.Visible = false;
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

    private void UpdateRegion()
    {
        using var path = new GraphicsPath();
        path.AddEllipse(new Rectangle(0, 0, Width, Height));
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Calculate size with scale
        var centerX = Width / 2f;
        var centerY = Height / 2f;
        var scaledRadius = (Width / 2f) * _scale;

        var rect = new RectangleF(
            centerX - scaledRadius,
            centerY - scaledRadius,
            scaledRadius * 2,
            scaledRadius * 2
        );

        // Background circle
        var bgColor = _isHovered
            ? Color.FromArgb(255, 100, 120, 140)
            : Color.FromArgb(255, 80, 80, 95);

        using (var bgBrush = new SolidBrush(bgColor))
        {
            g.FillEllipse(bgBrush, rect);
        }

        // Border
        using (var borderPen = new Pen(Color.FromArgb(180, 255, 255, 255), _isHovered ? 2f : 1f))
        {
            g.DrawEllipse(borderPen, rect);
        }

        // Icon or initial letter
        if (TargetIcon != null)
        {
            var iconSize = SatelliteIconSize * _scale;
            var iconRect = new RectangleF(
                centerX - iconSize / 2,
                centerY - iconSize / 2,
                iconSize,
                iconSize
            );

            g.DrawImage(TargetIcon, iconRect);
        }
        else
        {
            // Draw first letter of name if no icon
            DrawInitial(g, centerX, centerY);
        }
    }

    private void DrawInitial(Graphics g, float centerX, float centerY)
    {
        var displayName = string.IsNullOrWhiteSpace(Target.Name) ? "?" : Target.Name;
        var initial = displayName[0].ToString().ToUpper();

        var fontSize = 16f * _scale;
        using var font = new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Bold, GraphicsUnit.Point);

        var textSize = g.MeasureString(initial, font);

        // Draw shadow
        using (var shadowBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
        {
            g.DrawString(initial, font, shadowBrush,
                centerX - textSize.Width / 2 + 1,
                centerY - textSize.Height / 2 + 1);
        }

        // Draw text
        using (var textBrush = new SolidBrush(Color.White))
        {
            g.DrawString(initial, font, textBrush,
                centerX - textSize.Width / 2,
                centerY - textSize.Height / 2);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tooltipLabel?.Dispose();
        }

        base.Dispose(disposing);
    }
}
