using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.ContextMenu.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices.PropertyValueEqualityComparer;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddDataPortAddChildNodeContextMenu(this IServiceCollection services)
    {
        services.AddContextMenuCore();
        services.AddContextMenuRequest<DataPortAddChildNodeContextMenuContext>();

        return services;
    }

    internal static IServiceCollection AddDataPortContextMenu(this IServiceCollection services)
    {
        services.AddContextMenuCore();
        services.AddContextMenuRequest<AddDataPortContextMenuContext>();

        return services;
    }

    internal static IServiceCollection AddDataPortPropertyGrid(this IServiceCollection services)
    {
        services.AddPropertyGrid<DataPortChildNodeEditContext>()
            .WithPropertyDescriptorProvider<DataPortChildNodeModelPropertyDescriptorProvider>()
            .WithPropertyValueEqualityComparer<string, AlphaNumericPropertyValueEqualityComparer>();

        services.AddScoped<DataPortChildNodePropertyValueStore>();

        return services;
    }

    public static IServiceCollection AddDataPortSection(this IServiceCollection services)
    {
        services.AddDataPortPropertyGrid();
        services.AddDataPortContextMenu();
        services.AddDataPortAddChildNodeContextMenu();
        services.AddScoped<DataPortTreeAdapter>();

        return services;
    }
}
