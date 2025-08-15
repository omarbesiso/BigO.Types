using System.Diagnostics;
using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace BigO.Types;

/// <summary>
///     Represents an email address value object with validation and normalization.
/// </summary>
/// <remarks>
///     <para>
///         <b>Construction:</b> Use <see cref="From(string)" />, <see cref="Parse(string)" />, or
///         <see cref="TryParse(string, out EmailAddress)" />.
///     </para>
///     <para>
///         <b>Normalization:</b> input is trimmed; the domain part is converted to lower case and IDN-normalized to ASCII
///         (Punycode).
///         The local part is preserved as entered.
///     </para>
///     <para>
///         <b>Equality and ordering:</b> comparisons are case-insensitive (OrdinalIgnoreCase) over the entire address
///         to match common mailbox behavior. If you require strict RFC-local-part sensitivity, switch to Ordinal and
///         document it.
///     </para>
///     <para>
///         <b>Default value:</b> <c>default(EmailAddress)</c> is not valid and throws
///         <see cref="InvalidOperationException" /> when used.
///     </para>
///     <para><b>Length:</b> <see cref="MaxLength" /> (254) is enforced <i>after</i> normalization.</para>
///     <para>
///         <b>Thread safety:</b> This type is immutable and thread-safe.
///     </para>
/// </remarks>
[DebuggerDisplay("{_value,nq}")]
// ReSharper disable once InconsistentNaming
public readonly record struct EmailAddress : IComparable<EmailAddress>, IParsable<EmailAddress>
{
    /// <summary>
    ///     Commonly enforced maximum email length per RFC 5321 (total length of local@domain).
    /// </summary>
    public const int MaxLength = 254;

    private readonly string? _value;

    private EmailAddress(string value)
    {
        _value = value;
    }

    /// <summary>The normalized string value. Throws if the instance is the default value.</summary>
    public string Value => _value ?? throw new InvalidOperationException(
        "Uninitialized EmailAddress (default). Use EmailAddress.From/Parse/TryParse.");

    /// <summary>
    ///     Gets the local part of the email address (before the @).
    /// </summary>
    public string Local => Value[..Value.LastIndexOf('@')];

    /// <summary>
    ///     Gets the domain part of the email address (after the @).
    /// </summary>
    public string Domain => Value[(Value.LastIndexOf('@') + 1)..];

    /// <inheritdoc />
    public int CompareTo(EmailAddress other)
    {
        if (_value == null)
        {
            return other._value == null ? 0 : -1;
        }

        if (other._value == null)
        {
            return 1;
        }

        return StringComparer.OrdinalIgnoreCase.Compare(_value, other._value);
    }

    /// <summary>
    ///     Check if this email address is equal to another <see cref="EmailAddress" /> instance.
    /// </summary>
    /// <param name="other">The other email address to compare with.</param>
    /// <returns>True if both email addresses are equal (case-insensitive), otherwise false.</returns>
    public bool Equals(EmailAddress other) =>
        _value != null && other._value != null &&
        StringComparer.OrdinalIgnoreCase.Equals(_value, other._value);

    /// <inheritdoc />
    public static EmailAddress Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc />
    public static bool TryParse(string? s, IFormatProvider? provider, out EmailAddress value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        var normalized = Normalize(s);

        if (normalized.Any(char.IsWhiteSpace))
        {
            // Reject if any whitespace remains after normalization
            return false;
        }

        // length after normalization
        if (normalized.Length > MaxLength)
        {
            return false;
        }

        // enforce local/domain length limits
        var at = normalized.LastIndexOf('@');
        if (at < 1 || at == normalized.Length - 1)
        {
            return false;
        }

        var localLen = at;
        var domain = normalized.AsSpan(at + 1);

        if (localLen > 64)
        {
            return false;
        }

        if (!IsValidDomainLength(domain))
        {
            return false;
        }

        // Leverage MailAddress parser to reject obvious invalid forms.
        try
        {
            _ = new MailAddress(normalized);
        }
        catch
        {
            return false;
        }

        value = new EmailAddress(normalized);
        return true;
    }

    /// <summary>
    ///     Create an <see cref="EmailAddress" /> from a string, validating and normalizing it.
    /// </summary>
    /// <param name="email">The email address string to validate and normalize.</param>
    /// <returns>A validated and normalized <see cref="EmailAddress" /> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is null, whitespace, or not a valid email.</exception>
    public static EmailAddress From(string? email)
    {
        if (!TryParse(email, null, out var result))
        {
            throw new ArgumentException("Invalid email address.", nameof(email));
        }

        return result;
    }

    /// <summary>Parse an email address or throw if invalid.</summary>
    /// <exception cref="FormatException">Thrown when the value is not a valid email address.</exception>
    public static EmailAddress Parse(string s) =>
        TryParse(s, null, out var result)
            ? result
            : throw new FormatException("Invalid email address.");

    /// <summary>Try to parse an email address.</summary>
    public static bool TryParse(string? s, out EmailAddress value) =>
        TryParse(s, null, out value);

    /// <summary>Return the normalized email string; empty for default instances.</summary>
    public override string ToString() => _value ?? string.Empty;

    /// <summary>Check validity using the same rules as <see cref="TryParse(string, out EmailAddress)" />.</summary>
    public static bool IsEmailAddressValid(string? email) => TryParse(email, out _);

    /// <summary>Create a <see cref="MailAddress" /> for this value.</summary>
    /// <exception cref="InvalidOperationException">If the instance is default.</exception>
    public MailAddress ToMailAddress() => new(Value);

    /// <summary>Create a <see cref="MailAddress" /> for this value with a display name.</summary>
    /// <exception cref="InvalidOperationException">If the instance is default.</exception>
    public MailAddress ToMailAddress(string? displayName) => new(Value, displayName);

    /// <summary>Create a <see cref="MailAddress" /> with display name and encoding.</summary>
    /// <exception cref="InvalidOperationException">If the instance is default.</exception>
    public MailAddress ToMailAddress(string? displayName, Encoding? displayNameEncoding) =>
        new(Value, displayName, displayNameEncoding);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _value != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_value) : 0;

    /// <summary>
    ///     Trim whitespace, split on the rightmost '@' outside quotes, lowercase and IDN-normalize the domain.
    /// </summary>
    /// <param name="input">The email address string to normalize.</param>
    /// <returns>The normalized email address string.</returns>
    private static string Normalize(string input)
    {
        var trimmed = input.Trim();

        var at = FindAtSeparator(trimmed); // important: last '@' handles quoted locals with '@'
        if (at <= 0 || at >= trimmed.Length - 1)
        {
            return trimmed; // let the parser reject it
        }

        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];

        // Convert IDN to ASCII and lower-case the domain.
        string asciiDomain;
        try
        {
            asciiDomain = new IdnMapping().GetAscii(domain).ToLowerInvariant();
        }
        catch
        {
            asciiDomain = domain.ToLowerInvariant();
        } // bad IDN -> let MailAddress fail

        return $"{local}@{asciiDomain}";
    }

    /// <summary>
    ///     Find the position of the '@' separator in an email address, respecting quoted local parts.
    /// </summary>
    /// <param name="email">The email address to search.</param>
    /// <returns>The index of the '@' character, or -1 if not found.</returns>
    private static int FindAtSeparator(string email)
    {
        var inQuotes = false;
        for (var i = 0; i < email.Length; i++)
        {
            switch (email[i])
            {
                case '"' when i == 0 || email[i - 1] != '\\':
                    inQuotes = !inQuotes;
                    break;
                case '@' when !inQuotes:
                    return i;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Validate the domain length according to RFC 1035 and RFC 5321.
    /// </summary>
    /// <param name="domain">The domain part of the email address.</param>
    /// <returns>True if the domain length is valid, false otherwise.</returns>
    private static bool IsValidDomainLength(ReadOnlySpan<char> domain)
    {
        // max 253 chars for the textual domain (no trailing dot), and each label 1..63
        if (domain.Length > 253)
        {
            return false;
        }

        var labelLen = 0;
        foreach (var ch in domain)
        {
            if (ch == '.')
            {
                if (labelLen is 0 or > 63)
                {
                    return false;
                }

                labelLen = 0;
            }
            else
            {
                labelLen++;
            }
        }

        return labelLen is > 0 and <= 63;
    }
}