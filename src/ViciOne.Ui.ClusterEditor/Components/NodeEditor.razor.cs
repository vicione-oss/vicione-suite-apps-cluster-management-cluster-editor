using System;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class NodeEditor : ComponentBase, IDisposable
{
    private BlazorDiagram? _diagram;
    private bool _isNodeAlignmentBorderVisible;
    private LabelEditor? _labelEditor;

    [Inject] private NodeEditorBehaviorController BehaviorController { get; set; } = default!;
    [Inject] private ConnectorMarkerDeletionController ConnectorMarkerDeletionController { get; set; } = default!;
    [Inject] private IContextMenuRequest<NodeEditorContextMenuContext> ContextMenuRequest { get; set; } = default!;
    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramModelSyncController DiagramModelSyncController { get; set; } = default!;
    [Inject] private DiagramPointerInteractionController DiagramPointerInteractionController { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;
    [Inject] private NodeEditorJsInterop JsInterop { get; set; } = default!;
    [Inject] private LabelEditingController LabelEditingController { get; set; } = default!;
    [Inject] private LibraryGhostDragController LibraryGhostDragController { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    public void Dispose()
    {
        DiagramEventService.ContainerLoaded -= OnContainerLoaded;
        DiagramEventService.DiagramFocusRequested -= OnDiagramFocusRequested;
        DiagramEventService.GridModeChangeRequested -= OnGridModeChangeRequested;
        DiagramEventService.NodeAlignmentBorderVisibilityChanged -= OnNodeAlignmentBorderVisibilityChanged;
        DiagramEventService.ContextMenuAllowed = () => true;

        DiagramModelSyncController.LabelEditModeStarted -= LabelEditingController.Show;

        DiagramPointerInteractionController.Dispose();
        DiagramModelSyncController.Dispose();
        LibraryGhostDragController.Dispose();
        ConnectorMarkerDeletionController.Dispose();
        LabelEditingController.Dispose();

        BehaviorController.Dispose();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            LabelEditingController.AttachEditor(_labelEditor!);
    }

    private Task OnContainerLoaded(Container container)
    {
        // If the container had saved values for all of ViewportX & ViewportY & Zoom
        // then we set these values in Datastore.LoadContainerSafely after clearing the Diagram
        // and before we add any new nodes to the Diagram.
        // This optimizes the loading procedure as the Diagram doesn't has to calculate the size of the
        // nodes again and render them twice (possibly leading to bugs because the size calculation is async).
        // The code below is the fallback when any of these values are missing.
        var hasSavedViewport = container.ViewportX.HasValue && container.ViewportY.HasValue && container.Zoom.HasValue;

        if (!hasSavedViewport)
        {
            if (_diagram!.Nodes.Any())
            {
                if (!_diagram!.ArePartialVisibleNodesInViewport(DiagramService.Diagram.Nodes))
                    DiagramEventService.RequestZoomToFit();
            }
            else
            {
                _diagram!.SetPan(Point.Zero.X, Point.Zero.Y);
                _diagram!.SetZoom(DiagramSettings.DefaultZoom);
            }
        }

        if (DiagramService.DiagramState.LabelsLocked)
        {
            foreach (var node in _diagram!.Nodes.OfType<LabelNode>())
            {
                node.Locked = DiagramService.DiagramState.LabelsLocked;
                node.Refresh();
            }
        }

        return Task.CompletedTask;
    }

    private Task OnDiagramFocusRequested()
        => JsInterop.FocusDiagramCanvasAsync();

    private async Task OnGridModeChangeRequested(GridMode gridMode)
        => await InvokeAsync(StateHasChanged);

    protected override void OnInitialized()
    {
        _diagram = NodeEditorDiagramFactory.Create();
        DiagramService.Diagram = _diagram;

        BehaviorController.Initialize(_diagram);

        void RequestRender() => InvokeAsync(StateHasChanged);
        DiagramPointerInteractionController.Initialize(_diagram, RequestRender);
        DiagramModelSyncController.Initialize(_diagram);
        LibraryGhostDragController.Initialize(_diagram, RequestRender);
        ConnectorMarkerDeletionController.Initialize();
        DiagramModelSyncController.LabelEditModeStarted += LabelEditingController.Show;

        DiagramEventService.ContainerLoaded += OnContainerLoaded;
        DiagramEventService.DiagramFocusRequested += OnDiagramFocusRequested;
        DiagramEventService.GridModeChangeRequested += OnGridModeChangeRequested;
        DiagramEventService.NodeAlignmentBorderVisibilityChanged += OnNodeAlignmentBorderVisibilityChanged;
        DiagramEventService.ContextMenuAllowed = DiagramPointerInteractionController.ContextMenuAllowed;

        DiagramService.DiagramState.IsInitialized = true;
        SelectionManager.AttachDiagramEvents();
    }

    private async Task OnNodeAlignmentBorderVisibilityChanged(bool isVisible)
    {
        _isNodeAlignmentBorderVisible = isVisible;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ShowContextMenu(MouseEventArgs e)
    {
        if (DiagramPointerInteractionController.ContextMenuAllowed())
            await ContextMenuRequest.SendAsync(new() { ItemFilter = SelectionManager.GetContextMenuItemFilterForSelection(), MouseEventArgs = e });
    }
}
