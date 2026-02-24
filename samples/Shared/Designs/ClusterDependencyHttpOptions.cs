using Microsoft.Extensions.Configuration;

namespace Shared.Designs;

public class ClusterDependencyHttpOptions
{
    private const string ConfigKey = "ClusterDependencyHttp";

    public string? DataPortSet { get; set; }
    public bool Disable { get; set; }
    public string? HttpApi { get; set; }
    public string? Password { get; set; }
    public string? User { get; set; }

    public static ClusterDependencyHttpOptions GetValidatedOptions(IConfiguration config)
    {
        var options = config.GetSection(ConfigKey)
                    .Get<ClusterDependencyHttpOptions>()
                    ?? throw new InvalidOperationException($"Missing config section {ConfigKey}");

        if (options.Disable)
            return options;

        return options;
    }
}
