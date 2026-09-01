using Microsoft.AspNetCore.Components;

namespace ViciOne.Ui.ClusterEditor.Components;

public partial class SearchHighlighter : ComponentBase
{
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public string? Text { get; set; }
}
