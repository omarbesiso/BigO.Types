namespace BigO.Types.Tests;

/// <summary>
///     Tests for <see cref="DateRangeExtensions.EnumerateDays(DateRange, int?)" />.
/// </summary>
public sealed class DateRangeExtensionsEnumerateDaysTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void Closed_ShortRange_Yields_All_Days_Inclusive()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 3));
        var days = r.EnumerateDays().ToArray();
        Assert.Equal(new[] { D(2025, 1, 1), D(2025, 1, 2), D(2025, 1, 3) }, days);
    }

    [Fact]
    public void Closed_Range_MaxCount_Limits_Result()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        var days = r.EnumerateDays(5).ToArray();
        Assert.Equal(5, days.Length);
        Assert.Equal(D(2025, 1, 1), days[0]);
        Assert.Equal(D(2025, 1, 5), days[^1]);
    }

    [Fact]
    public void OpenEnded_With_MaxCount_Yields_Requested_Number_Of_Days()
    {
        var r = new DateRange(D(2025, 1, 1)); // open
        var days = r.EnumerateDays(10).ToArray();
        Assert.Equal(10, days.Length);
        Assert.Equal(D(2025, 1, 1), days[0]);
        Assert.Equal(D(2025, 1, 10), days[^1]);
    }

    [Fact]
    public void OpenEnded_Without_MaxCount_Can_Take_First_N()
    {
        var r = new DateRange(D(2025, 1, 1)); // open
        // Important: Do not enumerate all; just take the first 7
        var first7 = r.EnumerateDays().Take(7).ToArray();
        Assert.Equal(7, first7.Length);
        Assert.Equal(D(2025, 1, 1), first7[0]);
        Assert.Equal(D(2025, 1, 7), first7[^1]);
    }

    [Fact]
    public void EnumerateDays_MaxCount_Zero_Yields_Empty()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        var days = r.EnumerateDays(0).ToArray();
        Assert.Empty(days);
    }

    [Fact]
    public void EnumerateDays_Negative_MaxCount_Throws()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => r.EnumerateDays(-1).ToArray());
    }
}