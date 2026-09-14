using System.Linq;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Validators;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Validators;

internal class DataPortNodePropertyValueValidator<TPropertyValue>(Tree.Builder.TreeBuilder treeBuilder, IDataPortNodeModelProperty dataPortNodeModelProperty) : IPropertyValueValidator<TPropertyValue>
{
    public string? Validate(TPropertyValue value)
    {
        if (value is null)
            return null;

        var validationResult = treeBuilder.ValidatePropertyValue(dataPortNodeModelProperty.DependencyId, value);

        if (!validationResult.IsValid)
            return validationResult.Messages.FirstOrDefault();

        return null;
    }
}
