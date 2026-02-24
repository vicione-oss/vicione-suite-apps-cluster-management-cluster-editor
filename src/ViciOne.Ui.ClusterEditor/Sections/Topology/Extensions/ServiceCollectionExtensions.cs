using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices.PropertyValueEqualityComparer;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddTopologyNodeEditTemplatePropertyGrid(this IServiceCollection services)
    {
        services.AddPropertyGrid<TopologyNodeEditContext>()
            .WithPropertyDescriptorProvider<ClusterApplicationPropertyDescriptorProvider>()
            .WithPropertyDescriptorProvider<ClusterNodeGroupPropertyDescriptorProvider>()
            .WithPropertyDescriptorProvider<ClusterNodePropertyDescriptorProvider>()
            .WithPropertyDescriptorProvider<EngineHostPropertyDescriptorProvider>()
            .WithPropertyDescriptorProvider<EnginePropertyDescriptorProvider>()
            .WithPropertyValueEqualityComparer<string, AlphaNumericPropertyValueEqualityComparer>();

        return services;
    }

    public static IServiceCollection AddTopologySection(this IServiceCollection services)
    {
        services.AddScoped<TopologyTreeAdapter>();
        services.AddTopologyNodeEditTemplatePropertyGrid();

        return services;
    }
}
