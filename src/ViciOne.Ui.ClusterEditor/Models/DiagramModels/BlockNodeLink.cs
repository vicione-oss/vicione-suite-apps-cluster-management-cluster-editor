using System;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using ViciOne.Ui.ClusterEditor.Constants;

namespace ViciOne.Ui.ClusterEditor.Models.DiagramModels;

public sealed class BlockNodeLink : LinkModel, IDiagramModel, IDisposable
{
    private const string LineStyleDefault = "";
    private const string LineStyleDifferentClient = "10px 5px";
    private const string LineStyleNoClient = "4px 6px";

    private static readonly PathGenerator s_pathGenerator = new SmoothPathGenerator();
    private static readonly Router s_router = new NormalRouter();

    internal bool DrawOverlay { get; set; }
    internal bool Enabled { get; private set; } = true;
    internal string LineStyle { get; private set; } = LineStyleDefault;
    public NodeModel SourceNode => (Source as SinglePortAnchor)!.Port.Parent;
    public PortModel? SourcePort => (Source as SinglePortAnchor)?.Port;
    public NodeModel TargetNode => (Target as SinglePortAnchor)!.Port.Parent;
    public PortModel? TargetPort => (Target as SinglePortAnchor)?.Port;
    internal bool Traced { get; private set; }
    public new bool Visible { get; set; } = true;

    internal event Action<LinkModel, NodeModel>? NodeFocusInvoked;

    internal BlockNodeLink(PortModel sourcePort, PortModel targetPort) : this(new SinglePortAnchor(sourcePort), new SinglePortAnchor(targetPort)) { }

    internal BlockNodeLink(Anchor sourceAnchor, Anchor targetAnchor) : base(sourceAnchor, targetAnchor)
    {
        Color = LinkColors.Default;
        PathGenerator = s_pathGenerator;
        Router = s_router;
        SelectedColor = LinkColors.Selected;
        SourceMarker = LinkMarker.NewSquare(6);
        TargetMarker = LinkMarker.Arrow;
        Width = 2;

        Changed += OnLinkChanged;
    }

    public void Dispose()
        => Changed -= OnLinkChanged;

    internal void FocusAttachedNode(double t)
    {
        var node = t < 0.5 ? TargetNode : SourceNode;
        NodeFocusInvoked?.Invoke(this, node!);
    }

    private void OnLinkChanged(Model link)
    {
        // PathGeneratorResult already set for us
        UpdateColor();
        UpdateLineStyle();
    }

    internal void SetTraced(bool traced)
    {
        Traced = traced;
        UpdateColor();
    }

    private void UpdateColor()
        => Color = Selected ? LinkColors.Selected :
            Traced ? LinkColors.Traced :
            LinkColors.Default;

    internal void UpdateLineStyle()
    {
        if (!IsAttached)
            return;

        var sourceNodeEngine = (SourceNode as BlockNode)!.EngineDisplayText;
        var targetNodeEngine = (TargetNode as BlockNode)!.EngineDisplayText;
        if (string.IsNullOrEmpty(sourceNodeEngine) || string.IsNullOrEmpty(targetNodeEngine))
            LineStyle = LineStyleNoClient;
        else if (sourceNodeEngine != targetNodeEngine)
            LineStyle = LineStyleDifferentClient;
        else
            LineStyle = LineStyleDefault;
    }
}
