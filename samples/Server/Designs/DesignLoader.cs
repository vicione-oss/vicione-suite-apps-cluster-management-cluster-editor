using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.Options;
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
    IOptions<ClusterDependencyHttpOptions> options,
    IPackagesStore packagesStore,
    IWorkspaceProvider<FakeBackendModule> workspaceProvider) : BackgroundService, IDownloader
{
    private readonly IClusterDependencyStore _clusterDependencyStore = clusterDependencyStore;
    private readonly TaskCompletionSource _downloadProcess = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ILogger<DesignLoader> _logger = logger;
    private readonly IOptions<ClusterDependencyHttpOptions> _options = options;
    private readonly IWorkspaceProvider<FakeBackendModule> _workspaceProvider = workspaceProvider;

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

    private async Task DownloadClusterDependencies(IReadOnlyCollection<ClusterDependency> dependencies, CancellationToken stoppingToken)
    {
        var watch = new Stopwatch();
        watch.Start();

        var packagesPath = _fileSystem.GetDependenciesPath(_workspaceProvider);
        _fileSystem.Directory.CreateDirectory(packagesPath);

        using var functionBlockLoader = new ClusterDependencyHttpLoader(_options, packagesPath);

        var results = await Task.WhenAll(dependencies.Select(fb => functionBlockLoader.DownloadAndExtractAsync(fb.Name, fb.Version, stoppingToken)));

        watch.Stop();

        if (_logger.IsEnabled(LogLevel.Information))
        {
#pragma warning disable CA1873 // Avoid potentially expensive logging - there is a logger.IsEnabled check
            LogDownloadComplete(_logger,
                results.Count(r => r is { Skipped: false, Error: null }),
                results.Count(r => r.Skipped),
                results.Count(r => r.Error is not null),
                watch.Elapsed);
#pragma warning restore CA1873 // Avoid potentially expensive logging
        }

        foreach (var result in results.Where(k => k.Error is not null))
            LogUpdateFailed(_logger, result.Error, result.SourcePath);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _downloadProcess.TrySetCanceled(stoppingToken));
        try
        {
            var dependencies = await _clusterDependencyStore.LoadDependencies(stoppingToken);
            await DownloadClusterDependencies(dependencies, stoppingToken);

            var packagesPath = _fileSystem.GetDependenciesPath(_workspaceProvider);
            _fileSystem.Directory.CreateDirectory(packagesPath);

            if (packagesStore is InMemoryPackagesStore inMemoryPackagesStore)
                inMemoryPackagesStore.Initialize(dependencies);

            // ViciOne.Suite.System.DataPort is a FunctionBlock interface for the providers
            var systemDataPortDependency = LoadSystemDataPortFunctionBlock();

            var existingPackages = PackagesFileProvider.GetComponents(packagesPath, _fileSystem);
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
                    LogAddPackageFailed(_logger, component.Key.Name, ToVersion(component.Key.Version));
            }

            LogDependencyLoadingDone(_logger);

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
            LogAddSystemDataPortPackageFailed(_logger, systemDataPortDependency.Name, systemDataPortDependency.Version);

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

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update {SourcePath}.")]
    private static partial void LogUpdateFailed(ILogger logger, Exception? exception, string sourcePath);

    private static string ToVersion(SemVersion version)
        => $"{(int)version.Major}.{(int)version.Minor}.{(int)version.Patch}";
}
