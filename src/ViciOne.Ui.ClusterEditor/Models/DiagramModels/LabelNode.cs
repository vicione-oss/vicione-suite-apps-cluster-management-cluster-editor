using System;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Models;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

public sealed class LabelNode : SvgNodeModel, IDiagramModel
{
    internal string BackgroundColor
    {
        get;
        set => field = Helpers.Color.SanitizeCssColor(value, LabelColors.BackgroundDefault);
    } = LabelColors.BackgroundDefault;

    internal string BorderColor
    {
        get;
        set => field = Helpers.Color.SanitizeCssColor(value, LabelColors.BorderDefault);
    } = LabelColors.BorderDefault;

    internal int Height { get; set; }
    internal string Text { get; set; } = string.Empty;
    public new bool Visible { get; set; } = true;
    internal int Width { get; set; }

    internal event Func<LabelNode, Task>? EditModeStarted;

    internal LabelNode(Point point) : base(point) { }

    internal Task ProcessTextEditStarted()
        => EditModeStarted.InvokeEventAsync(this);
}
