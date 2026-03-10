using System.Diagnostics;
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.Options;
using Sdk.Backend.Modules;
using Semver;
using Shared.ClusterSerialization;
using Shared.Designs;
using ViciOne.Cluster.Model;

namespace Server.Designs;

internal sealed class DesignLoader(
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
            _logger.LogInformation("Downloading {Count} Dependencies (skipped={Skipped}, errors={Errors}) took {Elapsed}",
                results.Count(r => r is { Skipped: false, Error: null }),
                results.Count(r => r.Skipped),
                results.Count(r => r.Error is not null),
                watch.Elapsed);
        }

        foreach (var result in results.Where(k => k.Error is not null))
            _logger.LogError(result.Error, "Failed to update {Name}", result.SourcePath);
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
                    _logger.LogWarning("Failed to add package {Name} {Version} to store.", component.Key.Name, component.Key.Version);
            }

            _logger.LogInformation("Dependency loading done.");

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
            _logger.LogWarning("Failed to add System DataPort package {Name} {Version} to store.", systemDataPortDependency.Name, systemDataPortDependency.Version);
        if (packagesStore is InMemoryPackagesStore inMemoryPackagesStore)
            inMemoryPackagesStore.SetSystemDataPortDependency(systemDataPortDependency);

        return systemDataPortDependency;
    }

    private static string ToVersion(SemVersion version)
        => $"{(int)version.Major}.{(int)version.Minor}.{(int)version.Patch}";
}
