using ViciOne.Ui.Blazor.Components.PropertyGrid.Validators;
using ViciOne.Ui.ClusterEditor.Helpers;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Validators;

internal sealed class CssColorPropertyValueValidator : IPropertyValueValidator<string?>
{
    public string? Validate(string? value)
        => value is null || Color.IsValidCssColor(value)
            ? null
            : Localization.CssColorPropertyValueValidator.InvalidCssColor;
}
