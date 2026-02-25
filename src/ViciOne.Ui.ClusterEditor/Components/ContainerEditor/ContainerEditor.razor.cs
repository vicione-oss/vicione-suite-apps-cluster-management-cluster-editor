using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Behaviors;
using DevExpress.XtraRichEdit.Layout.Engine;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Behaviors;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Components;
using ViciOne.Ui.Shared.Dx.Components.Scrolling;
using ViciOne.Ui.Shared.Dx.Services;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor;

public sealed partial class ContainerEditor : ComponentBase, IDisposable
{
    private const int MaxPreviewWidth = 192;
    private const float UiScaleFactor = 1.2f;

    private double _allocationContainerHeight;
    private ChildContainerNode? _currentContainerNode;
    private BlazorDiagram? _diagram;
    private DiagramService? _diagramService;
    private readonly List<ContainerEditorConnector> _inputConnectors = [];
    private bool _inputDownEnabled;
    private bool _inputUpEnabled;
    private int _listsHeightOffset;
    private ChildContainerNode? _originalContainerNode;
    private readonly List<ContainerEditorConnector> _outputConnectors = [];
    private bool _outputDownEnabled;
    private bool _outputUpEnabled;
    private DxDialog? _refDialog;
    private readonly Timer _refreshTimer = new()
    {
        AutoReset = false,
        Enabled = false,
        Interval = 25,
    };
    private bool _removeInputPlaceholderEnabled;
    private bool _removeOutputPlaceholderEnabled;
    private ScrollContainer? _scrollContainer;
    private CESelectionBehavior? _selectionBehavior;
    private bool _zoomActive;
    private bool _zoomEnabled;
    private VOZoomToFitBehavior? _zoomToFitBehavior;

    [Inject] private ClusterBuilderEventBuffer ClusterBuilderEventBuffer { get; set; } = default!;
    [Inject] private ComparerService ComparerService { get; set; } = default!;
    [Inject] private IPropertyGridController<ContainerEditorPropertyGridContext> ContainerEditorPropertyGridController { get; set; } = default!;
    [Inject] private IContainerEditorRequest ContainerEditorRequest { get; set; } = default!;
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private Datastore Datastore { get; set; } = default!;
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
            var connectorsToMove = sourceCollection.Where(sc => sc.Index >= sourceSelection.First().Index)
                .OrderByDescending(sc => sc.Index)
                .ToArray();
            sourceCollection.Add(placeholder);

