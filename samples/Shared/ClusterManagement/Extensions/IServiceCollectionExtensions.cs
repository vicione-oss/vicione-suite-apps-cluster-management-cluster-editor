using Microsoft.Extensions.DependencyInjection;
using Shared.ClusterManagement.Services;

namespace Shared.ClusterManagement.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClusterManagement(this IServiceCollection services)
    {
        services.AddScoped<ClusterManagementService>();
        return services;
    }
}
