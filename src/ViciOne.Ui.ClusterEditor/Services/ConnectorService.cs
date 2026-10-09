using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Core.Contracts;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class ConnectorService(IDatastore datastore, DiagramService diagramService, SelectionManager selectionManager)
{
    public void DeleteInvisibleLinks(IEnumerable<Link> links)
    {
        foreach (var link in links.ToArray())
        {
            if (!link.Visible)
                datastore.Builder.Editors.Connector.RemoveLink(link);
        }
    }

    public string GetConnectorColor(IConnector connector)
    {
        var connectorDesign = datastore.Builder.ResolveConnectorDesign(connector.GetUnderlyingConnector());
        var connectorType = connectorDesign.ConnectorType;
        return ConnectorColor.Get(DataTypeCompatibilityValidator.DetermineValueType(connectorType));
    }

    public async Task ShowAndSelectConnector(IConnector targetConnector)
    {
        if (!await ShowConnector(targetConnector))
            return;

        selectionManager.SetSelection(datastore.DataflowDiagramMapping.GetDiagramModel(targetConnector));
    }

    public async Task ShowAndSelectDataPortConnectorMarker(IConnector targetConnector)
    {
        if (!await ShowConnector(targetConnector))
            return;

        selectionManager.DeselectAll();
        selectionManager.Select(datastore.DataflowDiagramMapping.GetDiagramModel(targetConnector).DataPortConnectorMarker);
    }

    public async Task ShowAndSelectPublishedConnectorMarker(IConnector targetConnector)
    {
        if (!await ShowConnector(targetConnector))
            return;

        selectionManager.DeselectAll();
        selectionManager.Select(datastore.DataflowDiagramMapping.GetDiagramModel(targetConnector).PublishedConnectorMarker);
    }

    /// <summary>
    /// Loads the container of <paramref name="targetConnector"/> and brings its function block into view.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> if the function block is not part of the loaded container, which is the case for a
    /// connector of a cluster that has since been replaced.
    /// </returns>
    private async Task<bool> ShowConnector(IConnector targetConnector)
    {
        var targetFunctionBlock = targetConnector.FunctionBlock;
        var containerToLoad = targetFunctionBlock.Container;
        await datastore.LoadContainer(containerToLoad, diagramService);

        if (!datastore.DataflowDiagramMapping.TryGetDiagramModel(targetFunctionBlock, out var node))
            return false;

        if (!diagramService.Diagram.IsNodeInViewport(node))
            diagramService.Diagram.PanToNode(node);

        return true;
    }
}
