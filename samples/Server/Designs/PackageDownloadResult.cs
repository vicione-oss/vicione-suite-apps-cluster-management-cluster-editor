namespace Shared.Designs;

internal sealed record PackageDownloadResult(string SourcePath, string TargetPath)
{
    public Exception? Error { get; set; }
    public required string PackageName { get; init; }
    public required string PackageVersion { get; init; }
    public bool Skipped { get; set; }
}
