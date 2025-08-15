namespace BigO.Types.Tests;

/// <summary>
///     Tests for duration-related extension methods on <see cref="DateRange" />.
/// </summary>
public sealed class DateRangeExtensionsDurationTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void Duration_Finite_Range_Is_Inclusive()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 1));
        Assert.Equal(1, r.Duration());

        var r2 = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        Assert.Equal(10, r2.Duration());
    }

    [Fact]
    public void Duration_Max_Span_Uses_DayNumbers_Inclusive()
    {
        var r = new DateRange(DateOnly.MinValue, DateOnly.MaxValue);
        var expected = DateOnly.MaxValue.DayNumber - DateOnly.MinValue.DayNumber + 1;
        Assert.Equal(expected, r.Duration());
    }

    [Fact]
    public void Duration_OpenEnded_Throws()
    {
        var r = new DateRange(D(2025, 1, 1));
        Assert.Throws<InvalidOperationException>(() => r.Duration());
    }

    [Fact]
    public void TryGetDuration_Finite_And_OpenEnded()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 3));
        Assert.True(r.TryGetDuration(out var days));
        Assert.Equal(3, days);

        var open = new DateRange(D(2025, 1, 1));
        Assert.False(open.TryGetDuration(out var d2));
        Assert.Equal(0, d2);
    }
}