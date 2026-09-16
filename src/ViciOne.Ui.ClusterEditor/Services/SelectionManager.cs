using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class SelectionManager(DiagramService diagramService, IJSRuntime jsRuntime, ILogger<SelectionManager> logger) : IDisposable
{
    private List<BlockNode>? _selectedBlockNodesCache;
    private readonly List<ConnectorMarker> _selectedConnectorMarker = [];
    private readonly List<BlockNodeConnector> _selectedConnectors = [];
    private List<ChildContainerNode>? _selectedContainersCache;
    private List<FunctionBlockNode>? _selectedFBsCache;
    private List<LabelNode>? _selectedLabelsCache;
    private List<BlockNodeLink>? _selectedLinksCache;
    private int _selectedMarkersWithLinksCount;
    private SelectableModel[]? _selectedModelsCache;
    private List<IDiagramModel>? _selectedModelsFullCache;

    public bool HasSelection
        => _selectedConnectors.Count > 0
        || _selectedConnectorMarker.Count > 0
        || SelectedContainers.Count > 0
        || SelectedFBs.Count > 0
        || SelectedLabels.Count > 0
        || SelectedLinks.Count > 0;
    public IReadOnlyList<BlockNode> SelectedBlockNodes
        => _selectedBlockNodesCache ??= [.. SelectedContainers, .. SelectedFBs];
    public IReadOnlyList<ConnectorMarker> SelectedConnectorMarker
        => _selectedConnectorMarker;
    public IReadOnlyList<BlockNodeConnector> SelectedConnectors
        => _selectedConnectors;
    public IReadOnlyList<ChildContainerNode> SelectedContainers
        => _selectedContainersCache ??= BuildTypedCache<ChildContainerNode>();
    public IReadOnlyList<FunctionBlockNode> SelectedFBs
        => _selectedFBsCache ??= BuildTypedCache<FunctionBlockNode>();
    public IReadOnlyList<LabelNode> SelectedLabels
        => _selectedLabelsCache ??= BuildTypedCache<LabelNode>();
    public IReadOnlyList<BlockNodeLink> SelectedLinks
        => _selectedLinksCache ??= BuildTypedCache<BlockNodeLink>();
    public IReadOnlyList<IDiagramModel> SelectedModels
        => _selectedModelsFullCache ??= [.. SelectedConnectors, .. SelectedContainers,
       .. SelectedFBs, .. SelectedLabels, .. SelectedLinks, .. SelectedConnectorMarker];

    public event Action<IEnumerable<BlockNodeConnector>>? ConnectorSelectionChanged;
    public event Func<SelectableModel, Task>? DiagramSelectionChanged;

    public void AttachDiagramEvents()
    {
        diagramService.Diagram.SelectionChanged += OnDiagramSelectionChanged;
        diagramService.Diagram.Nodes.Removed += OnDiagramNodesRemoved;
    }

    private List<T> BuildTypedCache<T>() where T : SelectableModel
    {
        var snapshot = GetSelectedModelsSnapshot();
        var result = new List<T>();

        foreach (var model in snapshot)
        {
            if (model is T typed)
                result.Add(typed);
        }

        return result;
    }

    public void Deselect(IDiagramModel model)
        => Deselect([model]);

    public void Deselect(IEnumerable<IDiagramModel> models)
    {
        var nodesToUpdate = new HashSet<BlockNode>();

        foreach (var model in models)
        {
            if (model is ChildContainerNode container)
            {
                diagramService.Diagram.UnselectModel(container);
            }
            else if (model is FunctionBlockNode fb)
            {
                diagramService.Diagram.UnselectModel(fb);
            }
            else if (model is BlockNodeConnector connector)
            {
                nodesToUpdate.Add(connector.Node);

                connector.SetSelection(false);
                _selectedConnectors.Remove(connector);
                InvokeConnectorSelectionChanged();
            }
            else if (model is BlockNodeLink link)
            {
                diagramService.Diagram.UnselectModel(link);
            }
            else if (model is LabelNode label)
            {
                diagramService.Diagram.UnselectModel(label);
            }
            else if (model is ConnectorMarker marker)
            {
                nodesToUpdate.Add(marker.Connector.Node);

                marker.Selected = false;
                _selectedConnectorMarker.Remove(marker);
                if (marker.Links.Count > 0)
                    _selectedMarkersWithLinksCount--;
            }
            else
            {
                throw new ArgumentException($"Unknown type of {nameof(IDiagramModel)}.");
            }
        }

        RefreshNodes(nodesToUpdate);
    }

    public void DeselectAll()
    {
        diagramService.Diagram.UnselectAll();
        DeselectAll(SelectionMode.FunctionBlockConnector | SelectionMode.ConnectorMarker);
    }

    public void DeselectAll(SelectionMode mode)
    {
        if (mode.HasFlag(SelectionMode.Container))
        {
            foreach (var container in SelectedContainers)
                diagramService.Diagram.UnselectModel(container);
        }
        if (mode.HasFlag(SelectionMode.FunctionBlock))
        {
            foreach (var fb in SelectedFBs)
                diagramService.Diagram.UnselectModel(fb);
        }
        if (mode.HasFlag(SelectionMode.FunctionBlockConnector))
        {
            var nodesToUpdate = new HashSet<BlockNode>();

            foreach (var connector in _selectedConnectors)
            {
                nodesToUpdate.Add(connector.Node);

                connector.SetSelection(false);
            }

            _selectedConnectors.Clear();
            RefreshNodes(nodesToUpdate);
            InvokeConnectorSelectionChanged();
        }
        if (mode.HasFlag(SelectionMode.FunctionBlockLink))
        {
            foreach (var link in SelectedLinks)
                diagramService.Diagram.UnselectModel(link);
        }
        if (mode.HasFlag(SelectionMode.Label))
        {
            foreach (var label in SelectedLabels)
                diagramService.Diagram.UnselectModel(label);
        }
        if (mode.HasFlag(SelectionMode.ConnectorMarker))
        {
            var nodesToUpdate = new HashSet<BlockNode>();

            foreach (var publishedConnectorMarker in _selectedConnectorMarker)
            {
                nodesToUpdate.Add(publishedConnectorMarker.Connector.Node);

                publishedConnectorMarker.Selected = false;
            }

            _selectedConnectorMarker.Clear();
            _selectedMarkersWithLinksCount = 0;
            RefreshNodes(nodesToUpdate);
        }
    }

    public void Dispose()
    {
        diagramService.Diagram.SelectionChanged -= OnDiagramSelectionChanged;
        diagramService.Diagram.Nodes.Removed -= OnDiagramNodesRemoved;
    }

    private void DoMarkerSelection(ConnectorMarker marker, HashSet<BlockNode> nodesToUpdate)
    {
        if (marker.Links.Count == 0 &&
            marker.Connector.Connector.Published &&
            _selectedMarkersWithLinksCount == 0)
        {
            SetMarkerSelection(true, marker, nodesToUpdate);
        }

        if (marker.Links.Count > 0)
        {
            foreach (var selectedMarker in _selectedConnectorMarker.ToArray())
            {
                if (selectedMarker.Links.Count > 0)
                    SetMarkerSelection(false, selectedMarker, nodesToUpdate);
            }

            SetMarkerSelection(true, marker, nodesToUpdate);
        }
    }

    private SelectableModel[] GetSelectedModelsSnapshot()
        => _selectedModelsCache ??= [.. diagramService.Diagram?.GetSelectedModels() ?? []];

    private void InvalidateSelectionCache()
    {
        _selectedModelsCache = null;
        _selectedContainersCache = null;
        _selectedFBsCache = null;
        _selectedLabelsCache = null;
        _selectedLinksCache = null;
        _selectedBlockNodesCache = null;
        _selectedModelsFullCache = null;
    }

    public void InvertSelection(SelectionMode mode)
    {
        if (mode.HasFlag(SelectionMode.FunctionBlockConnector))
            throw new NotImplementedException($"{nameof(InvertSelection)} is not implemented for {nameof(SelectionMode.FunctionBlockConnector)}.");
        if (mode.HasFlag(SelectionMode.FunctionBlockLink))
            throw new NotImplementedException($"{nameof(InvertSelection)} is not implemented for {nameof(SelectionMode.FunctionBlockLink)}.");
        if (mode.HasFlag(SelectionMode.ConnectorMarker))
            throw new NotImplementedException($"{nameof(InvertSelection)} is not implemented for {nameof(SelectionMode.ConnectorMarker)}.");

        var invertContainers = mode.HasFlag(SelectionMode.Container);
        var invertFBs = mode.HasFlag(SelectionMode.FunctionBlock);
        var invertLabels = mode.HasFlag(SelectionMode.Label);

        var selectedContainers = invertContainers ? SelectedContainers : null;
        var selectedFBs = invertFBs ? SelectedFBs : null;
        var selectedLabels = invertLabels ? SelectedLabels : null;

        if (invertContainers) DeselectAll(SelectionMode.Container);
        if (invertFBs) DeselectAll(SelectionMode.FunctionBlock);
        if (invertLabels) DeselectAll(SelectionMode.Label);

        foreach (var node in diagramService.Diagram.Nodes)
        {
            if (node is ChildContainerNode container && invertContainers && container.Visible)
            {
                if (!selectedContainers!.Contains(container))
                    diagramService.Diagram.SelectModel(container, false);
            }
            else if (node is FunctionBlockNode fb && invertFBs && fb.Visible)
            {
                if (!selectedFBs!.Contains(fb))
                    diagramService.Diagram.SelectModel(fb, false);
            }
            else if (node is LabelNode label && invertLabels && label.Visible)
            {
                if (!selectedLabels!.Contains(label))
                    diagramService.Diagram.SelectModel(label, false);
            }
        }
    }

    private void InvokeConnectorSelectionChanged()
        => ConnectorSelectionChanged?.Invoke(_selectedConnectors);

    private Task InvokeDiagramSelectionChanged(SelectableModel selectableModel)
        => DiagramSelectionChanged.InvokeEventAsync(selectableModel, logger, nameof(DiagramSelectionChanged));

    public bool IsSelected(IDiagramModel model)
        => model switch
        {
            BlockNodeConnector connector => SelectedConnectors.Contains(connector),
            ChildContainerNode container => SelectedContainers.Contains(container),
            FunctionBlockNode fb => SelectedFBs.Contains(fb),
            LabelNode label => SelectedLabels.Contains(label),
            BlockNodeLink link => SelectedLinks.Contains(link),
            ConnectorMarker marker => SelectedConnectorMarker.Contains(marker),
            _ => throw new ArgumentException($"Unknown type of {nameof(IDiagramModel)}."),
        };

    // We assume that any node removal is also a `SelectionChanged` event,
    // i.e. only selected nodes get deleted
    // In the future this might not hold true, e.g. when we introduce a wizard
    // that removes or exchanges blocks, but for now it should be good enough
    private void OnDiagramNodesRemoved(NodeModel nodeModel)
    {
        InvalidateSelectionCache();
        AsyncGuard.SafeFireAndForget(() => InvokeDiagramSelectionChanged(nodeModel), logger);
    }

    private void OnDiagramSelectionChanged(SelectableModel selectableModel)
    {
        InvalidateSelectionCache();
        AsyncGuard.SafeFireAndForget(() => InvokeDiagramSelectionChanged(selectableModel), logger);
    }

    private void RefreshNodes(IEnumerable<BlockNode> nodesToUpdate)
    {
        diagramService.DiagramState.SuppressEvents = true;

        foreach (var node in nodesToUpdate)
            node.Refresh();

        diagramService.DiagramState.SuppressEvents = false;
    }

    public void Select(IDiagramModel model)
        => Select([model]);

    public void Select(IEnumerable<IDiagramModel> models)
    {
        var nodesToUpdate = new HashSet<BlockNode>();

        foreach (var model in models)
        {
            if (model is ChildContainerNode container)
            {
                diagramService.Diagram.SelectModel(container, false);
            }
            else if (model is FunctionBlockNode fb)
            {
                diagramService.Diagram.SelectModel(fb, false);
            }
            else if (model is BlockNodeConnector connector)
            {
                connector.SetSelection(true);
                _selectedConnectors.Add(connector);
                InvokeConnectorSelectionChanged();

                nodesToUpdate.Add(connector.Node);
            }
            else if (model is BlockNodeLink link)
            {
                diagramService.Diagram.SelectModel(link, false);
            }
            else if (model is LabelNode label)
            {
                diagramService.Diagram.SelectModel(label, false);
            }
            else if (model is ConnectorMarker marker)
            {
                DoMarkerSelection(marker, nodesToUpdate);
            }
            else
            {
                throw new ArgumentException($"Unknown type of {nameof(IDiagramModel)}.");
            }
        }

        RefreshNodes(nodesToUpdate);
    }

    public void SelectAll(SelectionMode mode)
    {
        if (mode.HasFlag(SelectionMode.FunctionBlockConnector))
            throw new NotImplementedException($"{nameof(SelectAll)} is not implemented for {nameof(SelectionMode.FunctionBlockConnector)}.");
        if (mode.HasFlag(SelectionMode.FunctionBlockLink))
            throw new NotImplementedException($"{nameof(SelectAll)} is not implemented for {nameof(SelectionMode.FunctionBlockLink)}.");
        if (mode.HasFlag(SelectionMode.ConnectorMarker))
            throw new NotImplementedException($"{nameof(SelectAll)} is not implemented for {nameof(SelectionMode.ConnectorMarker)}.");

        var selectContainers = mode.HasFlag(SelectionMode.Container);
        var selectFBs = mode.HasFlag(SelectionMode.FunctionBlock);
        var selectLabels = mode.HasFlag(SelectionMode.Label);

        foreach (var node in diagramService.Diagram.Nodes)
        {
            if (node is ChildContainerNode container && selectContainers && container.Visible)
                diagramService.Diagram.SelectModel(container, false);
            else if (node is FunctionBlockNode fb && selectFBs && fb.Visible)
                diagramService.Diagram.SelectModel(fb, false);
            else if (node is LabelNode label && selectLabels && label.Visible)
                diagramService.Diagram.SelectModel(label, false);
        }
    }

    public async Task SelectInRectangle(SelectionMode mode, Rectangle rect)
    {
        if (mode.HasFlag(SelectionMode.FunctionBlockConnector))
            throw new NotImplementedException($"{nameof(SelectInRectangle)} is not implemented for {nameof(SelectionMode.FunctionBlockConnector)}.");

        var selectContainers = mode.HasFlag(SelectionMode.Container);
        var selectFBs = mode.HasFlag(SelectionMode.FunctionBlock);
        var selectLabels = mode.HasFlag(SelectionMode.Label);

        if (selectContainers || selectFBs || selectLabels)
        {
            foreach (var node in diagramService.Diagram.Nodes)
            {
                switch (node)
                {
                    case ChildContainerNode container when selectContainers && container.Visible
                        && (container.GetBounds()?.Intersects(rect) ?? false):
                        diagramService.Diagram.SelectModel(container, false);
                        break;

                    case FunctionBlockNode fb when selectFBs && fb.Visible
                        && (fb.GetBounds()?.Intersects(rect) ?? false):
                        diagramService.Diagram.SelectModel(fb, false);
                        break;

                    case LabelNode label when selectLabels && label.Visible && !label.Locked
                        && (label.GetBounds()?.Intersects(rect) ?? false):
                        diagramService.Diagram.SelectModel(label, false);
                        break;
                }
            }
        }

        if (mode.HasFlag(SelectionMode.FunctionBlockLink))
            await SelectInRectangleFunctionBlockLink(rect);

        if (mode.HasFlag(SelectionMode.ConnectorMarker))
            SelectInRectangleConnectorMarker(rect);
    }

    private void SelectInRectangleConnectorMarker(Rectangle rect)
    {
        if (SelectedBlockNodes.Count > 0 || SelectedLabels.Count > 0 || SelectedLinks.Count > 0)
        {
            _selectedConnectorMarker.Clear();
            _selectedMarkersWithLinksCount = 0;
            return;
        }

        var nodesToUpdate = new HashSet<BlockNode>();

        foreach (var diagramNode in diagramService.Diagram.Nodes)
        {
            if (diagramNode is not BlockNode node)
                continue;

            if (!node.GetAlignmentRect().Intersects(rect))
                continue;

            foreach (var connectorRow in node.Connectors)
            {
                for (var i = 0; i < connectorRow.Length; i++)
                {
                    var connector = connectorRow[i];
                    if (connector is null)
                        continue;

                    if (connector.GetPublishedMarkRect().Intersects(rect))
                        DoMarkerSelection(connector.PublishedConnectorMarker, nodesToUpdate);

                    if (connector.DataPortConnectorMarker.Links.Count > 0 &&
                        connector.GetDataPortMarkRect().Intersects(rect))
                    {
                        DoMarkerSelection(connector.DataPortConnectorMarker, nodesToUpdate);
                    }
                }
            }
        }

        RefreshNodes(nodesToUpdate);
    }

    private async Task SelectInRectangleFunctionBlockLink(Rectangle rect)
    {
        var (success, linkIds) = await jsRuntime.TryInvoke<IEnumerable<string>>(
            logger,
            "ViciOne.Diagram.Link.getIdsInRectangle",
            new
            {
                Left = (int)rect.Left,
                Top = (int)rect.Top,
                Right = (int)rect.Right,
                Bottom = (int)rect.Bottom
            });

        if (!success || linkIds is null)
            return;

        var links = diagramService.Diagram.Links;
        var linksById = new Dictionary<string, BaseLinkModel>(links.Count);
        foreach (var link in links)
            linksById[link.Id] = link;

        foreach (var id in linkIds)
        {
            if (linksById.TryGetValue(id, out var link))
                diagramService.Diagram.SelectModel(link, false);
        }
    }

    private void SetMarkerSelection(bool isSelected, ConnectorMarker marker, HashSet<BlockNode> nodesToUpdate)
    {
        marker.Selected = isSelected;

        if (isSelected)
        {
            _selectedConnectorMarker.Add(marker);
            if (marker.Links.Count > 0)
                _selectedMarkersWithLinksCount++;
        }
        else
        {
            _selectedConnectorMarker.Remove(marker);
            if (marker.Links.Count > 0)
                _selectedMarkersWithLinksCount--;
        }

        nodesToUpdate.Add(marker.Connector.Node);
    }

    public void SetSelection(IDiagramModel model)
    {
        DeselectAll();
        Select(model);
    }

    public void SetSelection(IEnumerable<IDiagramModel> models)
    {
        DeselectAll();
        Select(models);
    }
}
