using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace ViciOne.Ui.ClusterEditor.Sections.SearchAndTools.Components;

public partial class SearchAndToolsItem : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool Expanded { get; set; }
    [Parameter] public EventCallback<bool> ExpandedChanged { get; set; }
    [Parameter] public string Text { get; set; } = string.Empty;

    private async Task OnExpandClickAsync()
    {
        Expanded = !Expanded;
        await ExpandedChanged.InvokeAsync(Expanded);
    }
}
