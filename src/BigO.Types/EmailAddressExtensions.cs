using System.Net.Mail;
using System.Text;

namespace BigO.Types;

/// <summary>
///     Extension methods for <see cref="EmailAddress" /> focused on presentation and interop.
/// </summary>
/// <remarks>
///     Performance notes:
///     - Methods avoid re-parsing; they operate on the normalized fields already enforced by <see cref="EmailAddress" />.
///     - String work is O(n) with a single scan for username/domain extraction.
///     - <see cref="ToDisplayString(EmailAddress)" /> uses <see cref="MailAddress" /> only when a display name is present,
///     to ensure correct quoting/escaping; otherwise it returns the address directly.
/// </remarks>
public static class EmailAddressExtensions
{
    /// <summary>
    ///     Returns a display string for the email address.
    ///     If a display name exists, returns <c>Display Name &lt;address@example.com&gt;</c>; otherwise returns
    ///     <c>address@example.com</c>.
    /// </summary>
    /// <param name="emailAddress">The source <see cref="EmailAddress" />.</param>
    /// <returns>A formatted string suitable for UI, logs, or headers.</returns>
    public static string ToDisplayString(this EmailAddress emailAddress)
    {
        // Guard against the default sentinel; return empty to avoid misleading output.
        if (string.IsNullOrEmpty(emailAddress.Address))
        {
            return string.Empty;
        }

        // Use MailAddress to correctly quote/escape display names when needed.
        return emailAddress.DisplayName is { Length: > 0 }
            ? new MailAddress(emailAddress.Address, emailAddress.DisplayName).ToString()
            : emailAddress.Address;
    }

    /// <summary>
    ///     Indicates whether the value has a non-empty display name.
    /// </summary>
    /// <param name="emailAddress">The source <see cref="EmailAddress" />.</param>
    /// <returns><c>true</c> if a display name exists; otherwise, <c>false</c>.</returns>
    public static bool HasDisplayName(this EmailAddress emailAddress) =>
        emailAddress.DisplayName is { Length: > 0 };

    /// <summary>
    ///     Gets the username (local-part) portion of the address: the substring before '@'.
    ///     Returns the full address if no '@' is present (e.g., on default/invalid sentinel).
    /// </summary>
    /// <param name="emailAddress">The source <see cref="EmailAddress" />.</param>
    /// <returns>The username/local-part.</returns>
    public static string Username(this EmailAddress emailAddress)
    {
        var address = emailAddress.Address;
        var at = address.IndexOf('@');
        return at >= 0 ? address[..at] : address;
    }

    /// <summary>
    ///     Gets the domain portion of the address: the substring after '@'.
    ///     Returns an empty string if no '@' is present.
    /// </summary>
    /// <param name="emailAddress">The source <see cref="EmailAddress" />.</param>
    /// <returns>The domain/host part of the address.</returns>
    public static string Domain(this EmailAddress emailAddress)
    {
        var address = emailAddress.Address;
        var at = address.IndexOf('@');
        return at >= 0 && at + 1 < address.Length ? address[(at + 1)..] : string.Empty;
    }

    /// <summary>
    ///     Alias for <see cref="Domain(EmailAddress)" />; provided for discoverability by name.
    /// </summary>
    /// <param name="emailAddress">The source <see cref="EmailAddress" />.</param>
    /// <returns>The domain/host part of the address.</returns>
    public static string Host(this EmailAddress emailAddress) => Domain(emailAddress);

    /// <summary>
    ///     Converts the value to a <see cref="MailAddress" />, preserving the value object's display name if present.
    /// </summary>
    /// <param name="emailAddress">The source <see cref="EmailAddress" />.</param>
    /// <returns>A <see cref="MailAddress" /> instance.</returns>
    public static MailAddress ToMailAddress(this EmailAddress emailAddress) =>
        emailAddress.DisplayName is { Length: > 0 }
            ? new MailAddress(emailAddress.Address, emailAddress.DisplayName)
            : new MailAddress(emailAddress.Address);

    /// <summary>
    ///     Converts the value to a <see cref="MailAddress" />, overriding the display name.
    ///     When <paramref name="displayName" /> is null or whitespace, no display name is applied.
    /// </summary>
    /// <param name="emailAddress">The source <see cref="EmailAddress" />.</param>
    /// <param name="displayName">A display name to apply, or <c>null</c>/<c>whitespace</c> for none.</param>
    /// <returns>A <see cref="MailAddress" /> instance.</returns>
    public static MailAddress ToMailAddress(this EmailAddress emailAddress, string? displayName) =>
        string.IsNullOrWhiteSpace(displayName)
            ? new MailAddress(emailAddress.Address)
            : new MailAddress(emailAddress.Address, displayName.Trim());

    /// <summary>
    ///     Converts the value to a <see cref="MailAddress" />, overriding the display name and (optionally) its encoding.
    ///     When <paramref name="displayName" /> is null or whitespace, no display name is applied.
    ///     When <paramref name="displayNameEncoding" /> is <c>null</c>, the framework default constructor is used.
    /// </summary>
    /// <param name="emailAddress">The source <see cref="EmailAddress" />.</param>
    /// <param name="displayName">A display name to apply, or <c>null</c>/<c>whitespace</c> for none.</param>
    /// <param name="displayNameEncoding">Optional encoding for the display name (e.g., UTF-8).</param>
    /// <returns>A <see cref="MailAddress" /> instance.</returns>
    public static MailAddress ToMailAddress(this EmailAddress emailAddress, string? displayName,
        Encoding? displayNameEncoding)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return new MailAddress(emailAddress.Address);
        }

        var trimmed = displayName.Trim();
        return displayNameEncoding is null
            ? new MailAddress(emailAddress.Address, trimmed)
            : new MailAddress(emailAddress.Address, trimmed, displayNameEncoding);
    }
}