using System;
using System.Threading.Tasks;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models.Base;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.DiagramComponents;

public sealed partial class LabelComponent : ComponentBase
{
    private ResizingDirection _currentResizingDirection;
    private Label _labelModel = default!;
    private MarkdownPipeline _markdigPipeline = default!;

    [CascadingParameter] internal Diagram? Diagram { get; set; }

    [Inject] private IContextMenuSettings ContextMenuSettings { get; set; } = default!;
    [Inject] private Datastore Datastore { get; set; } = default!;
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private DiagramService DiagramService { get; set; } = default!;

    [Inject] private IContextMenuRequest<NodeEditorContextMenuContext> NodeEditorContextMenuRequest { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    [Parameter] public LabelNode? Node { get; set; }

    private static Point CalculateLocation(Point point, Rectangle bounds, int minimumSize, ResizingDirection labelResizingDirection)
    {
        var x = bounds.Left;
        var y = bounds.Top;

        if (labelResizingDirection.HasFlag(ResizingDirection.Left))
            x = bounds.Right - point.X >= minimumSize ? point.X : bounds.Right - minimumSize;

        if (labelResizingDirection.HasFlag(ResizingDirection.Top))
            y = bounds.Bottom - point.Y >= minimumSize ? point.Y : bounds.Bottom - minimumSize;

        return new(x, y);
    }

    private static Size CalculateSize(Point point, Rectangle bounds, int minimumSize, ResizingDirection labelResizingDirection)
    {
        var height = bounds.Height;
        var width = bounds.Width;

        if (labelResizingDirection.HasFlag(ResizingDirection.Top))
            height = bounds.Bottom - point.Y >= minimumSize ? bounds.Bottom - point.Y : minimumSize;

        if (labelResizingDirection.HasFlag(ResizingDirection.Bottom))
            height = point.Y - bounds.Top >= minimumSize ? point.Y - bounds.Top : minimumSize;

        if (labelResizingDirection.HasFlag(ResizingDirection.Left))
            width = bounds.Right - point.X >= minimumSize ? bounds.Right - point.X : minimumSize;

        if (labelResizingDirection.HasFlag(ResizingDirection.Right))
            width = point.X - bounds.Left >= minimumSize ? point.X - bounds.Left : minimumSize;

        return new(width, height);
    }

    private async Task OnContainerContextMenuAsync(MouseEventArgs e)
    {
        if (DiagramEventService.ContextMenuAllowed())
        {
            await NodeEditorContextMenuRequest.SendAsync(new()
            {
                ItemFilter = SelectionManager.GetContextMenuItemFilterForSelection(),
                MouseEventArgs = e,
                ObjectOpenedOn = Node
            });
        }
    }

    private void OnDiagramPointerLeave()
        => StopResizing();

    private void OnDiagramPointerMove(Model? _, global::Blazor.Diagrams.Core.Events.PointerEventArgs args)
        => Resize(new(args.ClientX, args.ClientY), _currentResizingDirection);

    private void OnDiagramPointerUp(Model? _, global::Blazor.Diagrams.Core.Events.PointerEventArgs _2)
        => StopResizing();

    private void OnEdgeDraggingPointerMove(MouseEventArgs args)
        => Resize(new(args.ClientX, args.ClientY), _currentResizingDirection);

    private void OnEdgeDraggingPointerUp(MouseEventArgs obj)
        => StopResizing();

    protected override void OnInitialized()
    {
        ArgumentNullException.ThrowIfNull(Diagram, nameof(Diagram));
        ArgumentNullException.ThrowIfNull(Node, nameof(Node));

        _labelModel = Datastore.DataflowDiagramMapping.GetModel(Node!);

        _markdigPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Node, nameof(Node));

    private void Resize(Point newPoint, ResizingDirection labelResizingDirection)
    {
        var bounds = new Rectangle(new(_labelModel.X, _labelModel.Y), new(_labelModel.Width, _labelModel.Height));
        var minimumSize = Datastore.Builder.Settings.LabelMinimumSize;
        var point = Diagram!.GetRelativeGridPoint(newPoint, Datastore);

        var location = CalculateLocation(point, bounds, minimumSize, labelResizingDirection);
        var size = CalculateSize(point, bounds, minimumSize, labelResizingDirection);

        Diagram!.Batch(() =>
        {
            Node!.SetPosition(location.X, location.Y);
            Node.Width = (int)size.Width;
            Node.Height = (int)size.Height;
            Node.Size = new(size.Width, size.Height);
        });
    }

    private void SetSelectedItemsLockingState(bool locked)
    {
        foreach (var label in SelectionManager.SelectedLabels)
            label.Locked = locked;

        foreach (var blockNode in SelectionManager.SelectedBlockNodes)
            blockNode.Locked = locked;
    }

    private void StartResizing(ResizingDirection resizingDirection)
    {
        _currentResizingDirection = resizingDirection;

        // The diagram elements are locked here to avoid moving them with the
        // 'DragMovablesBehavior' which is also executed while resizing the element
        SetSelectedItemsLockingState(true);

        DiagramEventService.RequestEdgeDraggingVisibilityChange(true);
        DiagramService.SetNodeAlignmentBorderVisibility(true);

        Diagram!.PointerMove += OnDiagramPointerMove;
        Diagram!.PointerUp += OnDiagramPointerUp;
        DiagramEventService.DiagramPointerLeave += OnDiagramPointerLeave;
        DiagramEventService.EdgeDraggingPointerMove += OnEdgeDraggingPointerMove;
        DiagramEventService.EdgeDraggingPointerUp += OnEdgeDraggingPointerUp;
    }

    private void StartTextEdit()
        => Node!.ProcessTextEditStarted();

    private void StopResizing()
    {
        var labelEditor = Datastore.Builder.Editors.Label;
        labelEditor.SetLocation(_labelModel, new((int)Node!.Position.X, (int)Node.Position.Y));
        labelEditor.SetSize(_labelModel, new(Node.Width, Node.Height));

        Diagram!.PointerMove -= OnDiagramPointerMove;
        Diagram!.PointerUp -= OnDiagramPointerUp;
        DiagramEventService.DiagramPointerLeave -= OnDiagramPointerLeave;
        DiagramEventService.EdgeDraggingPointerMove -= OnEdgeDraggingPointerMove;
        DiagramEventService.EdgeDraggingPointerUp -= OnEdgeDraggingPointerUp;

        DiagramEventService.RequestEdgeDraggingVisibilityChange(false);
        DiagramService.SetNodeAlignmentBorderVisibility(false);
        SetSelectedItemsLockingState(false);
    }
}
