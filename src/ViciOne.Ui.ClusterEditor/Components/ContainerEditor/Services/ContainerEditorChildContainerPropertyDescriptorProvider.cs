using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.Localization.Resources;
using LocalCommonVocabulary = ViciOne.Ui.ClusterEditor.Localization.Resources.CommonVocabulary;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ContainerEditorChildContainerNodePropertyDescriptorProvider<TContext>()
    : IPropertyDescriptorProvider<TContext, ContainerEditorChildContainer>
{
    public IEnumerable<IPropertyDescriptor<ContainerEditorChildContainer>> GetPropertyDescriptors(TContext context)
    {
        yield return new PropertyDescriptor<ContainerEditorChildContainer, string>
        {
            Category = LocalCommonVocabulary.Data,
            GetValue = (instance) => instance.Name,
            Name = nameof(ChildContainer.Name),
            SetValue = (instance, value) => instance.Name = value
        };

        yield return new PropertyDescriptor<ContainerEditorChildContainer, string?>
        {
            CanBeSetToNull = true,
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => null,
            GetValue = (instance) => instance.Description,
            Name = nameof(ChildContainer.Description),
            ResetValue = (instance) => instance.Description = instance.Backup.Description,
            SetValue = (instance, value) => instance.Description = value
        };

        yield return new PropertyDescriptor<ContainerEditorChildContainer, string?>
        {
            CanBeSetToNull = true,
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => BlockNodeColors.BackgroundDefault,
            GetValue = (instance) => instance.BackColor,
            Name = nameof(ChildContainer.BackColor),
            ResetValue = (instance) => instance.BackColor = instance.Backup.BackColor,
            SetValue = (instance, value) => instance.BackColor = value
        };

        yield return new PropertyDescriptor<ContainerEditorChildContainer, string?>
        {
            CanBeSetToNull = true,
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => BlockNodeColors.ForegroundDefault,
            GetValue = (instance) => instance.ForeColor,
            Name = nameof(ChildContainer.ForeColor),
            ResetValue = (instance) => instance.ForeColor = instance.Backup.ForeColor,
            SetValue = (instance, value) => instance.ForeColor = value
        };
    }
}
