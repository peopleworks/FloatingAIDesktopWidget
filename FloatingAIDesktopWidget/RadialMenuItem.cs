namespace FloatingAIDesktopWidget;

internal sealed class RadialMenuItem
{
    public TargetSettings Target { get; }
    public float Angle { get; set; }
    public PointF Position { get; set; }
    public RectangleF Bounds { get; set; }
    public Image? Icon { get; set; }
    public string DisplayName { get; }

    public RadialMenuItem(TargetSettings target, Image? icon = null)
    {
        Target = target;
        Icon = icon;
        DisplayName = string.IsNullOrWhiteSpace(target.Name) ? "App" : target.Name;
    }
}
