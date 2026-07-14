using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using Sdk.Backend.Modules;
using Semver;
using Shared.ClusterSerialization;
using Shared.Designs;
using ViciOne.Cluster.Model;

namespace Server.Designs;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used in testing")]
internal sealed partial class DesignLoader(
    IClusterDependencyStore clusterDependencyStore,
    IFileSystem fileSystem,
    ILogger<DesignLoader> logger,
    IPackagesStore packagesStore,
    IWorkspaceProvider<FakeBackendModule> workspaceProvider,
    PackageArtifactRepository packageDownloader) : BackgroundService, IDownloader
{
    private readonly TaskCompletionSource _downloadProcess = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal int LoadedDataPortDesigns { get; private set; }
    internal int LoadedFunctionBlockDesigns { get; private set; }

    public Task Task => _downloadProcess.Task;

    private static ClusterDependency CreateDependency(Type type)
        => CreateDependency(type.Assembly);

    private static ClusterDependency CreateDependency(Assembly assembly)
    {
        var assemblyName = assembly.GetName();
        var assemblyVersion = assemblyName.Version ?? throw new InvalidDataException();

        // assembly version might be 0.1.0.0 but our version has major.minor.build
        return new()
        {
            Name = assemblyName.Name!,
            Version = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}",
        };
    }

    private async Task<HashSet<ClusterDependency>> DownloadClusterDependencies(IReadOnlyCollection<ClusterDependency> dependencies, CancellationToken stoppingToken)
    {
        var watch = Stopwatch.StartNew();

        var packagesPath = fileSystem.GetDependenciesPath(workspaceProvider);
        fileSystem.Directory.CreateDirectory(packagesPath);

        var results = await Task.WhenAll(dependencies.Select(cd => packageDownloader.DownloadAndExtractAsync(packagesPath, cd.Name, cd.Version, stoppingToken)));

        watch.Stop();

        if (logger.IsEnabled(LogLevel.Information))
        {
#pragma warning disable CA1873 // Avoid potentially expensive logging - there is a logger.IsEnabled check
            LogDownloadComplete(logger,
                results.Count(r => r is { Skipped: false, Error: null }),
                results.Count(r => r.Skipped),
                results.Count(r => r.Error is not null),
                watch.Elapsed);
#pragma warning restore CA1873 // Avoid potentially expensive logging
        }

        HashSet<ClusterDependency> failed = [];
        foreach (var result in results.Where(k => k.Error is not null))
        {
            LogUpdateFailed(logger, result.Error, result.SourcePath);
            failed.Add(new ClusterDependency { Name = result.PackageName, Version = result.PackageVersion });
        }

        return failed;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _downloadProcess.TrySetCanceled(stoppingToken));
        try
        {
            var dependencies = await clusterDependencyStore.LoadDependencies(stoppingToken);
            var failedDownloads = await DownloadClusterDependencies(dependencies, stoppingToken);

            var validDependencies = dependencies.Except(failedDownloads).ToList().AsReadOnly();

            var packagesPath = fileSystem.GetDependenciesPath(workspaceProvider);
            fileSystem.Directory.CreateDirectory(packagesPath);

            if (packagesStore is InMemoryPackagesStore inMemoryPackagesStore)
                inMemoryPackagesStore.Initialize(validDependencies);

            // ViciOne.Suite.System.DataPort is a FunctionBlock interface for the providers
            var systemDataPortDependency = LoadSystemDataPortFunctionBlock();

            var existingPackages = PackagesFileProvider.GetComponents(packagesPath, fileSystem);
            var latestPackages = existingPackages.ResolveLatestVersion();

            foreach (var component in latestPackages)
            {
                ClusterDependency dependency = new()
                {
                    Name = component.Key.Name,
                    Version = ToVersion(component.Key.Version),
                };

                if (component.Key.Name.Equals(systemDataPortDependency.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!packagesStore.TryAddPackage(dependency, component.Value))
                    LogAddPackageFailed(logger, component.Key.Name, ToVersion(component.Key.Version));
            }

            LogDependencyLoadingDone(logger);

            ClusterSerializer.SetTypedSerializerOptions();
        }
        catch (Exception ex)
        {
            _downloadProcess.TrySetException(ex);
        }
        finally
        {
            _downloadProcess.TrySetResult();
        }
    }

    private ClusterDependency LoadSystemDataPortFunctionBlock()
    {
        var dataportType = typeof(ViciOne.Suite.System.DataPort);
        var systemDataPortDependency = CreateDependency(dataportType);

        if (!packagesStore.TryAddPackage(systemDataPortDependency, [dataportType], [], true))
            LogAddSystemDataPortPackageFailed(logger, systemDataPortDependency.Name, systemDataPortDependency.Version);

        if (packagesStore is InMemoryPackagesStore inMemoryPackagesStore)
            inMemoryPackagesStore.SetSystemDataPortDependency(systemDataPortDependency);

        return systemDataPortDependency;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add package {Name} {Version} to store.")]
    private static partial void LogAddPackageFailed(ILogger logger, string name, string version);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add System DataPort package {Name} {Version} to store.")]
    private static partial void LogAddSystemDataPortPackageFailed(ILogger logger, string name, string version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dependency loading done.")]
    private static partial void LogDependencyLoadingDone(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Downloading {Count} dependencies (skipped={Skipped}, errors={Errors}) took {Elapsed}.")]
    private static partial void LogDownloadComplete(ILogger logger, int count, int skipped, int errors, TimeSpan elapsed);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to update {SourcePath}.")]
    private static partial void LogUpdateFailed(ILogger logger, Exception? exception, string sourcePath);

    private static string ToVersion(SemVersion version)
        => $"{(int)version.Major}.{(int)version.Minor}.{(int)version.Patch}";
}
