using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Behaviors;
using DevExpress.XtraRichEdit.Layout.Engine;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Behaviors;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Components.Scrolling;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor;

public sealed partial class ContainerEditor : ComponentBase, IDisposable
{
    private const int MaxPreviewWidth = 192;
    private const float UiScaleFactor = 1.2f;

    private bool _addInputPlaceholderEnabled;
    private bool _addOutputPlaceholderEnabled;
    private double _allocationContainerHeight;
    private bool _containerSelected;
    private ContainerEditorChildContainer? _currentContainer;
    private ChildContainerNode? _currentContainerNode;
    private BlazorDiagram? _diagram;
    private DiagramService? _diagramService;
    private readonly List<ContainerEditorConnector> _inputConnectors = [];
    private bool _inputDownEnabled;
    private bool _inputUpEnabled;
    private ContainerEditorConnector? _lastClickedConnector;
    private int _listsHeightOffset;
    private ChildContainerNode? _originalContainerNode;
    private readonly List<ContainerEditorConnector> _outputConnectors = [];
    private bool _outputDownEnabled;
    private bool _outputUpEnabled;
    private Dialog? _refDialog;
    private CancellationTokenSource? _refreshCts;
    private bool _refreshDebounceRunning;
    private bool _removeInputPlaceholderEnabled;
    private bool _removeOutputPlaceholderEnabled;
    private bool _resetElements = true;
    private ScrollContainer? _scrollContainer;
    private CESelectionBehavior? _selectionBehavior;
    private bool _visible;
    private bool _zoomActive;
    private bool _zoomEnabled;
    private VOZoomToFitBehavior? _zoomToFitBehavior;

    [Inject] private ClusterBuilderEventBuffer ClusterBuilderEventBuffer { get; set; } = default!;
    [Inject] private ComparerService ComparerService { get; set; } = default!;
    [Inject] private IPropertyGridController<ContainerEditorPropertyGridContext> ContainerEditorPropertyGridController { get; set; } = default!;
    [Inject] private IContainerEditorRequest ContainerEditorRequest { get; set; } = default!;
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    private void AddPlaceholder(bool isInput)
    {
        var sourceCollection = isInput ? _inputConnectors : _outputConnectors;
        var sourceSelection = sourceCollection.Where(c => c.Selected).ToList();

        var placeholder = new ContainerEditorConnector(Datastore.Builder.Editors.Connector)
        {
            Backup = ContainerEditorConnectorBackup.Empty,
            Index = sourceCollection.Last().Index + 1,
            IsPlaceholder = true,
        };

        if (sourceSelection.Count > 0)
        {
            var connectorsToMove = sourceCollection.Where(sc => sc.Index >= sourceSelection[0].Index)
                .OrderByDescending(sc => sc.Index)
                .ToArray();
            sourceCollection.Add(placeholder);

            foreach (var movingConnector in connectorsToMove)
                Move(movingConnector, isInput, false);
        }
        else
        {
            sourceCollection.Add(placeholder);
            _scrollContainer?.SetAutoscrollToEnd();
        }

        StartRefreshDebounce();
    }

