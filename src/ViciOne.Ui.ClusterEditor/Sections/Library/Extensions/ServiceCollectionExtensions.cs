using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Sections.Library.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Library.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLibrarySection(this IServiceCollection services)
    {
        services.AddScoped<ILibraryService, LibraryService>();

        return services;
    }
}
