using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;
using BigO.Validation;

namespace BigO.Types;

/// <summary>
///     Represents an <b>inclusive</b> date range using <see cref="DateOnly" /> with an optional open end.
///     The canonical, culture-invariant string form is <c>yyyy-MM-dd|yyyy-MM-dd</c> or <c>yyyy-MM-dd|∞</c>.
///     Parsers accept optional ASCII whitespace around tokens; formatters always emit the canonical form.
/// </summary>
/// <remarks>
///     <para>Default value: <c>default(DateRange)</c> equals <c>DateOnly.MinValue|∞</c>.</para>
///     <para>For calculations on open-ended ranges, see <see cref="EffectiveEnd" />.</para>
/// </remarks>
[JsonConverter(typeof(DateRangeConverter))]
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct DateRange : IEquatable<DateRange>, ISpanFormattable, ISpanParsable<DateRange>
{
    private const char RangeSeparator = '|';
    private const string DateFormat = "yyyy-MM-dd";
    private const char InfinityChar = '\u221E'; // "∞"

    internal static readonly int MaxFormattedLength = DateFormat.Length + 1 + DateFormat.Length;

    /// <summary>
    ///     Gets the latest supported <see cref="DateOnly" /> value.
    ///     Used as the <em>effective</em> end for calculations on open-ended ranges;
    ///     note that <see cref="EndDate" /> remains <c>null</c> for open-ended ranges.
    /// </summary>
    public static readonly DateOnly MaxSupportedDate = DateOnly.MaxValue;

    internal const string InvalidFormatMessage =
        "Invalid DateRange. Expected 'yyyy-MM-dd|yyyy-MM-dd' or 'yyyy-MM-dd|∞'.";

    /// <summary>
    ///     Initializes a new instance of <see cref="DateRange" />.
    /// </summary>
    /// <param name="startDate">
    ///     The inclusive start of the range.
    /// </param>
    /// <param name="endDate">
    ///     The inclusive end of the range; <c>null</c> denotes an open-ended range.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </exception>
    public DateRange(DateOnly startDate, DateOnly? endDate = null)
    {
        ValidateDates(startDate, endDate, nameof(startDate));
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    ///     Gets the inclusive start date of the range.
    /// </summary>
    public DateOnly StartDate { get; }

    /// <summary>
    ///     Gets the inclusive end date of the range, or <c>null</c> for an open-ended range.
    /// </summary>
    public DateOnly? EndDate { get; }

    /// <summary>Indicates whether the range is open-ended (i.e., <see cref="EndDate" /> is <c>null</c>).</summary>
    /// <remarks>
    ///     When open-ended, <see cref="EffectiveEnd" /> returns <see cref="MaxSupportedDate" />, but
    ///     <see cref="EndDate" /> remains <c>null</c>.
    /// </remarks>
    public bool IsOpenEnded => EndDate is null;

    /// <summary>
    ///     Gets the inclusive end date to use for calculations.
    ///     Returns <see cref="EndDate" /> when present; otherwise <see cref="MaxSupportedDate" />.
    ///     This property does not change <see cref="EndDate" />.
    /// </summary>
    public DateOnly EffectiveEnd => EndDate ?? MaxSupportedDate;

    /// <inheritdoc />
    public bool Equals(DateRange other) =>
        StartDate.Equals(other.StartDate) && Nullable.Equals(EndDate, other.EndDate);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DateRange other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(StartDate, EndDate);

    /// <summary>
    ///     Compares two <see cref="DateRange" /> instances for equality.
    /// </summary>
    /// <param name="left">The left-hand side <see cref="DateRange" />.</param>
    /// <param name="right">The right-hand side <see cref="DateRange" />.</param>
    /// <returns><c>true</c> if both ranges are equal; otherwise <c>false</c>.</returns>
    public static bool operator ==(DateRange left, DateRange right) => left.Equals(right);

    /// <summary>
    ///     Compares two <see cref="DateRange" /> instances for inequality.
    /// </summary>
    /// <param name="left">The left-hand side <see cref="DateRange" />.</param>
    /// <param name="right">The right-hand side <see cref="DateRange" />.</param>
    /// <returns><c>true</c> if the ranges are not equal; otherwise <c>false</c>.</returns>
    public static bool operator !=(DateRange left, DateRange right) => !left.Equals(right);

    /// <summary>Factory method equivalent to the constructor.</summary>
    public static DateRange Create(DateOnly startDate, DateOnly? endDate = null) => new(startDate, endDate);

    /// <summary>
    ///     Returns the canonical string: <c>yyyy-MM-dd|yyyy-MM-dd</c> or <c>yyyy-MM-dd|∞</c>.
    ///     Uses invariant culture, ASCII digits, and Unicode infinity (<c>∞</c>) for open-ended ranges.
    /// </summary>
    public override string ToString()
    {
        Span<char> buffer = stackalloc char[MaxFormattedLength];
        return TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture)
            ? new string(buffer[..written])
            : string.Create(CultureInfo.InvariantCulture,
                $"{StartDate:yyyy-MM-dd}{RangeSeparator}{(EndDate.HasValue ? EndDate.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : InfinityChar)}");
    }

    /// <summary>
    ///     Formats using the canonical invariant representation. The <paramref name="format" /> is ignored.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>
    ///     Attempts to format the canonical invariant representation into <paramref name="destination" />.
    ///     Only the general (canonical) format is supported; <paramref name="format" /> and <paramref name="provider" /> are
    ///     ignored.
    /// </summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        charsWritten = 0;
        var endIsOpen = !EndDate.HasValue;
        var needed = DateFormat.Length + 1 + (endIsOpen ? 1 : DateFormat.Length);
        if (destination.Length < needed)
        {
            return false;
        }

        if (!StartDate.TryFormat(destination, out var w1, DateFormat.AsSpan(), CultureInfo.InvariantCulture))
        {
            return false;
        }

        destination[w1++] = RangeSeparator;

        if (endIsOpen)
        {
            destination[w1++] = InfinityChar;
            charsWritten = w1;
            return true;
        }

        if (!EndDate!.Value.TryFormat(destination[w1..], out var w2, DateFormat.AsSpan(), CultureInfo.InvariantCulture))
        {
            return false;
        }

        charsWritten = w1 + w2;
        return true;
    }

    /// <summary>
    ///     Attempts to parse a <see cref="DateRange" /> from <paramref name="input" />.
    ///     Expected formats: <c>yyyy-MM-dd|yyyy-MM-dd</c> or <c>yyyy-MM-dd|∞</c>.
    ///     Parsing is invariant and requires exactly one <c>'|'</c> separator (whitespace around tokens is allowed).
    /// </summary>
    /// <param name="input">The input string containing start and end parts separated by <c>'|'</c>.</param>
    /// <param name="range">When successful, the parsed range; otherwise <c>default</c>.</param>
    public static bool TryParse(string? input, out DateRange range)
    {
        if (input is not null)
        {
            return TryParse(input.AsSpan(), out range);
        }

        range = default;
        return false;
    }

    /// <summary>
    ///     Span-based equivalent of <see cref="TryParse(string?, out DateRange)" />.
    /// </summary>
    /// <param name="input">The input span containing start and end parts separated by <c>'|'</c>.</param>
    /// <param name="range">When successful, the parsed range; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(ReadOnlySpan<char> input, out DateRange range)
    {
        range = default;
        if (input.IsEmpty)
        {
            return false;
        }

        var s = input.Trim();
        var sep = s.IndexOf(RangeSeparator);

        if (sep <= 0 || sep >= s.Length - 1 || s[(sep + 1)..].IndexOf(RangeSeparator) >= 0)
        {
            return false;
        }

        var left = s[..sep].Trim();
        if (!DateOnly.TryParseExact(left, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            return false;
        }

        var right = s[(sep + 1)..].Trim();
        if (right.Length == 1 && right[0] == InfinityChar)
        {
            range = new DateRange(start);
            return true;
        }

        if (!DateOnly.TryParseExact(right, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return false;
        }

        if (end < start)
        {
            return false;
        }

        range = new DateRange(start, end);
        return true;
    }

    /// <summary>
    ///     Parses a <see cref="DateRange" /> or throws <see cref="FormatException" /> if invalid.
    /// </summary>
    /// <param name="input">The input string containing start and end parts separated by <c>'|'</c>.</param>
    public static DateRange Parse(string input)
    {
        if (TryParse(input, out var range))
        {
            return range;
        }

        throw new FormatException(InvalidFormatMessage);
    }

    /// <summary>Deconstructs the range into <paramref name="startDate" /> and <paramref name="endDate" />.</summary>
    /// <param name="startDate">The inclusive start date.</param>
    /// <param name="endDate">The inclusive end date, or <c>null</c> for open-ended ranges.</param>
    public void Deconstruct(out DateOnly startDate, out DateOnly? endDate)
    {
        startDate = StartDate;
        endDate = EndDate;
    }

    /// <summary>
    ///     Validates arguments for constructing a <see cref="DateRange" />.
    /// </summary>
    /// <param name="startDateToValidate">The start date to validate.</param>
    /// <param name="endDateToValidate">The end date to validate; can be <c>null</c> for open-ended ranges.</param>
    /// <param name="startDateParamName">The parameter name for <paramref name="startDateToValidate" />.</param>
    private static void ValidateDates(DateOnly startDateToValidate, DateOnly? endDateToValidate,
        string startDateParamName)
    {
        if (endDateToValidate.HasValue)
        {
            Guard.Maximum(startDateToValidate, endDateToValidate.Value, startDateParamName,
                "Start date cannot be after end date.");
        }
    }

    /// <inheritdoc />
    public static DateRange Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, out DateRange result) => TryParse(s, out result);

    /// <inheritdoc />
    public static DateRange Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (TryParse(s, out var result))
        {
            return result;
        }

        throw new FormatException(InvalidFormatMessage);
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DateRange result) =>
        TryParse(s, out result);
}