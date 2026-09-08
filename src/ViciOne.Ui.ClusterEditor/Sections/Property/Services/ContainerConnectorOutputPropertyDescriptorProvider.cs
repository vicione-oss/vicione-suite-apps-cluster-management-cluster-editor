using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.ContainerConnectors;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ContainerConnectorOutputPropertyDescriptorProvider<TContext>(IDatastore datastore)
    : IPropertyDescriptorProvider<TContext, ContainerConnectorOutput>
{
    public IEnumerable<IPropertyDescriptor<ContainerConnectorOutput>> GetPropertyDescriptors(TContext context)
    {
        var editor = datastore.Builder.Editors.Connector;

        yield return new NamePropertyDescriptor<ContainerConnectorOutput>();
        yield return new DescriptionPropertyDescriptor<ContainerConnectorOutput>(editor);
        yield return new EventEnabledPropertyDescriptor<ContainerConnectorOutput>(editor);
        yield return new MarkAsChangedOnlyIfNotEqualPropertyDescriptor<ContainerConnectorOutput>(editor);
        yield return new PublishedPropertyDescriptor<ContainerConnectorOutput>(editor);
        yield return new ShortNamePropertyDescriptor<ContainerConnectorOutput>(editor);
    }
}
