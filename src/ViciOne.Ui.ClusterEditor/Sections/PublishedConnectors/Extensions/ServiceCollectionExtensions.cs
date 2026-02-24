using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.ContextMenu.Extensions;
using ViciOne.Ui.ClusterEditor.Models.ContextMenu.Specialized;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPublishedConnectorsSection(this IServiceCollection services)
    {
        services.AddScoped<PublishedConnectorsService>();
        services.AddPublishedConnectorsSectionContextMenu();

        return services;
    }

    public static IServiceCollection AddPublishedConnectorsSectionContextMenu(this IServiceCollection services)
    {
        services.AddContextMenuCore();
        services.AddContextMenuRequest<PublishedConnectorsSectionContextMenuContext>();

        return services;
    }
}
