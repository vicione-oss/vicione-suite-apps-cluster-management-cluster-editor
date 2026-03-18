using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ColorableIcons;
using ViciOne.Ui.Localization.Resources;
using ViciOne.Ui.Shared.Dx.Components;
using LocalTechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Components.ConnectorDialogs;

public sealed partial class ConnectorSelectionDialog : ComponentBase, IDisposable
{
    private readonly string _collapseAllGroupsText = CompositeFormats.CollapseSomething($"{CommonVocabulary.All} {CommonVocabulary.GroupPlural}");
    private readonly string _columnChooserId = "id" + Guid.NewGuid();
    private List<DataGridConnectorWrapper> _connectorWrappers = [];
    private readonly string _expandAllGroupsText = CompositeFormats.ExpandSomething($"{CommonVocabulary.All} {CommonVocabulary.GroupPlural}");
    private bool _groupingButtonsEnabled;
    private DxDialog? _refDialog;
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
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private ConnectorSelectionDialogService DialogService { get; set; } = default!;

    public void Dispose()
    {
        DialogService.VisibilityChanged -= OnDialogServiceVisibilityChangedAsync;
        _refGrid = null;
        _selectedDataItems = null;
    }

    private MarkupString GetIconMarkup(DataGridConnectorWrapper connector)
        => (MarkupString)ColoredIconFactory.GetConnectorIcon(ConnectorService.GetConnectorColor(connector.Connector), connector.IsInput);

    private IEnumerable<DataGridConnectorWrapper> GetSelectableDataItems()
    {
        if (string.IsNullOrEmpty(_searchText))
        {
            return _connectorWrappers;
        }
        else
        {
            return _connectorWrappers.Where(cw =>
                cw.ParentName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                cw.ConnectorName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                cw.ConnectorTypeName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                cw.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }
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
            var containerConnectors = _selectedDataItems
                .Where(di => ((DataGridConnectorWrapper)di).Connector is ContainerConnector)
                .Select(di => ((DataGridConnectorWrapper)di).Connector);

            var fbConnectors = _selectedDataItems
                .Where(di => ((DataGridConnectorWrapper)di).Connector is Connector)
                .SelectMany(di => DialogService.Connectors
                    .Where(cw =>
                           cw.Connector is Connector
                        && cw.DesignName == ((DataGridConnectorWrapper)di).DesignName
                        && cw.ConnectorName == ((DataGridConnectorWrapper)di).ConnectorName
                        && cw.IsInput == ((DataGridConnectorWrapper)di).IsInput)
                    .Select(cw => cw.Connector));

            DialogService.SelectConnectors(containerConnectors.Concat(fbConnectors));
        }

        DialogService.SetVisibility(false);
    }

    private async void OnDialogServiceVisibilityChangedAsync()
    {
        if (_refDialog is null)
            return;

        if (DialogService.Visible)
            await _refDialog.OpenAsync();
        else
            await _refDialog.CloseAsync();
    }

    private void OnDialogShowing()
    {
        // Show all Container connectors.
        _connectorWrappers = [.. DialogService.Connectors.Where(cw => cw.Connector is ContainerConnector)];

        // Just show every FB-Design connector once, no matter how much
        // FBs of this design are in the selection.
        _connectorWrappers =
        [
            .. _connectorWrappers,
            .. DialogService.Connectors
                    .Where(cw => cw.Connector is Connector)
                    .GroupBy(cw => new { cw.DesignName })
                    .SelectMany(g => g
                        .GroupBy(cw => new { cw.ParentName })
                        .First()),
        ];
    }

    private void OnExpandAllGroups()
        => _refGrid?.ExpandAllGroupRows();

    private void OnFilterButton(Func<DataGridConnectorWrapper, bool> predicate)
    {
        if (_refDialog is null)
            return;

        _refGrid?.DeselectDataItems(_selectedDataItems);

        var dataItemsToSelect = GetSelectableDataItems().Where(predicate);

        _refGrid?.SelectDataItems(dataItemsToSelect, true);
    }

    protected override void OnInitialized()
        => DialogService.VisibilityChanged += OnDialogServiceVisibilityChangedAsync;

    private void OnSelectedDataItemsChanged(IReadOnlyList<object> dataItems)
        => _selectedDataItems = dataItems;

    private void OnShowColumnChooser()
        => _refGrid?.ShowColumnChooser(new DialogDisplayOptions("#" + _columnChooserId, HorizontalAlignment.Right, VerticalAlignment.Top));

    private void SetGroupingButtonsState()
        => _groupingButtonsEnabled = _refGrid?.GetGroupCount() > 0;
}
