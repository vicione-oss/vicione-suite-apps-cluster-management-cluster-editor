using System.IO.Abstractions;
using Microsoft.Extensions.Options;
using Sdk.Backend.Modules;
using Shared.Designs;

namespace Server.Designs;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClusterDependenciesSupport(this IServiceCollection services)
    {
        services
            .AddSingleton<IWorkspaceProvider<FakeBackendModule>, FakeWorkspaceProvider>()
            .AddTransient<IClusterDependencyStore, ClusterDependencyStore>()
            .AddSingleton<IFileSystem, FileSystem>()
            .AddSingleton<IPackagesStore, InMemoryPackagesStore>()
            .AddSingleton<IDesignProvider>(s => (InMemoryPackagesStore)s.GetRequiredService<IPackagesStore>())
            .AddSingleton(s => Options.Create(ClusterDependencyHttpOptions.GetValidatedOptions(s.GetRequiredService<IConfiguration>())));

        return services;
    }
}
