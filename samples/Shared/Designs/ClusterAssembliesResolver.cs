using System.IO.Abstractions;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using ViciOne.Cluster.Model;

namespace Shared.Designs;

public static class ClusterAssembliesResolver
{
    internal static AssetFileCandidate ByHighestVersionOrLastFile(AssetFileCandidate file1, AssetFileCandidate file2, IFileSystem fileSystem)
    {
        if (file1.Version is not null && file2.Version is not null)
            return file1.Version >= file2.Version ? file1 : file2;

        if (string.Compare(fileSystem.Path.GetFileName(file1.Filename), fileSystem.Path.GetFileName(file2.Filename), StringComparison.Ordinal) > 0)
            return file1;

        return file2;
    }

    private static Package GetPackageFromDirectory(string packagesDirectory, string name, string version, IFileSystem fileSystem)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (string.IsNullOrEmpty(version))
            throw new ArgumentNullException(nameof(version));

        var directory = fileSystem.Path.Combine(packagesDirectory, name, version);
        if (fileSystem.Directory.Exists(directory))
        {
            var assets = fileSystem.Directory
                .GetFiles(directory, "*", SearchOption.AllDirectories)
                .Select(CreateAsset)
                .ToList();
            return new(directory, assets);
        }
        else
        {
            throw new InvalidOperationException($"Package '{name}' with version '{version}' could not be found.");
        }

        static Asset CreateAsset(string filename)
        {
            if (filename.EndsWith(".deps.json", StringComparison.InvariantCultureIgnoreCase))
                return new DependencyFile(filename);
            else
                return new Asset(filename);
        }
    }

    internal static List<Package> GetPackages(IEnumerable<ClusterDependency> dependencies, string packagesDirectory, IFileSystem fileSystem)
    {
        List<Package> packages = [];
        List<Exception> exceptions = [];

        foreach (var reference in dependencies)
        {
            try
            {
                packages.Add(GetPackageFromDirectory(packagesDirectory, reference.Name, reference.Version.ToString(), fileSystem));
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
        }

        return exceptions.Count > 0
            ? throw new InvalidOperationException("Could not obtain some packages.", new AggregateException(exceptions))
            : packages;
    }

    private static Dictionary<string, AssemblyDependencyResolver> GetResolverMap((string Filename, string ComponentName)[] files)
    {
        var resolvers = files
            .DistinctBy(e => e.ComponentName)
            .ToDictionary(e => e.ComponentName, e => new AssemblyDependencyResolver(e.ComponentName));
        return files.ToDictionary(e => e.Filename, e => resolvers[e.ComponentName]);
    }

    private static Version? ReadAssemblyVersion(IFileSystem fileSystem, string filename)
    {
        if (!filename.EndsWith("dll", StringComparison.OrdinalIgnoreCase) || !fileSystem.File.Exists(filename))
            return default;

        Version version = new();
        using var stream = fileSystem.File.OpenRead(filename);
        using PEReader peReader = new(stream);
        if (peReader.HasMetadata)
        {
            var metaDataReader = peReader.GetMetadataReader();
            if (metaDataReader.IsAssembly)
            {
                var assemblyDefinition = metaDataReader.GetAssemblyDefinition();
                version = assemblyDefinition.Version;
            }
        }

        return version;
    }

    public static IReadOnlyDictionary<string, AssemblyDependencyResolver> Resolve(IEnumerable<ClusterDependency> dependencies, string packagesDirectory, IFileSystem fileSystem)
    {
        var packages = GetPackages(dependencies, packagesDirectory, fileSystem);
        var files = packages
            .SelectMany(p => p.Assets.Select(a => (Asset: a, Package: p)))
            .GroupBy(e => fileSystem.Path.GetFileNameWithoutExtension(e.Asset.Filename), e => new AssetFileCandidate(e.Asset.Filename, ReadAssemblyVersion(fileSystem, e.Asset.Filename), e.Package))
            .Select(e => (e.Key, e.Aggregate((a1, a2) => ByHighestVersionOrLastFile(a1, a2, fileSystem)).Package))
            .ToArray();
        var filesWithMainComponents = ToMainComponents(files).ToArray();
        var filesWithResolvers = GetResolverMap(filesWithMainComponents);

        return filesWithResolvers;
    }

    internal static IEnumerable<(string Filename, string PackageMainComponent)> ToMainComponents(this (string Filename, Package)[] files)
    {
        Dictionary<Package, string> components = [];

        foreach (var (filename, package) in files)
        {
            if (!components.TryGetValue(package, out var componentFilename))
            {
                componentFilename = package.Assets.OfType<DependencyFile>().First().Filename
                    .Replace(".deps.json", ".dll", StringComparison.OrdinalIgnoreCase);
                components.Add(package, componentFilename);
            }

            yield return (filename, componentFilename);
        }
    }

    internal sealed record Package(string Directory, IReadOnlyCollection<Asset> Assets);
    internal record Asset(string Filename);
    internal sealed record DependencyFile(string Filename) : Asset(Filename);
    internal sealed record AssetFileCandidate(string Filename, Version? Version, Package Package);
}
