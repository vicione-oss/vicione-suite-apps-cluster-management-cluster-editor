using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.Dataflow.Services;

internal sealed partial class DataflowStructureTreeAdapter : IDisposable
{
    public void Dispose()
    {
        if (_clusterBuilder is null)
            return;

        _clusterBuilderEventBuffer.ContainersAdded -= OnContainersAdded;
        _clusterBuilderEventBuffer.ContainersChanged -= InvokeChildrenChanged;
        _clusterBuilderEventBuffer.ContainerPropertiesChanged -= OnContainerPropertiesChanged;
        _clusterBuilderEventBuffer.ContainersRemoved -= OnContainersRemoved;

        _clusterBuilderEventBuffer.DataflowsAdded -= OnDataflowsAdded;
        _clusterBuilderEventBuffer.DataflowsRemoved -= OnDataflowsRemoved;
        _clusterBuilderEventBuffer.DataflowPropertiesChanged -= OnDataflowPropertiesChanged;

        _clusterBuilderEventBuffer.FunctionBlocksAdded -= OnFunctionBlocksAdded;
        _clusterBuilderEventBuffer.FunctionBlockPropertiesChanged -= OnFunctionBlockPropertiesChanged;
        _clusterBuilderEventBuffer.FunctionBlocksRemoved -= OnFunctionBlocksRemoved;

        _datastore.ActiveDataflowChanged -= UpdateDataflowActiveState;
        _diagramEventService.ContainerLoaded -= OnContainerLoaded;
    }

    private void InvokeChildrenChanged(IEnumerable<Container> changedContainers)
        => Builder.Notifications.NotifyChildrenChanged([.. changedContainers
            .Select<Container, ITreeNode?>(cc =>
            {
                if (cc is ChildContainer childContainer)
                    return _containerMap.TryGetValue(childContainer, out var value) ? value : null;
                else
                    return _dataflowMap.FirstOrDefault(kvp => kvp.Key.Root == cc).Value;
            })
            .Where(tn => tn is not null)
            .Cast<ITreeNode>()
        ]);

    private void LoadExistingRootBlocks()
    {
        _containerMap.Clear();
        _dataflowMap.Clear();
        _functionBlockMap.Clear();

        foreach (var dataflow in _clusterBuilder.Cluster.Dataflows)
        {
            _dataflowMap.Add(dataflow, new DataflowStructureTreeNode()
            {
                Dataflow = dataflow,
                Id = new GuidNodeIdentifier(dataflow.Id),
                Name = dataflow.Name,
            });

            var root = dataflow.Root;
            if (root is null)
                continue;

            foreach (var container in root.Containers)
            {
                _containerMap.Add(container, new ContainerStructureTreeNode()
                {
                    ChildContainer = container,
                    Id = new GuidNodeIdentifier(container.Id),
                    Name = container.Name,
                });
            }

            foreach (var fb in root.FunctionBlocks)
            {
                _functionBlockMap.Add(fb, new FunctionBlockStructureTreeNode()
                {
                    FunctionBlock = fb,
                    Id = new GuidNodeIdentifier(fb.Id),
                    Name = fb.Name,
                });
            }
        }

        Builder.Reset();
        Builder.Helper.Preload();
    }

    private void OnContainerLoaded(Container container)
    {
        if (container is not ChildContainer childContainer)
            return;

        _containerMap.TryAdd(childContainer, new ContainerStructureTreeNode()
        {
            ChildContainer = childContainer,
            Name = childContainer.Name,
        });
    }

