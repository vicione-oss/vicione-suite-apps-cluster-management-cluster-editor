using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.ContainerConnectors;

internal sealed class ShortNamePropertyDescriptor<TContainerConnector> : PropertyDescriptor<TContainerConnector, string>
    where TContainerConnector : ContainerConnector
{
    [SetsRequiredMembers]
    public ShortNamePropertyDescriptor(IConnectorEditor editor)
    {
        Category = CommonVocabulary.Data;
        GetDefaultValue = (instance) => instance.Connector.ShortName;
        GetValue = (instance) => instance.ShortName;
        Name = nameof(ContainerConnector.ShortName);
        ResetValue = editor.ResetShortName;
        SetValue = editor.SetShortName;
    }
}
