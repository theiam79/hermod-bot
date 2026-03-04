using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hermod.BGStats.Converters;

public sealed class DateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString()
            ?? throw new JsonException("Expected a non-null string for DateTime.");
        return DateTime.ParseExact(value, Format, null);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}
