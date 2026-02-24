namespace Shared.Designs;

public record ClusterDependencyHttpLoaderResult(string SourcePath, string TargetPath)
{
    public Exception? Error { get; set; }
    public bool Skipped { get; set; }
}
