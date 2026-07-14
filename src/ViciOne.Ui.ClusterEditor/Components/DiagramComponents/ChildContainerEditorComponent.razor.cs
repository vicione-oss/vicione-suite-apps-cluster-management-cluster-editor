using Blazor.Diagrams.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.DiagramComponents;

public sealed partial class ChildContainerEditorComponent : ComponentBase
{
    [CascadingParameter] internal Diagram? Diagram { get; set; }

    [Inject] private BoundsService BoundsService { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private TooltipService TooltipService { get; set; } = default!;

    [Parameter] public ChildContainerNode? Node { get; set; }

    private readonly object _connectorTooltipKey = new();

    private BlockNodeConnector GetOriginalConnectorDiagramModel(BlockNodeConnector nodeConnector)
        => Datastore.DataflowDiagramMapping.GetDiagramModel(((ContainerConnector)nodeConnector.Connector).Connector);

    private void OnPortContainerPointerEnter(PointerEventArgs e, BlockNodeConnector connector)
    {
        var originalConnector = GetOriginalConnectorDiagramModel(connector);

        TooltipService.StartTooltip(_connectorTooltipKey, TooltipConnectorData.GetConnectorTooltipInfo(Datastore.Builder, e, originalConnector, BoundsService.GetDiagramBounds()));
    }

    private void OnPortContainerPointerLeave()
        => TooltipService.StopTooltip(_connectorTooltipKey);
}
