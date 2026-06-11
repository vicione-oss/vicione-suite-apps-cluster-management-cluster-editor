using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions.Arguments;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal sealed partial class DataPortTreeAdapter : TreeAdapter
{
    public const string DataPortCategory = "DataPorts";

    private readonly List<DataPortRootNodeModel> _dataPorts = [];

    public Action<ITreeNode, Action>? OnDeleteNodeUserConfirmationRequest { get; set; }

    private DataPortRootNodeModel AddOrGetTreeRootNode(TreeBuilder.TreeBuilder treeBuilder)
    {
        // the root node is just a visual container and does not get stored in the cluster
        var rootNode = _dataPorts.FirstOrDefault(k => Equals(k.Builder.Ruleset.Root?.Id, treeBuilder.Ruleset.Root?.Id));
        if (rootNode is null)
        {
            rootNode = new()
            {
                AvailableIcons = treeBuilder.Ruleset.Root!.Icons, // always exactly one
                Builder = treeBuilder,
                DisplayText = treeBuilder.Ruleset.Root!.Name,
                Icon = treeBuilder.Ruleset.Root!.Icons.FirstOrDefault(),
                Id = Guid.TryParse(treeBuilder.Ruleset.Root!.Id, out var rnId)
                    ? new GuidNodeIdentifier(rnId)
                    : GuidNodeIdentifier.New(),
            };
            _dataPorts.Add(rootNode);
        }

        // We have a root node for e.g. Mqtt so we need a ClusterDependency to it's package
        _datastore.Builder?.EnsureSystemDataPortDependencyExists(_rulesetProvider);

        rootNode.PossibleChildren = [.. rootNode.GetPossibleChildNodes()];
        rootNode.CanHaveChildren = rootNode.PossibleChildren.Any();

        return rootNode;
    }

    public void CreateNewChildNode(ITreeNode parentNode, ITreeNode newChild)
    {
        if (parentNode is not DataPortNodeModel dataPortParentNode || newChild is not DataPortChildNodeModel dataPortChildNode)
            return;

        if (_datastore.Builder is null)
            return;

        // first DataPort element was added  (for MQTT eg. Broker|MqttTemplate)
        if (dataPortParentNode is DataPortRootNodeModel rootNode)
        {
            var dataPort = _datastore.Builder.GetOrCreateDataPort(_datastore, rootNode, dataPortChildNode);

            _datastore.Builder.SetSystemDataPortProperties(dataPort, dataPortChildNode, Builder);
            _datastore.Builder.SetCustomDataPortProperties(dataPort, dataPortChildNode);
        }
        else
        {
            // child was added to child (for MQTT eg. Folder|Value)
            var dataPort = _datastore.Builder.GetDataPort(dataPortChildNode);
            var treeNode = _datastore.Builder.GetOrCreateDataPortTreeNode(dataPort, dataPortChildNode);

            _datastore.Builder.SetSystemDataPortTreeNodeProperties(treeNode, dataPortChildNode);
            _datastore.Builder.SetCustomDataPortTreeNodeProperties(treeNode, dataPortChildNode);
        }

        dataPortParentNode.Children.Add(dataPortChildNode);

        dataPortParentNode.PossibleChildren = [.. dataPortParentNode.GetPossibleChildNodes()];
        dataPortChildNode.PossibleChildren = [.. dataPortChildNode.GetPossibleChildNodes()];

        if (dataPortChildNode.PossibleChildren.Any())
            SortChildren(dataPortParentNode, false);

        Builder.Notifications.NotifyNodeChanged(dataPortParentNode, ChangedNodeDetail.Actions);
        Builder.Notifications.NotifyChildrenChanged(dataPortParentNode);
    }

    public void CreateNewDataPortRootNode(string rulesetIdentifier)
    {
        var builder = GetOrCreateTreeBuilder(DataPortCategory, rulesetIdentifier);
        var node = AddOrGetTreeRootNode(builder);
        node.PossibleChildren = [.. node.GetPossibleChildNodes()];
        var firstChild = node.PossibleChildren[0];
        CreateNewChildNode(node, firstChild);

        Builder.Notifications.NotifyRootNodesChanged();

        foreach (var selectedNode in GetSelectedNodes())
            Builder.Selection.ChangeSelection(selectedNode, false);

        firstChild.ScrollToNode(Builder);
    }

    private void DeleteDataPortTreeNode(NodeButton _, VisibleActionArguments e)
    {
        if (_datastore.Builder is null)
            return;

        void DeleteAction()
        {
            _isDeletionInProgress = true;

            // root node (eg. MQTT DataPort)
            if (e.Node is DataPortRootNodeModel rootNode)
            {
                _dataPorts.Remove(rootNode);

                // whole data port container node gets removed so we need to remove all contained data port
                foreach (var rootChild in rootNode.Children)
                {
                    var nodeId = rootChild.Id.Value;
                    var dataPort = _datastore.Builder.Cache.DataPorts.First(k => k.Id == nodeId);

                    _datastore.Builder.Editors.Dataflow.RemoveDataPort(dataPort, true);
                }

                // The root node for e.g. Mqtt was removed so it's dependency is not needed anymore
                _datastore.Builder.RemoveUnusedSystemDataPortDependency(_rulesetProvider);

                Builder.Notifications.NotifyRootNodesChanged();
            }

            // we listen to the builder delete events of DataPorts and nodes to react on builder
            // changes done outside
            if (e.Node is DataPortChildNodeModel childNode)
            {
                var childNodeId = childNode.Id.Value;

                // child of child node
                if (GetParent(childNode) is DataPortChildNodeModel parentNode)
                {
                    var cachedNode = _datastore.Builder.Cache.DataPortTreeNodes.First(k => k.Id == childNodeId);
                    _datastore.Builder.Editors.DataPort.RemoveTreeNode(cachedNode);
                    return;
                }

                // child of root node (for MQTT e.g. MQTT Broker -> DataPort)
                var cachedDataPort = _datastore.Builder.Cache.DataPorts.First(k => k.Id == childNodeId);
                _datastore.Builder.Editors.Dataflow.RemoveDataPort(cachedDataPort, true);
            }

            _isDeletionInProgress = false;
        }

        if (OnDeleteNodeUserConfirmationRequest is null)
        {
            DeleteAction();
        }
        else
        {
            OnDeleteNodeUserConfirmationRequest(e.Node, DeleteAction);
        }
    }

    public void FilterNodes(string filterText)
    {
        TreeAdapterHelper.FilterNodesByDisplayText(Builder, ResolveParent, filterText);

        static ITreeNode? ResolveParent(ITreeNode node)
        {
            if (node is not DataPortChildNodeModel childNode)
                return null;

            return childNode.Parent;
        }
    }

    internal static void SortChildren(DataPortNodeModel dataPortParentNode, bool sortNonParentChildren)
    {
        var children = dataPortParentNode.Children;
        children = [.. SortNodes(children, sortNonParentChildren)];
        dataPortParentNode.Children.Clear();
        dataPortParentNode.Children.AddRange(children);
    }

    internal static IEnumerable<DataPortNodeModel> SortNodes(IEnumerable<DataPortNodeModel> children, bool sortNonParentChildren)
    {
        var childrenList = children.ToList();
        var parentChildNodes = childrenList.Where(c => c.PossibleChildren.Any()).ToList();
        var nonParentChildNodes = childrenList.Except(parentChildNodes).ToList();
        parentChildNodes = [.. parentChildNodes.OrderBy(c => c.DisplayText, AlphaNumericComparer<string>.Default)];

        if (sortNonParentChildren)
        {
            nonParentChildNodes = [.. nonParentChildNodes.OrderBy(c => c.DisplayText, AlphaNumericComparer<string>.Default)];
        }

        children = [.. parentChildNodes, .. nonParentChildNodes];
        return children;
    }
}
