using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices;

public sealed class DiagramEventService
{
    public Func<bool> ContextMenuAllowed { get; set; } = () => true;

    public event Action? BlockNodesUpdateRequested;
    public event Func<Task>? CloseContextMenuRequested;
    public event Action<ChildContainer>? ContainerAdded;
    public event Action<Container>? ContainerLoaded;
    public event Action<ChildContainer>? ContainerRemoved;
    public event Func<Task>? DiagramFocusRequested;
    public event Action? DiagramPointerLeave;
    public event Action<IDatastore, IConnector?>? DraggingLinkChanged;
    public event Action<MouseEventArgs>? EdgeDraggingPointerMove;
    public event Action<MouseEventArgs>? EdgeDraggingPointerUp;
    public event Action<bool>? EdgeDraggingVisibilityChangeRequested;
    public event Action<WheelEventArgs>? EdgeDraggingWheel;
    public event Action? FilterAttachedRequested;
    public event Action? FilterClearRequested;
    public event Action? FilterSelectedRequested;
    public event Action? FunctionBlockRemoved;
    public event Func<GridMode, Task>? GridModeChangeRequested;
    public event Action? MinimapColoringChanged;
    public event Action<bool>? MinimapVisibilityChangeRequested;
    public event Action<bool>? NodeAlignmentBorderVisibilityChanged;
    public event Action<bool>? PanBehaviorChangeRequested;
    public event Action<bool>? SimplifiedViewChangeRequested;
    public event Action<double>? ZoomChanged;
    public event Action? ZoomToFitRequested;

    public void InvokeBlockNodesUpdateRequested()
        => BlockNodesUpdateRequested?.Invoke();

    public void InvokeContainerAdded(ChildContainer container)
        => ContainerAdded?.Invoke(container);

    public void InvokeContainerLoaded(Container container)
        => ContainerLoaded?.Invoke(container);

    public void InvokeContainerRemoved(ChildContainer container)
        => ContainerRemoved?.Invoke(container);

    public bool InvokeContextMenuAllowed()
        => ContextMenuAllowed.Invoke();

    public void InvokeDiagramPointerLeave()
        => DiagramPointerLeave?.Invoke();

    public void InvokeDraggingLinkChanged(IDatastore datastore, IConnector? connector)
        => DraggingLinkChanged?.Invoke(datastore, connector);

    public void InvokeEdgeDraggingPointerMove(PointerEventArgs e)
        => EdgeDraggingPointerMove?.Invoke(e);

    public void InvokeEdgeDraggingPointerUp(PointerEventArgs e)
        => EdgeDraggingPointerUp?.Invoke(e);

    public void InvokeEdgeDraggingWheel(WheelEventArgs e)
        => EdgeDraggingWheel?.Invoke(e);

    public void InvokeFunctionBlockRemoved()
        => FunctionBlockRemoved?.Invoke();

    public void InvokeMinimapColoringChanged()
        => MinimapColoringChanged?.Invoke();

    public void InvokeNodeAlignmentBorderVisibilityChanged(bool isVisible)
        => NodeAlignmentBorderVisibilityChanged?.Invoke(isVisible);

    public void InvokeZoomChanged(double newZoom)
        => ZoomChanged?.Invoke(newZoom);

    public Task RequestCloseContextMenu()
    {
        if (CloseContextMenuRequested is null)
            return Task.CompletedTask;

        var handlers = CloseContextMenuRequested.GetInvocationList().Cast<Func<Task>>();
        return Task.WhenAll(handlers.Select(h => h()));
    }

    public void RequestDiagramFocus()
        => DiagramFocusRequested?.Invoke();

    public void RequestEdgeDraggingVisibilityChange(bool isVisible)
        => EdgeDraggingVisibilityChangeRequested?.Invoke(isVisible);

    public void RequestFilterAttached()
        => FilterAttachedRequested?.Invoke();

    public void RequestFilterClear()
        => FilterClearRequested?.Invoke();

    public void RequestFilterSelected()
        => FilterSelectedRequested?.Invoke();

    public void RequestGridModeChange(GridMode mode)
        => GridModeChangeRequested?.Invoke(mode);

    public void RequestMinimapVisibilityChange(bool isVisible)
        => MinimapVisibilityChangeRequested?.Invoke(isVisible);

    public void RequestPanBehaviorChange(bool useGimpPanBehavior)
        => PanBehaviorChangeRequested?.Invoke(useGimpPanBehavior);

    public void RequestSimplifiedViewChange(bool simplifiedView)
        => SimplifiedViewChangeRequested?.Invoke(simplifiedView);

    public void RequestZoomToFit()
        => ZoomToFitRequested?.Invoke();
}
