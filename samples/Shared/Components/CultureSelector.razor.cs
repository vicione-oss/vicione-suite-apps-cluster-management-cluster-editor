using System.Globalization;
using Microsoft.AspNetCore.Components;
using Shared.Services;

namespace Shared.Components;

public sealed partial class CultureSelector : ComponentBase
{
    [Inject] private ICultureService CultureService { get; set; } = default!;

    protected override Task OnAfterRenderAsync(bool firstRender)
        => firstRender ? CultureService.Init(true) : Task.CompletedTask;

    private Task OnCultureChanged(CultureInfo newCultureInfo)
        => CultureService.SetCurrentCulture(newCultureInfo);
}
