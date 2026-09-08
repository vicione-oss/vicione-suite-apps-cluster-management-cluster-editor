using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ConnectorOutputPropertyDescriptorProvider<TContext>(IDatastore datastore)
    : IPropertyDescriptorProvider<TContext, ConnectorOutput>
{
    public IEnumerable<IPropertyDescriptor<ConnectorOutput>> GetPropertyDescriptors(TContext context)
    {
        var editor = datastore.Builder.Editors.Connector;

        yield return new DescriptionPropertyDescriptor<ConnectorOutput>(editor);
        yield return new EventEnabledPropertyDescriptor<ConnectorOutput>(editor);
        yield return new MarkAsChangedOnlyIfNotEqualPropertyDescriptor<ConnectorOutput>(editor);
        yield return new NamePropertyDescriptor<ConnectorOutput>();
        yield return new PublishedPropertyDescriptor<ConnectorOutput>(editor);
    }
}
