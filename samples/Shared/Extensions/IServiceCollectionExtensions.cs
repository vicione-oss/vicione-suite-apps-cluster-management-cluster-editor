using Microsoft.Extensions.DependencyInjection;
using Shared.Services;

namespace Shared.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCultureSupport(this IServiceCollection services)
    {
        services.AddScoped<ICultureService, CultureService>();
        return services;
    }
}
