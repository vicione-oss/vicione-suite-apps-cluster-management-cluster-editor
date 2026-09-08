using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Property.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.ConnectorInputs;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.ContainerConnectors;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ContainerConnectorInputPropertyDescriptorProvider<TContext>(IDatastore datastore,
    ConnectorInputPropertyDescriptorFactory propertyDescriptorFactory)
        : IPropertyDescriptorProvider<TContext, ContainerConnectorInput>
            where TContext : class, IHasSelectedConnectors
{
    public IEnumerable<IPropertyDescriptor<ContainerConnectorInput>> GetPropertyDescriptors(TContext context)
    {
        var builder = datastore.Builder;

        var crossSourcePoolingStrategyPropertyDescriptor = new CrossSourcePoolingStrategyPropertyDescriptor<ContainerConnectorInput>(
            builder, connectorInputSelector: containerConnectorInput => (ConnectorInput)containerConnectorInput.Connector);

        yield return crossSourcePoolingStrategyPropertyDescriptor;

        yield return new CrossSourceAggregatingPoolingPropertyDescriptor<ContainerConnectorInput>(builder,
            crossSourcePoolingStrategyPropertyDescriptor,
            connectorInputSelector: containerConnectorInput => (ConnectorInput)containerConnectorInput.Connector);

        var valuePropertyDescriptors = propertyDescriptorFactory.CreateValuePropertyDescriptors<ContainerConnectorInput>(
            context.SelectedConnectors, connectorSelector: containerConnectorInput => (ConnectorInput)containerConnectorInput.Connector);

        foreach (var valuePropertyDescriptor in valuePropertyDescriptors)
            yield return valuePropertyDescriptor;

        var editor = builder.Editors.Connector;

        yield return new DescriptionPropertyDescriptor<ContainerConnectorInput>(editor);
        yield return new EventEnabledPropertyDescriptor<ContainerConnectorInput>(editor);
        yield return new MarkAsChangedOnlyIfNotEqualPropertyDescriptor<ContainerConnectorInput>(editor);
        yield return new NamePropertyDescriptor<ContainerConnectorInput>();
        yield return new PublishedPropertyDescriptor<ContainerConnectorInput>(editor);
        yield return new ShortNamePropertyDescriptor<ContainerConnectorInput>(editor);
    }
}
