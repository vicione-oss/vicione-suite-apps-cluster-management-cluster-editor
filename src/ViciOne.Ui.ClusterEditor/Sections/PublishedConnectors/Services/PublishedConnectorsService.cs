using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;

internal sealed class PublishedConnectorsService : IDisposable
{
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly Datastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly DragService _dragService;
    private bool _moveOccured;
    private readonly List<DataGridConnectorWrapper> _publishedConnectorWrappers = [];

    public IEnumerable<DataGridConnectorWrapper> PublishedConnectorWrappers
        => _publishedConnectorWrappers;

    public event Action? DraggingEnded;
    public event Action<Point?, bool>? DraggingPublishedConnectorPositionChanged;
    public event Action? PublishedConnectorsChanged;

    public PublishedConnectorsService(
        ClusterBuilderEventBuffer clusterBuilderEventBuffer,
        Datastore datastore,
        DiagramService diagramService,
        DragService dragService)
    {
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _clusterBuilderEventBuffer.ConnectorLinksAdded += OnConnectorLinksChanged;
        _clusterBuilderEventBuffer.ConnectorLinksRemoved += OnConnectorLinksChanged;
        _clusterBuilderEventBuffer.ConnectorPropertiesChanged += OnConnectorPropertiesChanged;
        _clusterBuilderEventBuffer.ContainersChanged += OnClusterStateChanged;
        _clusterBuilderEventBuffer.ContainerPropertiesChanged += OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlockPropertiesChanged += OnFunctionBlockPropertiesChanged;
        _clusterBuilderEventBuffer.FunctionBlocksRemoved += OnClusterStateChanged;

        _datastore = datastore;
        _datastore.BuilderChanged += OnClusterBuilderChanged;

        _diagramService = diagramService;
        _dragService = dragService;
    }

    private void AddLink(BlockNodeConnector? secondBlockNodeConnector)
    {
        if (!_dragService.DraggedItems.Any(d => d is DataGridConnectorWrapper) || secondBlockNodeConnector is null)
            return;

        var draggedPublishedConnectorWrappers = _dragService.DraggedItems
            .OfType<DataGridConnectorWrapper>()
            .ToArray();

        var secondConnector = _datastore.DataflowDiagramMapping.GetModel(secondBlockNodeConnector);

        foreach (var publishedConnector in draggedPublishedConnectorWrappers)
        {
            var sourceConnector = (IConnectorOutput)(publishedConnector.IsInput ? secondConnector : publishedConnector.Connector);
            var targetConnector = (IConnectorInput)(publishedConnector.IsInput ? publishedConnector.Connector : secondConnector);

            if (_datastore.Builder.Editors.Connector.CanCreateLink(sourceConnector, targetConnector, false))
                _datastore.Builder.Editors.Connector.AddLink(sourceConnector, targetConnector, false);
        }
    }

    public void Dispose()
    {
        _clusterBuilderEventBuffer.ConnectorLinksAdded -= OnConnectorLinksChanged;
        _clusterBuilderEventBuffer.ConnectorLinksRemoved -= OnConnectorLinksChanged;
        _clusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnConnectorPropertiesChanged;
        _clusterBuilderEventBuffer.ContainersChanged -= OnClusterStateChanged;
        _clusterBuilderEventBuffer.ContainerPropertiesChanged -= OnClusterStateChanged;
        _clusterBuilderEventBuffer.FunctionBlockPropertiesChanged -= OnFunctionBlockPropertiesChanged;
        _clusterBuilderEventBuffer.FunctionBlocksRemoved -= OnClusterStateChanged;

        _datastore.BuilderChanged -= OnClusterBuilderChanged;
    }

    private BlockNodeConnector? GetTargetBlockNodeConnector(Point position)
        => _dragService.DragTargets?.OfType<BlockNodeConnector>().FirstOrDefault(p => p.GetBounds().ContainsPoint(position));

    private void MoveDraggingPublishedConnector(Point position)
    {
        _moveOccured = true;

        var relativeMousePoint = _diagramService.Diagram.GetRelativeMousePoint(position.X, position.Y);
        var targetBlockNodeConnector = GetTargetBlockNodeConnector(relativeMousePoint);

        DraggingPublishedConnectorPositionChanged?.Invoke(position, targetBlockNodeConnector is not null);
    }

    private void OnClusterBuilderChanged()
        => UpdatePublishedConnectorEntries();

    private void OnClusterStateChanged<T>(IEnumerable<T> _)
        => UpdatePublishedConnectorEntries();

