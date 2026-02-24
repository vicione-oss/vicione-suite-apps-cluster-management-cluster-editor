using Microsoft.AspNetCore.Components;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class SelectBox : ComponentBase
{
    [Parameter] public double Height { get; set; }
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public double Left { get; set; }
    [Parameter] public double Top { get; set; }
    [Parameter] public double Width { get; set; }
}
