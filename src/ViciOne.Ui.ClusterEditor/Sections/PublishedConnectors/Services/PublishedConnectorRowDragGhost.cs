using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.Blazor.Components.Draggable.Abstractions;
using ViciOne.Ui.Blazor.Components.Draggable.Models;
using ViciOne.Ui.Blazor.Components.Draggable.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;

/// <summary>
/// Hands the published-connector drag ghost to every draggable table row. A ghost authored as markup exists
/// only once its component has rendered, which is what this indirection bridges.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed partial class PublishedConnectorRowDragGhost(ILogger<PublishedConnectorRowDragGhost> logger)
    : ITableRowDragGhost
{
    /// <summary>Assigned by the component once it has rendered.</summary>
    public IDragGhost? MarkupDragGhost { get; set; }

    /// <inheritdoc/>
    public DragGhostJsModuleDescriptor GetJsModule()
    {
        if (MarkupDragGhost is { } markupDragGhost)
            return markupDragGhost.GetJsModule();

        MarkupDragGhostMissing(logger);

        // The library's row-clone ghost is as wide as the row, putting the drop probe most of a row-width
        // from the pointer, so drops will miss small targets.
        return new()
        {
            CreateFunction = new() { Name = "createDragGhost" },
            ModuleName = "/_content/ViciOne.Ui.Blazor.Components/draggable/table-row-drag-ghost.js"
        };
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "No drag ghost was registered for draggable table rows; falling back to the row clone.")]
    private static partial void MarkupDragGhostMissing(ILogger logger);
}
