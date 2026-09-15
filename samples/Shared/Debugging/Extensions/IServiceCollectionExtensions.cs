using Microsoft.Extensions.DependencyInjection;
using Shared.Debugging.Services;

namespace Shared.Debugging.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDebugging(this IServiceCollection services)
    {
        services.AddScoped<ClusterGeneratorService>();
        return services;
    }
}
