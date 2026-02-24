using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ColorableIcons;
using ViciOne.Ui.Localization.Resources;
using ViciOne.Ui.Shared.Dx.Components;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class LinkDestinationDialog : ComponentBase, IDisposable
{
    private readonly string _collapseAllGroupsText = CompositeFormats.CollapseSomething($"{CommonVocabulary.All} {CommonVocabulary.GroupPlural}");
    private readonly string _columnChooserId = "id" + Guid.NewGuid();
    private readonly string _expandAllGroupsText = CompositeFormats.ExpandSomething($"{CommonVocabulary.All} {CommonVocabulary.GroupPlural}");
    private bool _groupingButtonsEnabled;
    private string _heading = string.Empty;
    private bool _okButtonEnabled;
    private DxDialog? _refDialog;
    private IGrid? _refGrid;
    private IReadOnlyList<object>? _selectedDataItems;
    private bool _showConnectors;
    private bool _showDataPorts;

    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private LinkDestinationDialogService DialogService { get; set; } = default!;

    private string? SearchText { get; set; }

    public void Dispose()
    {
        DialogService.VisibilityChanged -= OnDialogServiceVisibilityChangedAsync;
        _refGrid = null;
        _selectedDataItems = null;
    }

    private MarkupString GetIconMarkup(DataGridConnectorWrapper connector)
        => (MarkupString)ColoredIconFactory.GetConnectorIcon(ConnectorService.GetConnectorColor(connector.Connector), connector.IsInput);

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

    private void OnDialogClosing()
    {
        DialogService.IsDeletionMode = false;
        SearchText = null;
        _showConnectors = false;
        _showDataPorts = false;
    }

    private void OnDialogOk()
    {
        if (DialogService.IsDeletionMode)
        {
            if (_selectedDataItems is not null && _selectedDataItems.Any())
            {
                var firstSelectedDataItem = _selectedDataItems[0];

                if (firstSelectedDataItem is DataGridConnectorWrapper)
                {
                    var connectors = _selectedDataItems.Select(di => (Connector)((DataGridConnectorWrapper)di).Connector);
                    DialogService.InvokeLinksToDeleteSelected(connectors);
                }
                else if (firstSelectedDataItem is DataGridDataPortWrapper)
                {
                    var dataPortTreeNodes = _selectedDataItems.Select(di => ((DataGridDataPortWrapper)di).DataPortTreeNode);
                    DialogService.InvokeLinksToDeleteSelected(dataPortTreeNodes);
                }
            }
        }
        else
        {
            var focusedDataItem = _refGrid!.GetFocusedDataItem();

            if (focusedDataItem is DataGridConnectorWrapper publishedConnectorWrapper)
                DialogService.InvokeConnectorSelected((Connector)publishedConnectorWrapper.Connector);
            else if (focusedDataItem is DataGridDataPortWrapper dataPortWrapper)
                DialogService.InvokeDataPortSelected(dataPortWrapper.DataPortTreeNode);
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
        if (DialogService.ConnectorWrappers.Any())
            _showConnectors = true;
        else
            _showDataPorts = true;
    }

    private async void OnDialogShownAsync()
    {
        if (DialogService.IsDeletionMode)
            await _refGrid!.SelectAllAsync();

        SetHeading();
    }

    private static void OnDxGridCustomSort(GridCustomSortEventArgs args)
    {
        if (args.FieldName == nameof(DataGridConnectorWrapper.FunctionBlockId))
        {
            args.Result = AlphaNumericComparer.Default.Compare(
                ((DataGridConnectorWrapper)args.DataItem1).FunctionBlockName,
                ((DataGridConnectorWrapper)args.DataItem2).FunctionBlockName
            );
        }

        args.Handled = true;
    }

    private void OnExpandAllGroups()
        => _refGrid?.ExpandAllGroupRows();

    private void OnFocusedRowChanged(GridFocusedRowChangedEventArgs e)
    {
        _okButtonEnabled = e.DataItem is not null;
        SetGroupingButtonsState();
        StateHasChanged();
    }

    protected override void OnInitialized()
        => DialogService.VisibilityChanged += OnDialogServiceVisibilityChangedAsync;

    private async Task OnLayoutAutoSavingAsync(GridPersistentLayoutEventArgs _)
    {
        SetGroupingButtonsState();
        await InvokeAsync(StateHasChanged);
    }

    private async void OnRowDoubleClickAsync(GridRowClickEventArgs e)
    {
        if (_refGrid!.IsGroupRow(e.VisibleIndex) ||
            DialogService.IsDeletionMode ||
            _refDialog is null)
        {
            return;
        }

        var dataItem = _refGrid.GetDataItem(e.VisibleIndex);
        if (dataItem is null)
            return;

        DialogService.SetSourceConnectorMarker(null);
        DialogService.SetSourceDataPortTreeNode(null);
        await _refDialog.CloseAsync();

        if (dataItem is DataGridConnectorWrapper connectorWrapper)
            DialogService.InvokeConnectorSelected((Connector)connectorWrapper.Connector);
        else if (dataItem is DataGridDataPortWrapper dataPortWrapper)
            DialogService.InvokeDataPortSelected(dataPortWrapper.DataPortTreeNode);
    }

    private void OnSelectedDataItemsChanged(IReadOnlyList<object> dataItems)
    {
        _okButtonEnabled = dataItems is not null && dataItems.Any();
        _selectedDataItems = dataItems;
    }

    private void OnShowColumnChooser()
        => _refGrid?.ShowColumnChooser(new DialogDisplayOptions("#" + _columnChooserId, HorizontalAlignment.Right, VerticalAlignment.Top));

    private void SetGroupingButtonsState()
        => _groupingButtonsEnabled = _refGrid?.GetGroupCount() > 0;

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
}
