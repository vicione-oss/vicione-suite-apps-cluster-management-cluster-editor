using System;
using System.Globalization;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.DiagramComponents;

public sealed partial class BlockLinkComponent : ComponentBase
{
    private static readonly string[] s_separator = [",", " "];

    private string? _highlightColor;

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    [Parameter] public BlockNodeLink? Link { get; set; }

    private string[] GetLinkPathPoints()
    {
        var path = Link!.PathGeneratorResult!.FullPath;
        return path.ToString().Split(s_separator, StringSplitOptions.RemoveEmptyEntries);
    }

    private string GetSourceMarkerTransform()
        => string.Format(CultureInfo.InvariantCulture,
            "translate({0}, {1}) rotate({2})",
            Link!.PathGeneratorResult!.SourceMarkerPosition!.X,
            Link.PathGeneratorResult.SourceMarkerPosition.Y,
            Link.PathGeneratorResult.SourceMarkerAngle);

    private string GetTargetMarkerTransform()
        => string.Format(CultureInfo.InvariantCulture,
            "translate({0}, {1}) rotate({2})",
            Link!.PathGeneratorResult!.TargetMarkerPosition!.X,
            Link.PathGeneratorResult.TargetMarkerPosition.Y,
            Link.PathGeneratorResult.TargetMarkerAngle);

    private async Task OnDblClickAsync(MouseEventArgs e)
    {
        // Das funktioniert nur solange, wie wir keine Links mit "Vertices"
        // (https://blazor-diagrams.zhaytam.com/links/vertices) haben
        var curvePoints = GetLinkPathPoints();
        var t = await JSRuntime.InvokeAsync<double>("ViciOne.Diagram.Link.getRatio",
            curvePoints[1],
            curvePoints[2],
            curvePoints[4],
            curvePoints[5],
            curvePoints[6],
            curvePoints[7],
            curvePoints[8],
            curvePoints[9],
            e.OffsetX,
            e.OffsetY);

        BlockNodeConnector? connector;
        NodeModel node;
        if (t < 0.5)
        {
            connector = (BlockNodeConnector?)Link!.TargetPort;
            node = Link.TargetNode;
        }
        else
        {
            connector = (BlockNodeConnector?)Link!.SourcePort;
            node = Link.SourceNode;
        }

        if (connector is not null)
            SelectionManager.SetSelection(connector);

        if (!DiagramService.Diagram.IsNodeInViewport(node))
            DiagramService.Diagram.PanToNode(node);
    }

    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Link, nameof(Link));

    private void OnPointerEnter()
    {
        var draggingSourcePort = DiagramService.DraggingLink?.SourcePort;
        if (draggingSourcePort is null)
            return;

        var sourceConnector = (IConnectorOutput)Datastore.DataflowDiagramMapping.GetModel((BlockNodeConnector)draggingSourcePort);
        var destinationConnector = (IConnectorInput)Datastore.DataflowDiagramMapping.GetModel((BlockNodeConnector)Link!.TargetPort!);

        _highlightColor = Link! == DiagramService.DraggingLink
            ? null
            : Datastore.Builder.Editors.Connector.CanCreateLink(sourceConnector, destinationConnector)
                ? LinkColors.LinkDropAllowed
                : LinkColors.LinkDropNotAllowed;
    }

    private void OnPointerLeave()
        => _highlightColor = null;

    private void OnPointerUp()
        => OnPointerLeave();
}
