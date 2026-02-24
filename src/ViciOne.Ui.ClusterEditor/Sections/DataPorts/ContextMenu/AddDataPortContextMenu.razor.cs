using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.ContextMenu.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;

public sealed partial class AddDataPortContextMenu : SpecializedContextMenuBase<AddDataPortContextMenuContext>
{
    private readonly Dictionary<string, string> _iconData = [];

    [Inject] private DiagramEventService DiagramEventService { get; set; } = default!;
    [Inject] private IRulesetProvider RulesetProvider { get; set; } = default!;

    [Parameter]
    public EventCallback<string> OnContextMenuItemClick { get; set; }

    private async Task ContextMenuItemClickAsync(string possibleChild)
    {
        if (OnContextMenuItemClick.HasDelegate && Context is not null)
            await OnContextMenuItemClick.InvokeAsync(possibleChild);
    }

    private string? GetIconData(string possibleChild)
    {
        if (_iconData.TryGetValue(possibleChild, out var iconData))
            return iconData;

        var rulesetId = new RulesetIdentifier(DataPortTreeAdapter.DataPortCategory, possibleChild);
        var ruleSet = RulesetProvider.GetRuleset(rulesetId);
        var icon = ruleSet.Root?.Icons.FirstOrDefault() ?? string.Empty;

        icon = TreeBuilder.TreeBuilder.GetIconMarkupString(icon);
        icon = icon?.Replace("viewBox=\"0 0 32 32\"", "viewBox=\"4 4 28 28\" width=\"16\" height=\"16\"",
            StringComparison.InvariantCulture);

        var iconBase64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(icon ?? string.Empty));

        _iconData[possibleChild] = $"data:image/svg+xml;base64,{iconBase64Encoded}";

        return iconData;
    }

    private void OnContextMenuVisibilityChanged(bool isVisible)
    {
        if (!isVisible)
            DiagramEventService.RequestDiagramFocus();
    }

    protected override void OnInitialized()
    {
        if (Context?.AddDataPortContextMenuItems is null)
        {
            base.OnInitialized();
            return;
        }

        foreach (var contextMenuItem in Context.AddDataPortContextMenuItems)
            GetIconData(contextMenuItem.Identifier);

        base.OnInitialized();
    }
}
