namespace BigO.Types.Tests;

/// <summary>
///     Tests for the <see cref="DateRangeExtensions.Contains(DateRange, DateOnly)" /> extension method.
/// </summary>
public sealed class DateRangeExtensionsContainsTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void Contains_Returns_True_For_Boundaries_And_Interior()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        Assert.True(r.Contains(D(2025, 1, 1)));
        Assert.True(r.Contains(D(2025, 1, 5)));
        Assert.True(r.Contains(D(2025, 1, 10)));
    }

    [Fact]
    public void Contains_Returns_False_Outside()
    {
        var r = new DateRange(D(2025, 1, 2), D(2025, 1, 10));
        Assert.False(r.Contains(D(2025, 1, 1)));
        Assert.False(r.Contains(D(2025, 1, 11)));
    }

    [Fact]
    public void Contains_OpenEnded_Includes_Max()
    {
        var r = new DateRange(D(2025, 1, 1)); // open-ended
        Assert.True(r.Contains(DateOnly.MaxValue));
    }

    [Fact]
    public void Default_Range_Contains_All_Valid_Dates()
    {
        var r = default(DateRange);
        Assert.True(r.Contains(DateOnly.MinValue));
        Assert.True(r.Contains(D(2000, 1, 1)));
        Assert.True(r.Contains(DateOnly.MaxValue));
    }
}