    private void OnContainerPropertiesChanged(IEnumerable<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> changedProperties)
    {
        foreach (var (sender, e) in changedProperties)
        {
            if (sender is not ChildContainer childContainer || !_containerMap.TryGetValue(childContainer, out var mappedContainer))
                continue;

            var propertyChanged = false;

            if (e.PropertyName == nameof(ChildContainer.Name))
            {
                mappedContainer.Name = childContainer.Name;
                propertyChanged = true;
            }

            if (propertyChanged)
            {
                Builder.Notifications.NotifyNodeChanged(mappedContainer);

                // Refresh parents children to reflect property change in node order
                if (childContainer.Parent is ChildContainer parentContainer)
                {
                    if (_containerMap.TryGetValue(parentContainer, out var mappedParent))
                        Builder.Notifications.NotifyChildrenChanged(mappedParent);
                }
                else
                {
                    var dataflow = _dataflowMap.Keys.FirstOrDefault(df => df.Root == childContainer.Parent);
                    if (dataflow is not null)
                        Builder.Notifications.NotifyChildrenChanged(_dataflowMap[dataflow]);
                }
            }
        }
    }

    private void OnContainersAdded(IEnumerable<(Container Parent, Container Container)> containers)
    {
        var updatedParents = new List<Container>();
        foreach (var container in containers)
        {
            if (container.Container is not ChildContainer childContainer)
                continue;

            var added = _containerMap.TryAdd(childContainer, new ContainerStructureTreeNode()
            {
                ChildContainer = childContainer,
                Id = new GuidNodeIdentifier(childContainer.Id),
                Name = childContainer.Name,
            });

            if (added)
                updatedParents.Add(container.Parent);
        }

        InvokeChildrenChanged(updatedParents.Distinct());
    }

    private void OnContainersRemoved(IEnumerable<(Container Parent, Container Container)> containers)
    {
        var updatedParents = new List<Container>();
        foreach (var container in containers)
        {
            if (container.Container is not ChildContainer childContainer)
                continue;

            if (_containerMap.Remove(childContainer))
                updatedParents.Add(container.Parent);
        }

        InvokeChildrenChanged(updatedParents.Distinct());
    }

    private void OnDataflowPropertiesChanged(IEnumerable<(object? s, System.ComponentModel.PropertyChangedEventArgs e)> changedProperties)
    {
        foreach (var (s, e) in changedProperties)
        {
            if (s is not Cluster.Model.Dataflow dataflow || !_dataflowMap.TryGetValue(dataflow, out var dataflowNode))
                continue;

            if (e.PropertyName == nameof(Cluster.Model.Dataflow.Name))
                dataflowNode.Name = dataflow.Name;

            Builder.Notifications.NotifyNodeChanged(dataflowNode);
        }

        Builder.Notifications.NotifyRootNodesChanged();
        Builder.Filter.Apply();
    }

    private void OnDataflowsAdded(IEnumerable<(Cluster.Model.Cluster Parent, Cluster.Model.Dataflow Dataflow)> addedDataflows)
    {
        foreach (var dataflow in addedDataflows)
        {
            _dataflowMap[dataflow.Dataflow] = new()
            {
                Dataflow = dataflow.Dataflow,
                Id = new GuidNodeIdentifier(dataflow.Dataflow.Id),
                Name = dataflow.Dataflow.Name,
            };
        }

        Builder.Notifications.NotifyRootNodesChanged();
        Builder.Filter.Apply();
    }

    private void OnDataflowsRemoved(IEnumerable<(Cluster.Model.Cluster Parent, Cluster.Model.Dataflow Dataflow)> removedDataflows)
    {
        foreach (var dataflow in removedDataflows)
            _dataflowMap.Remove(dataflow.Dataflow);

        Builder.Notifications.NotifyRootNodesChanged();
    }

    private void OnFunctionBlockPropertiesChanged(IEnumerable<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> changedProperties)
    {
        foreach (var (sender, e) in changedProperties)
        {
            if (sender is not FunctionBlock functionBlock || !_functionBlockMap.TryGetValue(functionBlock, out var mappedFb))
                continue;

            var propertyChanged = false;

            if (e.PropertyName == nameof(FunctionBlock.Name))
            {
                mappedFb.Name = functionBlock.Name;
                propertyChanged = true;
            }

            if (propertyChanged)
            {
                Builder.Notifications.NotifyNodeChanged(mappedFb);

                // Refresh parents children to reflect property change in node order
                if (functionBlock.Container is ChildContainer parentContainer)
                {
                    if (_containerMap.TryGetValue(parentContainer, out var mappedParent))
                        Builder.Notifications.NotifyChildrenChanged(mappedParent);
                }
                else
                {
                    var dataflow = _dataflowMap.Keys.FirstOrDefault(df => df.Root == functionBlock.Container);
                    if (dataflow is not null)
                        Builder.Notifications.NotifyChildrenChanged(_dataflowMap[dataflow]);
                }
            }
        }
    }

