namespace Shared.Designs;

internal sealed record PackageArtifactLoadResult(string SourcePath, string TargetPath)
{
    public Exception? Error { get; set; }
    public bool Skipped { get; set; }
}
