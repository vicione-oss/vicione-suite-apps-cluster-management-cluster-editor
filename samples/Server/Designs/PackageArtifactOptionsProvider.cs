using System.Diagnostics.CodeAnalysis;
using Core.Artifacts;

namespace Server.Designs;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used in testing")]
internal sealed class PackageArtifactOptionsProvider(IConfiguration config) : IArtifactRepositoryOptionsProvider
{
    public ArtifactRepositoryOptions GetOptions()
        => config.GetSection(ArtifactRepositoryOptions.ConfigSection)
            .Get<ArtifactRepositoryOptions>()
            ?? throw new InvalidOperationException($"Configure settings section '{ArtifactRepositoryOptions.ConfigSection}' to support artifact downloads");
}
