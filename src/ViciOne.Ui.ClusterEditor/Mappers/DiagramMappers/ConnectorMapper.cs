using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Factories;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;

namespace ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;

internal static class ConnectorMapper
{
    public static BlockNodeConnector CreateNodeConnector(
        ComparerService comparerService,
        IDatastore datastore,
        DiagramService diagramService,
        BlockNode blockNode,
        IConnector connector,
        bool isSystemConnector)
    {
        var isInputConnector = connector is IConnectorInput;
        var underlyingConnector = connector.GetUnderlyingConnector();

        var nodeConnector = new BlockNodeConnector(comparerService, connector, datastore, diagramService, blockNode, isInputConnector)
        {
            IsSystemConnector = isSystemConnector,
            Text = connector.ShortName
        };

        if (datastore.ActiveContainer is ChildContainer parent)
            nodeConnector.SetParentContainerConnector(parent.GetConnector(underlyingConnector));

        nodeConnector.SetEventEnabled(connector.EventEnabled, ConnectorDefaults.EventEnabled);
        nodeConnector.SetHasUpstreamLinks(connector.HasUpstreamLinks());
        nodeConnector.SetMarkAsChangedOnlyIfNotEqual(connector.MarkAsChangedOnlyIfNotEqual, ConnectorDefaults.MarkAsChangedOnlyIfNotEqual);

        if (isInputConnector)
        {
            var connectorInput = (IConnectorInput)connector;
            nodeConnector.SetPoolingMode(connectorInput);
            nodeConnector.SetValue(connectorInput);
        }

        var connectorType = datastore.Builder.DetermineValueType(underlyingConnector);
        nodeConnector.SetPortColor(ConnectorColor.Get(connectorType));
        nodeConnector.SetTooltipEntries(PublishedConnectorTooltipEntriesFactory.Create(connector, datastore.ActiveContainer.Id));

        nodeConnector.SetIsOnContainer(connector.Parent.Parent is ChildContainer parentAsChildContainer
            && parentAsChildContainer.GetConnector(underlyingConnector) is not null);

        return nodeConnector;
    }

    public static void Dispose(DiagramEventService diagramEventService)
        => diagramEventService.DraggingLinkChanged -= OnDraggingLinkChanged;

    public static void Init(DiagramEventService diagramEventService)
        => diagramEventService.DraggingLinkChanged += OnDraggingLinkChanged;

    private static void OnDraggingLinkChanged(IDatastore datastore, IConnector? connector)
    {
        var nodeConnectors = datastore.DataflowDiagramMapping
            .GetInputNodeConnectors();

        if (connector is null)
        {
            foreach (var nodeConn in nodeConnectors)
                nodeConn.SetIsValidDropTarget(false);
        }
        else
        {
            foreach (var nodeConn in nodeConnectors)
            {
                nodeConn.SetIsValidDropTarget(datastore.Builder.Editors.Connector.CanCreateLink(
                    (IConnectorOutput)connector,
                    (IConnectorInput)datastore.DataflowDiagramMapping.GetModel(nodeConn))
                );
            }
        }
    }

    public static void PropertyChanged(IConnector connector, BlockNodeConnector nodeConnector, string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Connector.EventEnabled):
                nodeConnector.SetEventEnabled(connector.EventEnabled, ConnectorDefaults.EventEnabled);
                break;
            case nameof(Connector.MarkAsChangedOnlyIfNotEqual):
                nodeConnector.SetMarkAsChangedOnlyIfNotEqual(connector.MarkAsChangedOnlyIfNotEqual,
                    ConnectorDefaults.MarkAsChangedOnlyIfNotEqual);
                break;
            case nameof(Connector.Published):
                nodeConnector.SetPublished(connector.Published);
                break;
            case nameof(ConnectorInput.CrossSourcePoolingStrategy):
            case nameof(ConnectorInput.CrossSourceAggregatingPooling):
                if (connector is ConnectorInput inputPooling)
                    nodeConnector.SetPoolingMode(inputPooling);
                break;
            case nameof(Connector.ShortName):
                nodeConnector.Text = connector.ShortName;
                break;
            case nameof(ConnectorInput.Value):
                if (connector is ConnectorInput inputValue)
                    nodeConnector.SetValue(inputValue);
                break;
        }

        nodeConnector.Node.RefreshAll();
    }
}
