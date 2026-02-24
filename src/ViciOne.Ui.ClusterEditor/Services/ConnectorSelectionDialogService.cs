using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class ConnectorSelectionDialogService(Datastore datastore, SelectionManager selectionManager)
{
    public IEnumerable<DataGridConnectorWrapper> Connectors { get; private set; } = [];
    public bool Visible { get; set; }

    public event Action? VisibilityChanged;

    public void SelectConnectors(IEnumerable<IConnector> connectors)
    {
        selectionManager.DeselectAll();
        selectionManager.Select(connectors.Select(datastore.DataflowDiagramMapping.GetDiagramModel));
    }

    public void SetSourceConnectors(IEnumerable<IConnector> connectors)
        => Connectors = connectors.Select(c => new DataGridConnectorWrapper(
                c,
                datastore.Builder.ResolveConnectorDesign(c.GetUnderlyingConnector()),
                datastore.Builder.ResolveFunctionBlockDesign(c.FunctionBlock.DesignId)
        ));

    public void SetVisibility(bool visible)
    {
        Visible = visible;
        VisibilityChanged?.Invoke();
    }
}
