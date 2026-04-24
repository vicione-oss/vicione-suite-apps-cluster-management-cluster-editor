using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Sdk.Backend.Artifacts;

namespace Shared.Designs;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used in testing")]
internal sealed class PackageArtifactRepository(IFileSystem fileSystem, IArtifactRepository repository)
{
    private const string PackageFolder = "fbs";

    private static void DeleteExistingVersions(string targetFolder, string name, string version)
    {
        var targetPath = Path.Combine(targetFolder, name);

        if (!Directory.Exists(targetPath))
            return;

        DirectoryInfo di = new(targetPath);
        foreach (var file in di.EnumerateFiles())
        {
            file.Delete();
        }
        foreach (var dir in di.EnumerateDirectories())
        {
            if (dir.Name.Equals(version, StringComparison.Ordinal))
                continue;

            dir.Delete(true);
        }
    }

    public Task<PackageArtifactLoadResult> DownloadAndExtractAsync(string targetFolder, string name, string version, CancellationToken cancellationToken)
    {
        var targetPath = fileSystem.Path.Combine(targetFolder, name, version.ToString());

        DeleteExistingVersions(targetFolder, name, version);

        return ProcessDownload(name, version, targetPath, () => FunctionBlockVersionExists(fileSystem, targetPath), cancellationToken);

        static bool FunctionBlockVersionExists(IFileSystem fs, string functionBlockPath)
            => fs.Directory.Exists(functionBlockPath) && fs.Directory.GetFiles(functionBlockPath).Length > 0;
    }

    public async Task<PackageArtifactLoadResult> ProcessDownload(string functionBlockName, string version, string targetPath, Func<bool>? skipTarget, CancellationToken cancellationToken)
    {
        var result = new PackageArtifactLoadResult($"{PackageFolder}/{functionBlockName}/{version}", targetPath);

        try
        {
            if (skipTarget is not null && skipTarget())
            {
                result.Skipped = true;
                return result;
            }

            var sourceArtifact = await QueryArtifact(functionBlockName, version, cancellationToken)
                ?? throw new InvalidOperationException($"Artifact '{functionBlockName}' version '{version}' not found in repository.");

            await repository.DownloadAndExtract(sourceArtifact, targetPath, cancellationToken);
        }
        catch (Exception e)
        {
            result.Error = e;
        }

        return result;
    }

    private async Task<IArtifact?> QueryArtifact(string functionBlockName, string version, CancellationToken cancellationToken)
    {
        var fileNameWithoutExtension = $"{version}-{RuntimeInformation.RuntimeIdentifier}";
        var queryBuilder = repository.CreateQueryBuilder();
        var query = queryBuilder
                .AndPathMatches($"{PackageFolder}/{functionBlockName}")
                .AndNameMatches($"{fileNameWithoutExtension}.zip")
                .Build();

        var artifacts = await repository.Query(query, cancellationToken);
        return artifacts.Artifacts.FirstOrDefault();
    }
}
