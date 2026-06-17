using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using ViciOne.Ui.ClusterEditor.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Services;

internal sealed class ContainerEditorConnectorPropertyDescriptorProvider<TContext>
    : IPropertyDescriptorProvider<TContext, ContainerEditorConnector>
{
    public IEnumerable<IPropertyDescriptor<ContainerEditorConnector>> GetPropertyDescriptors(TContext context)
    {
        yield return new PropertyDescriptor<ContainerEditorConnector, string?>
        {
            Category = CommonVocabulary.Data,
            GetValue = (instance) => instance.Description,
            Name = nameof(ContainerEditorConnector.Description),
            ResetValue = (instance) => instance.Description = instance.Backup.Description,
            SetValue = (instance, value) => instance.Description = value
        };

        yield return new PropertyDescriptor<ContainerEditorConnector, string>
        {
            Category = CommonVocabulary.Data,
            Enabled = (instance) => false,
            GetValue = (instance) => instance.Backup.Connector.FunctionBlock.Name,
            Name = $"{nameof(FunctionBlock)} {nameof(FunctionBlock.Name)}"
        };

        yield return new PropertyDescriptor<ContainerEditorConnector, string>
        {
            Category = CommonVocabulary.Data,
            GetDefaultValue = (instance) => instance.Backup.Name,
            GetValue = (instance) => instance.Name,
            Name = nameof(ContainerEditorConnector.Name),
            ResetValue = (instance) => instance.Name = instance.Backup.Name,
            SetValue = (instance, value) => instance.Name = value
        };

        yield return new PropertyDescriptor<ContainerEditorConnector, string>
        {
            Category = CommonVocabulary.Data,
            GetDefaultValue = (instance) => instance.Backup.ShortName,
            GetValue = (instance) => instance.ShortName,
            Name = nameof(ContainerEditorConnector.ShortName),
            ResetValue = (instance) =>
            {
                instance.ShortName = instance.Backup.ShortName;
                instance.BlockNodeConnector?.Text = instance.Backup.ShortName;
            },
            SetValue = (instance, value) =>
            {
                instance.ShortName = value;
                instance.BlockNodeConnector?.Text = value;
            }
        };
    }
}
