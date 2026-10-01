using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.ContextMenu.Extensions;
using ViciOne.Ui.Blazor.Components.Draggable.Services;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPublishedConnectorsSection(this IServiceCollection services)
    {
        services.AddScoped<PublishedConnectorsService>();
        services.AddPublishedConnectorsSectionContextMenu();

        // Also the drag payload provider the section content hands to its table.
        services.AddScoped<PublishedConnectorVisibleSelection>();

        services.AddScoped<IDropPolicy<BlockNodeConnector>, PublishedConnectorDropPolicy>();
        services.AddScoped<IDropHandler<BlockNodeConnector>, PublishedConnectorDropHandler>();
        services.AddScoped<ConnectorDropTargets>();

        // The container holds one ITableRowDragGhost slot for the whole application, so every draggable table
        // row gets this ghost, and a host registering its own after AddClusterEditor takes the slot back.
        services.AddScoped<PublishedConnectorRowDragGhost>();
        services.AddScoped<ITableRowDragGhost>(serviceProvider => serviceProvider.GetRequiredService<PublishedConnectorRowDragGhost>());

        return services;
    }

    public static IServiceCollection AddPublishedConnectorsSectionContextMenu(this IServiceCollection services)
    {
        services.AddContextMenuCore();
        services.AddContextMenuRequest<PublishedConnectorsSectionContextMenuContext>();

        return services;
    }
}
