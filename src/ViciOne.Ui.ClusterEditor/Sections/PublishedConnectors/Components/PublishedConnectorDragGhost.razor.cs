using System.Threading.Tasks;
using ViciOne.Ui.Blazor.Components.Draggable.Abstractions;
using ViciOne.Ui.Blazor.Components.Draggable.Components;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Components;

/// <summary>
/// The visual that follows the pointer while published-connector rows are dragged onto the diagram. Its cursor
/// is the only feedback on whether releasing here would create a link.
/// </summary>
public sealed partial class PublishedConnectorDragGhost
    : DragGhostBase, IDragStartListener, IDragEndListener, IDropzoneEnterListener, IDropzoneLeaveListener
{
    private const string OverValidTargetCssModifier = "over-valid-target";

    private string? _targetStateCssModifier;

    /// <inheritdoc/>
    public Task DragEndAsync()
        => SetTargetState(null);

    /// <inheritdoc/>
    public Task DragStartAsync()
        => SetTargetState(null);

    /// <inheritdoc/>
    public Task DropzoneEnterAsync()
        => SetTargetState(OverValidTargetCssModifier);

    /// <inheritdoc/>
    public Task DropzoneLeaveAsync()
        => SetTargetState(null);

    private async Task SetTargetState(string? targetStateCssModifier)
    {
        if (_targetStateCssModifier == targetStateCssModifier)
            return;

        _targetStateCssModifier = targetStateCssModifier;

        await RenderContentAsync();
    }
}
