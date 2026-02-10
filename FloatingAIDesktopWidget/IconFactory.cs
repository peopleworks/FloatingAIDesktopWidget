using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace FloatingAIDesktopWidget;

internal static class IconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint hIcon);

    public static Image CreateDefaultButtonImage(int size)
    {
        size = Math.Clamp(size, 32, 512);

        var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        var rect = new Rectangle(0, 0, size, size);
        var circle = Rectangle.Inflate(rect, -2, -2);

        using (var brush = new LinearGradientBrush(circle, Color.FromArgb(255, 59, 130, 246), Color.FromArgb(255, 139, 92, 246), 45f))
        {
            g.FillEllipse(brush, circle);
        }

        using (var shadow = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
        {
            g.FillEllipse(shadow, Rectangle.Inflate(circle, -1, -1));
        }

        using (var highlight = new LinearGradientBrush(circle, Color.FromArgb(120, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
        {
            var hRect = new Rectangle(circle.X, circle.Y, circle.Width, (int)(circle.Height * 0.55));
            using var path = new GraphicsPath();
            path.AddEllipse(circle);
            g.SetClip(path);
            g.FillEllipse(highlight, hRect);
            g.ResetClip();
        }

        using (var pen = new Pen(Color.FromArgb(200, 255, 255, 255), Math.Max(1f, size / 64f)))
        {
            g.DrawEllipse(pen, circle);
        }

        DrawAiMark(g, circle);
        return bmp;
    }

    public static Icon CreateTrayIcon(int size = 32)
    {
        using var img = CreateDefaultButtonImage(Math.Clamp(size, 16, 256));
        return CreateIconFromImage(img, size);
    }

    public static Icon CreateIconFromImage(Image image, int size = 32)
    {
        size = Math.Clamp(size, 16, 256);

        using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            g.DrawImage(image, new Rectangle(0, 0, size, size));
        }

        var hIcon = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            _ = DestroyIcon(hIcon);
        }
    }

    private static void DrawAiMark(Graphics g, Rectangle circle)
    {
        var padding = Math.Max(10, circle.Width / 6);
        var inner = Rectangle.Inflate(circle, -padding, -padding);

        using var font = new Font("Segoe UI", inner.Width / 3.2f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.FromArgb(245, 255, 255, 255));
        using var shadow = new SolidBrush(Color.FromArgb(55, 0, 0, 0));
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        var shadowRect = new Rectangle(inner.X, inner.Y + 1, inner.Width, inner.Height);
        g.DrawString("AI", font, shadow, shadowRect, format);
        g.DrawString("AI", font, brush, inner, format);
    }
}

