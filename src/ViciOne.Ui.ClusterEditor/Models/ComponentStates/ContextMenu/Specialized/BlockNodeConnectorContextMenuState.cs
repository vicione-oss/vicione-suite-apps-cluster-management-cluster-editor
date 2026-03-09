using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Models.ComponentStates.ContextMenu.Specialized;

public sealed class BlockNodeConnectorContextMenuState(IDatastore datastore, SelectionManager selectionManager) : IContextMenuState<BlockNodeConnectorContextMenuContext>
{
    public bool AddConnectorToParentContainerEnabled { get; private set; }
    public bool CancelConnectorPublicationEnabled { get; private set; }
    public bool PublishConnectorEnabled { get; private set; }
    public bool RemoveConnectorFromContainerEnabled { get; private set; }
    public bool RemoveConnectorFromContainerVisible { get; private set; }
    public bool RemoveConnectorFromParentContainerEnabled { get; private set; }

    private void ConfigureConnectorEntries()
    {
        var (enableAdd, enableRemoveFromParent, enableRemove) = GetConnectorAddRemoveVisibility();

        if (datastore.ActiveContainer is ChildContainer)
        {
            AddConnectorToParentContainerEnabled = enableAdd;
            RemoveConnectorFromParentContainerEnabled = enableRemoveFromParent;
        }
        else
        {
            AddConnectorToParentContainerEnabled = false;
            RemoveConnectorFromParentContainerEnabled = false;
        }

        RemoveConnectorFromContainerEnabled = enableRemove;
        RemoveConnectorFromContainerVisible = enableRemove;
    }

    private void ConfigurePublishEntry()
    {
        PublishConnectorEnabled = selectionManager.SelectedConnectors.Any(c => !c.Published);
        CancelConnectorPublicationEnabled = selectionManager.SelectedConnectors.Any(c => c.Published);
    }

    private (bool EnableAdd, bool EnableRemoveFromParent, bool EnableRemove) GetConnectorAddRemoveVisibility()
    {
        var connectorEditor = datastore.Builder.Editors.Connector;
        var selectedConModels = datastore.DataflowDiagramMapping.GetModels(selectionManager.SelectedConnectors).ToArray();
        var contConSelectedOnly = selectedConModels.Length == selectionManager.SelectedConnectors.Count(c => c.Node is ChildContainerNode);

        var isChildContainer = datastore.ActiveContainer is ChildContainer;
        var activeContainer = isChildContainer ? (ChildContainer)datastore.ActiveContainer : null;

        var enableAdd = isChildContainer && selectedConModels
            .Where(c => !IsOnParentContainer(c))
            .Any(c => connectorEditor.CanAddToParentContainer(c));

        var enableRemove = isChildContainer && selectedConModels
            .Any(IsOnParentContainer);

        return (enableAdd, enableRemove, contConSelectedOnly);

        bool IsOnParentContainer(IConnector connector)
            => isChildContainer && activeContainer!.GetConnector(connector.GetUnderlyingConnector()) is not null;
    }

    public void Update(BlockNodeConnectorContextMenuContext context)
    {
        ConfigureConnectorEntries();
        ConfigurePublishEntry();
    }
}
