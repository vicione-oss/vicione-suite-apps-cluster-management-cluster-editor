using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Sdk.Backend.Modules;
using Sdk.Messaging;
using Shared.Designs;
using ViciOne.Cluster.Model;

namespace Server.Designs;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used in testing")]
internal sealed class ClusterDependencyStore(
    IFileSystem fileSystem,
    IWorkspaceProvider<FakeBackendModule> workspaceProvider) : IClusterDependencyStore
{
    public async Task<IReadOnlyCollection<ClusterDependency>> LoadDependencies(CancellationToken cancellationToken)
    {
        var packagesFilename = PackagesFileProvider.GetPackagesFilename(workspaceProvider.Home, null, fileSystem);
        return await PackagesFileProvider.DeserializePackagesFile(packagesFilename, DefaultJsonSerializerSettings.Default, cancellationToken);
    }
}
