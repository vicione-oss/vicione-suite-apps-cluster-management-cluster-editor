using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Core.Contracts;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
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

    public async Task ShowAndSelectConnectorAsync(Connector targetConnector)
    {
        var targetFunctionBlock = targetConnector.FunctionBlock;
        var containerToLoad = targetFunctionBlock.Container;
        await datastore.LoadContainer(containerToLoad, diagramService);
        BlockNode node = datastore.DataflowDiagramMapping.GetDiagramModel(targetFunctionBlock);

        if (!diagramService.Diagram!.IsNodeInViewport(node))
            diagramService.Diagram!.PanToNode(node);

        selectionManager.SetSelection(datastore.DataflowDiagramMapping.GetDiagramModel(targetConnector));
    }
}
