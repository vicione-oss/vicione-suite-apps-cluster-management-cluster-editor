using System;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Models;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

public sealed class LabelNode : SvgNodeModel, IDiagramModel
{
    internal string BackgroundColor { get; set; } = string.Empty;
    internal string BorderColor { get; set; } = string.Empty;
    internal int Height { get; set; }
    internal string Text { get; set; } = string.Empty;
    public new bool Visible { get; set; } = true;
    internal int Width { get; set; }

    internal event Action<LabelNode>? EditModeStarted;

    internal LabelNode(Point point) : base(point) { }

    internal void ProcessTextEditStarted()
        => EditModeStarted?.Invoke(this);
}