    private void OnFunctionBlocksAdded(IEnumerable<(Container Parent, FunctionBlock FunctionBlock)> functionBlocks)
    {
        var updatedParents = new List<Container>();
        foreach (var functionBlock in functionBlocks)
        {
            var added = _functionBlockMap.TryAdd(functionBlock.FunctionBlock, new FunctionBlockStructureTreeNode()
            {
                FunctionBlock = functionBlock.FunctionBlock,
                Id = new GuidNodeIdentifier(functionBlock.FunctionBlock.Id),
                Name = functionBlock.FunctionBlock.Name,
            });

            if (added && !updatedParents.Contains(functionBlock.Parent))
                updatedParents.Add(functionBlock.Parent);
        }

        InvokeChildrenChanged(updatedParents.Distinct());
    }

    private void OnFunctionBlocksRemoved(IEnumerable<(Container Parent, FunctionBlock FunctionBlock)> functionBlocks)
    {
        var updatedParents = new List<Container>();
        foreach (var functionBlock in functionBlocks)
        {
            var removed = _functionBlockMap.Remove(functionBlock.FunctionBlock);

            if (removed && !updatedParents.Contains(functionBlock.Parent))
                updatedParents.Add(functionBlock.Parent);
        }

        InvokeChildrenChanged(updatedParents.Distinct());
    }

    public void UseBuilder(IClusterBuilder newBuilder)
    {
        // Null check to secure against the internal `[...] = null!` assignment
        if (_clusterBuilder is not null)
        {
            _clusterBuilderEventBuffer.ContainersAdded -= OnContainersAdded;
            _clusterBuilderEventBuffer.ContainersChanged -= InvokeChildrenChanged;
            _clusterBuilderEventBuffer.ContainerPropertiesChanged -= OnContainerPropertiesChanged;
            _clusterBuilderEventBuffer.ContainersRemoved -= OnContainersRemoved;

            _clusterBuilderEventBuffer.DataflowsAdded -= OnDataflowsAdded;
            _clusterBuilderEventBuffer.DataflowsRemoved -= OnDataflowsRemoved;
            _clusterBuilderEventBuffer.DataflowPropertiesChanged -= OnDataflowPropertiesChanged;

            _clusterBuilderEventBuffer.FunctionBlocksAdded -= OnFunctionBlocksAdded;
            _clusterBuilderEventBuffer.FunctionBlockPropertiesChanged -= OnFunctionBlockPropertiesChanged;
            _clusterBuilderEventBuffer.FunctionBlocksRemoved -= OnFunctionBlocksRemoved;
        }

        _clusterBuilder = newBuilder;

        _clusterBuilderEventBuffer.ContainersAdded += OnContainersAdded;
        _clusterBuilderEventBuffer.ContainersChanged += InvokeChildrenChanged;
        _clusterBuilderEventBuffer.ContainerPropertiesChanged += OnContainerPropertiesChanged;
        _clusterBuilderEventBuffer.ContainersRemoved += OnContainersRemoved;

        _clusterBuilderEventBuffer.DataflowsAdded += OnDataflowsAdded;
        _clusterBuilderEventBuffer.DataflowsRemoved += OnDataflowsRemoved;
        _clusterBuilderEventBuffer.DataflowPropertiesChanged += OnDataflowPropertiesChanged;

        _clusterBuilderEventBuffer.FunctionBlocksAdded += OnFunctionBlocksAdded;
        _clusterBuilderEventBuffer.FunctionBlockPropertiesChanged += OnFunctionBlockPropertiesChanged;
        _clusterBuilderEventBuffer.FunctionBlocksRemoved += OnFunctionBlocksRemoved;

        LoadExistingRootBlocks();
    }
}
