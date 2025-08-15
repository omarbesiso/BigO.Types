using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigO.Types;

/// <summary>
///     JSON converter for <see cref="DateRange" /> that (de)serializes a single canonical string using invariant culture:
///     <c>yyyy-MM-dd|yyyy-MM-dd</c> or <c>yyyy-MM-dd|∞</c> (Unicode infinity for open-ended).
///     Round-trips with <see cref="DateRange.ToString()" />/<see cref="DateRange.TryParse(string?, out DateRange)" />.
/// </summary>
public sealed class DateRangeConverter : JsonConverter<DateRange>
{
    /// <inheritdoc />
    public override DateRange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a JSON string for DateRange.");
        }

        var dateRangeString = reader.GetString();
        if (string.IsNullOrWhiteSpace(dateRangeString))
        {
            throw new JsonException("DateRange string cannot be null/empty.");
        }

        if (!DateRange.TryParse(dateRangeString, out var range))
        {
            throw new JsonException(DateRange.InvalidFormatMessage);
        }

        return range;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateRange value, JsonSerializerOptions options)
    {
        Span<char> buffer = stackalloc char[DateRange.MaxFormattedLength];
        if (value.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
        {
            writer.WriteStringValue(buffer[..written]);
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}