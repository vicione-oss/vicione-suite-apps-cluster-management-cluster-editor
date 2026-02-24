using System;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Behaviors;

internal sealed class VOKeyboardBehavior : Behavior
{
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly Datastore _datastore;
    private readonly DiagramEventService _diagramEventService;
    private readonly DiagramService _diagramService;

    public VOKeyboardBehavior(
        Datastore datastore,
        ClusterBuilderEventBuffer clusterBuilderEventBuffer,
        Diagram diagram,
        DiagramEventService diagramEventService,
        DiagramService diagramService) : base(diagram)
    {
        _datastore = datastore;
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _diagramEventService = diagramEventService;
        _diagramService = diagramService;

        Diagram.KeyDown += OnDiagramKeyDownAsync;
    }

    // copied & slightly modified from Blazor.Diagrams.Core.Behaviors.KeyboardShortcutsDefaults
    private async ValueTask DeleteSelectionAsync()
    {
        _clusterBuilderEventBuffer.StartBatchOpertation();
        Diagram.SuspendRefresh = true;

        foreach (var sm in Diagram.GetSelectedModels().ToArray())
        {
            if (sm.Locked)
                continue;

            if (sm is GroupModel group && (await Diagram.Options.Constraints.ShouldDeleteGroup(group)))
            {
                Diagram.Groups.Delete(group);
            }
            else if (sm is NodeModel node && (await Diagram.Options.Constraints.ShouldDeleteNode(node)))
            {
                Diagram.Nodes.Remove(node);
            }
            else if (sm is BaseLinkModel link && (await Diagram.Options.Constraints.ShouldDeleteLink(link)))
            {
                Diagram.Links.Remove(link);
            }
        }

        _clusterBuilderEventBuffer.EndBatchOperation();
        Diagram.SuspendRefresh = false;
        Diagram.Refresh();
    }

    public override void Dispose()
    {
        Diagram.KeyDown -= OnDiagramKeyDownAsync;
        GC.SuppressFinalize(this);
    }

    private async void OnDiagramKeyDownAsync(global::Blazor.Diagrams.Core.Events.KeyboardEventArgs e)
    {
        if (e.Code == KeyboardCodes.Escape)
        {
            if (_diagramService.DiagramState.IsContextMenuVisible)
            {
                await _diagramEventService.RequestCloseContextMenu();
            }
            else
            {
                var container = _datastore.ActiveContainer as ChildContainer;
                if (container is not null)
                    await _datastore.LoadContainer(container.Parent, _diagramService);
            }
        }
        else if (e.Code == KeyboardCodes.Delete)
        {
            await DeleteSelectionAsync();
        }
        else if (e.Code == KeyboardCodes.A)
        {
            _diagramEventService.RequestFilterAttached();
        }
        else if (e.Code == KeyboardCodes.S)
        {
            _diagramEventService.RequestFilterSelected();
        }
        else if (e.CtrlKey && e.Code == KeyboardCodes.B)
        {
            _diagramEventService.RequestFilterClear();
        }
    }
}
