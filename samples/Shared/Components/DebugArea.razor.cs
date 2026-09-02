using Microsoft.AspNetCore.Components;
using Shared.Services;

namespace Shared.Components;

public partial class DebugArea : ComponentBase
{
    [Inject] private IndexService IndexService { get; set; } = default!;

    protected override void OnInitialized()
        => IndexService.StateHasChanged = StateHasChanged;
}
