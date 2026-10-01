using System;
using System.Collections.Generic;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class ConnectorSelectionDialogService(IDatastore datastore, SelectionManager selectionManager)
{
    public IReadOnlyList<DataGridConnectorWrapper> Connectors { get; private set; } = [];
    public bool Visible { get; private set; }

    public event Action? VisibilityChanged;

    public void SelectConnectors(IEnumerable<IConnector> connectors)
    {
        selectionManager.DeselectAll();

        List<IDiagramModel> diagramModels = [];
        foreach (var connector in connectors)
            diagramModels.Add(datastore.DataflowDiagramMapping.GetDiagramModel(connector));

        selectionManager.Select(diagramModels);
    }

    public void SetSourceConnectors(IEnumerable<IConnector> connectors)
    {
        List<DataGridConnectorWrapper> result = [];
        foreach (var c in connectors)
        {
            result.Add(new DataGridConnectorWrapper(
                c,
                datastore.Builder.ResolveConnectorDesign(c.GetUnderlyingConnector()),
                datastore.Builder.ResolveFunctionBlockDesign(c.FunctionBlock.DesignId),
                dataflowName: datastore.Builder.Cache.GetDataflow(c.FunctionBlock)?.Name));
        }

        Connectors = result;
    }

    public void SetVisibility(bool visible)
    {
        if (visible != Visible)
        {
            Visible = visible;

            VisibilityChanged?.Invoke();
        }
    }
}
