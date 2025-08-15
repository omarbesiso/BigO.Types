namespace BigO.Types.Tests;

public sealed class DateRangeExtensionsOverlapsIntersectionTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void Overlaps_True_When_Sharing_Boundary_Day()
    {
        var a = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        var b = new DateRange(D(2025, 1, 10), D(2025, 2, 1));
        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    [Fact]
    public void Overlaps_False_When_Disjoint()
    {
        var a = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        var b = new DateRange(D(2025, 1, 11), D(2025, 1, 20));
        Assert.False(a.Overlaps(b));
        Assert.False(b.Overlaps(a));
    }

    [Fact]
    public void Overlaps_With_OpenEnded()
    {
        var open = new DateRange(D(2025, 1, 10));
        var closedBefore = new DateRange(D(2025, 1, 1), D(2025, 1, 9));
        var closedCross = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        var closedAfter = new DateRange(D(2025, 1, 11), D(2025, 1, 20));

        Assert.False(open.Overlaps(closedBefore));
        Assert.True(open.Overlaps(closedCross));
        Assert.True(open.Overlaps(closedAfter));
    }

    [Fact]
    public void Intersection_Returns_Null_When_No_Overlap()
    {
        var a = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        var b = new DateRange(D(2025, 1, 11), D(2025, 1, 20));
        Assert.Null(a.Intersection(b));
    }

    [Fact]
    public void Intersection_Boundary_Is_Single_Day()
    {
        var a = new DateRange(D(2025, 1, 1), D(2025, 1, 10));
        var b = new DateRange(D(2025, 1, 10), D(2025, 1, 20));
        var i = a.Intersection(b);
        Assert.NotNull(i);
        Assert.Equal(new DateRange(D(2025, 1, 10), D(2025, 1, 10)), i!.Value);
    }

    [Fact]
    public void Intersection_With_OpenEnded_And_Closed()
    {
        var open = new DateRange(D(2025, 1, 10));
        var closed = new DateRange(D(2025, 1, 5), D(2025, 1, 12));
        var i = open.Intersection(closed);
        Assert.NotNull(i);
        Assert.Equal(new DateRange(D(2025, 1, 10), D(2025, 1, 12)), i!.Value);
    }

    [Fact]
    public void Intersection_Both_OpenEnded_Remains_OpenEnded()
    {
        var a = new DateRange(D(2025, 1, 1));
        var b = new DateRange(D(2025, 1, 10));
        var i = a.Intersection(b);
        Assert.NotNull(i);
        Assert.Equal(new DateRange(D(2025, 1, 10)), i!.Value);
    }

    [Fact]
    public void Intersection_With_Default_Range_Behavior()
    {
        var def = default(DateRange);
        var other = new DateRange(D(2020, 1, 1), D(2020, 1, 31));
        Assert.Equal(other, def.Intersection(other));
        Assert.Equal(def, def.Intersection(def));
    }
}