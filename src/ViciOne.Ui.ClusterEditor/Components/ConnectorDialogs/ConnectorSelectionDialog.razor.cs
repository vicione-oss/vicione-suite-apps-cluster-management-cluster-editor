using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.ClusterEditor.Components.ConnectorDialogs.Models;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ColorableIcons;
using ViciOne.Ui.Localization.Resources;
using LocalTechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Components.ConnectorDialogs;

public sealed partial class ConnectorSelectionDialog : ComponentBase, IDisposable
{
    private static readonly Expression<Func<DataGridConnectorWrapper, object>> s_connectorTypeNameSortExpression
        = wrapper => wrapper.ConnectorTypeName;

    /// <summary>
    /// Sorts connectors by direction, so that ascending lists inputs before outputs.
    /// </summary>
    private static readonly Expression<Func<DataGridConnectorWrapper, object>> s_isOutputSortExpression
        = wrapper => !wrapper.IsInput;

    private static readonly Expression<Func<DataGridConnectorWrapper, object>> s_parentNameSortExpression
        = wrapper => wrapper.ParentName;

    private readonly object _columnChooserToggleId = new();

    private List<DataGridConnectorWrapper> _connectorWrappers = [];

    /// <summary>
    /// The rows that pass the current filter. Empty when the filter matches no row.
    /// </summary>
    private IReadOnlyList<DataGridConnectorWrapper> _filteredItems = [];

    private FilterState _filterState = FilterState.Empty;

    private readonly SortingState _initialSorting = SortingState.Empty
        .WithColumnSorting(nameof(DataGridConnectorWrapper.ParentName), ascending: true)
        .WithColumnSorting(nameof(DataGridConnectorWrapper.ConnectorName), ascending: true);

    private Dialog? _refDialog;
    private string? _searchText;
    private readonly string _selectAllConnectorsText = CompositeFormats.SelectSomething($"{CommonVocabulary.All} {LocalTechnicalTerms.ConnectorPlural}") + " (" + LocalTechnicalTerms.IncludingSystemConnectors + ")";
    private readonly string _selectAllInputConnectorsText = CompositeFormats.SelectSomething($"{CommonVocabulary.All} {LocalTechnicalTerms.InputConnectorPlural}") + " (" + LocalTechnicalTerms.IncludingSystemConnectors + ")";
    private readonly string _selectAllOutputConnectorsText = CompositeFormats.SelectSomething($"{CommonVocabulary.All} {LocalTechnicalTerms.OutputConnectorPlural}") + " (" + LocalTechnicalTerms.IncludingSystemConnectors + ")";
    private readonly string _selectConnectorsText = CompositeFormats.SelectSomething($"{LocalTechnicalTerms.ConnectorPlural}");
    private List<DataGridConnectorWrapper> _selectedItems = [];
    private readonly string _selectInputConnectorsText = CompositeFormats.SelectSomething(LocalTechnicalTerms.InputConnectorPlural);
    private readonly string _selectOutputConnectorsText = CompositeFormats.SelectSomething(LocalTechnicalTerms.OutputConnectorPlural);

    /// <summary>
    /// The selected rows that pass the current filter.
    /// </summary>
    /// <remarks>
    /// Taken from the table instead of intersecting <see cref="_selectedItems"/> with <see cref="_filteredItems"/>,
    /// because the table matches rows with its own selection comparer.
    /// </remarks>
    private IReadOnlyList<DataGridConnectorWrapper> _visibleSelection = [];

    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private ConnectorSelectionDialogService DialogService { get; set; } = default!;

    public void Dispose()
    {
        DialogService.VisibilityChanged -= OnDialogServiceVisibilityChangedAsync;
        _selectedItems = [];
        _filteredItems = [];
        _visibleSelection = [];
    }

    private MarkupString GetIconMarkup(DataGridConnectorWrapper connector)
        => (MarkupString)ColoredIconFactory.GetConnectorIcon(ConnectorService.GetConnectorColor(connector.Connector), connector.IsInput);

    /// <summary>
    /// Clears the whole selection, including selected rows that the current filter hides.
    /// </summary>
    private void OnDeselectAllButton()
        => _selectedItems = [];

