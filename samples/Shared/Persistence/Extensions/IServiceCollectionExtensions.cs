using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence.Services;

namespace Shared.Persistence.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddScoped<PersistenceService>();
        return services;
    }
}
