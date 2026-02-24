using Semver;

namespace Shared.Designs;

public sealed record PackageInfo(string Name, SemVersion Version);
