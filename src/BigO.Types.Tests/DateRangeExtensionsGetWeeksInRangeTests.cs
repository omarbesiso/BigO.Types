namespace BigO.Types.Tests;

/// <summary>
///     Tests for splitting <see cref="DateRange" /> values into week-sized chunks.
/// </summary>
public sealed class DateRangeExtensionsGetWeeksInRangeTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void ClosedRange_LessThan7Days_YieldsSingleChunk()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 3)); // 3 days
        var chunks = r.GetWeeksInRange().ToArray();
        Assert.Single(chunks);
        Assert.Equal(new DateRange(D(2025, 1, 1), D(2025, 1, 3)), chunks[0]);
    }

    [Fact]
    public void ClosedRange_Exactly7Days_YieldsSingle_7Day_Chunk()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 7));
        var chunks = r.GetWeeksInRange().ToArray();
        Assert.Single(chunks);
        Assert.Equal(new DateRange(D(2025, 1, 1), D(2025, 1, 7)), chunks[0]);
    }

    [Fact]
    public void ClosedRange_FifteenDays_Yields_Three_Chunks()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 15)); // 15 days: [1..7],[8..14],[15..15]
        var chunks = r.GetWeeksInRange().ToArray();
        Assert.Equal(3, chunks.Length);
        Assert.Equal(new DateRange(D(2025, 1, 1), D(2025, 1, 7)), chunks[0]);
        Assert.Equal(new DateRange(D(2025, 1, 8), D(2025, 1, 14)), chunks[1]);
        Assert.Equal(new DateRange(D(2025, 1, 15), D(2025, 1, 15)), chunks[2]);
    }

    [Fact]
    public void ClosedRange_MaxWeeks_Caps_Results()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 31)); // spans 5 chunks (7,7,7,7,3)
        var chunks = r.GetWeeksInRange(2).ToArray();
        Assert.Equal(2, chunks.Length);
    }

    [Fact]
    public void ClosedRange_MaxWeeks_Zero_Returns_No_Results()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 31));
        var chunks = r.GetWeeksInRange(0).ToArray();
        Assert.Empty(chunks);
    }

    [Fact]
    public void ClosedRange_Negative_MaxWeeks_Throws()
    {
        var r = new DateRange(D(2025, 1, 1), D(2025, 1, 31));
        Assert.Throws<ArgumentException>(() => r.GetWeeksInRange(-1).ToArray());
    }

    [Fact]
    public void OpenEnded_Null_MaxWeeks_Throws()
    {
        var r = new DateRange(D(2025, 1, 1)); // open
        Assert.Throws<ArgumentNullException>(() => r.GetWeeksInRange().ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OpenEnded_NonPositive_MaxWeeks_Throws(int cap)
    {
        var r = new DateRange(D(2025, 1, 1)); // open
        Assert.Throws<ArgumentException>(() => r.GetWeeksInRange(cap).ToArray());
    }

    [Fact]
    public void OpenEnded_Produces_OpenEnded_Final_Chunk_When_Hitting_MaxSupportedDate()
    {
        // start so that the first chunk ends at MaxValue
        var start = DateOnly.MaxValue.AddDays(-6);
        var r = new DateRange(start); // open
        var chunks = r.GetWeeksInRange(1).ToArray();
        Assert.Single(chunks);
        Assert.Equal(new DateRange(start), chunks[0]); // end remains null (open-ended)
    }

    [Fact]
    public void OpenEnded_With_Cap_Emits_Closed_Chunks_Unless_MaxValue_Reached()
    {
        var r = new DateRange(D(2025, 1, 1)); // open
        var chunks = r.GetWeeksInRange(2).ToArray();
        Assert.Equal(2, chunks.Length);
        Assert.Equal(new DateRange(D(2025, 1, 1), D(2025, 1, 7)), chunks[0]);
        Assert.Equal(new DateRange(D(2025, 1, 8), D(2025, 1, 14)), chunks[1]);
    }
}