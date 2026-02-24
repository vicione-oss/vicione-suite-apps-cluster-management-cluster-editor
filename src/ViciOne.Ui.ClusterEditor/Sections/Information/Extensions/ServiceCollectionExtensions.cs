using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Sections.Information.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Information.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInformationSection(this IServiceCollection services)
    {
        services.AddScoped<StatisticService>();

        return services;
    }
}
