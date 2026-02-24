using Microsoft.AspNetCore.Components;
using Shared.Services;

namespace Shared.Components;

public partial class DebugArea : ComponentBase
{
    [Inject] private ICultureService CultureService { get; set; } = default!;
    [Inject] private IndexService IndexService { get; set; } = default!;

    // Prevent CultureSelector triggering page reload on render component
    // when Browser culture is different than the one stored in the cache
    protected override Task OnAfterRenderAsync(bool firstRender)
        => firstRender ? CultureService.Init(true) : Task.CompletedTask;

    protected override void OnInitialized()
        => IndexService.StateHasChanged = StateHasChanged;
}
