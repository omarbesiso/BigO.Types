namespace BigO.Types.Tests;

/// <summary>
///     Unit tests for core <see cref="DateRange" /> behavior.
/// </summary>
public sealed class DateRangeCoreTests
{
    private const char Infinity = '\u221E';
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void Default_Semantics_Are_Min_To_Infinity()
    {
        var r = default(DateRange);
        Assert.Equal(DateOnly.MinValue, r.StartDate);
        Assert.Null(r.EndDate);
        Assert.True(r.IsOpenEnded);
        Assert.Equal(DateOnly.MaxValue, r.EffectiveEnd);
        Assert.Equal($"0001-01-01|{Infinity}", r.ToString());
    }

    [Fact]
    public void Constructor_Allows_EndEqualStart()
    {
        var d = D(2025, 8, 1);
        var r = new DateRange(d, d);
        Assert.False(r.IsOpenEnded);
        Assert.Equal(d, r.StartDate);
        Assert.Equal(d, r.EndDate);
    }

    [Fact]
    public void Constructor_Throws_When_EndBeforeStart()
    {
        var start = D(2025, 8, 2);
        var end = D(2025, 8, 1);
        _ = Assert.ThrowsAny<ArgumentException>(() => new DateRange(start, end));
    }

    [Fact]
    public void Equality_And_Operators_Work_For_Open_And_Closed()
    {
        var a1 = new DateRange(D(2025, 1, 1));
        var a2 = new DateRange(D(2025, 1, 1));
        Assert.True(a1.Equals(a2));
        Assert.True(a1 == a2);
        Assert.False(a1 != a2);
        Assert.Equal(a1.GetHashCode(), a2.GetHashCode());

        var b1 = new DateRange(D(2025, 1, 1), D(2025, 1, 31));
        var b2 = new DateRange(D(2025, 1, 1), D(2025, 1, 31));
        Assert.True(b1.Equals(b2));
        Assert.True(b1 == b2);
        Assert.False(b1 != b2);
        Assert.Equal(b1.GetHashCode(), b2.GetHashCode());

        var c1 = new DateRange(D(2025, 1, 1), D(2025, 1, 31));
        var c2 = new DateRange(D(2025, 1, 2), D(2025, 1, 31));
        Assert.False(c1.Equals(c2));
        Assert.False(c1 == c2);
        Assert.True(c1 != c2);
    }

    [Fact]
    public void Deconstruct_Works_For_Open_And_Closed()
    {
        var open = new DateRange(D(2025, 1, 1));
        open.Deconstruct(out var s1, out var e1);
        Assert.Equal(D(2025, 1, 1), s1);
        Assert.Null(e1);

        var closed = new DateRange(D(2025, 1, 1), D(2025, 1, 2));
        closed.Deconstruct(out var s2, out var e2);
        Assert.Equal(D(2025, 1, 1), s2);
        Assert.Equal(D(2025, 1, 2), e2);
    }

    [Fact]
    public void EffectiveEnd_Is_Max_For_OpenEnded_Otherwise_End()
    {
        var open = new DateRange(D(2025, 1, 1));
        Assert.Equal(DateOnly.MaxValue, open.EffectiveEnd);

        var closed = new DateRange(D(2025, 1, 1), D(2025, 1, 5));
        Assert.Equal(D(2025, 1, 5), closed.EffectiveEnd);
    }
}