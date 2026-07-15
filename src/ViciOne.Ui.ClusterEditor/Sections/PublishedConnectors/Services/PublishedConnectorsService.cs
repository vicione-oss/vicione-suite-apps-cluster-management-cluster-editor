using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;

internal sealed class PublishedConnectorsService : IDisposable
{
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly IDatastore _datastore;
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
        IDatastore datastore,
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
        if (secondBlockNodeConnector is null)
            return;

        var secondConnector = _datastore.DataflowDiagramMapping.GetModel(secondBlockNodeConnector);

        foreach (var item in _dragService.DraggedItems)
        {
            if (item is not DataGridConnectorWrapper publishedConnector)
                continue;

            var sourceConnector = (IConnectorOutput)(publishedConnector.IsInput ? secondConnector : publishedConnector.Connector);
            var targetConnector = (IConnectorInput)(publishedConnector.IsInput ? publishedConnector.Connector : secondConnector);

            if (_datastore.Builder.Editors.Connector.CanCreateLink(sourceConnector, targetConnector, false))
                _datastore.Builder.Editors.Connector.AddLink(sourceConnector, targetConnector, false);
        }
    }

    private static int CompareConnectorWrappers(DataGridConnectorWrapper? x, DataGridConnectorWrapper? y)
    {
        if (x is null || y is null)
            return 0;

        var pathCompare = string.Compare(x.Path, y.Path, StringComparison.Ordinal);
        if (pathCompare != 0)
            return pathCompare;

        var fbCompare = string.Compare(x.FunctionBlockName, y.FunctionBlockName, StringComparison.Ordinal);
        if (fbCompare != 0)
            return fbCompare;

        var inputCompare = x.IsInput.CompareTo(y.IsInput);
        if (inputCompare != 0)
            return inputCompare;

        return string.Compare(x.ConnectorName, y.ConnectorName, StringComparison.Ordinal);
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
    {
        if (_dragService.DragTargets is null)
            return null;

        foreach (var target in _dragService.DragTargets)
        {
            if (target is BlockNodeConnector connector && connector.GetBounds().ContainsPoint(position))
                return connector;
        }

        return null;
    }

    private void MoveDraggingPublishedConnector(Point position)
    {
        _moveOccured = true;

        var relativeMousePoint = _diagramService.Diagram.GetRelativeMousePoint(position.X, position.Y);
        var targetBlockNodeConnector = GetTargetBlockNodeConnector(relativeMousePoint);

        DraggingPublishedConnectorPositionChanged?.Invoke(position, targetBlockNodeConnector is not null);
    }

    private Task OnClusterBuilderChanged()
    {
        UpdatePublishedConnectorEntries();
        return Task.CompletedTask;
    }

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
            break;
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

            if (updateNeeded)
                break;
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

            if (e.PropertyName == nameof(FunctionBlock.Name))
            {
                foreach (var c in _datastore.Builder.Cache.PublishedConnectors)
                {
                    if (c.FunctionBlock == functionBlock)
                    {
                        updateNeeded = true;
                        break;
                    }
                }

                if (updateNeeded)
                    break;
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
        var connectors = new List<Connector>();
        foreach (var wrapper in connectorWrappersToDrag)
            connectors.Add((Connector)wrapper.Connector);

        if (connectors.Count == 0)
            return;

        var validTargetConnectors = _datastore.GetValidTargetConnectors(connectors, false);
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
        var publishedConnectors = _datastore.Builder.Cache.PublishedConnectors;

        var currentlyPublishedIds = new HashSet<Guid>(publishedConnectors.Count);
        foreach (var pc in publishedConnectors)
            currentlyPublishedIds.Add(pc.Id);

        var existingIds = new HashSet<Guid>(_publishedConnectorWrappers.Count);
        foreach (var pc in _publishedConnectorWrappers)
            existingIds.Add(pc.Connector.Id);

        _publishedConnectorWrappers.RemoveAll(pc => !currentlyPublishedIds.Contains(pc.Connector.Id));

        foreach (var pc in publishedConnectors)
        {
            if (!existingIds.Contains(pc.Id))
            {
                _publishedConnectorWrappers.Add(new DataGridConnectorWrapper(
                    pc,
                    _datastore.Builder.ResolveConnectorDesign(pc),
                    _datastore.Builder.ResolveFunctionBlockDesign(pc.FunctionBlock.DesignId)
                ));
            }
        }

        _publishedConnectorWrappers.Sort(CompareConnectorWrappers);

        PublishedConnectorsChanged?.Invoke();
    }
}
