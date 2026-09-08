using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

/// <summary>
/// Subscribes to the buffered cluster-builder events and projects them onto the
/// diagram models, raising the corresponding <see cref="DatastoreState"/> change
/// notifications. Behavior services detach these handlers around their own diagram
/// mutations to avoid feedback loops.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class BuilderEventProjectionService(ClusterBuilderEventBuffer clusterBuilderEventBuffer, DatastoreState state, DiagramProjectionService diagramProjectionService) : IDisposable
{
    private bool _disposed;

    public void Attach()
    {
        clusterBuilderEventBuffer.ConnectorLinksAdded += OnConnectorLinksAdded;
        clusterBuilderEventBuffer.ConnectorLinksRemoved += OnConnectorLinksRemoved;
        clusterBuilderEventBuffer.ConnectorPropertiesChanged += OnConnectorPropertiesChanged;
        clusterBuilderEventBuffer.ContainerPropertiesChanged += OnContainerPropertiesChanged;
        clusterBuilderEventBuffer.EnginesAssigned += OnFunctionBlockEnginesChanged;
        clusterBuilderEventBuffer.EnginesUnassigned += OnFunctionBlockEnginesChanged;
        clusterBuilderEventBuffer.FunctionBlockPropertiesChanged += OnFunctionBlockPropertiesChanged;
        clusterBuilderEventBuffer.LabelPropertiesChanged += OnLabelPropertiesChanged;
    }

    public void Detach()
    {
        clusterBuilderEventBuffer.ConnectorLinksAdded -= OnConnectorLinksAdded;
        clusterBuilderEventBuffer.ConnectorLinksRemoved -= OnConnectorLinksRemoved;
        clusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnConnectorPropertiesChanged;
        clusterBuilderEventBuffer.ContainerPropertiesChanged -= OnContainerPropertiesChanged;
        clusterBuilderEventBuffer.EnginesAssigned -= OnFunctionBlockEnginesChanged;
        clusterBuilderEventBuffer.EnginesUnassigned -= OnFunctionBlockEnginesChanged;
        clusterBuilderEventBuffer.FunctionBlockPropertiesChanged -= OnFunctionBlockPropertiesChanged;
        clusterBuilderEventBuffer.LabelPropertiesChanged -= OnLabelPropertiesChanged;
    }

    /// <summary>
    /// Detaches all builder-event handlers so the <see cref="ClusterBuilderEventBuffer"/> no
    /// longer holds references to this service. Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Detach();
    }

    private void OnConnectorLinksAdded(IEnumerable<Link> links)
    {
        foreach (var link in links)
        {
            if (link.Visible)
                continue;

            UpdateConnectorMarker(link);
        }
    }

    private void OnConnectorLinksRemoved(IEnumerable<Link> links)
    {
        var mapping = state.DataflowDiagramMapping;

        foreach (var link in links)
        {
            if (!link.Visible)
                UpdateConnectorMarker(link);

            // refresh source connector properties to reflect link changes
            var sourceConnector = link.SourceConnector;
            if (sourceConnector is not null && mapping.TryGetDiagramModel(sourceConnector, out var diagramConnector))
            {
                diagramConnector.SetHasUpstreamLinks(sourceConnector.HasUpstreamLinks());
                diagramConnector.Parent.Refresh();
            }

            // refresh destination connector properties to reflect link changes
            var destinationConnector = link.DestinationConnector;
            if (destinationConnector is not null && mapping.TryGetDiagramModel(destinationConnector, out diagramConnector))
            {
                diagramConnector.SetHasUpstreamLinks(destinationConnector.HasUpstreamLinks());
                diagramConnector.Parent.Refresh();
            }

            if (mapping.TryGetDiagramModel(link, out var linkNode))
                state.InvokeConnectorLinkRemoved(linkNode);
        }
    }

    private void OnConnectorPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedConnectorProperties)
    {
        foreach (var (sender, e) in changedConnectorProperties)
        {
            if (e.PropertyName is null)
                continue;

            if (sender is not IConnector connector)
                continue;

            // Changing a FB setting raises this event twice for a [setting name] connector for the properties
            // "Value" and "ValueSerialized". We don't have a mapping for these connectors since we don't display
            // them or interact with them directly.
            if (!state.DataflowDiagramMapping.TryGetDiagramModel(connector, out var nodeConnector))
                continue;

            ConnectorMapper.PropertyChanged(connector, nodeConnector, e.PropertyName);
            state.InvokePropertyChanged(e.PropertyName);
        }
    }

    private void OnContainerPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedContainerProperties)
    {
        List<(BlockNode Node, string Name)>? nameChanges = null;

        foreach (var (sender, e) in changedContainerProperties)
        {
            if (sender is not ChildContainer childContainer
                || e.PropertyName is null
                || !state.DataflowDiagramMapping.TryGetDiagramModel(childContainer, out var childContainerNode))
            {
                continue;
            }

            ChildContainerMapper.PropertyChanged(childContainer, childContainerNode, state, e.PropertyName);
            state.InvokePropertyChanged(e.PropertyName);
            state.InvokeContainerPropertyChanged(childContainer, e.PropertyName);

            if (e.PropertyName == nameof(ChildContainer.Name))
                (nameChanges ??= []).Add((childContainerNode, childContainer.Name));
        }

        if (nameChanges is not null)
            _ = diagramProjectionService.UpdateNameFieldHeights(nameChanges, CancellationToken.None);
    }

    private void OnFunctionBlockEnginesChanged(IEnumerable<FunctionBlock> functionBlocks)
    {
        var fbs = functionBlocks.ToArray();
        HashSet<(ChildContainer, ChildContainerNode)> containersToRefresh = [];

        foreach (var fb in fbs)
        {
            if (state.DataflowDiagramMapping.TryGetDiagramModel(fb, out var functionBlockNode))
                FunctionBlockMapper.PropertyChanged(state, fb, functionBlockNode, nameof(FunctionBlock.Engine));
            else if (fb.Container is ChildContainer childContainer && state.DataflowDiagramMapping.TryGetDiagramModel(childContainer, out var childContainerNode))
                containersToRefresh.Add((childContainer, childContainerNode));
        }

        foreach (var (childContainer, childContainerNode) in containersToRefresh)
            ChildContainerMapper.PropertyChanged(childContainer, childContainerNode, state, nameof(FunctionBlock.Engine));

        state.InvokePropertyChanged(nameof(FunctionBlock.Engine));
    }

    private void OnFunctionBlockPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedFunctionBlockProperties)
    {
        List<(BlockNode Node, string Name)>? nameChanges = null;

        foreach (var (sender, e) in changedFunctionBlockProperties)
        {
            if (sender is not FunctionBlock functionBlock
                || e.PropertyName is null
                || e.PropertyName == nameof(FunctionBlock.Engine)
                || !state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out var functionBlockNode))
            {
                continue;
            }

            FunctionBlockMapper.PropertyChanged(state, functionBlock, functionBlockNode, e.PropertyName);
            state.InvokePropertyChanged(e.PropertyName);

            if (e.PropertyName == nameof(FunctionBlock.Name))
                (nameChanges ??= []).Add((functionBlockNode, functionBlock.Name));
        }

        if (nameChanges is not null)
            _ = diagramProjectionService.UpdateNameFieldHeights(nameChanges, CancellationToken.None);
    }

    private void OnLabelPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedLabelProperties)
    {
        foreach (var (sender, e) in changedLabelProperties)
        {
            if (sender is not Label label || e.PropertyName is null || !state.DataflowDiagramMapping.TryGetDiagramModel(label, out var labelNode))
                continue;

            LabelMapper.PropertyChanged(label, labelNode, e.PropertyName);
            state.InvokePropertyChanged(e.PropertyName);
        }
    }

    private void UpdateConnectorMarker(Link link)
    {
        if (link.SourceConnector is not null && state.DataflowDiagramMapping.TryGetDiagramModel(link.SourceConnector, out var sourceConnectorNode))
            sourceConnectorNode.UpdateConnectorMarker();

        if (link.DestinationConnector is not null && state.DataflowDiagramMapping.TryGetDiagramModel(link.DestinationConnector, out var targetConnectorNode))
            targetConnectorNode.UpdateConnectorMarker();
    }
}
