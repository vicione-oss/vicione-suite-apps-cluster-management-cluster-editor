using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.Blazor.Components.Tables.SimpleTable.Components;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Models;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ColorableIcons;
using ViciOne.Ui.MonochromeIcons.Core.Enums;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Components;

public sealed partial class PublishedConnectorsSectionContent : ComponentBase, IDisposable
{
    // A lambda written inline in markup is rebuilt every render, and the items provider would recompile its
    // sort selector with it.
    private static readonly Expression<Func<DataGridConnectorWrapper, object>> s_designNameSortExpression
        = wrapper => wrapper.DesignName;

    private static readonly Expression<Func<DataGridConnectorWrapper, object>> s_functionBlockNameSortExpression
        = wrapper => wrapper.FunctionBlockName;

    private readonly object _columnChooserToggleId = new();

    private PublishedConnectorDragGhost? _dragGhost;
    private readonly List<FilterButton> _filterButtons = [];

    // Never reset: the section is not torn down, so what it accumulates is the state the user left behind.
    private FilterState _filterState = FilterState.Empty;

    private readonly HashSet<string> _hiddenColumnIds = [];

    // Sorting on design first reproduces the clustering the view had while it grouped by design.
    private readonly SortingState _initialSorting = SortingState.Empty
        .WithColumnSorting(nameof(DataGridConnectorWrapper.DesignName), ascending: true)
        .WithColumnSorting(nameof(DataGridConnectorWrapper.FunctionBlockName), ascending: true);

    private FilterButton _inputFilterButton = new();
    private FilterButton _outputFilterButton = new();
    private EventCallback<RowContextMenuEventArgs<DataGridConnectorWrapper>> _rowContextMenu;
    private string? _searchText;
    private List<DataGridConnectorWrapper> _selectedItems = [];

    /// <summary>Whether <see cref="_initialSorting"/> still has to be handed to the table.</summary>
    /// <remarks>
    /// Hiding a column drops the sorting keyed to it, so the initial sorting has to be handed over again once the
    /// columns are back. The sorting the user chose before the table turned empty is lost with it.
    /// </remarks>
    private bool _sortingReapplyPending;
    private SimpleTable<DataGridConnectorWrapper>? _table;
    private IReadOnlyList<DataGridConnectorWrapper> _wrappers = [];

    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private IContextMenuRequest<PublishedConnectorsSectionContextMenuContext> ContextMenuRequest { get; set; } = default!;
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private ILogger<PublishedConnectorsSectionContent> Logger { get; set; } = default!;
    [Inject] private PublishedConnectorsService PublishedConnectorsService { get; set; } = default!;
    [Inject] private PublishedConnectorRowDragGhost RowDragGhost { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;
    [Inject] private PublishedConnectorVisibleSelection VisibleSelection { get; set; } = default!;

    private bool DataAvailable => _wrappers.Count > 0;

    public void Dispose()
    {
        RowDragGhost.MarkupDragGhost = null;

        PublishedConnectorsService.PublishedConnectorSelectionRequested -= OnPublishedConnectorSelectionRequested;
        PublishedConnectorsService.PublishedConnectorsChanged -= OnPublishedConnectorsChangedAsync;
    }

    private MarkupString GetIconMarkup(DataGridConnectorWrapper connectorWrapper)
        => (MarkupString)ColoredIconFactory.GetConnectorIcon(ConnectorService.GetConnectorColor(connectorWrapper.Connector), connectorWrapper.IsInput);

    private void InitFilterButtons()
    {
        _inputFilterButton = new FilterButton()
        {
            IsDisabled = DataAvailable,
            MonochromeIconName = MonochromeIconName.ConnectorInput,
            OnFilterClickedFn = OnFilterInputs,
            Title = Localization.PublishedConnectorsSection.FilterInputs,
        };
        _filterButtons.Add(_inputFilterButton);

        _outputFilterButton = new FilterButton()
        {
            IsDisabled = DataAvailable,
            MonochromeIconName = MonochromeIconName.ConnectorOutput,
            OnFilterClickedFn = OnFilterOutputs,
            Title = Localization.PublishedConnectorsSection.FilterOutputs,
        };
        _filterButtons.Add(_outputFilterButton);
    }

    private bool IsColumnVisible(string columnId)
        => DataAvailable && !_hiddenColumnIds.Contains(columnId);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            RowDragGhost.MarkupDragGhost = _dragGhost;

            StateHasChanged();
        }

