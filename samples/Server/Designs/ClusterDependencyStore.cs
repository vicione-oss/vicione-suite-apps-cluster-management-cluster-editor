using System.IO.Abstractions;
using Microsoft.Extensions.Options;
using Sdk.Backend.Modules;
using Sdk.Messaging;
using Shared.Designs;
using ViciOne.Cluster.Model;

namespace Server.Designs;

internal sealed class ClusterDependencyStore(
    IFileSystem fileSystem,
    IOptions<ClusterDependencyHttpOptions> options,
    IWorkspaceProvider<FakeBackendModule> workspaceProvider) : IClusterDependencyStore
{
    public async Task<IReadOnlyCollection<ClusterDependency>> LoadDependencies(CancellationToken cancellationToken)
    {
        var packagesFilename = PackagesFileProvider.GetPackagesFilename(workspaceProvider.Home, options.Value.DataPortSet, fileSystem);
        return await PackagesFileProvider.DeserializePackagesFile(packagesFilename, DefaultJsonSerializerSettings.Default, cancellationToken);
    }
}
