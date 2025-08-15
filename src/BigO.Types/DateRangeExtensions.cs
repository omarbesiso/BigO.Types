using System.Runtime.CompilerServices;
using BigO.Validation;

namespace BigO.Types;

/// <summary>
///     Extensions for <see cref="DateRange" /> to provide additional functionality.
/// </summary>
public static class DateRangeExtensions
{
    /// <summary>
    ///     Returns <see langword="true" /> if <paramref name="date" /> lies within <paramref name="dateRange" /> (inclusive).
    /// </summary>
    /// <param name="dateRange">
    ///     The range to test. For open-ended ranges (<see cref="DateRange.EndDate" /> is <c>null</c>),
    ///     the check uses <see cref="DateRange.EffectiveEnd" />.
    /// </param>
    /// <param name="date">The <see cref="DateOnly" /> to test for membership.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="date" /> is between
    ///     <see cref="DateRange.StartDate" /> and <see cref="DateRange.EffectiveEnd" />, inclusive; otherwise
    ///     <see langword="false" />.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         <c>default(DateRange)</c> is equivalent to <c>DateOnly.MinValue|∞</c>;
    ///         therefore this method returns <see langword="true" /> for any valid <see cref="DateOnly" />.
    ///     </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this DateRange dateRange, DateOnly date) =>
        date >= dateRange.StartDate && date <= dateRange.EffectiveEnd;

    /// <summary>
    ///     Determines whether two <see cref="DateRange" /> values overlap.
    ///     Ranges overlap iff <c>max(startA, startB) ≤ min(endA, endB)</c>.
    /// </summary>
    /// <param name="range">The first range.</param>
    /// <param name="other">The second range.</param>
    /// <returns>
    ///     <c>true</c> if the ranges share at least one day; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     Bounds are inclusive. Open-ended ranges (where <see cref="DateRange.EndDate" /> is <c>null</c>)
    ///     are evaluated using <see cref="DateRange.EffectiveEnd" /> (i.e., <see cref="DateOnly.MaxValue" />).
    /// </remarks>
    public static bool Overlaps(this DateRange range, DateRange other)
    {
        var start = range.StartDate > other.StartDate ? range.StartDate : other.StartDate;
        var end = range.EffectiveEnd < other.EffectiveEnd ? range.EffectiveEnd : other.EffectiveEnd;
        return start <= end;
    }

    /// <summary>
    ///     Enumerates the range as contiguous 7‑day chunks anchored at <see cref="DateRange.StartDate" />.
    ///     Each yielded <see cref="DateRange" /> is inclusive; the final chunk may be shorter than 7 days.
    ///     This method does <b>not</b> align to calendar/ISO weeks—it's strictly "<c>start, start+6</c>", then step by 7.
    /// </summary>
    /// <param name="dateRange">The date range to split.</param>
    /// <param name="maxWeeks">
    ///     Optional cap on the number of emitted chunks.
    ///     For open‑ended ranges (<see cref="DateRange.EndDate" /> is <c>null</c>), this value is required and must be &gt; 0
    ///     to avoid unbounded enumeration.
    ///     For closed ranges, a negative value throws and zero returns no results.
    /// </param>
    /// <returns>
    ///     A sequence of inclusive <see cref="DateRange" /> chunks: [<c>start..min(start+6, end)</c>] stepping by 7 days.
    ///     When the original range is open‑ended and enumeration reaches <see cref="DateRange.MaxSupportedDate" />,
    ///     the final yielded chunk is open‑ended (i.e., <c>end = null</c>).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the range is open‑ended and <paramref name="maxWeeks" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the range is open‑ended and <paramref name="maxWeeks" /> is ≤ 0,
    ///     or when the range is closed and <paramref name="maxWeeks" /> is negative.
    /// </exception>
    public static IEnumerable<DateRange> GetWeeksInRange(this DateRange dateRange, int? maxWeeks = null)
    {
        var isOpenEnded = dateRange.IsOpenEnded;

        if (isOpenEnded)
        {
            Guard.NotNull(maxWeeks, "Open-ended range requires maxWeeks to avoid unbounded enumeration.");
            Guard.Positive(maxWeeks.Value, nameof(maxWeeks),
                "For open-ended ranges, maxWeeks must be greater than zero.");
        }
        else if (maxWeeks is < 0)
        {
            Guard.NonNegative(maxWeeks.Value, nameof(maxWeeks), "For closed ranges, maxWeeks cannot be negative.");
        }

        var endInclusive = dateRange.EffectiveEnd;

        var cap = maxWeeks ?? int.MaxValue;
        if (cap == 0)
        {
            yield break;
        }

        var start = dateRange.StartDate;
        var emitted = 0;

        // Drive the loop by DayNumber to avoid DateOnly.AddDays overflow near MaxSupportedDate.
        while (start <= endInclusive && emitted < cap)
        {
            var daysLeft = endInclusive.DayNumber - start.DayNumber; // >= 0
            var span = daysLeft >= 6 ? 6 : daysLeft; // 0..6
            var chunkEnd = start.AddDays(span);

            var outEnd = isOpenEnded && chunkEnd == DateRange.MaxSupportedDate
                ? (DateOnly?)null
                : chunkEnd;

            yield return new DateRange(start, outEnd);
            emitted++;

            // Compute next start safely; stop if stepping 7 days would pass endInclusive.
            var nextStartDay = start.DayNumber + 7;
            if (nextStartDay > endInclusive.DayNumber)
            {
                yield break;
            }

            start = DateOnly.FromDayNumber(nextStartDay);
        }
    }