    private void OnConnectorLinksChanged(IEnumerable<Link> links)
    {
        var updateNeeded = false;

        foreach (var link in links)
        {
            if (link.Visible)
                continue;

            updateNeeded = true;
        }

        if (updateNeeded)
            UpdatePublishedConnectorEntries();
    }

    private void OnConnectorPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedConnectorProperties)
    {
        var updateNeeded = false;

        foreach (var (sender, e) in changedConnectorProperties)
        {
            if (e.PropertyName is null)
                continue;

            switch (e.PropertyName)
            {
                case nameof(Connector.Published):
                case nameof(Connector.Description):
                    updateNeeded = true;
                    break;
            }
        }

        if (updateNeeded)
            UpdatePublishedConnectorEntries();
    }

    private void OnDraggingEnded(Point position)
    {
        var relativeMousePoint = _diagramService.Diagram.GetRelativeMousePoint(position.X, position.Y);
        var targetBlockNodeConnector = GetTargetBlockNodeConnector(relativeMousePoint);

        if (targetBlockNodeConnector is not null)
            AddLink(targetBlockNodeConnector);

        StopPublishedConnectorDragging();
    }

    private void OnFunctionBlockPropertiesChanged(IEnumerable<(object? Sender, PropertyChangedEventArgs EventArgs)> functionBlockProperties)
    {
        var updateNeeded = false;

        foreach (var (sender, e) in functionBlockProperties)
        {
            if (e.PropertyName is null)
                continue;

            if (sender is not FunctionBlock functionBlock)
                continue;

            if (e.PropertyName == nameof(FunctionBlock.Name) &&
                _datastore.Builder.Cache.PublishedConnectors.Any(c => c.FunctionBlock == functionBlock))
            {
                updateNeeded = true;
            }
        }

        if (updateNeeded)
            UpdatePublishedConnectorEntries();
    }

    internal void RemovePublishedConnectors(IEnumerable<DataGridConnectorWrapper>? publishedConnectorsWrappers)
    {
        if (publishedConnectorsWrappers is null)
            return;

        foreach (var publishedConnector in publishedConnectorsWrappers)
            _datastore.Builder.Editors.Connector.SetPublished((Connector)publishedConnector.Connector, false);
    }

    public void StartPublishedConnectorDragging(IEnumerable<DataGridConnectorWrapper> connectorWrappersToDrag)
    {
        if (!connectorWrappersToDrag.Any())
            return;

        var validTargetConnectors = _datastore.GetValidTargetConnectors(connectorWrappersToDrag.Select(pc => (Connector)pc.Connector), false);
        _dragService.StartDragging(connectorWrappersToDrag, validTargetConnectors);
        _dragService.DraggingEnded += OnDraggingEnded;
        _dragService.DraggingPositionChanged += MoveDraggingPublishedConnector;
    }

    private void StopPublishedConnectorDragging()
    {
        _dragService.DraggingEnded -= OnDraggingEnded;
        _dragService.DraggingPositionChanged -= MoveDraggingPublishedConnector;

        if (_moveOccured)
        {
            _moveOccured = false;
            DraggingEnded?.Invoke();
        }

        DraggingPublishedConnectorPositionChanged?.Invoke(null, false);
    }

    private void UpdatePublishedConnectorEntries()
    {
        var currentlyPublished = _datastore.Builder.Cache.PublishedConnectors.ToArray();
        var removed = _publishedConnectorWrappers
            .Where(pc => !currentlyPublished.Any(cp => cp.Id == pc.Connector.Id))
            .Select(pc => pc.Connector.Id)
            .ToArray();
        var added = currentlyPublished
            .Where(cp => !_publishedConnectorWrappers.Any(pc => pc.Connector.Id == cp.Id))
            .ToArray();

        _publishedConnectorWrappers.RemoveAll(pc => removed.Contains(pc.Connector.Id));
        _publishedConnectorWrappers.AddRange(
            added.Select(c => new DataGridConnectorWrapper(
                c,
                _datastore.Builder.ResolveConnectorDesign(c),
                _datastore.Builder.ResolveFunctionBlockDesign(c.FunctionBlock.DesignId)
        )));

        _publishedConnectorWrappers.Sort((x, y) =>
        {
            if (x is null || y is null)
                return 0;

            var pathCompare = string.Compare($"{x.Path}_{x.FunctionBlockName}", $"{y.Path}_{y.FunctionBlockName}", StringComparison.Ordinal);
            if (pathCompare != 0)
                return pathCompare;

            var inputCompare = x.IsInput.CompareTo(y.IsInput);
            if (inputCompare != 0)
                return inputCompare;

            return string.Compare(x.ConnectorName, y.ConnectorName, StringComparison.Ordinal);
        });

        PublishedConnectorsChanged?.Invoke();
    }
}
