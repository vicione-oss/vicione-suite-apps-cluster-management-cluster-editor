using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;

namespace ViciOne.Ui.ClusterEditor.Sections.SearchAndTools.Components;

public sealed partial class SearchAndToolsSectionContent : ComponentBase, IDisposable
{
    private const string FilterAttachedToken = "::attached::";
    private const string FilterSelectedToken = "::selected::";

    private static readonly string s_filterSolidIconCssClass =
        MonochromeIconName.FilterSolid.GetCssClasses(MonochromeIconSize.SmallPlus2).ToSpaceSeparated();

    private bool _alignEnabled;
    private bool _alignExpanded = true;
    private bool _arrangeEnabled;
    private bool _arrangeExpanded = true;
    private string _filterText = string.Empty;
    private SearchBlocksService _searchService = null!;
    private SearchBlocksService _searchServiceFilter = null!;
    private string _searchText = string.Empty;
    private bool _selectClearEnabled;
    private bool _selectEnabled;
    private bool _selectExpanded = true;
    private bool _traceEnabled;
    private bool _traceExpanded = true;

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private LabelOrderService LabelOrderService { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;
    [Inject] private TraceService TraceService { get; set; } = default!;

    private void ApplyFilter(string value)
    {
        _filterText = value;

        ClearSearch();
        ResetFilteredDiagramModels();

        switch (_filterText)
        {
            case FilterAttachedToken:
                FilterAttachedNodes();
                break;
            case FilterSelectedToken:
                FilterSelectedNodes();
                break;
            case "" or null:
                break;
            default:
                FilterNodesByText(_filterText);
                break;
        }
    }

    private void ClearFilter()
    {
        ApplyFilter(string.Empty);

        InvokeAsync(StateHasChanged);
    }

    private void ClearSearch()
    {
        _searchService.Reset();

        _searchText = string.Empty;
    }

    public void Dispose()
    {
        DiagramEventService.ContainerLoaded -= OnContainerLoaded;
        DiagramEventService.FilterAttachedRequested -= OnFilterAttachedRequested;
        DiagramEventService.FilterClearRequested -= OnFilterClearRequested;
        DiagramEventService.FilterSelectedRequested -= OnFilterSelectedRequested;

        DiagramService.Diagram.Nodes.Added -= OnDiagramNodeAmountChanged;
        DiagramService.Diagram.Nodes.Removed -= OnDiagramNodeAmountChanged;

        SelectionManager.DiagramSelectionChanged -= OnDiagramSelectionChanged;

        GC.SuppressFinalize(this);
    }

    private void FilterAttachedNodes()
    {
        var selectedBlockNodes = SelectionManager.SelectedBlockNodes;
        if (selectedBlockNodes.Count == 0)
            return;

        var relevantNodes = selectedBlockNodes.Union(Datastore.GetConnectedNodes(selectedBlockNodes));
        var connectedLinks = Datastore.GetConnectedNodeLinks(relevantNodes);
        SelectionManager.DeselectAll(SelectionMode.FunctionBlockLink);

        RenderNodesFilter(relevantNodes, connectedLinks);
    }

    private void FilterNodesByText(string filterText)
    {
        _searchServiceFilter.Search(filterText);
        var filteredNodes = _searchServiceFilter.GetAll();
        if (filteredNodes is null)
            return;

        var connectedLinks = Datastore.GetConnectedNodeLinks(filteredNodes);
        SelectionManager.DeselectAll(SelectionMode.FunctionBlockLink);

        RenderNodesFilter(filteredNodes, connectedLinks);
    }

    private void FilterSelectedNodes()
    {
        var selectedBlockNodes = SelectionManager.SelectedBlockNodes;
        if (selectedBlockNodes.Count == 0)
            return;

        var connectedLinks = Datastore.GetConnectedNodeLinks(selectedBlockNodes);
        SelectionManager.DeselectAll(SelectionMode.FunctionBlockLink);

        RenderNodesFilter(selectedBlockNodes, connectedLinks);
    }

    private void OnAlignClick(Alignment align)
        => align.ApplyToSelection(SelectionManager);

    private Task OnContainerLoaded(Container _)
    {
        ClearFilter();
        return Task.CompletedTask;
    }

    private void OnDiagramNodeAmountChanged(NodeModel _)
    {
        foreach (var node in DiagramService.Diagram.Nodes)
        {
            if (node is ChildContainerNode or FunctionBlockNode)
            {
                _selectEnabled = true;
                break;
            }
        }

        InvokeAsync(StateHasChanged);
    }

    private async Task OnDiagramSelectionChanged(SelectableModel _)
    {
        _alignEnabled = (SelectionManager.SelectedBlockNodes.Count + SelectionManager.SelectedLabels.Count) >= 2;
        _arrangeEnabled = SelectionManager.SelectedLabels.Count > 0;
        _traceEnabled = SelectionManager.SelectedBlockNodes.Count > 0;
        _selectClearEnabled = SelectionManager.SelectedBlockNodes.Count > 0;

        await InvokeAsync(StateHasChanged);
    }

    private void OnFilterAttachedClick()
        => ApplyFilter(FilterAttachedToken);

    private void OnFilterAttachedRequested()
        => OnFilterAttachedClick();

    private void OnFilterClearRequested()
        => ClearFilter();

    private void OnFilterSelectedClick()
        => ApplyFilter(FilterSelectedToken);

    private void OnFilterSelectedRequested()
        => OnFilterSelectedClick();

    protected override void OnInitialized()
    {
        _searchService = new(DiagramService);
        _searchServiceFilter = new(DiagramService);

        DiagramEventService.ContainerLoaded += OnContainerLoaded;
        DiagramEventService.FilterAttachedRequested += OnFilterAttachedRequested;
        DiagramEventService.FilterClearRequested += OnFilterClearRequested;
        DiagramEventService.FilterSelectedRequested += OnFilterSelectedRequested;

        DiagramService.Diagram.Nodes.Added += OnDiagramNodeAmountChanged;
        DiagramService.Diagram.Nodes.Removed += OnDiagramNodeAmountChanged;

        SelectionManager.DiagramSelectionChanged += OnDiagramSelectionChanged;
    }

    private void OnSearchNextClick()
    {
        if (!_searchService.HasResult)
            return;

        var next = _searchService.GetNext();
        if (next is null)
            return;

        SelectionManager.SetSelection(next);

        if (!DiagramService.Diagram.IsNodeInViewport(next))
            DiagramService.Diagram.PanToNode(next);
    }

    private void OnSearchPreviousClick()
    {
        if (!_searchService.HasResult)
            return;

        var prev = _searchService.GetPrevious();
        if (prev is null)
            return;

        SelectionManager.SetSelection(prev);

        if (!DiagramService.Diagram.IsNodeInViewport(prev))
            DiagramService.Diagram.PanToNode(prev);
    }

    private void OnSearchTextChanging(string value)
    {
        _searchText = value;

        _searchService.Search(_searchText);

        var all = _searchService.GetAll();
        if (all is null)
        {
            SelectionManager.DeselectAll();
            return;
        }

        SelectionManager.SetSelection(all);
    }

    private void OnSelectAllClick()
        => SelectionManager.SelectAll(SelectionMode.Container | SelectionMode.FunctionBlock);

    private void OnSelectClearClick()
        => SelectionManager.DeselectAll();

    private void OnSelectInvertClick()
        => SelectionManager.InvertSelection(SelectionMode.Container | SelectionMode.FunctionBlock);

    private void OnToBackClick()
        => LabelOrderService.SendToBack(SelectionManager.SelectedLabels);

    private void OnToFrontClick()
        => LabelOrderService.BringToFront(SelectionManager.SelectedLabels);

    private void OnTraceClearClick()
        => TraceService.ClearTrace();

    private void OnTracePredecessorClick()
        => TraceService.TraceConnections(ConnectionDirection.Predecessor, 1);

    private void OnTracePredecessorsClick()
        => TraceService.TraceConnections(ConnectionDirection.Predecessor, null);

    private void OnTraceSuccessorClick()
        => TraceService.TraceConnections(ConnectionDirection.Successor, 1);

    private void OnTraceSuccessorsClick()
        => TraceService.TraceConnections(ConnectionDirection.Successor, null);

    private void RenderNodesFilter(IEnumerable<BlockNode> filteredNodes, IEnumerable<BlockNodeLink> connectedLinks)
        => DiagramService.Diagram.Batch(() =>
        {
            var filteredSet = filteredNodes as ISet<BlockNode> ?? new HashSet<BlockNode>(filteredNodes);
            var linksSet = connectedLinks as ISet<BlockNodeLink> ?? new HashSet<BlockNodeLink>(connectedLinks);

            foreach (var node in DiagramService.Diagram.Nodes)
            {
                if (node is IDiagramModel dmNode)
                {
                    dmNode.Visible = filteredSet.Contains((BlockNode)dmNode);
                    if (!dmNode.Visible)
                        SelectionManager.Deselect(dmNode);

                    node.Refresh();
                }
            }

            foreach (var link in DiagramService.Diagram.Links)
            {
                if (link is BlockNodeLink blockNodeLink)
                {
                    blockNodeLink.Visible = linksSet.Contains(blockNodeLink);
                    blockNodeLink.Refresh();
                }
            }
        });

    private void ResetFilteredDiagramModels()
        => DiagramService.Diagram.Batch(() =>
        {
            foreach (var node in DiagramService.Diagram.Nodes)
            {
                if (node is IDiagramModel dmNode)
                {
                    if (!dmNode.Visible)
                    {
                        dmNode.Visible = true;
                        node.Refresh();
                    }
                }
            }

            foreach (var link in DiagramService.Diagram.Links)
            {
                if (link is BlockNodeLink blockNodeLink && !blockNodeLink.Visible)
                {
                    blockNodeLink.Visible = true;
                    blockNodeLink.Refresh();
                }
            }
        });
}
