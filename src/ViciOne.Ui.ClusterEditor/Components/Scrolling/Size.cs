namespace ViciOne.Ui.ClusterEditor.Components.Scrolling;

public record Size
{
    public double Height { get; init; }
    public double Width { get; init; }

    public static Size Zero { get; } = new Size(0.0, 0.0);

    public Size(double width, double height)
    {
        Height = height;
        Width = width;
    }
}
