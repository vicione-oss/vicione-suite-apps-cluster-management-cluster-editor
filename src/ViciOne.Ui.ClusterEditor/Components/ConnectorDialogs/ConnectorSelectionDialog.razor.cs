using System;
using System.Collections.Generic;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ColorableIcons;
using ViciOne.Ui.Localization.Resources;
using LocalTechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Components.ConnectorDialogs;

public sealed partial class ConnectorSelectionDialog : ComponentBase, IDisposable
{
    private readonly string _collapseAllGroupsText = CompositeFormats.CollapseSomething($"{CommonVocabulary.All} {CommonVocabulary.GroupPlural}");
    private List<DataGridConnectorWrapper> _connectorWrappers = [];
    private readonly string _expandAllGroupsText = CompositeFormats.ExpandSomething($"{CommonVocabulary.All} {CommonVocabulary.GroupPlural}");
    private bool _groupingButtonsEnabled;
    private Dialog? _refDialog;
    private IGrid? _refGrid;
    private string? _searchText;
    private readonly string _selectAllConnectorsText = CompositeFormats.SelectSomething($"{CommonVocabulary.All} {LocalTechnicalTerms.ConnectorPlural}") + " (" + LocalTechnicalTerms.IncludingSystemConnectors + ")";
    private readonly string _selectAllInputConnectorsText = CompositeFormats.SelectSomething($"{CommonVocabulary.All} {LocalTechnicalTerms.InputConnectorPlural}") + " (" + LocalTechnicalTerms.IncludingSystemConnectors + ")";
    private readonly string _selectAllOutputConnectorsText = CompositeFormats.SelectSomething($"{CommonVocabulary.All} {LocalTechnicalTerms.OutputConnectorPlural}") + " (" + LocalTechnicalTerms.IncludingSystemConnectors + ")";
    private readonly string _selectConnectorsText = CompositeFormats.SelectSomething($"{LocalTechnicalTerms.ConnectorPlural}");
    private IReadOnlyList<object>? _selectedDataItems;
    private readonly string _selectInputConnectorsText = CompositeFormats.SelectSomething(LocalTechnicalTerms.InputConnectorPlural);
    private readonly string _selectOutputConnectorsText = CompositeFormats.SelectSomething(LocalTechnicalTerms.OutputConnectorPlural);

    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private ConnectorSelectionDialogService DialogService { get; set; } = default!;

    public void Dispose()
    {
        DialogService.VisibilityChanged -= OnDialogServiceVisibilityChangedAsync;
        _refGrid = null;
        _selectedDataItems = null;
    }

    private MarkupString GetIconMarkup(DataGridConnectorWrapper connector)
        => (MarkupString)ColoredIconFactory.GetConnectorIcon(ConnectorService.GetConnectorColor(connector.Connector), connector.IsInput);

    private List<DataGridConnectorWrapper> GetSelectableDataItems()
    {
        if (string.IsNullOrEmpty(_searchText))
            return _connectorWrappers;

        List<DataGridConnectorWrapper> result = [];
        foreach (var cw in _connectorWrappers)
        {
            if (cw.ParentName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                cw.ConnectorName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                cw.ConnectorTypeName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                cw.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(cw);
            }
        }

        return result;
    }

    private void OnCollapseAllGroups()
        => _refGrid?.CollapseAllGroupRows();

    private static void OnCustomizeCellDisplayText(GridCustomizeCellDisplayTextEventArgs e)
    {
        if (e.FieldName == nameof(DataGridConnectorWrapper.FunctionBlockId)
            && e.DataItem is DataGridConnectorWrapper connectorWrapper)
        {
            e.DisplayText = connectorWrapper.FunctionBlockName;
        }
    }

    private static void OnCustomizeCustomGroup(GridCustomGroupEventArgs e)
    {
        if (e.FieldName == nameof(DataGridConnectorWrapper.FunctionBlockId)
            && e.DataItem1 is DataGridConnectorWrapper connectorWrapper1
            && e.DataItem2 is DataGridConnectorWrapper connectorWrapper2)
        {
            e.SameGroup = connectorWrapper1.FunctionBlockId == connectorWrapper2.FunctionBlockId;
        }
    }

    private static void OnCustomSort(GridCustomSortEventArgs args)
    {
        if (args.FieldName == nameof(DataGridConnectorWrapper.ConnectorName))
            args.Result = ((DataGridConnectorWrapper)args.DataItem2).IsInput.CompareTo(((DataGridConnectorWrapper)args.DataItem1).IsInput);

        args.Handled = true;
    }

    private void OnDialogCancel()
        => DialogService.SetVisibility(false);

    private void OnDialogClosing()
    {
        _refGrid?.DeselectDataItems(_selectedDataItems);
        _selectedDataItems = null;
        _searchText = null;
    }

    private void OnDialogOk()
    {
        if (_selectedDataItems is not null)
        {
            List<IConnector> selectedConnectors = [];

            // Collect all selected container connectors directly.
            foreach (var di in _selectedDataItems)
            {
                var wrapper = (DataGridConnectorWrapper)di;
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
            foreach (var di in _selectedDataItems)
            {
                var wrapper = (DataGridConnectorWrapper)di;
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

    private void OnExpandAllGroups()
        => _refGrid?.ExpandAllGroupRows();

    private void OnFilterButton(Func<DataGridConnectorWrapper, bool> predicate)
    {
        if (_refGrid is null)
            return;

        _refGrid.DeselectDataItems(_selectedDataItems);

        List<DataGridConnectorWrapper> dataItemsToSelect = [];
        foreach (var item in GetSelectableDataItems())
        {
            if (predicate(item))
                dataItemsToSelect.Add(item);
        }

        _refGrid.SelectDataItems(dataItemsToSelect, true);
    }

    protected override void OnInitialized()
        => DialogService.VisibilityChanged += OnDialogServiceVisibilityChangedAsync;

    private void OnSelectedDataItemsChanged(IReadOnlyList<object> dataItems)
        => _selectedDataItems = dataItems;

    private void SetGroupingButtonsState()
        => _groupingButtonsEnabled = _refGrid?.GetGroupCount() > 0;
}