    private void OnDialogCancel()
        => DialogService.SetVisibility(false);

    private void OnDialogClosing()
    {
        _selectedItems = [];
        _searchText = null;

        // A surviving filter would narrow the next session's rows with nothing on screen explaining why.
        _filterState = FilterState.Empty;

        // The table pushes new mirrors only once it has provided, so the next session's first frame would
        // otherwise show this session's count.
        _filteredItems = [];
        _visibleSelection = [];
    }

    private void OnDialogOk()
    {
        var selected = _visibleSelection;

        if (selected.Count > 0)
        {
            List<IConnector> selectedConnectors = [];

            // Collect all selected container connectors directly.
            foreach (var wrapper in selected)
            {
                if (wrapper.Connector is ContainerConnector)
                    selectedConnectors.Add(wrapper.Connector);
            }

            // Build a lookup for FB connectors from the full dialog connector list.
            var fbConnectorLookup = new Dictionary<(string DesignName, string ConnectorName, bool IsInput), List<IConnector>>();
            foreach (var cw in DialogService.Connectors)
            {
                if (cw.Connector is not Connector)
                    continue;

                var key = (cw.DesignName, cw.ConnectorName, cw.IsInput);
                if (!fbConnectorLookup.TryGetValue(key, out var list))
                {
                    list = [];
                    fbConnectorLookup[key] = list;
                }

                list.Add(cw.Connector);
            }

            // Resolve selected FB connectors through the lookup.
            foreach (var wrapper in selected)
            {
                if (wrapper.Connector is not Connector)
                    continue;

                var key = (wrapper.DesignName, wrapper.ConnectorName, wrapper.IsInput);
                if (fbConnectorLookup.TryGetValue(key, out var list))
                {
                    selectedConnectors.AddRange(list);
                }
            }

            DialogService.SelectConnectors(selectedConnectors);
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
        _connectorWrappers = [];

        // Add all container connectors.
        foreach (var cw in DialogService.Connectors)
        {
            if (cw.Connector is ContainerConnector)
                _connectorWrappers.Add(cw);
        }

        // Show every FB-Design connector once, no matter how many
        // FBs of this design are in the selection.
        HashSet<string> seenDesigns = [];
        foreach (var cw in DialogService.Connectors)
        {
            if (cw.Connector is not Connector)
                continue;

            if (!seenDesigns.Add(cw.DesignName))
                continue;

            // Found first group for this design – add all connectors of its parent.
            var parentName = cw.ParentName;
            foreach (var inner in DialogService.Connectors)
            {
                if (inner.Connector is Connector && inner.DesignName == cw.DesignName && inner.ParentName == parentName)
                    _connectorWrappers.Add(inner);
            }
        }
    }

    /// <summary>
    /// Replaces the selection with the rows that pass the current filter and match <paramref name="predicate"/>.
    /// Rows selected before are deselected, including those that the current filter hides.
    /// </summary>
    private void OnFilterButton(Func<DataGridConnectorWrapper, bool> predicate)
    {
        List<DataGridConnectorWrapper> dataItemsToSelect = [];

        foreach (var item in _filteredItems)
        {
            if (predicate(item))
                dataItemsToSelect.Add(item);
        }

        // Assigning a new list is what makes the table adopt it.
        _selectedItems = dataItemsToSelect;
    }

    private void OnFilteredItemsChanged(IReadOnlyList<DataGridConnectorWrapper> filteredItems)
        => _filteredItems = filteredItems;

    protected override void OnInitialized()
        => DialogService.VisibilityChanged += OnDialogServiceVisibilityChangedAsync;

    private void OnVisibleSelectionChanged(IReadOnlyList<DataGridConnectorWrapper> visibleSelection)
        => _visibleSelection = visibleSelection;

    private void SearchTextChanging(string? searchText)
    {
        _searchText = searchText;

        // The table reads a new instance as a command, so this may only run from an event: building one per
        // render would re-apply the filter on every render.
        _filterState = string.IsNullOrWhiteSpace(searchText)
            ? _filterState.WithoutGlobalFilter<ConnectorSelectionSearchFilter>()
            : _filterState.WithGlobalFilter(new ConnectorSelectionSearchFilter(searchText));
    }
}
