using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.Library.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class LibraryGhostDragController(
    ILibraryService libraryService,
    IDatastore datastore,
    DiagramEventService diagramEventService,
    DiagramService diagramService,
    SelectionManager selectionManager,
    NodeEditorJsInterop jsInterop,
    NodeEditorBehaviorController behaviorController) : IDisposable
{
    private BlazorDiagram? _diagram;
    private readonly List<FunctionBlockNode> _draggingNodes = [];
    private CancellationTokenSource? _ghostDragCts;
    private Task? _ghostDragEnterTask;
    private bool _initialized;
    private bool _libraryDragInProgress;
    private Action _requestRender = () => { };

    public bool DragInProgress => _libraryDragInProgress;

    public void Dispose()
        => Teardown();

    public void Initialize(BlazorDiagram diagram, Action requestRender)
    {
        if (_initialized)
            Teardown();

        _diagram = diagram;
        _requestRender = requestRender;

        libraryService.DragEnded += OnLibraryDragEnded;
        libraryService.DragStarted += OnLibraryDragStarted;
        libraryService.FunctionBlockCreationRequested += OnFunctionBlockCreationRequested;

        _initialized = true;
    }

    public async Task OnContainerGhostDragEnter(DragEventArgs e)
    {
        if (_ghostDragCts is not null)
        {
            await _ghostDragCts.CancelAsync();
            _ghostDragCts.Dispose();
        }

        if (_ghostDragEnterTask is not null)
        {
            try { await _ghostDragEnterTask; }
            catch (OperationCanceledException) { }
        }

        _ghostDragCts = new();
        _ghostDragEnterTask = OnContainerGhostDragEnterCore(e, _ghostDragCts.Token);
    }

    private async Task OnContainerGhostDragEnterCore(DragEventArgs e, CancellationToken ct)
    {
        if (libraryService.DraggingEntries is null)
            return;

        var validDraggingEntries = libraryService.DraggingEntries
            .Where(le => le.UniqueId != Guid.Empty)
            .ToArray();

        if (validDraggingEntries.Length == 0)
            return;

        var startPosition = _diagram!.GetRelativeGridPoint(new(e.ClientX, e.ClientY), datastore);
        var nextXPos = startPosition.X;
        var nextYPos = startPosition.Y;
        var index = 1;
        var colCount = Math.Ceiling(Math.Sqrt(validDraggingEntries.Length));
        var maxColHeight = 0.0;

        foreach (var libraryEntry in validDraggingEntries)
        {
            var draggingNode = await datastore.AddFunctionBlock(diagramService, libraryEntry.UniqueId, new(nextXPos, nextYPos), ct);

            _diagram!.Nodes.Add(draggingNode);
            _draggingNodes.Add(draggingNode);

            if (ct.IsCancellationRequested)
                return;

            var nodeExtendedSize = draggingNode.GetAlignmentRect();
            if (index % colCount == 0)
            {
                nextXPos = startPosition.X;
                nextYPos += Math.Max(nodeExtendedSize.Height, maxColHeight);
                maxColHeight = 0.0;
            }
            else
            {
                nextXPos += nodeExtendedSize.Width;
                maxColHeight = Math.Max(nodeExtendedSize.Height, maxColHeight);
            }
            index++;
        }

        if (ct.IsCancellationRequested)
            return;

        selectionManager.SetSelection(_draggingNodes);

        // Make sure the created nodes are fully rendered to the diagram and have the 'data-node-id' populated
        var ids = _draggingNodes.Select(n => n.Id);
        var ready = await jsInterop.WaitForNodesAsync(ids, ct);
        if (!ready)
            return;

        behaviorController.StartGhostMove(e.ClientX, e.ClientY);
    }

    public async Task OnContainerGhostDragLeave(DragEventArgs e)
    {
        if (_ghostDragCts is not null)
            await _ghostDragCts.CancelAsync();

        if (_ghostDragEnterTask is not null)
        {
            try { await _ghostDragEnterTask; }
            catch (OperationCanceledException) { }

            _ghostDragEnterTask = null;
        }

        diagramEventService.InvokeDiagramPointerLeave();

        if (_draggingNodes.Count != 0)
        {
            behaviorController.EndGhostMove(e.ClientX, e.ClientY);
            _diagram!.Nodes.Remove(_draggingNodes);
            _draggingNodes.Clear();
        }
    }

    public void OnContainerGhostDragOver(DragEventArgs e)
        => behaviorController.ExternalGhostMove(e.ClientX, e.ClientY);

    public async Task OnContainerGhostDrop(DragEventArgs e)
    {
        if (_ghostDragEnterTask is not null)
        {
            try { await _ghostDragEnterTask; }
            catch (OperationCanceledException) { }

            _ghostDragEnterTask = null;
        }

        behaviorController.EndGhostMove(e.ClientX, e.ClientY);
        _draggingNodes.Clear();

        // In theory this shouldn't be necessary and is only for safety as the DraggingEntries
        // get populated in LibrarySectionContent.OnTreeDragStarted(). So every new drag should set
        // the correct entries.
        // However, there is a bug somewhere. When an active drag is ended by a drop while
        // OnContainerGhostDragEnterCore is still running (can happen when a large amount of blocks
        // is already on the diagram and the user drags all ~65 blocks at once, so that the FB creation
        // takes a long time), and the user tries to start a new drag immediately with the already selected
        // library blocks, then DraggingEntries stays 'null' and the drag fails. Somehow OnTreeDragStarted()
        // is not executed correctly.
        // Removing this line allows for the instant 're-drag' as we would just use the previous entries,
        // however in the current state of the program this can cause a _deadlock_ between two or more threads,
        // involving the DataflowStructureTreeAdapter.
        // Since this interaction is so niche that most likely nobody will ever do that, we play it safe for
        // now and effectively disable the instant 're-drag'.
        libraryService.DraggingEntries = null;
    }

    private async Task OnFunctionBlockCreationRequested(Guid designId)
    {
        if (_diagram is null)
            return;

        var center = _diagram.GetViewport().Center;
        var fbPosition = new Point(
            center.X - (BlockNodeLayout.Width / 2),
            center.Y - (BlockNodeLayout.DefaultNameHeight + BlockNodeLayout.SettingsRowHeight + (BlockNodeLayout.SystemConnectorRows * BlockNodeLayout.RowHeight)));
        var newNode = await datastore.AddFunctionBlock(diagramService, designId, fbPosition);

        _diagram.Nodes.Add(newNode);
        selectionManager.SetSelection(newNode);
    }

    private void OnLibraryDragEnded()
    {
        _libraryDragInProgress = false;
        _requestRender();
    }

    private void OnLibraryDragStarted()
    {
        _libraryDragInProgress = true;
        _requestRender();
    }

    private void Teardown()
    {
        libraryService.DragEnded -= OnLibraryDragEnded;
        libraryService.DragStarted -= OnLibraryDragStarted;
        libraryService.FunctionBlockCreationRequested -= OnFunctionBlockCreationRequested;

        _ghostDragCts?.Cancel();
        _ghostDragCts?.Dispose();
        _ghostDragCts = null;
        _ghostDragEnterTask = null;
        _draggingNodes.Clear();
        _diagram = null;
        _requestRender = () => { };
        _initialized = false;
    }
}
