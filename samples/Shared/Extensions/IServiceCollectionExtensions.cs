using Microsoft.Extensions.DependencyInjection;
using Shared.Settings.Services;

namespace Shared.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddCultureSupport(this IServiceCollection services)
    {
        services.AddScoped<SettingsDialogService>();
        services.AddScoped<SettingsService>();
        return services;
    }
}
