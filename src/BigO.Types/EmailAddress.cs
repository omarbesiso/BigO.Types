using System.Diagnostics;
using System.Globalization;
using System.Net.Mail;

namespace BigO.Types;

/// <summary>
///     Represents an immutable email address value object (DDD).
/// </summary>
/// <remarks>
///     - Construction is funneled through <see cref="From(string?, string?)" /> (explicit address + optional display name)
///     or the <see cref="IParsable{TSelf}" /> APIs (single string input, e.g. <c>John Doe &lt;john@example.com&gt;</c>).
///     - Validation leverages <see cref="MailAddress.TryCreate(string?, string?, out MailAddress?)" /> to reject invalid
///     forms
///     without using exceptions for control flow. Although <see cref="MailAddress" /> accepts many forms, other servers
///     may
///     still reject some addresses; it's a parser, not a full validator. See
///     <see href="https://learn.microsoft.com/dotnet/api/system.net.mail.mailaddress" />.
///     - Normalization trims whitespace, lower-cases the entire <see cref="Address" /> (by specification here),
///     and Title-Cases the <see cref="DisplayName" /> (culture-aware).
///     - Equality is by value (record struct). Sorting is ordinal by <see cref="Address" /> then
///     <see cref="DisplayName" />.
///     - <c>default(EmailAddress)</c> bypasses validation and should be treated as an "empty" sentinel.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly record struct EmailAddress :
    IComparable<EmailAddress>,
    IParsable<EmailAddress>,
    IFormattable
{
    // Canonical constructor used by all creation paths.
    private EmailAddress(string address, string? displayName)
    {
        Address = address;
        DisplayName = displayName;
    }

    /// <summary>
    ///     Gets the normalized email address (trimmed and lower-cased).
    ///     Guaranteed non-empty for instances created via <see cref="From(string?, string?)" />,
    ///     <see cref="Parse(string, IFormatProvider?)" />, or
    ///     <see cref="TryParse(string?, IFormatProvider?, out EmailAddress)" />.
    /// </summary>
    public string Address { get; } = string.Empty;

    /// <summary>
    ///     Gets the optional display name in Title Case, or <c>null</c> when not supplied.
    /// </summary>
    public string? DisplayName { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => DisplayName is null ? Address : $"{DisplayName} <{Address}>";

    /// <summary>
    ///     Compares this instance with another <see cref="EmailAddress" /> to provide a stable sort order.
    /// </summary>
    /// <param name="other">The other <see cref="EmailAddress" /> to compare to.</param>
    /// <returns>
    ///     Negative if this instance precedes <paramref name="other" />; zero if equal; positive if it follows.
    ///     Ordering is by <see cref="Address" /> (ordinal), then <see cref="DisplayName" /> (ordinal; <c>null</c> precedes
    ///     non-null).
    /// </returns>
    public int CompareTo(EmailAddress other)
    {
        var byAddress = StringComparer.Ordinal.Compare(Address, other.Address);
        if (byAddress != 0)
        {
            return byAddress;
        }

        return StringComparer.Ordinal.Compare(DisplayName, other.DisplayName);
    }

    /// <summary>
    ///     Formats this value using the specified format and provider.
    /// </summary>
    /// <param name="format">
    ///     Supported format specifiers:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <c>"G"</c>/<c>"g"</c> (General): <c>DisplayName &lt;Address&gt;</c> if display name exists;
    ///                 otherwise <c>Address</c>.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description><c>"A"</c>/<c>"a"</c> (Address): <c>Address</c> only.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>"F"</c>/<c>"f"</c> (Full): same as <c>"G"</c>, but explicitly intended for mail header
    ///                 display.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <param name="formatProvider">Culture used for any culture-sensitive behavior in display name formatting.</param>
    /// <returns>A formatted string representation.</returns>
    /// <exception cref="FormatException">Thrown when an unsupported format specifier is provided.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var fmt = string.IsNullOrEmpty(format) ? "G" : format!;
        switch (fmt)
        {
            case "A":
            case "a":
                return Address;

            case "F":
            case "f":
            case "G":
            case "g":
                // Use MailAddress for correct quoting/escaping semantics.
                return DisplayName is null
                    ? Address
                    : new MailAddress(Address, DisplayName).ToString();

            default:
                throw new FormatException(
                    $"The {nameof(EmailAddress)} format string '{format}' is not supported. Use 'G', 'A', or 'F'.");
        }
    }

    /// <summary>
    ///     Parses a string into an <see cref="EmailAddress" />.
    ///     Accepts either a raw address (e.g., <c>user@example.com</c>) or a combined form
    ///     (e.g., <c>John Doe &lt;user@example.com&gt;</c>).
    /// </summary>
    /// <param name="s">Input string to parse.</param>
    /// <param name="provider">
    ///     Optional <see cref="IFormatProvider" /> whose culture is used for Title Casing the display name.
    ///     If <c>null</c>, the invariant culture is used.
    /// </param>
    /// <returns>A validated and normalized <see cref="EmailAddress" />.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is null/whitespace or not a valid address.</exception>
    public static EmailAddress Parse(string s, IFormatProvider? provider)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new FormatException("Input cannot be null or whitespace.");
        }

        if (!MailAddress.TryCreate(s.Trim(), out var parsed) || parsed is null)
        {
            throw new FormatException("The input string is not a recognized email address.");
        }

        return CreateFromParsed(parsed, provider);
    }

    /// <summary>
    ///     Attempts to parse a string into an <see cref="EmailAddress" />.
    ///     Accepts either a raw address or a combined form (e.g., <c>John Doe &lt;user@example.com&gt;</c>).
    /// </summary>
    /// <param name="s">Input string to parse.</param>
    /// <param name="provider">
    ///     Optional <see cref="IFormatProvider" /> whose culture is used for Title Casing the display name.
    ///     If <c>null</c>, the invariant culture is used.
    /// </param>
    /// <param name="result">When this method returns, contains the parsed value if successful; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out EmailAddress result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        if (!MailAddress.TryCreate(s.Trim(), out var parsed) || parsed is null)
        {
            return false;
        }

        result = CreateFromParsed(parsed, provider);
        return true;
    }

    /// <summary>
    ///     Creates a new <see cref="EmailAddress" /> after validating and normalizing the input.
    /// </summary>
    /// <param name="email">The email address to parse. Must be non-empty and in a valid format.</param>
    /// <param name="displayName">
    ///     Optional display name. If provided, it is validated alongside the address, trimmed, and Title-Cased.
    ///     Empty/whitespace values become <c>null</c>.
    /// </param>
    /// <returns>A normalized <see cref="EmailAddress" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="email" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="email" /> is empty or whitespace.</exception>
    /// <exception cref="FormatException">Thrown when inputs cannot be parsed as a valid address/display name.</exception>
    public static EmailAddress From(string? email, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));

        var trimmedEmail = email!.Trim();
        var trimmedDisplay = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();

        if (trimmedDisplay is null)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (!MailAddress.TryCreate(trimmedEmail, out var parsed) || parsed is null)
            {
                throw new FormatException("The email address is not in a recognized format.");
            }

            return CreateFromParsed(parsed, CultureInfo.InvariantCulture);
        }
        else
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (!MailAddress.TryCreate(trimmedEmail, trimmedDisplay, out var parsed) || parsed is null)
            {
                throw new FormatException("The email address or display name is not in a recognized format.");
            }

            return CreateFromParsed(parsed, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    ///     Returns a string that represents the current value using the "general" format.
    ///     Equivalent to <c>ToString("G", <see cref="CultureInfo.InvariantCulture" />)</c>.
    /// </summary>
    public override string ToString() => ToString(null, CultureInfo.InvariantCulture);

    // ---------- helpers (private) ----------

    private static EmailAddress CreateFromParsed(MailAddress parsed, IFormatProvider? provider)
    {
        var normalizedAddress = NormalizeAddress(parsed.Address);
        var normalizedDisplay = NormalizeDisplayName(parsed.DisplayName, provider);
        return new EmailAddress(normalizedAddress, normalizedDisplay);
    }

    private static string NormalizeAddress(string address) =>
        // Per requirement, canonicalize the *entire* address to lower case.
        // Note: RFC 5321 says the local-part is *technically* case-sensitive;
        // this is intentionally ignored to enforce deterministic equality here.
        address.Trim().ToLowerInvariant();

    private static string? NormalizeDisplayName(string? displayName, IFormatProvider? provider)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var culture = provider as CultureInfo ?? CultureInfo.InvariantCulture;

        // Lower first to achieve consistent TitleCasing across inputs like "JOHN DOE".
        var lowered = displayName.Trim().ToLower(culture);
        return culture.TextInfo.ToTitleCase(lowered);
    }
}