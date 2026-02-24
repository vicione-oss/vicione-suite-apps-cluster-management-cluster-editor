using Microsoft.AspNetCore.Components;
using ViciOne.Ui.MonochromeIcons.Core.Enums;

namespace ViciOne.Ui.ClusterEditor.Sections.SearchAndTools.Components;

public partial class SearchAndToolsChildItem : ComponentBase
{
    [Parameter] public bool Enabled { get; set; } = true;
    [Parameter] public MarkupString? IconMarkup { get; set; }
    [Parameter] public MonochromeIconName? IconName { get; set; }
    [Parameter] public EventCallback OnClickCallback { get; set; }
    [Parameter] public string Text { get; set; } = string.Empty;
}
