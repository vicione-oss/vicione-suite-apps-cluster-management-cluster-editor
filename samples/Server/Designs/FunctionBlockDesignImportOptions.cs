using System.Diagnostics.CodeAnalysis;

namespace Server.Designs;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by the options binder")]
internal sealed class FunctionBlockDesignImportOptions
{
    public const string ConfigSection = "DesignImport";

    // When empty, DesignLoader falls back to a single default source.
    public IReadOnlyList<FunctionBlockDesignSource> Sources { get; init; } = [];
}

internal sealed class FunctionBlockDesignSource
{
    public string Name { get; init; } = "ViciOne.Json.Import";
    public string SourcePath { get; init; } = "SourceJsons";
    public string Version { get; init; } = "1.0.0";
}
