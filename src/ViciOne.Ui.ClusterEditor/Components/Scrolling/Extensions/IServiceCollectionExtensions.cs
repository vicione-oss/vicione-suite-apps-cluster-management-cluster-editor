using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.Resizing.Extensions;

namespace ViciOne.Ui.ClusterEditor.Components.Scrolling.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddScrolling(this IServiceCollection services)
    {
        services.AddResizeObserver();

        return services;
    }
}
