using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ColorableIcons;
using SelectionMode = ViciOne.Ui.Blazor.Components.Tables.Shared.Enums.SelectionMode;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class LinkDestinationDialog : ComponentBase, IDisposable
{
    private static readonly Comparer<object> s_alphaNumericComparer
        = Comparer<object>.Create(AlphaNumericComparer.Default.Compare);

    private static readonly Expression<Func<object, object>> s_connectorSortExpression
        = item => ((DataGridConnectorWrapper)item).FunctionBlockName;

    private static readonly Func<object, object> s_connectorSortKeySelector = s_connectorSortExpression.Compile();

    private static readonly Expression<Func<object, object>> s_dataPortSortExpression
        = item => ((DataGridDataPortWrapper)item).Name;

    private static readonly Func<object, object> s_dataPortSortKeySelector = s_dataPortSortExpression.Compile();

    private readonly object _columnChooserToggleId = new();

    private FilterState _filterState = FilterState.Empty;

    private string _heading = string.Empty;
    private SortingState _initialSorting = SortingState.Empty;
    private List<object> _items = [];
    private Dialog? _refDialog;
    private string? _searchText;
    private List<object> _selectedItems = [];
    private bool _showConnectors;
    private bool _showDataPorts;

    /// <summary>
    /// The selected rows that pass the current filter.
    /// </summary>
    /// <remarks>
    /// Taken from the table instead of intersecting <see cref="_selectedItems"/> with the filtered rows, because the
    /// table matches rows with its own selection comparer.
    /// </remarks>
    private IReadOnlyList<object> _visibleSelection = [];

    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private LinkDestinationDialogService DialogService { get; set; } = default!;

    private bool OkButtonEnabled => _selectedItems.Count > 0;

    private SelectionMode TableSelectionMode => DialogService.IsDeletionMode ? SelectionMode.Multiple : SelectionMode.Single;

    public void Dispose()
    {
        DialogService.VisibilityChanged -= OnDialogServiceVisibilityChangedAsync;
        _selectedItems = [];
        _visibleSelection = [];
    }

    /// <summary>
    /// Returns the row that the table renders first under the initial sorting.
    /// </summary>
    /// <remarks>
    /// Each branch uses the comparer that its column declares as sort comparer, so that the result matches the table.
    /// </remarks>
    private object? FirstRowInTableOrder()
    {
        if (_showConnectors)
            return _items.MinBy(s_connectorSortKeySelector, s_alphaNumericComparer);

        return _items.MinBy(s_dataPortSortKeySelector, s_alphaNumericComparer);
    }

    private static string GetDescription(object item) => item switch
    {
        DataGridConnectorWrapper connector => connector.Description,
        DataGridDataPortWrapper dataPort => dataPort.Description,
        _ => string.Empty,
    };

    private MarkupString GetIconMarkup(DataGridConnectorWrapper connector)
        => (MarkupString)ColoredIconFactory.GetConnectorIcon(ConnectorService.GetConnectorColor(connector.Connector), connector.IsInput);

    private static string GetPath(object item) => item switch
    {
        DataGridConnectorWrapper connector => connector.Path,
        DataGridDataPortWrapper dataPort => dataPort.Path,
        _ => string.Empty,
    };

    private void OnDialogClosing()
    {
        DialogService.IsDeletionMode = false;
        _searchText = null;

        // A surviving filter would narrow the next session's rows with nothing on screen explaining why.
        _filterState = FilterState.Empty;

        _showConnectors = false;
        _showDataPorts = false;
        _selectedItems = [];

        // The table pushes a new mirror only once it has provided, so the next session's first frame would
        // otherwise show this session's count.
        _visibleSelection = [];
    }

    private void OnDialogOk()
    {
        var selected = _visibleSelection;

        if (DialogService.IsDeletionMode)
        {
            if (selected.Count > 0)
            {
                if (selected[0] is DataGridConnectorWrapper)
                {
                    var connectors = selected.Select(item => (Connector)((DataGridConnectorWrapper)item).Connector);
                    DialogService.InvokeLinksToDeleteSelected(connectors);
                }
                else if (selected[0] is DataGridDataPortWrapper)
                {
                    var dataPortTreeNodes = selected.Select(item => ((DataGridDataPortWrapper)item).DataPortTreeNode);
                    DialogService.InvokeLinksToDeleteSelected(dataPortTreeNodes);
                }
            }
        }
        else if (selected.Count == 1)
        {
            if (selected[0] is DataGridConnectorWrapper connectorWrapper)
                DialogService.InvokeConnectorSelected((Connector)connectorWrapper.Connector, connectorWrapper.DestinationMarker);
            else if (selected[0] is DataGridDataPortWrapper dataPortWrapper)
                DialogService.InvokeDataPortSelected(dataPortWrapper.DataPortTreeNode);
        }

        DialogService.SetVisibility(false);
    }

    private async void OnDialogServiceVisibilityChangedAsync()
    {
        if (_refDialog is null)
            return;

        if (DialogService.Visible)
            await _refDialog.ShowAsync();
        else
            await _refDialog.CloseAsync();
    }

    private void OnDialogShowing()
    {
        if (DialogService.ConnectorWrappers.Count > 0)
        {
            _showConnectors = true;
            _items = [.. DialogService.ConnectorWrappers];
            _initialSorting = SortingState.Empty.WithColumnSorting(nameof(DataGridConnectorWrapper.FunctionBlockId), ascending: true);
        }
        else
        {
            _showDataPorts = true;
            _items = [.. DialogService.DataPortWrappers];
            _initialSorting = SortingState.Empty.WithColumnSorting(nameof(DataGridDataPortWrapper.Name), ascending: true);
        }
    }

    private void OnDialogShown()
    {
        // Reads the dialog's own source list, so the seeding does not depend on the window the table has
        // loaded.
        if (DialogService.IsDeletionMode)
        {
            _selectedItems = [.. _items];
        }
        else if (FirstRowInTableOrder() is { } firstRow)
        {
            // Assigning a new list is what makes the table adopt it.
            _selectedItems = [firstRow];
        }

        SetHeading();
    }

    protected override void OnInitialized()
        => DialogService.VisibilityChanged += OnDialogServiceVisibilityChangedAsync;

    private void OnVisibleSelectionChanged(IReadOnlyList<object> visibleSelection)
        => _visibleSelection = visibleSelection;

    private async Task RowDoubleClick(object dataItem)
    {
        if (DialogService.IsDeletionMode || _refDialog is null)
            return;

        DialogService.SetSourceConnectorMarker(null);
        DialogService.SetSourceDataPortTreeNode(null);
        await _refDialog.CloseAsync();

        if (dataItem is DataGridConnectorWrapper connectorWrapper)
            DialogService.InvokeConnectorSelected((Connector)connectorWrapper.Connector, connectorWrapper.DestinationMarker);
        else if (dataItem is DataGridDataPortWrapper dataPortWrapper)
            DialogService.InvokeDataPortSelected(dataPortWrapper.DataPortTreeNode);
    }

    private void SearchTextChanging(string? searchText)
    {
        _searchText = searchText;

        // The table reads a new instance as a command, so this may only run from an event: building one per
        // render would re-apply the filter on every render.
        _filterState = string.IsNullOrWhiteSpace(searchText)
            ? _filterState.WithoutGlobalFilter<LinkDestinationSearchFilter>()
            : _filterState.WithGlobalFilter(new LinkDestinationSearchFilter(searchText));
    }

    private void SetHeading()
    {
        var sourcePart = DialogService.SourceConnectorMarker is not null
            ? $"{DialogService.SourceConnectorMarker.Connector.Connector.FunctionBlock.Name}.{DialogService.SourceConnectorMarker.Connector.Connector.Name}"
            : DialogService.SourceDataPortTreeNode?.Name ?? string.Empty;

        if (DialogService.IsDeletionMode)
            _heading = CompositeFormats.DeleteSomething(Ui.ClusterEditor.Localization.Resources.TechnicalTerms.LinkPlural) + " - " + sourcePart;
        else
            _heading = Localization.LinkDestinationDialog.SelectTarget + " - " + sourcePart;
    }

    private void VisibleChanged(bool visible)
    {
        DialogService.SetVisibility(visible);

        if (visible)
            OnDialogShown();
    }
}
