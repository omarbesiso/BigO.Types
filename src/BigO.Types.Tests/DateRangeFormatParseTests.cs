using System.Globalization;

namespace BigO.Types.Tests;

public sealed class DateRangeFormatParseTests
{
    private const char Infinity = '\u221E';
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void ToString_Canonical_Closed()
    {
        var r = new DateRange(D(2025, 8, 1), D(2025, 8, 15));
        Assert.Equal("2025-08-01|2025-08-15", r.ToString());
    }

    [Fact]
    public void ToString_Canonical_OpenEnded()
    {
        var r = new DateRange(D(2025, 8, 1));
        Assert.Equal($"2025-08-01|{Infinity}", r.ToString());
    }

    [Fact]
    public void ToString_Is_Invariant_Ignores_CurrentCulture()
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA"); // Arabic digits in current culture
            var r = new DateRange(D(2025, 8, 1), D(2025, 8, 15));
            var s = r.ToString();
            Assert.Equal("2025-08-01|2025-08-15", s);
            foreach (var ch in s)
            {
                if (ch is >= '0' and <= '9')
                {
                    continue;
                }

                // allow dash, pipe
                Assert.True(ch == '-' || ch == '|');
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = prev;
        }
    }

    [Fact]
    public void TryFormat_Succeeds_With_Exact_Buffer_Open_And_Closed()
    {
        var closed = new DateRange(D(2025, 8, 1), D(2025, 8, 15));
        Span<char> bufClosed = stackalloc char["2025-08-01|2025-08-15".Length];
        Assert.True(closed.TryFormat(bufClosed, out var w1, default, CultureInfo.InvariantCulture));
        Assert.Equal("2025-08-01|2025-08-15", new string(bufClosed[..w1]));

        var open = new DateRange(D(2025, 8, 1));
        Span<char> bufOpen = stackalloc char[$"2025-08-01|{Infinity}".Length];
        Assert.True(open.TryFormat(bufOpen, out var w2, default, CultureInfo.InvariantCulture));
        Assert.Equal($"2025-08-01|{Infinity}", new string(bufOpen[..w2]));
    }

    [Fact]
    public void TryFormat_Fails_When_Buffer_Too_Small()
    {
        var closed = new DateRange(D(2025, 8, 1), D(2025, 8, 15));
        Span<char> tooSmallClosed = stackalloc char["2025-08-01|2025-08-15".Length - 1];
        Assert.False(closed.TryFormat(tooSmallClosed, out var w1, default, CultureInfo.InvariantCulture));
        Assert.Equal(0, w1);

        var open = new DateRange(D(2025, 8, 1));
        Span<char> tooSmallOpen = stackalloc char[$"2025-08-01|{Infinity}".Length - 1];
        Assert.False(open.TryFormat(tooSmallOpen, out var w2, default, CultureInfo.InvariantCulture));
        Assert.Equal(0, w2);
    }

    [Theory]
    [InlineData("2025-08-01|2025-08-01")]
    [InlineData("2025-08-01|2025-08-15")]
    [InlineData("2025-08-01|∞")]
    [InlineData(" 2025-08-01 | ∞ ")]
    [InlineData("\t2025-08-01|2025-08-15\t")]
    [InlineData("\n2025-08-01|2025-08-15\r")]
    public void TryParse_String_Valid_Samples(string input)
    {
        Assert.True(DateRange.TryParse(input, out var r));
        Assert.Equal("2025-08-01", r.StartDate.ToString("yyyy-MM-dd"));
        if (input.Contains('∞'))
        {
            Assert.Null(r.EndDate);
        }
        else
        {
            Assert.NotNull(r.EndDate);
        }
    }

    [Theory]
    [InlineData("2025-08-02|2025-08-01")] // end before start
    [InlineData("2025-08-01|")] // missing end
    [InlineData("|2025-08-01")] // missing start
    [InlineData("2025-08-01|2025-08-01|2025-08-02")] // too many separators
    [InlineData("2025-08-01,2025-08-02")] // wrong separator
    [InlineData("2025-8-01|∞")] // bad month format
    [InlineData("2025-02-30|∞")] // invalid date
    [InlineData("")] // empty
    [InlineData("   ")] // whitespace-only
    [InlineData("2025-08-01|infinity")] // wrong infinity token
    public void TryParse_String_Invalid_Samples(string input)
    {
        Assert.False(DateRange.TryParse(input, out _));
    }

    [Fact]
    public void Parse_String_Throws_FormatException_With_InvalidFormatMessage()
    {
        var ex1 = Assert.Throws<FormatException>(() => DateRange.Parse("2025-08-02|2025-08-01"));
        Assert.Equal(DateRange.InvalidFormatMessage, ex1.Message);

        var ex2 = Assert.Throws<FormatException>(() => DateRange.Parse(null!));
        Assert.Equal(DateRange.InvalidFormatMessage, ex2.Message);
    }

    [Fact]
    public void TryParse_Span_And_Provider_Overloads_Work()
    {
        var s = "2025-08-01|∞".AsSpan();
        Assert.True(DateRange.TryParse(s, out var r));
        Assert.True(DateRange.TryParse("2025-08-01|∞", new CultureInfo("ar-SA"), out var r2));
        Assert.Equal(r, r2);

        var parsed = DateRange.Parse(s, new CultureInfo("ar-SA"));
        Assert.Equal(r, parsed);
    }
}