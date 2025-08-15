using System.Diagnostics;
using System.Globalization;

namespace BigO.Types.Tests;

/// <summary>
///     Utility helpers used by <see cref="EmailAddress" /> unit tests.
/// </summary>
internal static class EmailTestHelper
{
    /// <summary>
    ///     Build an ASCII domain of an exact length using labels that respect the <=63 rule.
    ///     Example: "bbbb...(63).bbbb...(63).bbbb...(63).bbbb...(leftover)".
    /// </summary>
    public static string BuildAsciiDomainOfLength(int totalLength)
    {
        if (totalLength < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLength), "Domain length must be >= 3.");
        }

        // We'll fill with 'b' labels. Each label <= 63, joined by '.'.
        var labels = new List<string>();
        var remaining = totalLength;

        while (remaining > 0)
        {
            // if we still need a dot and at least one label already appended, reserve 1 char for '.'
            var reserveForDots = labels.Count > 0 ? 1 : 0;
            var maxLabel = Math.Min(63, remaining - reserveForDots);
            if (maxLabel <= 0)
            {
                break;
            }

            labels.Add(new string('b', maxLabel));
            remaining -= maxLabel;

            if (remaining > 0)
            {
                labels[^1] += "."; // append dot to this piece
                remaining -= 1;
            }
        }

        var domain = string.Concat(labels);
        if (domain.Length != totalLength)
        {
            throw new InvalidOperationException(
                $"Failed to build domain of length {totalLength}, got {domain.Length}.");
        }

        // sanity: no leading/trailing dots, labels not empty
        Debug.Assert(!domain.StartsWith('.') && !domain.EndsWith('.'));
        Debug.Assert(domain.Split('.').All(l => l.Length is > 0 and <= 63));
        return domain;
    }

    /// <summary>
    ///     Construct a Unicode IDN domain that, when IDN-normalized to ASCII, exceeds the requested ASCII length.
    ///     Uses a repeated non-ASCII label (e.g. 'ü') to cause Punycode expansion, then appends ".example".
    ///     Returns (unicodeDomain, asciiDomain).
    /// </summary>
    public static (string unicodeDomain, string asciiDomain) BuildIdnDomainExceedingAsciiLength(
        int asciiLengthThreshold)
    {
        var idn = new IdnMapping();
        // Start with a label of 'ü' repeated; increase until ascii exceeds threshold or label cap hit.
        var baseSuffix = ".example"; // ASCII suffix to stabilize parsing
        for (var n = 1; n <= 63; n++)
        {
            var label = new string('ü', n);
            var unicode = label + baseSuffix;
            string ascii;
            try
            {
                ascii = idn.GetAscii(unicode);
            }
            catch
            {
                continue;
            }

            if (ascii.Length > asciiLengthThreshold)
            {
                return (unicode, ascii);
            }
        }

        throw new InvalidOperationException("Could not produce an IDN domain exceeding the requested ASCII length.");
    }

    public static string Repeat(char c, int count) => new(c, count);
}