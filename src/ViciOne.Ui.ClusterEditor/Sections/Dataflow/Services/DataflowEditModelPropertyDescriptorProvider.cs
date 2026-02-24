using System.Collections.Generic;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Validators;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.Dataflow.Services;

internal sealed class DataflowEditModelPropertyDescriptorProvider : IPropertyDescriptorProvider<DataflowStructureTreeNode, DataflowEditModel>
{
    private readonly StringMustNotBeEmptyPropertyValueValidator _stringMustNotBeEmptyPropertyValueValidator = new();

    public IEnumerable<IPropertyDescriptor<DataflowEditModel>> GetPropertyDescriptors(DataflowStructureTreeNode context)
    {
        yield return new PropertyDescriptor<DataflowEditModel, string>
        {
            GetValue = (instance) => instance.Name,
            Name = nameof(DataflowEditModel.Name),
            SetValue = (instance, value) => instance.Name = value,
            ValueValidators = [_stringMustNotBeEmptyPropertyValueValidator]
        };
    }
}
