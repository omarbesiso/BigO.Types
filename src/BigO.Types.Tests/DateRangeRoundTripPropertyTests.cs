using System.Text.Json;

namespace BigO.Types.Tests;

public sealed class DateRangeRoundTripPropertyTests
{
    private static readonly Random Rng = new(42);

    private static DateOnly RandDate(int minDay = 0, int maxDay = 365_2000) // ~ 10k years cap; constrained later
    {
        // Clamp to DateOnly's valid range using DayNumber
        var min = DateOnly.MinValue.DayNumber;
        var max = DateOnly.MaxValue.DayNumber;
        var span = max - min + 1;
        var offset = Rng.Next(0, span);
        return DateOnly.FromDayNumber(min + offset);
    }

    [Fact]
    public void Closed_Range_ToString_Parse_RoundTrip_Samples()
    {
        for (var i = 0; i < 100; i++)
        {
            var start = RandDate();
            // ensure we can add up to 365*5 days safely
            var maxAdd = Math.Min(365 * 5, DateOnly.MaxValue.DayNumber - start.DayNumber);
            var add = Rng.Next(0, Math.Max(1, maxAdd + 1)); // inclusive
            var end = DateOnly.FromDayNumber(start.DayNumber + add);

            var r = new DateRange(start, end);
            var s = r.ToString();

            Assert.True(DateRange.TryParse(s, out var r2));
            Assert.Equal(r, r2);

            var json = JsonSerializer.Serialize(r);
            var r3 = JsonSerializer.Deserialize<DateRange>(json);
            Assert.Equal(r, r3);
        }
    }

    [Fact]
    public void Open_Range_ToString_Parse_RoundTrip_Samples()
    {
        for (var i = 0; i < 50; i++)
        {
            var start = RandDate();
            var r = new DateRange(start);
            var s = r.ToString();

            Assert.True(DateRange.TryParse(s, out var r2));
            Assert.Equal(r, r2);

            var json = JsonSerializer.Serialize(r);
            var r3 = JsonSerializer.Deserialize<DateRange>(json);
            Assert.Equal(r, r3);
        }
    }
}