    /// <summary>
    ///     Computes the intersection of two ranges.
    ///     Returns <c>null</c> iff there is no overlap.
    /// </summary>
    /// <param name="left">The first range.</param>
    /// <param name="right">The second range.</param>
    /// <remarks>
    ///     Bounds are inclusive. Open-ended ranges (where <see cref="DateRange.EndDate" /> is <c>null</c>)
    ///     are evaluated using <see cref="DateRange.EffectiveEnd" /> for overlap testing.
    ///     <para>
    ///         Note: <c>default(DateRange)</c> is a valid value equivalent to <c>DateOnly.MinValue|∞</c>.
    ///         Its intersection with any valid range is that other range (and with another default is default).
    ///     </para>
    /// </remarks>
    public static DateRange? Intersection(this in DateRange left, in DateRange right)
    {
        if (!left.Overlaps(right))
        {
            return null;
        }

        var start = left.StartDate > right.StartDate ? left.StartDate : right.StartDate;

        // End is the minimum of the actual ends; treat null as infinity for comparison,
        // but preserve null in the result if both are open-ended.
        var end = EndMin(left.EndDate, right.EndDate);

        return new DateRange(start, end);
    }

    /// <summary>
    ///     Lazily enumerates each day in <paramref name="dateRange" /> (inclusive).
    /// </summary>
    /// <param name="dateRange">The date range to enumerate.</param>
    /// <param name="maxCount">
    ///     Optional upper bound on the number of days to return. When <c>null</c>, the entire range is enumerated.
    /// </param>
    /// <returns>
    ///     A sequence of <see cref="DateOnly" /> values starting at <see cref="DateRange.StartDate" /> and ending at
    ///     <see cref="DateRange.EffectiveEnd" />, limited to <paramref name="maxCount" /> items when provided.
    ///     Returns an empty sequence if <paramref name="dateRange" /> is invalid (start &gt; end).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCount" /> is less than 0.</exception>
    /// <remarks>
    ///     For open-ended ranges, enumeration proceeds until <see cref="DateRange.MaxSupportedDate" /> unless
    ///     <paramref name="maxCount" /> limits it. The sequence is lazy; avoid iterating an unbounded open range unless a
    ///     limit is supplied.
    /// </remarks>
    public static IEnumerable<DateOnly> EnumerateDays(this DateRange dateRange, int? maxCount = null)
    {
        if (maxCount.HasValue)
        {
            Guard.Minimum(maxCount.Value, 0, nameof(maxCount), "maxCount must be >= 0.");
        }

        if (maxCount == 0)
        {
            yield break;
        }

        var start = dateRange.StartDate;
        var end = dateRange.EffectiveEnd;

        // Defensive: should never happen for a properly constructed DateRange.
        if (start > end)
        {
            yield break;
        }

        var startDay = start.DayNumber;
        var lastDay = end.DayNumber;

        // ReSharper disable once ConvertTypeCheckPatternToNullCheck
        if (maxCount is int limit) // limit > 0 here
        {
            // Use long to avoid overflow when startDay + limit - 1 exceeds int range (then clamp to end).
            var target = (long)startDay + limit - 1L;
            if (target < lastDay)
            {
                lastDay = (int)target;
            }
        }

        for (var day = startDay; day <= lastDay; day++)
        {
            yield return DateOnly.FromDayNumber(day);
        }
    }

    /// <summary>
    ///     Returns the inclusive number of days in <paramref name="dateRange" />.
    ///     For example, <c>2025-08-01|2025-08-01</c> returns <c>1</c>.
    /// </summary>
    /// <param name="dateRange">
    ///     The range to measure. Must be finite (<see cref="DateRange.EndDate" /> is not
    ///     <see langword="null" />).
    /// </param>
    /// <returns>The inclusive day count.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the range is open-ended.</exception>
    public static int Duration(this DateRange dateRange)
    {
        var end = dateRange.EndDate ?? throw new InvalidOperationException("Open-ended range has no finite duration.");
        return end.DayNumber - dateRange.StartDate.DayNumber + 1;
    }

    /// <summary>
    ///     Attempts to get the inclusive duration in days for <paramref name="dateRange" />.
    ///     For example, <c>2025-08-01|2025-08-01</c> yields <c>1</c>.
    /// </summary>
    /// <param name="dateRange">The date range to evaluate.</param>
    /// <param name="days">On success, receives the inclusive day count; otherwise <c>0</c>.</param>
    /// <returns>
    ///     <see langword="true" /> if the range is finite (<see cref="DateRange.EndDate" /> is not <see langword="null" />);
    ///     otherwise <see langword="false" />.
    /// </returns>
    public static bool TryGetDuration(this in DateRange dateRange, out int days)
    {
        var end = dateRange.EndDate;
        if (!end.HasValue)
        {
            days = 0;
            return false;
        }

        days = end.Value.DayNumber - dateRange.StartDate.DayNumber + 1;
        return true;
    }

    /// <summary>
    ///     Returns the minimum of two end dates, treating <c>null</c> as "infinity" (i.e., no end).
    /// </summary>
    /// <param name="a"> The first end date, or <c>null</c>.</param>
    /// <param name="b">The second end date, or <c>null</c>.</param>
    /// <returns>The minimum end date, or <c>null</c> if both are <c>null</c>.</returns>
    private static DateOnly? EndMin(DateOnly? a, DateOnly? b)
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (a is null && b is null)
        {
            return null; // ∞ with ∞
        }

        if (a is null)
        {
            return b; // min(∞, b) = b
        }

        if (b is null)
        {
            return a; // min(a, ∞) = a
        }

        return a < b ? a : b;
    }
}