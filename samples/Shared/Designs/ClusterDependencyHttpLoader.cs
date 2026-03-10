using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Options;

namespace Shared.Designs;

public sealed class ClusterDependencyHttpLoader : IDisposable
{
    private const string NexusFolder = "fbs";

    private readonly string _apiUrl;
    private readonly HttpClient _client = new();
    private readonly string _downloadPath;

    public ClusterDependencyHttpLoader(IOptions<ClusterDependencyHttpOptions> options, string downloadPath)
    {
        if (string.IsNullOrEmpty(options.Value.HttpApi))
            throw new ArgumentException(nameof(options.Value.HttpApi));

        _apiUrl = options.Value.HttpApi;

        if (!string.IsNullOrEmpty(options.Value.User) && !string.IsNullOrEmpty(options.Value.Password))
        {
            var byteArray = Encoding.ASCII.GetBytes($"{options.Value.User}:{options.Value.Password}");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        _downloadPath = downloadPath;
    }

    private Uri CreateUri(string packageName, string version, string runtimeIdentifier)
    {
        var uriBuilder = new UriBuilder(_apiUrl);
        uriBuilder.Path = Path.Combine(
            uriBuilder.Path,
            NexusFolder,
            packageName,
            $"{version}-{runtimeIdentifier}.zip"
        );

        return uriBuilder.Uri;
    }

    private void DeleteExistingVersions(string name, string version)
    {
        var targetPath = Path.Combine(_downloadPath, name);

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

    private static bool DependencyPathExists(string path)
        => Directory.Exists(path) && Directory.GetFiles(path).Length > 0;

    public void Dispose()
        => _client.Dispose();

    public async Task<ClusterDependencyHttpLoaderResult> DownloadAndExtractAsync(string name, string version, CancellationToken cancellationToken)
    {
        var sourceUri = CreateUri(name, version, RuntimeInformation.RuntimeIdentifier);
        var targetPath = Path.Combine(
            _downloadPath,
            name,
            version);

        var result = new ClusterDependencyHttpLoaderResult(sourceUri.AbsoluteUri, targetPath);

        try
        {
            DeleteExistingVersions(name, version);

            if (DependencyPathExists(targetPath))
            {
                result.Skipped = true;
                return result;
            }

            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            await ExtractDownloadStreamAsync(sourceUri, targetPath, cancellationToken);
        }
        catch (Exception e)
        {
            result.Error = e;
        }

        return result;
    }

    private async Task ExtractDownloadStreamAsync(Uri sourceUri, string targetPath, CancellationToken cancellationToken)
    {
        await using var httpStream = await _client.GetStreamAsync(sourceUri, cancellationToken);
        await using var ms = new MemoryStream();

        await httpStream.CopyToAsync(ms, cancellationToken);
        await httpStream.DisposeAsync();

        using var archive = new ZipArchive(ms);
        foreach (var archiveEntry in archive.Entries)
        {
            if (archiveEntry.FullName.EndsWith('/'))
            {
                var entryFolderPath = Path.Combine(targetPath, archiveEntry.FullName);
                Directory.CreateDirectory(entryFolderPath);
                continue;
            }

            var entryFilePath = Path.Combine(targetPath, archiveEntry.FullName);
            await archiveEntry.ExtractToFileAsync(entryFilePath, true, cancellationToken);
        }
    }
}
