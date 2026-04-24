using System.IO.Abstractions;
using Core.Artifacts.Extensions;
using Sdk.Backend.Modules;
using Shared.Designs;

namespace Server.Designs;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClusterDependenciesSupport(this IServiceCollection services)
    {
        services
            .AddArtifactRepository<PackageArtifactOptionsProvider>()
            .AddTransient<PackageArtifactRepository>()
            .AddSingleton<IWorkspaceProvider<FakeBackendModule>, FakeWorkspaceProvider>()
            .AddTransient<IClusterDependencyStore, ClusterDependencyStore>()
            .AddSingleton<IFileSystem, FileSystem>()
            .AddSingleton<IPackagesStore, InMemoryPackagesStore>()
            .AddSingleton<IDesignProvider>(s => (InMemoryPackagesStore)s.GetRequiredService<IPackagesStore>());

        return services;
    }
}
