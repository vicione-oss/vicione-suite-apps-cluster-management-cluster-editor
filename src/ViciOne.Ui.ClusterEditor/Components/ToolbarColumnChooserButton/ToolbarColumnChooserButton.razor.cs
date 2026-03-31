using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;

namespace ViciOne.Ui.ClusterEditor.Components.ToolbarColumnChooserButton;

public sealed partial class ToolbarColumnChooserButton
{
    private readonly string _cssClass = "toolbar-column-chooser-button";

    [Parameter] public IGrid? AssociatedGrid { get; set; }

    private void ToolbarButtonClick()
    => AssociatedGrid?.ShowColumnChooser(new DialogDisplayOptions(targetSelector: $".{_cssClass}:not(.hidden)",
            HorizontalAlignment.Right, VerticalAlignment.Top));
}