            foreach (var movingConnector in connectorsToMove)
                Move(movingConnector, false, isInput);
        }
        else
        {
            sourceCollection.Add(placeholder);
            _scrollContainer?.SetAutoscrollToEnd();
        }

        sourceSelection.Clear();
        sourceSelection.Add(placeholder);

        StartRefreshTimer();
    }

    private void Cleanup()
    {
        if (_currentContainerNode is not null)
        {
            _currentContainerNode.SizeChanged -= OnCurrentContainerNodeSizeChanged;
            _diagram?.Nodes.Remove(_currentContainerNode);
            _currentContainerNode = null;
        }

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
        ClusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnPropertyChanged;
        ContainerEditorRequest.ContainerEditorRequestedAsync -= OnContainerEditorRequestedAsync;

        _currentContainerNode?.SizeChanged -= OnCurrentContainerNodeSizeChanged;

        if (_scrollContainer is not null)
        {
            _scrollContainer.AllocationContainerSizeChanged -= OnAllocationContainerSizeChangedAsync;
            _scrollContainer.ScrolledPixelsChanged -= OnScrolledPixelsChanged;
            _scrollContainer.Dispose();
        }

        _selectionBehavior?.SelectedConnectorChanged -= OnSelectedConnectorChanged;

        Cleanup();

        _diagramService?.Dispose();
        _selectionBehavior?.Dispose();
        _zoomToFitBehavior?.Dispose();

        _refreshTimer.Elapsed -= OnRefreshTimerElapsedAsync;
        _refreshTimer.Dispose();
    }

    private int GetHighestConnectorIndex()
        => Math.Max(
            _inputConnectors.Any(c => !c.IsPlaceholder) ? _inputConnectors.IndexOf(_inputConnectors.Last(c => !c.IsPlaceholder)) : 0,
            _outputConnectors.Any(c => !c.IsPlaceholder) ? _outputConnectors.IndexOf(_outputConnectors.Last(c => !c.IsPlaceholder)) : 0
        );

    private static bool GetMoveDownButtonEnabled(List<ContainerEditorConnector> connectors, IList<ContainerEditorConnector> selectedConnectors)
        => selectedConnectors.Any() && selectedConnectors.All(c => c.Index < connectors.Count - 1);

    private static bool GetMoveUpButtonEnabled(IList<ContainerEditorConnector> selectedConnectors)
        => selectedConnectors.Any() && selectedConnectors.All(c => c.Index > 0);

    private static bool GetRemoveInputPlaceholderButtonEnabled(IEnumerable<ContainerEditorConnector> connectors, IList<ContainerEditorConnector> selectedConnectors)
    {
        if (selectedConnectors is null || connectors is null)
            return false;

        var minimalNeededIndex = BlockNodeLayout.MinimumConnectorRows - BlockNodeLayout.SystemConnectorRows - 1;

        return selectedConnectors.Any()
            && selectedConnectors.All(c => c.IsPlaceholder)
            && connectors.Any(c => c.Index > minimalNeededIndex);
    }

    private int GetRequiredPreviewContainerConnectorRowCount()
    {
        var previewContainerConnectorRowCount = GetHighestConnectorIndex() + BlockNodeLayout.SystemConnectorRows + 1;

        var requiredConnectorRowCount = Math.Max(previewContainerConnectorRowCount, BlockNodeLayout.MinimumConnectorRows);

        return requiredConnectorRowCount;
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
        _selectionBehavior.SelectedConnectorChanged += OnSelectedConnectorChanged;

        _zoomToFitBehavior = new(_diagram);
        _diagram.RegisterBehavior(_zoomToFitBehavior);

        _diagramService?.Diagram = _diagram;
    }

    private void InitializeDiagramService()
        => _diagramService = new(Datastore, new());

    private void Move(ContainerEditorConnector connector, bool moveUp, bool isInput)
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

        StartRefreshTimer();
    }

    private async void OnAllocationContainerSizeChangedAsync(Size size)
    {
        _allocationContainerHeight = size.Height;

        if (_allocationContainerHeight > 0)
            _zoomEnabled = _currentContainerNode?.Size?.Height > _allocationContainerHeight;

        await InvokeAsync(StateHasChanged);
    }

    private void OnConnectorClicked(ContainerEditorConnector connector, bool ctrlKey, bool isInput)
    {
        connector.Selected ^= true; // toggle selection

        if (!ctrlKey)
        {
            var sourceSelection = isInput ? _inputConnectors : _outputConnectors;
            foreach (var con in sourceSelection)
            {
                if (con == connector || !con.Selected)
                    continue;

                con.Selected = false;
            }
        }

        ClearSelectedConnectors(isInput ? _outputConnectors : _inputConnectors);
        StartRefreshTimer();
    }

    private async Task OnContainerEditorRequestedAsync()
        => await ShowAsync();

    private void OnCurrentContainerNodeSizeChanged(global::Blazor.Diagrams.Core.Models.NodeModel node)
        => _zoomEnabled = _allocationContainerHeight > 0 && node.Size?.Height > _allocationContainerHeight;

    private async Task OnDialogCancelAsync()
    {
        if (_refDialog is null)
            return;

        ResetConnectors();
        await _refDialog.CloseAsync();
    }

    private async Task OnDialogClosingAsync()
    {
        ClusterBuilderEventBuffer.ConnectorPropertiesChanged -= OnPropertyChanged;

        await ChildContainerMapper.ReloadConnectorsAsync(
            ComparerService,
            Datastore.DataflowDiagramMapping.GetModel(_originalContainerNode!),
            _originalContainerNode!,
            Datastore,
            DiagramService,
            JsRuntime
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

    private async Task OnDialogOkAsync()
    {
        if (_refDialog is null)
            return;

        await _refDialog.CloseAsync();
    }

    private void OnDialogShowing()
    {
        ClusterBuilderEventBuffer.ConnectorPropertiesChanged += OnPropertyChanged;

        ContainerEditorPropertyGridController.SetInstances([], new ContainerEditorPropertyGridContext());
    }

    private void OnDialogShown()
    {
        if (_scrollContainer is not null)
        {
            _scrollContainer.AllocationContainerSizeChanged += OnAllocationContainerSizeChangedAsync;
            _scrollContainer.ScrolledPixelsChanged += OnScrolledPixelsChanged;
        }
    }

    protected override void OnInitialized()
    {
        ContainerEditorRequest.ContainerEditorRequestedAsync += OnContainerEditorRequestedAsync;

        InitializeDiagramService();
        InitializeDiagram();
    }

    private void OnMoveDownClicked(bool isInput)
    {
        var sourceCollection = isInput ? _inputConnectors : _outputConnectors;
        var sourceSelection = sourceCollection.Where(c => c.Selected).ToList();
        foreach (var connector in sourceSelection.OrderByDescending(con => con.Index))
            Move(connector, false, isInput);
    }

    private void OnMoveUpClicked(bool isInput)
    {
        var sourceCollection = isInput ? _inputConnectors : _outputConnectors;
        var sourceSelection = sourceCollection.Where(c => c.Selected).ToList();
        foreach (var connector in sourceSelection.OrderBy(con => con.Index))
            Move(connector, true, isInput);
    }

    private void OnPropertyChanged(IEnumerable<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> _)
        => StartRefreshTimer();

    private async void OnRefreshTimerElapsedAsync(object? sender, ElapsedEventArgs e)
    {
        _refreshTimer.Stop();
        await RefreshAsync();
    }

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

            if (connector is not null)
                sourceSelection.Add(connector);
        }

        StartRefreshTimer();
    }

    private void OnScrolledPixelsChanged(int scrolledPixels)
    {
        var deltaY = scrolledPixels - _diagram!.Pan.Y;
        if (_zoomActive)
            _diagram?.UpdatePan(0, deltaY);
    }

    private void OnSelectedConnectorChanged(BlockNodeConnector selectedConnector, bool ctrlKey)
    {
        var isInput = selectedConnector.IsInput;
        var sourceConnectors = selectedConnector.IsInput ? _inputConnectors : _outputConnectors;

        if (!ctrlKey)
            ClearSelectedConnectors(sourceConnectors);

        var connector = sourceConnectors.FirstOrDefault(c => c.BlockNodeConnector == selectedConnector);
        connector?.Selected = !connector.Selected;

        ClearSelectedConnectors(isInput ? _outputConnectors : _inputConnectors);

        StartRefreshTimer();
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

    private async Task RefreshAsync()
    {
        SyncConnectorLists();
        SetButtonStates();
        UpdatePreviewContainer();
        UpdatePropertyGrid();
        await InvokeAsync(StateHasChanged);
    }

    private void RemovePlaceholder(ContainerEditorConnector placeholder, bool isInput)
    {
        if (!placeholder.IsPlaceholder)
            return;

        var sourceCollection = isInput ? _inputConnectors : _outputConnectors;
        var indexOfRemoved = placeholder.Index;

        foreach (var connector in sourceCollection.Where(con => con.Index > indexOfRemoved).ToArray())
            Move(connector, true, isInput);

        sourceCollection.Remove(placeholder);

        StartRefreshTimer();
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

    private void SetButtonStates()
    {
        var selectedInputConnectors = _inputConnectors.Where(c => c.Selected).ToList();
        _removeInputPlaceholderEnabled = GetRemoveInputPlaceholderButtonEnabled(_inputConnectors, selectedInputConnectors);
        _inputDownEnabled = GetMoveDownButtonEnabled(_inputConnectors, selectedInputConnectors);
        _inputUpEnabled = GetMoveUpButtonEnabled(selectedInputConnectors);

        var selectedOutputConnectors = _outputConnectors.Where(c => c.Selected).ToList();
        _removeOutputPlaceholderEnabled = GetRemoveInputPlaceholderButtonEnabled(_outputConnectors, selectedOutputConnectors);
        _outputDownEnabled = GetMoveDownButtonEnabled(_outputConnectors, selectedOutputConnectors);
        _outputUpEnabled = GetMoveUpButtonEnabled(selectedOutputConnectors);
    }

    internal async Task ShowAsync()
    {
        _originalContainerNode = SelectionManager.SelectedContainers.FirstOrDefault();

        if (_originalContainerNode is null)
            return;

        var containerModel = Datastore.DataflowDiagramMapping.GetModel(_originalContainerNode);
        using var cts = new System.Threading.CancellationTokenSource();
        _currentContainerNode = await ChildContainerMapper.CreateNodeAsync(ComparerService, containerModel, Datastore, _diagramService!, JsRuntime, cts.Token);

        _currentContainerNode.Position = new(0, 0);

        _listsHeightOffset = Convert.ToInt32((_currentContainerNode.NameFieldHeight + (5 * BlockNodeLayout.RowHeight)) * UiScaleFactor);

        InitializeConnectorLists();

        _diagram?.Nodes.Add(_currentContainerNode);
        _currentContainerNode.SizeChanged += OnCurrentContainerNodeSizeChanged;

        _refreshTimer.Elapsed += OnRefreshTimerElapsedAsync;

        await _refDialog!.OpenAsync();
    }

    private void StartRefreshTimer()
    {
        _refreshTimer.Stop();
        _refreshTimer.Start();
    }

    private void SyncConnectorLists()
    {
        var minimumConnectorRows = BlockNodeLayout.MinimumConnectorRows - BlockNodeLayout.SystemConnectorRows;
        var rowCount = Math.Max(GetHighestConnectorIndex() + 1, minimumConnectorRows);

        for (var lc = 0; lc < 2; lc++)
        {
            var sourceCollection = lc == 0 ? _inputConnectors : _outputConnectors;
            if (sourceCollection.Count < rowCount)
            {
                for (var i = sourceCollection.Count; i < rowCount; i++)
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
    }

    private void UpdatePreviewContainer()
    {
        if (_currentContainerNode is null)
            return;

        var requiredConnectorRowCount = GetRequiredPreviewContainerConnectorRowCount();
        if (requiredConnectorRowCount > _currentContainerNode.Connectors.Count)
        {
            var addCount = requiredConnectorRowCount - _currentContainerNode.Connectors.Count;
            for (var i = 0; i < addCount; i++)
                _currentContainerNode.Connectors.Add(new BlockNodeConnector[2]);
        }
        else if (requiredConnectorRowCount < _currentContainerNode.Connectors.Count)
        {
            for (var i = _currentContainerNode.Connectors.Count - 1; i > requiredConnectorRowCount - 1; i--)
                _currentContainerNode.Connectors.Remove(_currentContainerNode.Connectors[i]);
        }

        UpdatePreviewContainerConnectors(_currentContainerNode, ConnectorSide.Input, _inputConnectors);
        UpdatePreviewContainerConnectors(_currentContainerNode, ConnectorSide.Output, _outputConnectors);

        _currentContainerNode.Refresh();
    }

    private static void UpdatePreviewContainerConnectors(ChildContainerNode containerNode, ConnectorSide connectorSide, IEnumerable<ContainerEditorConnector> connectors)
    {
        var containerNodeConnectorIndex = BlockNodeLayout.SystemConnectorRows;
        var blockNodeConnectorArrayIndex = connectorSide == ConnectorSide.Input ? 0 : 1;

        foreach (var connector in connectors)
        {
            connector.BlockNodeConnector?.SetSelection(connector.Selected);

            if (containerNodeConnectorIndex < containerNode.Connectors.Count)
                containerNode.Connectors[containerNodeConnectorIndex++][blockNodeConnectorArrayIndex] = connector.BlockNodeConnector;
        }
    }

    private void UpdatePropertyGrid()
    {
        var selection = _inputConnectors.Concat(_outputConnectors).Where(c => c.Selected).ToList();
        if (selection.All(c => !c.IsPlaceholder))
            ContainerEditorPropertyGridController.SetInstances(selection, new());
        else
            ContainerEditorPropertyGridController.SetInstances([], new());
    }
}
