using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions.Arguments;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed partial class DataPortTreeMutator(
    IDatastore datastore,
    IRulesetProvider rulesetProvider,
    ILogger<DataPortTreeMutator> logger,
    DataPortTreeState state,
    DataPortTreeBuilderRegistry registry)
{
    private DataPortRootNodeModel AddOrGetTreeRootNode(Tree.Builder.TreeBuilder treeBuilder)
    {
        // the root node is just a visual container and does not get stored in the cluster
        var rootNode = state.FindRootNode(k => Equals(k.Builder.Ruleset.Root?.Id, treeBuilder.Ruleset.Root?.Id));
        if (rootNode is null)
        {
            rootNode = new()
            {
                Builder = treeBuilder,
                Icon = treeBuilder.Ruleset.Root!.Icon.GetName(),
                Id = Guid.TryParse(treeBuilder.Ruleset.Root!.Id, out var rnId)
                    ? new GuidNodeIdentifier(rnId)
                    : GuidNodeIdentifier.New(),
                Name = treeBuilder.Ruleset.Root!.Name,
            };
            state.AddRootNode(rootNode);
        }

        // We have a root node for e.g. Mqtt so we need a ClusterDependency to it's package
        datastore.Builder?.EnsureSystemDataPortDependencyExists(rulesetProvider);

        rootNode.PossibleChildren = [.. rootNode.GetPossibleChildNodes()];
        rootNode.CanHaveChildren = rootNode.PossibleChildren.Any();

        return rootNode;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to build tree for DataPort {DataPortId}.")]
    public static partial void CreateDataPortTreeFailed(ILogger<DataPortTreeMutator> logger, Exception ex, Guid dataPortId);

    public void CreateNewChildNode(ITreeNode parentNode, ITreeNode newChild)
    {
        if (parentNode is not DataPortNodeModel dataPortParentNode || newChild is not DataPortChildNodeModel dataPortChildNode)
            return;

        if (datastore.Builder is null)
            return;

        // first DataPort element was added  (for MQTT eg. Broker|MqttTemplate)
        if (dataPortParentNode is DataPortRootNodeModel rootNode)
        {
            var dataPort = datastore.Builder.GetOrCreateDataPort(datastore, rootNode, dataPortChildNode);

            datastore.Builder.SetSystemDataPortProperties(dataPort, dataPortChildNode, state.Builder);
            datastore.Builder.SetCustomDataPortProperties(dataPort, dataPortChildNode);
        }
        else
        {
            // child was added to child (for MQTT eg. Folder|Value)
            var dataPort = datastore.Builder.GetDataPort(dataPortChildNode);
            var treeNode = datastore.Builder.GetOrCreateDataPortTreeNode(dataPort, dataPortChildNode);

            datastore.Builder.SetSystemDataPortTreeNodeProperties(treeNode, dataPortChildNode);
            datastore.Builder.SetCustomDataPortTreeNodeProperties(treeNode, dataPortChildNode);
        }

        dataPortParentNode.Children.Add(dataPortChildNode);

        dataPortParentNode.PossibleChildren = [.. dataPortParentNode.GetPossibleChildNodes()];
        dataPortChildNode.PossibleChildren = [.. dataPortChildNode.GetPossibleChildNodes()];

        if (DataPortNodeSorter.IsContainerNode(dataPortChildNode))
            DataPortNodeSorter.SortChildren(dataPortParentNode, false);

        state.Builder.Notifications.NotifyNodeChanged(dataPortParentNode, ChangedNodeDetail.Actions);
        state.Builder.Notifications.NotifyChildrenChanged(dataPortParentNode);
    }

    public void CreateNewDataPortRootNode(string rulesetIdentifier)
    {
        var builder = registry.GetOrCreateTreeBuilder(DataPortTreeBuilderRegistry.DataPortCategory, rulesetIdentifier);
        var node = AddOrGetTreeRootNode(builder);
        node.PossibleChildren = [.. node.GetPossibleChildNodes()];
        var firstChild = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(node.PossibleChildren[0], node);
        CreateNewChildNode(node, firstChild);

        state.Builder.Notifications.NotifyRootNodesChanged();

        foreach (var selectedNode in state.GetSelectedNodes())
            state.Builder.Selection.ChangeSelection(selectedNode, false);

        firstChild.ScrollToNode(state.Builder);
    }

    public void DeleteDataPortTreeNode(NodeButton _, VisibleActionArguments e)
    {
        if (datastore.Builder is null)
            return;

        void DeleteAction()
        {
            var deleted = e.Node switch
            {
                // root node (eg. MQTT DataPort)
                DataPortRootNodeModel rootNode => TryDeleteRootNode(datastore.Builder, rootNode),
                DataPortChildNodeModel childNode => TryDeleteChildNode(datastore.Builder, childNode),
                _ => false,
            };

            // Only a deletion the builder accepted raises the events that reset the flag again.
            if (deleted)
                state.IsDeletionInProgress = true;
        }

        if (state.OnDeleteNodeUserConfirmationRequest is null)
        {
            DeleteAction();
        }
        else
        {
            state.OnDeleteNodeUserConfirmationRequest(e.Node, DeleteAction);
        }
    }

    public void InitializeDataPortTree()
    {
        state.ClearRootNodes();

        // Create visual tree using DataPort with matching treebuilder
        foreach (var dataPort in datastore.Builder.Cache.DataPorts)
            TryCreateDataPortTree(dataPort);

        state.Builder.Reset();
        state.Builder.Helper.Preload();
    }

    public void ProcessNodeChanges(ITreeNode node)
    {
        if (datastore.Builder is null)
            return;

        if (node is not DataPortChildNodeModel childNode)
            return;

        // Parent is root node so child is DataPort
        if (childNode.Parent is DataPortRootNodeModel)
        {
            if (!datastore.Builder.TryGetDataPort(childNode, out var dataPort))
                return;

            // TODO - make it possible to change the engine in the node edit section
            datastore.Builder.Editors.DataPort.SetName(dataPort, childNode.Name);
            if (dataPort.Name != childNode.Name)
                childNode.Name = dataPort.Name;
            datastore.Builder.Editors.DataPort.SetIcon(dataPort, childNode.Icon);
            datastore.Builder.SetCustomDataPortProperties(dataPort, childNode);
            datastore.Builder.SetSystemDataPortProperties(dataPort, childNode, state.Builder);
            return;
        }

        // Normal child eg. for MQTT Folder, Value
        var childNodeId = childNode.Id.Value;
        var clusterNode = datastore.Builder.Cache.DataPortTreeNodes.FirstOrDefault(k => k.Id == childNodeId);
        if (clusterNode is null)
            return;

        datastore.Builder.Editors.DataPortTreeNode.SetName(clusterNode, childNode.Name);
        if (clusterNode.Name != childNode.Name)
            childNode.Name = clusterNode.Name;
        datastore.Builder.Editors.DataPortTreeNode.SetIcon(clusterNode, childNode.Icon);
        datastore.Builder.SetSystemDataPortTreeNodeProperties(clusterNode, childNode);
        datastore.Builder.SetCustomDataPortTreeNodeProperties(clusterNode, childNode);
    }

    public void RevertNodeChanges(DataPortChildNodeModel childNode)
    {
        if (datastore.Builder is null)
            return;

        var root = childNode.GetRootNode();
        if (childNode.Parent.Id == root.Id)
        {
            if (datastore.Builder.TryGetDataPort(childNode, out var dataPort))
                childNode.AssignValuesAndProperties(dataPort);
        }
        else if (datastore.Builder.Cache.DataPortTreeNodeIds.TryGetValue(childNode.Id.Value, out var treeNode))
        {
            childNode.AssignValuesAndProperties(treeNode, root.Builder);
        }
    }

    private void TryCreateDataPortTree(DataPort dataPort)
    {
        try
        {
            if (!registry.TryGetTreeBuilderForDataPort(dataPort.RulesetId, out var treeBuilder) || treeBuilder is null)
                throw new InvalidOperationException($"Failed to find TreeBuilder for DataPort:{dataPort.Name} with ruleset id {dataPort.RulesetId}.");

            var rootNode = AddOrGetTreeRootNode(treeBuilder);

            treeBuilder.CreateDataPortTree(dataPort, rootNode);
            DataPortNodeSorter.SortChildren(rootNode, true);
        }
        catch (Exception ex)
        {
            CreateDataPortTreeFailed(logger, ex, dataPort.Id);
        }
    }

    // we listen to the builder delete events of DataPorts and nodes to react on builder
    // changes done outside. Until such an event arrives the tree still shows nodes the cluster
    // has already dropped - deleted a second time, or together with an ancestor - and the event
    // removes them, so a node that is no longer cached needs no deletion of its own.
    private bool TryDeleteChildNode(IClusterBuilder builder, DataPortChildNodeModel childNode)
    {
        var childNodeId = childNode.Id.Value;

        // child of child node
        if (childNode.Parent is DataPortChildNodeModel)
        {
            if (!builder.Cache.DataPortTreeNodeIds.TryGetValue(childNodeId, out var cachedNode))
                return false;

            builder.Editors.DataPort.RemoveTreeNode(cachedNode);
            return true;
        }

        // child of root node (for MQTT e.g. MQTT Broker -> DataPort)
        if (!builder.Cache.DataPortIds.TryGetValue(childNodeId, out var cachedDataPort))
            return false;

        builder.Editors.Dataflow.RemoveDataPort(cachedDataPort, true);
        return true;
    }

    private bool TryDeleteRootNode(IClusterBuilder builder, DataPortRootNodeModel rootNode)
    {
        state.RemoveRootNode(rootNode);

        var removedAnyDataPort = false;

        // whole data port container node gets removed so we need to remove all contained data port
        foreach (var rootChild in rootNode.Children)
        {
            if (!builder.Cache.DataPortIds.TryGetValue(rootChild.Id.Value, out var dataPort))
                continue;

            builder.Editors.Dataflow.RemoveDataPort(dataPort, true);
            removedAnyDataPort = true;
        }

        // The root node for e.g. Mqtt was removed so it's dependency is not needed anymore
        builder.RemoveUnusedSystemDataPortDependency(rulesetProvider);

        state.Builder.Notifications.NotifyRootNodesChanged();

        return removedAnyDataPort;
    }
}
