using System.Linq;
using ViciOne.Tree.Builder;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Validators;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Validators.Localization;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Validators;

internal class DataPortNodeNameValidator(Tree.Builder.TreeBuilder treeBuilder, ITreeNode node) : IPropertyValueValidator<string>
{
    public string? Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DataPortNodeValidationMessages.NameCannotBeEmptyOrWhitespace;

        var validationResult = treeBuilder.ValidateNodeName(node, value);
        if (!validationResult.IsValid)
            return validationResult.Messages.FirstOrDefault();

        return null;
    }
}
