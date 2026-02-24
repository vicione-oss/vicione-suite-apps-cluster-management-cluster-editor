using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.Json;
using Semver;
using ViciOne.Cluster.Model;

namespace Shared.Designs;

public static class PackagesFileProvider
{
    internal const string PackagesFileExtension = "json";
    internal const string PackagesFileName = "packages";

    public static async Task<ClusterDependency[]> DeserializePackagesFile(string filename, JsonSerializerOptions serializerOptions, CancellationToken cancellationToken)
    {
        await using FileStream fileStream = new(filename, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Options = FileOptions.Asynchronous,
            Share = FileShare.Read,
        });
        return await JsonSerializer.DeserializeAsync<ClusterDependency[]>(fileStream, serializerOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to deserialize {PackagesFileName} file '{filename}'.");
    }

    public static IDictionary<PackageInfo, string> GetComponents(string directory, IFileSystem fileSystem)
    {
        Dictionary<PackageInfo, string> components = [];

        var mainComponentFilenames = fileSystem.Directory.GetFiles(directory, "*.deps.json", SearchOption.AllDirectories);

        foreach (var mainComponentFilename in mainComponentFilenames)
        {
            var versionDirectory = fileSystem.Path.GetDirectoryName(mainComponentFilename) ?? string.Empty;
            var rawVersion = fileSystem.Path.GetFileName(versionDirectory) ?? string.Empty;
            var packageName = fileSystem.Path.GetFileName(fileSystem.Path.GetDirectoryName(versionDirectory)) ?? string.Empty;

            if (string.IsNullOrEmpty(rawVersion) || string.IsNullOrEmpty(packageName))
                throw new InvalidOperationException("The packages directory should have the following structure: 'packages/MyPackage/1.2.3'.");

            if (!SemVersion.TryParse(rawVersion, out var version))
                throw new InvalidOperationException($"The version of the package '{packageName}' is not a valid SemVer. Or the directory structure is not correct. (Expected: 'packages/MyPackage/1.2.3')");

            if (!fileSystem.Path.IsPathRooted(versionDirectory))
                versionDirectory = fileSystem.Path.GetFullPath(versionDirectory);

            if (!components.TryAdd(new(packageName, version), new(versionDirectory)))
                throw new InvalidOperationException($"The package '{packageName}' with version '{version}' is already added.");
        }

        return components;
    }

    /// <summary>
    /// Returns the path to the packages file. If a variant is specified, the path will be suffixed with the variant.
    /// Variants are always separated by a dot and be lowercase. If the variant is null or empty, the path will be
    /// without variant. 
    /// e.g. packages.json, packages.variant.json
    /// </summary>
    [SuppressMessage("Globalization", "CA1308:Zeichenfolgen in Großbuchstaben normalisieren")]
    public static string GetPackagesFilename(string baseDirectory, string? variant, IFileSystem fileSystem)
    {
        string filename;

        if (string.IsNullOrEmpty(variant))
            filename = $"{PackagesFileName}.{PackagesFileExtension}";
        else
            filename = $"{PackagesFileName}.{variant.ToLowerInvariant()}.{PackagesFileExtension}";

        return fileSystem.Path.Combine(baseDirectory, filename);
    }

    public static IDictionary<PackageInfo, string> ResolveLatestVersion(this IDictionary<PackageInfo, string> components)
        => components
            .GroupBy(p => p.Key.Name)
            .Select(g => g.OrderByDescending(p => p.Key.Version, SemVersion.PrecedenceComparer).First())
            .ToDictionary();
}
