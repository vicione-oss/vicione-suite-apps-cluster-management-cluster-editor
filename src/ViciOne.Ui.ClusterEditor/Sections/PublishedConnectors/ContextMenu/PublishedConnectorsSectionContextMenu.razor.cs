using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.Blazor.Components.ContextMenu.Components;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.ContextMenu;

public sealed partial class PublishedConnectorsSectionContextMenu : SpecializedContextMenuBase<PublishedConnectorsSectionContextMenuContext>
{
    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private InputEventService InputEventService { get; set; } = default!;
    [Inject] private PublishedConnectorsService PublishedConnectorsService { get; set; } = default!;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        InputEventService.KeyDown -= OnKeyDown;
    }

    private void OnContextMenuVisibilityChanged(bool isVisible)
    {
        if (isVisible)
        {
            InputEventService.KeyDown += OnKeyDown;
        }
        else
        {
            InputEventService.KeyDown -= OnKeyDown;
            DiagramEventService.RequestDiagramFocus();
        }
    }

    private async void OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Code == KeyboardCodes.Escape && ContextMenu is not null)
            await ContextMenu.HideAsync();
    }

    private void RemovePublishedConnectorClick()
    {
        if (Context is not null)
            PublishedConnectorsService.RemovePublishedConnectors(Context.PublishedConnectorsWrappers);
    }
}