    private void Cleanup()
    {
        var oldCts = Interlocked.Exchange(ref _refreshCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();

        if (_currentContainerNode is not null)
        {
            _currentContainerNode.SizeChanged -= OnCurrentContainerNodeSizeChanged;
            _diagram?.Nodes.Remove(_currentContainerNode);
            _currentContainerNode = null;
        }

        _currentContainer = null;
        _originalContainerNode = null;

        _inputConnectors.Clear();
        _outputConnectors.Clear();

        if (_zoomActive && _diagram is not null)
        {
            _zoomActive = false;
            _diagram!.Batch(() =>
            {
                _diagram.UpdatePan(-_diagram.Pan.X, -_diagram.Pan.Y);
                _diagram.SetZoom(1.0);
            });
        }

        _resetElements = true;
        _containerSelected = false;
    }

    private static void ClearSelectedConnectors(List<ContainerEditorConnector> connectors)
    {
        foreach (var connector in connectors)
            connector.Selected = false;
    }

    private ContainerEditorConnector CreateContainerEditorConnector(BlockNodeConnector? blockNodeConnector, int index)
    {
        if (blockNodeConnector is not null)
        {
            var model = (ContainerConnector)Datastore.DataflowDiagramMapping.GetModel(blockNodeConnector);

            var connector = new ContainerEditorConnector(Datastore.Builder.Editors.Connector)
            {
                Backup = new()
                {
                    Connector = model,
                    Description = model.Description,
                    Index = Convert.ToInt32(model.Index),
                    Name = model.Name,
                    ShortName = model.ShortName,
                },
                BlockNodeConnector = blockNodeConnector,
                Color = blockNodeConnector.PortColor,
            };

            return connector;
        }
        else
        {
            var connector = new ContainerEditorConnector(Datastore.Builder.Editors.Connector)
            {
                Backup = ContainerEditorConnectorBackup.Empty,
                Index = index,
                IsPlaceholder = true
            };

            return connector;
        }
    }

    public void Dispose()
    {
        ClusterBuilderEventBuffer.ContainerPropertiesChanged -= OnPropertyChanged;
        ClusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnPropertyChanged;
        ContainerEditorRequest.ContainerEditorRequestedAsync -= OnContainerEditorRequested;

        _currentContainerNode?.SizeChanged -= OnCurrentContainerNodeSizeChanged;

        if (_scrollContainer is not null)
        {
            _scrollContainer.AllocationContainerSizeChanged -= OnAllocationContainerSizeChanged;
            _scrollContainer.ScrolledPixelsChanged -= OnScrolledPixelsChanged;
            _scrollContainer.Dispose();
        }

        _selectionBehavior?.ContainerSelected -= OnContainerSelected;
        _selectionBehavior?.SelectedConnectorChanged -= OnSelectedConnectorChanged;

        Cleanup();

        _diagramService?.Dispose();
        _selectionBehavior?.Dispose();
        _zoomToFitBehavior?.Dispose();

        var oldCts = Interlocked.Exchange(ref _refreshCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
    }

    private static bool GetAddPlaceholderButtonEnabled(List<ContainerEditorConnector> selectedConnectors)
        => selectedConnectors.Count > 0;

    private static bool GetMoveDownButtonEnabled(List<ContainerEditorConnector> connectors, List<ContainerEditorConnector> selectedConnectors)
        => selectedConnectors.Count > 0 && selectedConnectors.All(c => c.Index < connectors.Count - 1);

    private static bool GetMoveUpButtonEnabled(List<ContainerEditorConnector> selectedConnectors)
        => selectedConnectors.Count > 0 && selectedConnectors.All(c => c.Index > 0);

    private static bool GetRemoveInputPlaceholderButtonEnabled(List<ContainerEditorConnector> selectedConnectors)
        => selectedConnectors.Count > 0 && selectedConnectors.All(c => c.IsPlaceholder);

    private int GetRequiredPreviewContainerConnectorRowCount()
    {
        var listRowCount = Math.Max(_inputConnectors.Count, _outputConnectors.Count) + BlockNodeLayout.SystemConnectorRows;
        return Math.Max(listRowCount, BlockNodeLayout.MinimumConnectorRows);
    }

    private void InitializeConnectorLists()
    {
        if (_originalContainerNode is null)
            return;

        var index = -BlockNodeLayout.SystemConnectorRows;

        foreach (var connectorArray in _originalContainerNode.Connectors)
        {
            var isSystemConnector = index < 0;

            if (!isSystemConnector)
            {
                var inputConnector = CreateContainerEditorConnector(connectorArray[0], index);
                _inputConnectors.Add(inputConnector);

                var outputConnector = CreateContainerEditorConnector(connectorArray[1], index);
                _outputConnectors.Add(outputConnector);
            }

            index++;
        }
    }

    private void InitializeDiagram()
    {
        _diagram = new(new()
        {
            GridSize = DiagramSettings.DefaultGridSize,
            Virtualization =
            {
                Enabled = false
            },
            Zoom =
            {
                Inverse = true,
                Maximum = DiagramSettings.ZoomMaximum,
                Minimum = DiagramSettings.ZoomMinimum,
                ScaleFactor = 1.3
            }
        });

        _diagram.SetZoom(1.0);

        _diagram.RegisterComponent<ChildContainerNode, ChildContainerEditorComponent>();

        _diagram.UnregisterBehavior<PanBehavior>();
        _diagram.UnregisterBehavior<ZoomBehavior>();
        _diagram.UnregisterBehavior<DragNewLinkBehavior>();
        _diagram.UnregisterBehavior<DragMovablesBehavior>();
        _diagram.UnregisterBehavior<SelectionBehavior>();

        _selectionBehavior = new(_diagram);
        _diagram.RegisterBehavior(_selectionBehavior);
        _selectionBehavior.ContainerSelected += OnContainerSelected;
        _selectionBehavior.SelectedConnectorChanged += OnSelectedConnectorChanged;

        _zoomToFitBehavior = new(_diagram);
        _diagram.RegisterBehavior(_zoomToFitBehavior);

        _diagramService?.Diagram = _diagram;
    }

    private void InitializeDiagramService()
        => _diagramService = new(Datastore, new());

    private void Move(ContainerEditorConnector connector, bool isInput, bool moveUp)
    {
        var sourceCollection = isInput ? _inputConnectors : _outputConnectors;
        var direction = moveUp ? -1 : 1;

        var nextCon = sourceCollection.First(con => con.Index == connector.Index + direction);
        nextCon.Index = sourceCollection.Count;

        connector.Index += direction;
        nextCon.Index = connector.Index - direction;

        var newListIndex = sourceCollection.IndexOf(connector);
        var oldListIndex = newListIndex + direction;
        var item = sourceCollection[oldListIndex];
        sourceCollection.RemoveAt(oldListIndex);
        sourceCollection.Insert(newListIndex, item);
    }

    private async void OnAllocationContainerSizeChanged(Size size)
    {
        _allocationContainerHeight = size.Height;

        if (_allocationContainerHeight > 0)
            _zoomEnabled = _currentContainerNode?.Size?.Height > _allocationContainerHeight;

        await InvokeAsync(StateHasChanged);
    }

    private void OnConnectorClicked(ContainerEditorConnector connector, bool isInput, bool ctrlKey, bool shiftKey)
    {
        var sourceCollection = isInput ? _inputConnectors : _outputConnectors;

        if (shiftKey && _lastClickedConnector is not null && _lastClickedConnector != connector)
        {
            var anchorIndex = sourceCollection.IndexOf(_lastClickedConnector);
            var targetIndex = sourceCollection.IndexOf(connector);

            if (anchorIndex >= 0 && targetIndex >= 0)
            {
                var from = Math.Min(anchorIndex, targetIndex);
                var to = Math.Max(anchorIndex, targetIndex);

                if (!ctrlKey)
                    ClearSelectedConnectors(sourceCollection);

                for (var i = from; i <= to; i++)
                    sourceCollection[i].Selected = true;
            }
        }
        else
        {
            connector.Selected ^= true;

            if (!ctrlKey)
            {
                foreach (var con in sourceCollection)
                {
                    if (con == connector || !con.Selected)
                        continue;

                    con.Selected = false;
                }
            }

            _lastClickedConnector = connector;
        }

        _containerSelected = false;
        ClearSelectedConnectors(isInput ? _outputConnectors : _inputConnectors);
        StartRefreshDebounce();
    }

    private async Task OnContainerEditorRequested()
        => await Show();

    private void OnContainerSelected()
    {
        _containerSelected = true;
        ClearSelectedConnectors(_inputConnectors);
        ClearSelectedConnectors(_outputConnectors);
        StartRefreshDebounce();
    }

    private void OnCurrentContainerNodeSizeChanged(global::Blazor.Diagrams.Core.Models.NodeModel node)
        => _zoomEnabled = _allocationContainerHeight > 0 && node.Size?.Height > _allocationContainerHeight;

    private async Task OnDialogCancel()
    {
        if (_refDialog is null)
            return;

        var oldCts = Interlocked.Exchange(ref _refreshCts, null);
        oldCts?.CancelAsync();
        oldCts?.Dispose();

        _resetElements = true;
        await _refDialog.CloseAsync();
    }

    private async Task OnDialogClosing()
    {
        if (_resetElements)
        {
            ResetContainer();
            ResetConnectors();
        }

        ClusterBuilderEventBuffer.ContainerPropertiesChanged -= OnPropertyChanged;
        ClusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnPropertyChanged;

        ChildContainerMapper.ReloadConnectors(
            ComparerService,
            Datastore.DataflowDiagramMapping.GetModel(_originalContainerNode!),
            _originalContainerNode!,
            Datastore,
            DiagramService
        );

        // Refresh container node in diagram to refresh port relations from changed connectors
        // Events need to be suppressed to ensure it isn't removed again by the datastore
        DiagramService.DiagramState.SuppressEvents = true;
        DiagramService.Diagram.Nodes.Remove(_originalContainerNode!);
        DiagramService.Diagram.Nodes.Add(_originalContainerNode!);

        LinkMapper.ReloadLinks(
            Datastore,
            DiagramService,
            _originalContainerNode!.Connectors
                .SelectMany(c => c)
                .SelectMany(c => c is null ? [] : Datastore.DataflowDiagramMapping.GetModel(c).GetVisibleLinksConnectedToThis())
        );

        DiagramService.DiagramState.SuppressEvents = false;
        _originalContainerNode!.RefreshAll();

        Cleanup();
    }

    private async Task OnDialogOk()
    {
        if (_refDialog is null)
            return;

        var oldCts = Interlocked.Exchange(ref _refreshCts, null);
        oldCts?.CancelAsync();
        oldCts?.Dispose();

        _resetElements = false;
        await _refDialog.CloseAsync();
    }

    private void OnDialogShowing()
    {
        ClusterBuilderEventBuffer.ContainerPropertiesChanged += OnPropertyChanged;
        ClusterBuilderEventBuffer.ConnectorPropertiesChanged += OnPropertyChanged;

        ContainerEditorPropertyGridController.SetInstances([], new ContainerEditorPropertyGridContext());
    }

    private void OnDialogShown()
    {
        if (_scrollContainer is not null)
        {
            _scrollContainer.AllocationContainerSizeChanged += OnAllocationContainerSizeChanged;
            _scrollContainer.ScrolledPixelsChanged += OnScrolledPixelsChanged;
        }
    }

    protected override void OnInitialized()
    {
        ContainerEditorRequest.ContainerEditorRequestedAsync += OnContainerEditorRequested;

        InitializeDiagramService();
        InitializeDiagram();
    }

    private void OnMoveDownClicked(bool isInput)
    {
        var sourceSelection = (isInput ? _inputConnectors : _outputConnectors)
            .Where(c => c.Selected)
            .OrderByDescending(sc => sc.Index)
            .ToArray();

        foreach (var connector in sourceSelection)
            Move(connector, isInput, false);

        StartRefreshDebounce();
    }

    private void OnMoveUpClicked(bool isInput)
    {
        var sourceSelection = (isInput ? _inputConnectors : _outputConnectors)
            .Where(c => c.Selected)
            .OrderBy(sc => sc.Index)
            .ToArray();

        foreach (var connector in sourceSelection)
            Move(connector, isInput, true);

        StartRefreshDebounce();
    }

    private void OnPropertyChanged(IEnumerable<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> _)
        => StartRefreshDebounce();

    private void OnRemovePlaceholderClicked(bool isInput)
    {
        var sourceCollection = isInput ? _inputConnectors : _outputConnectors;
        var sourceSelection = sourceCollection.Where(c => c.Selected).ToList();

        int? previouslySelectedConnectorIndex = null;

        foreach (var connector in sourceSelection)
        {
            if (!connector.IsPlaceholder)
                continue;

            if (!previouslySelectedConnectorIndex.HasValue)
                previouslySelectedConnectorIndex = connector.Index;

            RemovePlaceholder(connector, isInput);
        }

        if (previouslySelectedConnectorIndex.HasValue)
        {
            var connector = sourceCollection.FirstOrDefault(ic => ic.Index == previouslySelectedConnectorIndex);
            connector ??= sourceCollection.LastOrDefault(ic => ic.Index < previouslySelectedConnectorIndex);
            connector?.Selected = true;
        }

        StartRefreshDebounce();
    }

    private void OnScrolledPixelsChanged(int scrolledPixels)
    {
        var deltaY = scrolledPixels - _diagram!.Pan.Y;
        if (_zoomActive)
            _diagram?.UpdatePan(0, deltaY);
    }

    private void OnSelectedConnectorChanged(BlockNodeConnector selectedConnector, bool ctrlKey)
    {
        var sourceConnectors = selectedConnector.IsInput ? _inputConnectors : _outputConnectors;

        if (!ctrlKey)
            ClearSelectedConnectors(sourceConnectors);

        var connector = sourceConnectors.FirstOrDefault(c => c.BlockNodeConnector == selectedConnector);
        connector?.Selected = !connector.Selected;

        ClearSelectedConnectors(selectedConnector.IsInput ? _outputConnectors : _inputConnectors);

        _containerSelected = false;
        StartRefreshDebounce();
    }

    private async Task OnVisibleChanged()
    {
        if (_visible)
            OnDialogShown();
    }

    private void OnZoomClicked()
    {
        var zoomFactor = 1.0;
        var panX = -_diagram!.Pan.X;
        var panY = -_diagram!.Pan.Y;

        if (!_zoomActive)
        {
            zoomFactor = _allocationContainerHeight / _currentContainerNode!.Size!.Height;
            panX = (MaxPreviewWidth - (_currentContainerNode!.Size!.Width * zoomFactor)) / 2;
            panY = _scrollContainer?.ScrolledPixels ?? 0;
        }

        _zoomActive = !_zoomActive;

        _diagram!.Batch(() =>
        {
            _diagram.UpdatePan(panX, panY);
            _diagram.SetZoom(zoomFactor);
        });
    }

    private void Refresh()
    {
        SyncConnectorLists();
        SetButtonStates();
        UpdatePreviewContainer();
        UpdatePropertyGrid();
        StateHasChanged();
    }

    private async Task RefreshAfterDelay()
    {
        _refreshDebounceRunning = true;
        try
        {
            while (true)
            {
                var cts = Volatile.Read(ref _refreshCts);
                if (cts is null)
                    return;

                await Task.Delay(25, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

                if (cts == Volatile.Read(ref _refreshCts))
                {
                    await InvokeAsync(Refresh);
                    return;
                }
            }
        }
        finally
        {
            _refreshDebounceRunning = false;
        }
    }

    private void RemovePlaceholder(ContainerEditorConnector placeholder, bool isInput)
    {
        if (!placeholder.IsPlaceholder)
            return;

        var sourceList = isInput ? _inputConnectors : _outputConnectors;
        var indexOfRemoved = placeholder.Index;
        var connectorsToMove = sourceList
            .Where(con => con.Index > indexOfRemoved)
            .ToArray();

        foreach (var connector in connectorsToMove)
            Move(connector, isInput, true);

        sourceList.Remove(placeholder);
    }

    private void ResetConnectors()
    {
        var changedConnectors = _inputConnectors.Concat(_outputConnectors).Where(c => !c.IsPlaceholder && c.Changed).ToArray();

        foreach (var connector in changedConnectors)
            connector.Index = int.MaxValue - connector.Index;

        foreach (var connector in changedConnectors)
        {
            connector.Description = connector.Backup.Description;
            connector.Index = connector.Backup.Index;
            connector.Name = connector.Backup.Name;
            connector.ShortName = connector.Backup.ShortName;
        }
    }

    private void ResetContainer()
    {
        _currentContainer?.BackColor = _currentContainer.Backup.BackColor;
        _currentContainer?.Description = _currentContainer.Backup.Description;
        _currentContainer?.ForeColor = _currentContainer.Backup.ForeColor;
        _currentContainer?.Name = _currentContainer.Backup.Name;
    }

    private void SetButtonStates()
    {
        var selectedInputConnectors = _inputConnectors.Where(c => c.Selected).ToList();
        _addInputPlaceholderEnabled = GetAddPlaceholderButtonEnabled(selectedInputConnectors);
        _removeInputPlaceholderEnabled = GetRemoveInputPlaceholderButtonEnabled(selectedInputConnectors);
        _inputDownEnabled = GetMoveDownButtonEnabled(_inputConnectors, selectedInputConnectors);
        _inputUpEnabled = GetMoveUpButtonEnabled(selectedInputConnectors);

        var selectedOutputConnectors = _outputConnectors.Where(c => c.Selected).ToList();
        _addOutputPlaceholderEnabled = GetAddPlaceholderButtonEnabled(selectedOutputConnectors);
        _removeOutputPlaceholderEnabled = GetRemoveInputPlaceholderButtonEnabled(selectedOutputConnectors);
        _outputDownEnabled = GetMoveDownButtonEnabled(_outputConnectors, selectedOutputConnectors);
        _outputUpEnabled = GetMoveUpButtonEnabled(selectedOutputConnectors);
    }

    internal async Task Show()
    {
        _originalContainerNode = SelectionManager.SelectedContainers.Count > 0 ? SelectionManager.SelectedContainers[0] : null;
        if (_originalContainerNode is null)
            return;

        var containerModel = Datastore.DataflowDiagramMapping.GetModel(_originalContainerNode);
        _currentContainer = new(Datastore.Builder.Editors.Container)
        {
            Backup = new()
            {
                BackColor = containerModel.BackColor,
                Container = containerModel,
                Description = containerModel.Description,
                ForeColor = containerModel.ForeColor,
                Name = containerModel.Name
            }
        };
        using var cts = new CancellationTokenSource();

        var nameFieldHeight = (await JsRuntime.MeasureNameFieldHeights([containerModel.Name], cts.Token))[0];
        _currentContainerNode = ChildContainerMapper.CreateNode(ComparerService, Datastore, _diagramService!, containerModel, nameFieldHeight);
        _currentContainerNode.Position = new(0, 0);

        _listsHeightOffset = Convert.ToInt32((_currentContainerNode.NameFieldHeight + (5 * BlockNodeLayout.RowHeight)) * UiScaleFactor);

        InitializeConnectorLists();

        _diagram?.Nodes.Add(_currentContainerNode);
        _currentContainerNode.SizeChanged += OnCurrentContainerNodeSizeChanged;

        if (_refDialog is not null)
            await _refDialog.ShowAsync();
    }

    private void StartRefreshDebounce()
    {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _refreshCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        if (!_refreshDebounceRunning)
            _ = RefreshAfterDelay();
    }

    private void SyncConnectorLists()
    {
        var minimumConnectorRows = BlockNodeLayout.MinimumConnectorRows - BlockNodeLayout.SystemConnectorRows;

        var highestInputIndex = _inputConnectors.FindLastIndex(c => !c.IsPlaceholder);
        var highestOutputIndex = _outputConnectors.FindLastIndex(c => !c.IsPlaceholder);
        var highestRealIndex = Math.Max(highestInputIndex, highestOutputIndex);

        var requiredRows = Math.Max(highestRealIndex + 1, minimumConnectorRows);

        for (var lc = 0; lc < 2; lc++)
        {
            var sourceCollection = lc == 0 ? _inputConnectors : _outputConnectors;

            // Trim trailing placeholders beyond the required row count
            while (sourceCollection.Count > requiredRows && sourceCollection[^1].IsPlaceholder)
                sourceCollection.RemoveAt(sourceCollection.Count - 1);

            // Fill up to required row count
            for (var i = sourceCollection.Count; i < requiredRows; i++)
            {
                sourceCollection.Add(new(Datastore.Builder.Editors.Connector)
                {
                    Backup = ContainerEditorConnectorBackup.Empty,
                    Index = i,
                    IsPlaceholder = true,
                });
            }
        }
    }

    private void UpdatePreviewContainer()
    {
        if (_currentContainerNode is null)
            return;

        _currentContainerNode.Name = _currentContainer?.Name ?? string.Empty;
        _currentContainerNode.NameBackgroundColor = _currentContainer?.BackColor ?? BlockNodeColors.BackgroundDefault;
        _currentContainerNode.NameForeColor = _currentContainer?.ForeColor ?? BlockNodeColors.ForegroundDefault;

        var requiredConnectorRowCount = GetRequiredPreviewContainerConnectorRowCount();
        if (requiredConnectorRowCount > _currentContainerNode.Connectors.Count)
        {
            var addCount = requiredConnectorRowCount - _currentContainerNode.Connectors.Count;
            for (var i = 0; i < addCount; i++)
                _currentContainerNode.Connectors.Add(new BlockNodeConnector[2]);

            _currentContainerNode.InvalidateConnectorsCache();
        }
        else if (requiredConnectorRowCount < _currentContainerNode.Connectors.Count)
        {
            for (var i = _currentContainerNode.Connectors.Count - 1; i > requiredConnectorRowCount - 1; i--)
                _currentContainerNode.Connectors.Remove(_currentContainerNode.Connectors[i]);

            _currentContainerNode.InvalidateConnectorsCache();
        }

        UpdatePreviewContainerConnectors(_currentContainerNode, ConnectorSide.Input, _inputConnectors);
        UpdatePreviewContainerConnectors(_currentContainerNode, ConnectorSide.Output, _outputConnectors);

        UpdatePreviewContainerConnectorSelection(_inputConnectors);
        UpdatePreviewContainerConnectorSelection(_outputConnectors);

        _currentContainerNode.Refresh();
    }

    private static void UpdatePreviewContainerConnectors(ChildContainerNode containerNode, ConnectorSide connectorSide, IEnumerable<ContainerEditorConnector> connectors)
    {
        var containerNodeConnectorIndex = BlockNodeLayout.SystemConnectorRows;
        var blockNodeConnectorArrayIndex = connectorSide == ConnectorSide.Input ? 0 : 1;

        foreach (var connector in connectors)
        {
            if (containerNodeConnectorIndex < containerNode.Connectors.Count)
                containerNode.Connectors[containerNodeConnectorIndex++][blockNodeConnectorArrayIndex] = connector.BlockNodeConnector;
        }
    }

    private static void UpdatePreviewContainerConnectorSelection(IEnumerable<ContainerEditorConnector> connectors)
    {
        foreach (var connector in connectors)
            connector.BlockNodeConnector?.SetSelection(connector.Selected);
    }

    private void UpdatePropertyGrid()
    {
        if (_containerSelected)
        {
            ContainerEditorPropertyGridController.SetInstances([_currentContainer!], new ContainerEditorPropertyGridContext());
            return;
        }

        var selection = _inputConnectors.Concat(_outputConnectors).Where(c => c.Selected).ToList();
        if (selection.All(c => !c.IsPlaceholder))
            ContainerEditorPropertyGridController.SetInstances(selection, new());
        else
            ContainerEditorPropertyGridController.SetInstances([], new());
    }
}
