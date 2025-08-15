using System.Text.Encodings.Web;
using System.Text.Json;

namespace BigO.Types.Tests;

public sealed class DateRangeJsonConverterTests
{
    private const string InfinityLiteral = "\u221E";
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void Serialize_Closed_And_OpenEnded_As_Single_String()
    {
        var closed = new DateRange(D(2025, 8, 1), D(2025, 8, 15));
        var open = new DateRange(D(2025, 8, 1));

        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var jsClosed = JsonSerializer.Serialize(closed);
        var jsOpen = JsonSerializer.Serialize(open, options);

        Assert.Equal("\"2025-08-01|2025-08-15\"", jsClosed);
        Assert.Equal($"\"2025-08-01|{InfinityLiteral}\"", jsOpen);
    }

    [Fact]
    public void Deserialize_Closed_And_OpenEnded_From_String()
    {
        var closed = JsonSerializer.Deserialize<DateRange>("\"2025-08-01|2025-08-15\"");
        var open = JsonSerializer.Deserialize<DateRange>($"\"2025-08-01|{InfinityLiteral}\"");

        Assert.Equal(new DateRange(D(2025, 8, 1), D(2025, 8, 15)), closed);
        Assert.Equal(new DateRange(D(2025, 8, 1)), open);
    }

    [Fact]
    public void Roundtrip_Through_Containing_Type()
    {
        var w = new Wrapper { Range = new DateRange(D(2025, 1, 1), D(2025, 1, 3)) };
        var json = JsonSerializer.Serialize(w);
        var w2 = JsonSerializer.Deserialize<Wrapper>(json);
        Assert.NotNull(w2);
        Assert.Equal(w.Range, w2!.Range);
    }

    [Fact]
    public void Deserialize_Invalid_Token_Type_Throws()
    {
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateRange>("123"));
        Assert.Contains("Expected a JSON string", ex.Message);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public void Deserialize_Empty_Or_Whitespace_String_Throws(string json)
    {
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateRange>(json));
        Assert.Contains("cannot be null/empty", ex.Message);
    }

    [Fact]
    public void Deserialize_Invalid_Format_Throws_With_InvalidFormatMessage()
    {
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateRange>("\"2025-08-01|\""));
        Assert.Equal(DateRange.InvalidFormatMessage, ex.Message);
    }

    private sealed class Wrapper
    {
        public DateRange Range { get; init; }
    }
}