using Microsoft.Extensions.DependencyInjection;
#if DEBUG
using ViciOne.Ui.ClusterEditor.Sections.Debugging.Services;
#endif

namespace ViciOne.Ui.ClusterEditor.Sections.Debugging.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDebugSection(this IServiceCollection services)
    {
#if DEBUG
        services.AddScoped<DebugService>();
#endif

        return services;
    }
}
