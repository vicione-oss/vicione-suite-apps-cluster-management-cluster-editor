using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.ClusterSerialization;

internal static class DefaultJsonSerializerSettings
{
    public sealed class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDateTime().ToUniversalTime();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToUniversalTime());
    }

    public static readonly JsonSerializerOptions Default = new()
    {
        Converters = { new DateTimeConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
}
