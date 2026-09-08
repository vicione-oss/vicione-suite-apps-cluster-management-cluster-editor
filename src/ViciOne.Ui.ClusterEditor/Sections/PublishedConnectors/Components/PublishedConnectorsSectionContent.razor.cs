using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Blazor;
using DevExpress.Data.Filtering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Comparer;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ColorableIcons;
using ViciOne.Ui.MonochromeIcons.Core.Enums;

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
    private DataGridConnectorWrapper? _pendingSelection;
    private string? _searchText;

    [Inject] private ConnectorService ConnectorService { get; set; } = default!;
    [Inject] private IContextMenuRequest<PublishedConnectorsSectionContextMenuContext> ContextMenuRequest { get; set; } = default!;
    [Inject] private ILogger<PublishedConnectorsSectionContent> Logger { get; set; } = default!;
    [Inject] private PublishedConnectorsService PublishedConnectorsService { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    private bool DataAvailable => PublishedConnectorsService.PublishedConnectorWrappers.Any();

    // Runs after the render that expanded the section. The section is kept in the DOM while
    // inactive but collapsed to zero height, so scrolling the virtualized grid only works
    // once it is actually visible.
    private async Task ApplyPendingSelectionAsync()
    {
        if (_pendingSelection is null || _gridRef is null)
            return;

        var target = _pendingSelection;
        _pendingSelection = null;

        _gridRef.ClearSelection();
        _gridRef.SelectDataItem(target, true);

        // An active filter or search can hide the row entirely. The selection above still sticks
        // to the data item, so clearing the filter reveals it as selected; there is nothing to
        // scroll to in the meantime.
        if (ExpandGroupRowsTo(target))
            await _gridRef.MakeDataItemVisibleAsync(target);
    }

    public void Dispose()
    {
        PublishedConnectorsService.DraggingEnded -= OnDraggingEnded;
        PublishedConnectorsService.PublishedConnectorSelectionRequested -= OnPublishedConnectorSelectionRequested;
        PublishedConnectorsService.PublishedConnectorsChanged -= OnPublishedConnectorsChangedAsync;
    }

    // Expands only the group rows that contain the target, so a jump from the diagram leaves
    // groups the user deliberately collapsed alone. Returns false when the row cannot be
    // reached at all, which means an active filter or search text excludes it.
    private bool ExpandGroupRowsTo(DataGridConnectorWrapper target)
    {
        var groupedFieldNames = GetGroupedFieldNames();

        for (var rowIndex = 0; rowIndex < _gridRef!.GetVisibleRowCount(); rowIndex++)
        {
            if (!_gridRef.IsGroupRow(rowIndex))
            {
                if (ReferenceEquals(_gridRef.GetDataItem(rowIndex), target))
                    return true;

                continue;
            }

            var level = _gridRef.GetRowLevel(rowIndex);
            if (level >= groupedFieldNames.Count)
                continue;

            var fieldName = groupedFieldNames[level];
            if (!Equals(_gridRef.GetRowValue(rowIndex, fieldName), _gridRef.GetDataItemValue(target, fieldName)))
                continue;

            if (!_gridRef.IsGroupRowExpanded(rowIndex))
                _gridRef.ExpandGroupRow(rowIndex, false);
        }

        return false;
    }

    private List<string> GetGroupedFieldNames()
    {
        var groupedColumns = new List<IGridDataColumn>();
        foreach (var column in _gridRef!.GetDataColumns())
        {
            if (column.GroupIndex >= 0)
                groupedColumns.Add(column);
        }

        groupedColumns.Sort((first, second) => first.GroupIndex.CompareTo(second.GroupIndex));

        var fieldNames = new List<string>(groupedColumns.Count);
        foreach (var column in groupedColumns)
            fieldNames.Add(column.FieldName);

        return fieldNames;
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        SetGroupingButtonsState();
        SetFilterButtonsState();

        await ApplyPendingSelectionAsync();
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
        PublishedConnectorsService.PublishedConnectorSelectionRequested += OnPublishedConnectorSelectionRequested;
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

    private void OnPublishedConnectorSelectionRequested(IConnector connector)
    {
        foreach (var wrapper in PublishedConnectorsService.PublishedConnectorWrappers)
        {
            if (wrapper.Connector.Id != connector.Id)
                continue;

            _pendingSelection = wrapper;
            break;
        }

        if (_pendingSelection is not null)
            InvokeAsync(StateHasChanged);
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
                    if (selectedItems.Count > 0 && selectedItems[0].ConnectorType != currentConnectorWrapper.ConnectorType)
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

        if (!DataAvailable)
        {
            _inputFilterButton.IsDisabled = true;
            _outputFilterButton.IsDisabled = true;
        }
        else
        {
            var hasInput = false;
            var hasOutput = false;
            foreach (var pc in PublishedConnectorsService.PublishedConnectorWrappers)
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

    private void SetGroupingButtonsState()
    {
        var prevGBE = _groupingButtonsEnabled;
        _groupingButtonsEnabled = DataAvailable && _gridRef?.GetVisibleRowCount() > 0 && _gridRef?.GetGroupCount() > 0;
        if (prevGBE != _groupingButtonsEnabled)
            InvokeAsync(StateHasChanged);
    }
}
