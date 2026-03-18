using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Blazor;
using DevExpress.Data.Filtering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Resources;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ColorableIcons;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Components;

public sealed partial class PublishedConnectorsSectionContent : ComponentBase, IDisposable
{
    private const int LeftButton = 0;
    private bool _blockMouseUp;
    private readonly List<FilterButton> _filterButtons = [];
    private IGrid? _gridRef;
    private bool _groupingButtonsEnabled;
    private FilterButton _inputFilterButton = new();
    private int _lastRowIndex;
    private FilterButton _outputFilterButton = new();
    private string? _searchText;

    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private IContextMenuRequest<PublishedConnectorsSectionContextMenuContext> ContextMenuRequest { get; set; } = default!;
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private ILogger<PublishedConnectorsSectionContent> Logger { get; set; } = default!;
    [Inject] private PublishedConnectorsService PublishedConnectorsService { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    private bool DataAvailable => PublishedConnectorsService.PublishedConnectorWrappers.Any();

    public void Dispose()
    {
        PublishedConnectorsService.DraggingEnded -= OnDraggingEnded;
        PublishedConnectorsService.PublishedConnectorsChanged -= OnPublishedConnectorsChangedAsync;
    }

    private MarkupString GetIconMarkup(DataGridConnectorWrapper connectorWrapper)
        => (MarkupString)ColoredIconFactory.GetConnectorIcon(ConnectorService.GetConnectorColor(connectorWrapper.Connector), connectorWrapper.IsInput);

    private void InitFilterButtons()
    {
        _inputFilterButton = new FilterButton()
        {
            Icon = (MarkupString)SvgIcons.published_connectors_section_filter_inputs,
            IsDisabled = DataAvailable,
            OnFilterClickedFn = OnFilterInputs,
            Title = Localization.PublishedConnectorsSection.FilterInputs,
        };
        _filterButtons.Add(_inputFilterButton);

        _outputFilterButton = new FilterButton()
        {
            Icon = (MarkupString)SvgIcons.published_connectors_section_filter_outputs,
            IsDisabled = DataAvailable,
            OnFilterClickedFn = OnFilterOutputs,
            Title = Localization.PublishedConnectorsSection.FilterOutputs,
        };
        _filterButtons.Add(_outputFilterButton);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        SetGroupingButtonsState();
        SetFilterButtonsState();
    }

    private void OnCollapseAllGroups()
        => _gridRef?.CollapseAllGroupRows();

    private void OnColumnChooser(string positionTarget)
        => _gridRef?.ShowColumnChooser(new DialogDisplayOptions(positionTarget, HorizontalAlignment.Right, VerticalAlignment.Top));

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

    private void OnCustomizeElement(GridCustomizeElementEventArgs args)
    {
        if (args.ElementType == GridElementType.DataRow)
        {
            args.Attributes.Add("oncontextmenu", async (MouseEventArgs e) => await OnRowContextMenuAsync(e, args.VisibleIndex));
            args.Attributes.Add("oncontextmenu:preventDefault", ContextMenuSettings.UseCustomMenu);
            args.Attributes.Add("oncontextmenu:stopPropagation", true);
            args.Attributes.Add("onpointerdown", (MouseEventArgs e) => OnRowPointerDown(e, args.VisibleIndex));
            args.Attributes.Add("onpointerup", (MouseEventArgs e) => OnRowPointerUp(e, args.VisibleIndex));
            args.CssClass = "grabable-row";
        }
    }

    private void OnDraggingEnded()
        => _blockMouseUp = true;

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
        => _gridRef?.ExpandAllGroupRows();

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

        if (filter)
        {
            var criteriaOperator = CriteriaOperator.FromLambda<DataGridConnectorWrapper>(c => c.IsInput);
            _gridRef?.SetFilterCriteria(criteriaOperator);
        }
        else
        {
            _gridRef?.ClearFilter();
        }
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

        if (filter)
        {
            var criteriaOperator = CriteriaOperator.FromLambda<DataGridConnectorWrapper>(c => !c.IsInput);
            _gridRef?.SetFilterCriteria(criteriaOperator);
        }
        else
        {
            _gridRef?.ClearFilter();
        }
    }

    protected override void OnInitialized()
    {
        PublishedConnectorsService.DraggingEnded += OnDraggingEnded;
        PublishedConnectorsService.PublishedConnectorsChanged += OnPublishedConnectorsChangedAsync;

        InitFilterButtons();
    }

    private async void OnPublishedConnectorsChangedAsync()
    {
        try
        {
            if (_gridRef is not null)
                await InvokeAsync(_gridRef.Reload);

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            RefreshPublishedConnectorsFailed(Logger, ex);
        }
    }

