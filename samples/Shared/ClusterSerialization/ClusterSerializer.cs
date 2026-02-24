using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.Cluster.Model;

namespace Shared.ClusterSerialization;

public static class ClusterSerializer
{
    /// <summary>
    /// We need to be aware that SerializerOptions should only get created after the fbs are loaded to be
    /// able to access their types from their prefixed LoadContexts
    /// </summary>
    public static JsonSerializerOptions? TypedSerializerOptions { get; private set; }
    public static JsonSerializerOptions UntypedSerializerOptions { get; } = new()
    {
        Converters = { new TypeJsonConverter(), },
    };

    public static byte[] Compress(Cluster cluster, JsonSerializerOptions? serializerOptions = null)
    {
        using MemoryStream ms = new();
        using (GZipStream gs = new(ms, CompressionMode.Compress))
            JsonSerializer.Serialize(gs, cluster, serializerOptions ?? UntypedSerializerOptions);
        return ms.ToArray();
    }

    public static byte[] Compress(string str)
    {
        using MemoryStream ms = new();
        using GZipStream gs = new(ms, CompressionMode.Compress);
        using (StreamWriter writer = new(gs))
            writer.WriteAsync(str);
        return ms.ToArray();
    }

    public static JsonSerializerOptions CreateSerializerOptions()
    {
        var assemblies = GetAssemblies().ToArray();
        return new(DefaultJsonSerializerSettings.Default)
        {
            Converters =
            {
                new TypeJsonConverter(),
                new TypeNameHandlingConfig(additionalAssemblies: assemblies.Length > 0 ? assemblies : null, useTypeCache: false),
            },
        };
    }

    public static async Task<Cluster> Decompress(byte[] bytes, JsonSerializerOptions? serializerOptions = null, CancellationToken cancellationToken = default)
        => await Decompress(new MemoryStream(bytes), serializerOptions, cancellationToken);

    public static async Task<Cluster> Decompress(Stream stream, JsonSerializerOptions? serializerOptions = null, CancellationToken cancellationToken = default)
    {
        using GZipStream compressedStream = new(stream, CompressionMode.Decompress);
        return await JsonSerializer.DeserializeAsync<Cluster>(compressedStream, serializerOptions ?? TypedSerializerOptions ?? UntypedSerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to deserialize decompressed {nameof(Cluster)}");
    }

    public static Cluster Deserialize(string clusterJson, JsonSerializerOptions? serializerOptions = null)
        => JsonSerializer.Deserialize<Cluster>(clusterJson, serializerOptions ?? TypedSerializerOptions ?? UntypedSerializerOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize {nameof(Cluster)}");

    private static Assembly[] GetAssemblies()
        => [.. AssemblyLoadContext.All
            .Where(c => c.Name?.Equals(Constants.PackagesContext, StringComparison.Ordinal) is true)
            .SelectMany(c => c.Assemblies)
            .Where(a => !a.IsDynamic)];

    public static string Serialize(Cluster cluster, JsonSerializerOptions? serializerOptions = null)
        => JsonSerializer.Serialize(cluster, serializerOptions ?? UntypedSerializerOptions);

    public static void SetTypedSerializerOptions()
    {
        if (TypedSerializerOptions is not null)
            return;

        if (!AssemblyLoadContext.All.Any(a => a.Name?.Equals(Constants.PackagesContext, StringComparison.Ordinal) is true))
            throw new InvalidOperationException("Cannot find types.");

        TypedSerializerOptions = CreateSerializerOptions();
    }
}
