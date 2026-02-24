using Microsoft.Extensions.DependencyInjection;
using ViciOne.Cluster.Builder;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Sections.Property.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Property.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices.PropertyValueEqualityComparer;

namespace ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Extensions;

internal static class IServiceCollectionExtensions
{
    internal static IServiceCollection AddPropertyGrid(this IServiceCollection services)
    {
        services.AddPropertyGrid<DataflowToolbarPropertyGridContext>()
            .WithPropertyDescriptorProvider<ChildContainerPropertyDescriptorProvider<DataflowToolbarPropertyGridContext>>()
            .WithPropertyDescriptorProvider<ConnectorInputPropertyDescriptorProvider<DataflowToolbarPropertyGridContext>>()
            .WithPropertyDescriptorProvider<ConnectorOutputPropertyDescriptorProvider<DataflowToolbarPropertyGridContext>>()
            .WithPropertyDescriptorProvider<ContainerConnectorInputPropertyDescriptorProvider<DataflowToolbarPropertyGridContext>>()
            .WithPropertyDescriptorProvider<ContainerConnectorOutputPropertyDescriptorProvider<DataflowToolbarPropertyGridContext>>()
            .WithPropertyDescriptorProvider<ContainerEditorConnectorPropertyDescriptorProvider<DataflowToolbarPropertyGridContext>>()
            .WithPropertyDescriptorProvider<FunctionBlockPropertyDescriptorProvider<DataflowToolbarPropertyGridContext>>()
            .WithPropertyDescriptorProvider<LabelPropertyDescriptorProvider<DataflowToolbarPropertyGridContext>>()

            .WithPropertyValueEqualityComparer<string, AlphaNumericPropertyValueEqualityComparer>()
            .WithPropertyValueEqualityComparer<IAggregatingPooling, IAggregatingPoolingPropertyValueEqualityComparer>()
            .WithPropertyValueEqualityComparer<AvailableAggregatingPooling, AvailableAggregatingPoolingPropertyValueEqualityComparer>();

        services.AddScoped<ConnectorInputPropertyDescriptorFactory>();

        return services;
    }

    public static IServiceCollection AddToolbarDataflow(this IServiceCollection services)
        => services.AddPropertyGrid();
}