        SetFilterButtonsState();

        if (_sortingReapplyPending && _table is not null)
        {
            _sortingReapplyPending = false;

            await _table.SetSortingStateAsync(_initialSorting);
        }
    }

    private void OnFilterInputs()
    {
        bool filter;

        if (_outputFilterButton.IsActive)
        {
            _outputFilterButton.IsActive = false;
            _inputFilterButton.IsActive = true;
            filter = true;
        }
        else
        {
            _inputFilterButton.IsActive = !_inputFilterButton.IsActive;
            filter = _inputFilterButton.IsActive;
        }

        SetDirectionFilter(filter ? ConnectorDirection.Input : null);
    }

    private void OnFilterOutputs()
    {
        bool filter;

        if (_inputFilterButton.IsActive)
        {
            _inputFilterButton.IsActive = false;
            _outputFilterButton.IsActive = true;
            filter = true;
        }
        else
        {
            _outputFilterButton.IsActive = !_outputFilterButton.IsActive;
            filter = _outputFilterButton.IsActive;
        }

        SetDirectionFilter(filter ? ConnectorDirection.Output : null);
    }

    protected override void OnInitialized()
    {
        _wrappers = PublishedConnectorsService.PublishedConnectorWrappers;

        PublishedConnectorsService.PublishedConnectorSelectionRequested += OnPublishedConnectorSelectionRequested;
        PublishedConnectorsService.PublishedConnectorsChanged += OnPublishedConnectorsChangedAsync;

        if (ContextMenuSettings.UseCustomMenu)
            _rowContextMenu = EventCallback.Factory.Create<RowContextMenuEventArgs<DataGridConnectorWrapper>>(this, RowContextMenu);

        InitFilterButtons();
    }

    private async void OnPublishedConnectorsChangedAsync()
    {
        try
        {
            await InvokeAsync(() =>
            {
                var hadRows = DataAvailable;
                _wrappers = PublishedConnectorsService.PublishedConnectorWrappers;

                // Only once the columns are shown again, after this render, can the sorting be handed over.
                _sortingReapplyPending |= !hadRows && DataAvailable;

                DropRemovedRowsFromSelection();
                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            RefreshPublishedConnectorsFailed(Logger, ex);
        }
    }

    /// <summary>
    /// Removes every row from the selection that <see cref="_wrappers"/> no longer holds.
    /// </summary>
    /// <remarks>
    /// The table keeps whatever selection it is bound to, so a row that left the list would otherwise stay
    /// selected with nothing on screen to show it.
    /// </remarks>
    private void DropRemovedRowsFromSelection()
    {
        if (_selectedItems.Count == 0)
            return;

        var currentRows = new HashSet<DataGridConnectorWrapper>(_wrappers);
        if (_selectedItems.TrueForAll(currentRows.Contains))
            return;

        _selectedItems = _selectedItems.FindAll(currentRows.Contains);
    }

    // The diagram asks for a row by connector, not by wrapper, because it holds no wrapper: the section owns
    // those.
    private void OnPublishedConnectorSelectionRequested(IConnector connector)
    {
        foreach (var wrapper in PublishedConnectorsService.PublishedConnectorWrappers)
        {
            if (wrapper.Connector.Id != connector.Id)
                continue;

            // Replacing the bound reference is what the table reads as a selection set from outside; it
            // re-raises VisibleSelectionChanged, so the footer count follows. An unpublished connector reaches
            // no row and the current selection is deliberately left alone.
            _selectedItems = [wrapper];

            // Raised from the diagram's call stack, so the render has to be requested explicitly.
            InvokeAsync(StateHasChanged);

            return;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Refresh PublishedConnectors failed.")]
    public static partial void RefreshPublishedConnectorsFailed(ILogger logger, Exception ex);

    // The table raises this without touching the selection, so the rows the menu acts on are settled here.
    private async Task RowContextMenu(RowContextMenuEventArgs<DataGridConnectorWrapper> context)
    {
        SelectionManager.DeselectAll();

        // The menu removes what it is handed, so it is handed the number the footer is showing.
        var menuSelection = VisibleSelection.Rows;

        if (!_selectedItems.Contains(context.Item))
        {
            _selectedItems = [context.Item];

            // The mirror cannot say so for another render, and the pressed row is on screen by definition.
            menuSelection = _selectedItems;

            StateHasChanged();
        }

        await ContextMenuRequest.SendAsync(new()
        {
            MouseEventArgs = context.MouseEventArgs,
            PublishedConnectorsWrappers = menuSelection
        });
    }

    private Task RowDoubleClick(DataGridConnectorWrapper connectorWrapper)
        => ConnectorService.ShowAndSelectPublishedConnectorMarker(connectorWrapper.Connector);

    private void SearchTextChanged(string? searchText)
    {
        _searchText = searchText;

        // The table reads a new instance as a command, so this may only run from an event: building one per
        // render would re-apply the filter on every render.
        _filterState = string.IsNullOrWhiteSpace(searchText)
            ? _filterState.WithoutGlobalFilter<PublishedConnectorSearchFilter>()
            : _filterState.WithGlobalFilter(new PublishedConnectorSearchFilter(searchText));
    }

    // A selection is dragged onto a connector as one payload, so it may only hold connectors of a single data
    // type — a mixed selection has no common valid target.
    private static bool SelectionAllowed(SelectionRequest<DataGridConnectorWrapper> request)
    {
        if (request.CurrentSelection.Count == 0)
            return true;

        return request.CurrentSelection[0].ConnectorType == request.Item.ConnectorType;
    }

    private void SetColumnVisible(string columnId, bool visible)
    {
        if (visible)
            _hiddenColumnIds.Remove(columnId);
        else
            _hiddenColumnIds.Add(columnId);
    }

    private void SetDirectionFilter(ConnectorDirection? requestedDirection)
    {
        _filterState = requestedDirection is { } direction
            ? _filterState.WithGlobalFilter(new PublishedConnectorDirectionFilter(direction))
            : _filterState.WithoutGlobalFilter<PublishedConnectorDirectionFilter>();

        // The direction buttons are FilterButton models owned by SearchAndFilterComponent, so their click marks
        // that component dirty and not this one. Without an explicit render the new FilterState is never handed
        // to the table, and the rows keep whichever filter was last applied.
        InvokeAsync(StateHasChanged);
    }

    private void SetFilterButtonsState()
    {
        var prevIFBS = _inputFilterButton.IsDisabled;
        var prevOFBS = _outputFilterButton.IsDisabled;

        if (!DataAvailable)
        {
            _inputFilterButton.IsDisabled = true;
            _outputFilterButton.IsDisabled = true;
        }
        else
        {
            var hasInput = false;
            var hasOutput = false;
            foreach (var pc in _wrappers)
            {
                if (pc.IsInput)
                    hasInput = true;
                else
                    hasOutput = true;

                if (hasInput && hasOutput)
                    break;
            }

            _inputFilterButton.IsDisabled = !hasInput;
            _outputFilterButton.IsDisabled = !hasOutput;
        }

        if (prevIFBS != _inputFilterButton.IsDisabled || prevOFBS != _outputFilterButton.IsDisabled)
            InvokeAsync(StateHasChanged);
    }
}
