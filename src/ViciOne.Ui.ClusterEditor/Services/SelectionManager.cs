using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.JSInterop;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed class SelectionManager(DiagramService diagramService, IJSRuntime jsRuntime) : IDisposable
{
    private readonly List<ConnectorMarker> _selectedConnectorMarker = [];
    private readonly List<BlockNodeConnector> _selectedConnectors = [];

    public IEnumerable<BlockNode> SelectedBlockNodes
        => Enumerable.Empty<BlockNode>()
            .Concat(SelectedContainers)
            .Concat(SelectedFBs);
    public IEnumerable<ConnectorMarker> SelectedConnectorMarker
        => _selectedConnectorMarker;
    public IEnumerable<BlockNodeConnector> SelectedConnectors
        => _selectedConnectors;
    public IEnumerable<ChildContainerNode> SelectedContainers
        => [.. (diagramService.Diagram?.GetSelectedModels() ?? []).OfType<ChildContainerNode>()];
    public IEnumerable<FunctionBlockNode> SelectedFBs
        => [.. (diagramService.Diagram?.GetSelectedModels() ?? []).OfType<FunctionBlockNode>()];
    public IEnumerable<LabelNode> SelectedLabels
        => [.. (diagramService.Diagram?.GetSelectedModels() ?? []).OfType<LabelNode>()];
    public IEnumerable<BlockNodeLink> SelectedLinks
        => [.. (diagramService.Diagram?.GetSelectedModels() ?? []).OfType<BlockNodeLink>()];
    public IEnumerable<IDiagramModel> SelectedModels
        => Enumerable.Empty<IDiagramModel>()
            .Concat(SelectedConnectors)
            .Concat(SelectedContainers)
            .Concat(SelectedFBs)
            .Concat(SelectedLabels)
            .Concat(SelectedLinks)
            .Concat(SelectedConnectorMarker);

    public event Action<IEnumerable<BlockNodeConnector>>? ConnectorSelectionChanged;
    public event Action<SelectableModel>? DiagramSelectionChanged;

    public void AttachDiagramEvents()
    {
        diagramService.Diagram.SelectionChanged += OnDiagramSelectionChanged;
        diagramService.Diagram.Nodes.Removed += OnDiagramNodesRemoved;
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
            var nodesToUpdate = new List<BlockNode>();

            foreach (var connector in _selectedConnectors)
            {
                if (!nodesToUpdate.Contains(connector.Node))
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
            var nodesToUpdate = new List<BlockNode>();

            foreach (var publishedConnectorMarker in _selectedConnectorMarker)
            {
                if (!nodesToUpdate.Contains(publishedConnectorMarker.Connector.Node))
                    nodesToUpdate.Add(publishedConnectorMarker.Connector.Node);

                publishedConnectorMarker.Selected = false;
            }

            _selectedConnectorMarker.Clear();
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
            _selectedConnectorMarker.All(m => m.Links.Count == 0))
        {
            SetMarkerSelection(true, marker, nodesToUpdate);
        }

        if (marker.Links.Count > 0)
        {
            var invalidMarkers = _selectedConnectorMarker.Where(m => m.Links.Count == 0).ToArray();
            foreach (var invalidMarker in invalidMarkers)
                SetMarkerSelection(false, invalidMarker, nodesToUpdate);

            SetMarkerSelection(true, marker, nodesToUpdate);
        }
    }

    public void InvertSelection(SelectionMode mode)
    {
        if (mode.HasFlag(SelectionMode.Container))
        {
            var selectedContainers = SelectedContainers;

            DeselectAll(SelectionMode.Container);
            foreach (var container in diagramService.Diagram.Nodes.OfType<ChildContainerNode>().Where(n => n.Visible))
            {
                if (!selectedContainers.Contains(container))
                    diagramService.Diagram.SelectModel(container, false);
            }
        }
        if (mode.HasFlag(SelectionMode.FunctionBlock))
        {
            var selectedFBs = SelectedFBs;

            DeselectAll(SelectionMode.FunctionBlock);
            foreach (var fb in diagramService.Diagram.Nodes.OfType<FunctionBlockNode>().Where(n => n.Visible))
            {
                if (!selectedFBs.Contains(fb))
                    diagramService.Diagram.SelectModel(fb, false);
            }
        }
        if (mode.HasFlag(SelectionMode.FunctionBlockConnector))
            throw new NotImplementedException($"{nameof(InvertSelection)} is not implemented for {nameof(SelectionMode.FunctionBlockConnector)}.");
        if (mode.HasFlag(SelectionMode.FunctionBlockLink))
            throw new NotImplementedException($"{nameof(InvertSelection)} is not implemented for {nameof(SelectionMode.FunctionBlockLink)}.");
        if (mode.HasFlag(SelectionMode.Label))
        {
            var selectedLabels = SelectedLabels;

            DeselectAll(SelectionMode.Label);
            foreach (var label in diagramService.Diagram.Nodes.OfType<LabelNode>().Where(n => n.Visible))
            {
                if (!selectedLabels.Contains(label))
                    diagramService.Diagram.SelectModel(label, false);
            }
        }
        if (mode.HasFlag(SelectionMode.ConnectorMarker))
            throw new NotImplementedException($"{nameof(InvertSelection)} is not implemented for {nameof(SelectionMode.ConnectorMarker)}.");
    }

    private void InvokeConnectorSelectionChanged()
        => ConnectorSelectionChanged?.Invoke(_selectedConnectors);

    private void InvokeDiagramSelectionChanged(SelectableModel selectableModel)
        => DiagramSelectionChanged?.Invoke(selectableModel);

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
        => InvokeDiagramSelectionChanged(nodeModel);

    private void OnDiagramSelectionChanged(SelectableModel selectableModel)
        => InvokeDiagramSelectionChanged(selectableModel);

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
        if (mode.HasFlag(SelectionMode.Container))
        {
            foreach (var node in diagramService.Diagram.Nodes.OfType<ChildContainerNode>().Where(n => n.Visible))
                diagramService.Diagram.SelectModel(node, false);
        }
        if (mode.HasFlag(SelectionMode.FunctionBlock))
        {
            foreach (var node in diagramService.Diagram.Nodes.OfType<FunctionBlockNode>().Where(n => n.Visible))
                diagramService.Diagram.SelectModel(node, false);
        }
        if (mode.HasFlag(SelectionMode.FunctionBlockConnector))
            throw new NotImplementedException($"{nameof(SelectAll)} is not implemented for {nameof(SelectionMode.FunctionBlockConnector)}.");
        if (mode.HasFlag(SelectionMode.FunctionBlockLink))
            throw new NotImplementedException($"{nameof(SelectAll)} is not implemented for {nameof(SelectionMode.FunctionBlockLink)}.");
        if (mode.HasFlag(SelectionMode.Label))
        {
            foreach (var node in diagramService.Diagram.Nodes.OfType<LabelNode>().Where(n => n.Visible))
                diagramService.Diagram.SelectModel(node, false);
        }
        if (mode.HasFlag(SelectionMode.ConnectorMarker))
            throw new NotImplementedException($"{nameof(SelectAll)} is not implemented for {nameof(SelectionMode.ConnectorMarker)}.");
    }

    public async Task SelectInRectangleAsync(SelectionMode mode, Rectangle rect)
    {
        if (mode.HasFlag(SelectionMode.Container))
        {
            SelectInRectangleContainer(rect);
        }
        if (mode.HasFlag(SelectionMode.FunctionBlock))
        {
            SelectInRectangleFunctionBlock(rect);
        }
        if (mode.HasFlag(SelectionMode.FunctionBlockConnector))
            throw new NotImplementedException($"{nameof(SelectInRectangleAsync)} is not implemented for {nameof(SelectionMode.FunctionBlockConnector)}.");
        if (mode.HasFlag(SelectionMode.FunctionBlockLink))
        {
            await SelectInRectangleFunctionBlockLinkAsync(rect);
        }
        if (mode.HasFlag(SelectionMode.Label))
        {
            SelectInRectangleLabel(rect);
        }
        if (mode.HasFlag(SelectionMode.ConnectorMarker))
        {
            SelectInRectangleConnectorMarker(rect);
        }
    }

    private void SelectInRectangleConnectorMarker(Rectangle rect)
    {
        if (SelectedBlockNodes.Any() || SelectedLabels.Any() || SelectedLinks.Any())
        {
            _selectedConnectorMarker.Clear();
            return;
        }

        var blockNodes = Enumerable.Empty<BlockNode>()
            .Concat(diagramService.Diagram.Nodes.OfType<ChildContainerNode>())
            .Concat(diagramService.Diagram.Nodes.OfType<FunctionBlockNode>());

        var nodesToUpdate = new HashSet<BlockNode>();

        foreach (var node in blockNodes)
        {
            if (!node.GetAlignmentRect().Intersects(rect))
                continue;

            foreach (var connectorRow in node.Connectors)
            {
                for (var i = 0; i < connectorRow.Length; i++)
                {
                    if (connectorRow[i] is null)
                        continue;

                    if (connectorRow[i]!.GetPublishedMarkRect().Intersects(rect))
                        DoMarkerSelection(connectorRow[i]!.PublishedConnectorMarker, nodesToUpdate);

                    if (connectorRow[i]!.DataPortConnectorMarker.Links.Count > 0 &&
                        connectorRow[i]!.GetDataPortMarkRect().Intersects(rect))
                    {
                        DoMarkerSelection(connectorRow[i]!.DataPortConnectorMarker, nodesToUpdate);
                    }
                }
            }
        }

        RefreshNodes(nodesToUpdate);
    }

    private void SelectInRectangleContainer(Rectangle rect)
    {
        foreach (var node in diagramService.Diagram.Nodes.OfType<ChildContainerNode>().Where(n => n.Visible))
        {
            if (node.GetBounds()?.Intersects(rect) ?? false)
                diagramService.Diagram.SelectModel(node, false);
        }
    }

    private void SelectInRectangleFunctionBlock(Rectangle rect)
    {
        foreach (var node in diagramService.Diagram.Nodes.OfType<FunctionBlockNode>().Where(n => n.Visible))
        {
            if (node.GetBounds()?.Intersects(rect) ?? false)
                diagramService.Diagram.SelectModel(node, false);
        }
    }

    private async Task SelectInRectangleFunctionBlockLinkAsync(Rectangle rect)
    {
        var linkIds = await jsRuntime.InvokeAsync<IEnumerable<string>>("ViciOne.Diagram.Link.getIdsInRectangle", new
        {
            Left = (int)rect.Left,
            Top = (int)rect.Top,
            Right = (int)rect.Right,
            Bottom = (int)rect.Bottom
        });

        foreach (var id in linkIds)
            diagramService.Diagram.SelectModel(diagramService.Diagram.Links.First(l => l.Id == id), false);
    }

    private void SelectInRectangleLabel(Rectangle rect)
    {
        var nodes = diagramService.Diagram.Nodes.OfType<LabelNode>().Where(n => n.Visible && !n.Locked);
        foreach (var node in nodes)
        {
            if (node.GetBounds()?.Intersects(rect) ?? false)
                diagramService.Diagram.SelectModel(node, false);
        }
    }

    private void SetMarkerSelection(bool isSelected, ConnectorMarker marker, HashSet<BlockNode> nodesToUpdate)
    {
        marker.Selected = isSelected;

        if (isSelected)
            _selectedConnectorMarker.Add(marker);
        else
            _selectedConnectorMarker.Remove(marker);

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
