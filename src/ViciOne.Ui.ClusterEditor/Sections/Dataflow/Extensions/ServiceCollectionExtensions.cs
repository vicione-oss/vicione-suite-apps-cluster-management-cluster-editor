using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Models;
using ViciOne.Ui.ClusterEditor.Sections.Dataflow.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices.PropertyValueEqualityComparer;

namespace ViciOne.Ui.ClusterEditor.Sections.Dataflow.Extensions;

internal static class ServiceCollectionExtensions
{
    private static IServiceCollection AddDataflowPropertyGrid(this IServiceCollection services)
    {
        services.AddPropertyGrid<DataflowStructureTreeNode>()
            .WithPropertyDescriptorProvider<DataflowEditModelPropertyDescriptorProvider>()
            .WithPropertyValueEqualityComparer<string, AlphaNumericPropertyValueEqualityComparer>();

        return services;
    }

    public static IServiceCollection AddDataflowSection(this IServiceCollection services)
    {
        services.AddScoped<DataflowStructureTreeAdapter>();

        services.AddDataflowPropertyGrid();

        return services;
    }
}