    private async Task OnRowContextMenuAsync(MouseEventArgs e, int rowIndex)
    {
        SelectionManager.DeselectAll();

        if (_gridRef!.GetDataItem(rowIndex) is not DataGridConnectorWrapper currentConnectorWrapper)
            return;

        var selectedItems = _gridRef.SelectedDataItems.Cast<DataGridConnectorWrapper>().ToList();

        if (selectedItems.Count == 0 || !selectedItems.Contains(currentConnectorWrapper))
        {
            _lastRowIndex = rowIndex;

            selectedItems.Clear();
            selectedItems.Add(currentConnectorWrapper);

            _gridRef.ClearSelection();
            _gridRef.SelectDataItems(selectedItems, true);

            await InvokeAsync(StateHasChanged);
        }

        await ContextMenuRequest.SendAsync(new() { MouseEventArgs = e, PublishedConnectorsWrappers = selectedItems });
    }

    private async Task OnRowDoubleClickAsync(GridRowClickEventArgs e)
    {
        if (_gridRef!.IsGroupRow(e.VisibleIndex))
            return;

        var dataItem = _gridRef.GetDataItem(e.VisibleIndex);
        if (dataItem is null || dataItem is not DataGridConnectorWrapper publishedConnector)
            return;

        await ConnectorService.ShowAndSelectPublishedConnectorMarker(publishedConnector.Connector);
    }

    private void OnRowPointerDown(MouseEventArgs e, int rowIndex)
    {
        SelectionManager.DeselectAll();

        if (e.Button == LeftButton)
        {
            if (_gridRef!.GetDataItem(rowIndex) is not DataGridConnectorWrapper currentConnectorWrapper)
                return;

            var selectedItems = _gridRef!.SelectedDataItems.Cast<DataGridConnectorWrapper>().ToList();

            if (e.ShiftKey)
            {
                var lastDataItem = _gridRef!.GetDataItem(_lastRowIndex) as DataGridConnectorWrapper;

                var start = Math.Min(_lastRowIndex, rowIndex);
                var end = Math.Max(_lastRowIndex, rowIndex);
                var newItems = Enumerable.Range(start, end - start + 1)
                    .Where(i => !_gridRef.IsGroupRow(i))
                    .Select(i => _gridRef.GetDataItem(i))
                    .Cast<DataGridConnectorWrapper>()
                    .ToList()
                    .Where(c => c.ConnectorType == lastDataItem?.ConnectorType);

                selectedItems = e.CtrlKey ? [.. selectedItems.Union(newItems)] : selectedItems = [.. newItems];
            }
            else
            {
                _lastRowIndex = rowIndex;

                if (e.CtrlKey)
                {
                    if (selectedItems.Count > 0 && selectedItems.First().ConnectorType != currentConnectorWrapper.ConnectorType)
                        return;

                    if (!selectedItems.Remove(currentConnectorWrapper))
                        selectedItems.Add(currentConnectorWrapper);
                }
                else
                {
                    if (!selectedItems.Contains(currentConnectorWrapper))
                    {
                        selectedItems.Clear();
                        selectedItems.Add(currentConnectorWrapper);
                    }
                }
            }

            _gridRef.ClearSelection();
            _gridRef.SelectDataItems(selectedItems, true);

            if (selectedItems.Contains(currentConnectorWrapper))
                PublishedConnectorsService.StartPublishedConnectorDragging(selectedItems);
        }
    }

    private void OnRowPointerUp(MouseEventArgs e, int rowIndex)
    {
        if (_blockMouseUp)
        {
            _blockMouseUp = false;
            return;
        }

        if (e.Button == LeftButton)
        {
            if (_gridRef!.GetDataItem(rowIndex) is not DataGridConnectorWrapper focusedConnectorWrapper)
                return;

            var selectedItems = _gridRef!.SelectedDataItems.Cast<DataGridConnectorWrapper>().ToList();

            if (!e.CtrlKey && !e.ShiftKey)
            {
                selectedItems.Clear();
                selectedItems.Add(focusedConnectorWrapper);

                _gridRef.ClearSelection();
                _gridRef.SelectDataItems(selectedItems, true);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Refresh PublishedConnectors failed.")]
    public static partial void RefreshPublishedConnectorsFailed(ILogger logger, Exception ex);

    private void SetFilterButtonsState()
    {
        var prevIFBS = _inputFilterButton.IsDisabled;
        var prevOFBS = _outputFilterButton.IsDisabled;
        _inputFilterButton.IsDisabled = !DataAvailable || !PublishedConnectorsService.PublishedConnectorWrappers.Any(pc => pc.IsInput);
        _outputFilterButton.IsDisabled = !DataAvailable || !PublishedConnectorsService.PublishedConnectorWrappers.Any(pc => !pc.IsInput);

        if (prevIFBS != _inputFilterButton.IsDisabled || prevOFBS != _outputFilterButton.IsDisabled)
            InvokeAsync(StateHasChanged);
    }

    private void SetGroupingButtonsState()
    {
        var prevGBE = _groupingButtonsEnabled;
        _groupingButtonsEnabled = DataAvailable && _gridRef?.GetVisibleRowCount() > 0 && _gridRef?.GetGroupCount() > 0;
        if (prevGBE != _groupingButtonsEnabled)
            InvokeAsync(StateHasChanged);
    }
}
