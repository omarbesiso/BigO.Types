using System.Globalization;

namespace BigO.Types.Tests;

/// <summary>
///     Tests confirming the normalization rules applied to <see cref="EmailAddress" />.
/// </summary>
public sealed class EmailAddressNormalizationTests
{
    [Fact]
    public void TrimsWhitespace_And_LowercasesDomain()
    {
        var e = EmailAddress.From("  UsEr+Tag@ExAmple.Com  ");
        Assert.Equal("UsEr+Tag@example.com", e.Value);
    }

    [Fact]
    public void PreservesLocalPartCasing()
    {
        var e = EmailAddress.From("LoCaL@EXAMPLE.COM");
        Assert.StartsWith("LoCaL@", e.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void IdnDomain_IsNormalized_ToPunycode()
    {
        // Use runtime IDN mapping to derive expected ascii form.
        var idn = new IdnMapping();
        var unicode = "user@" + "müller.de";
        var expectedAsciiDomain = idn.GetAscii("müller.de").ToLowerInvariant();
        var e = EmailAddress.From(unicode);

        Assert.EndsWith("@" + expectedAsciiDomain, e.Value, StringComparison.Ordinal);
        Assert.Equal($"user@{expectedAsciiDomain}", e.Value);
    }

    [Fact]
    public void DomainCaseIsIgnoredForEquality_ButValueStoresLowercaseDomain()
    {
        var a = EmailAddress.From("user@EXAMPLE.COM");
        var b = EmailAddress.From("user@example.com");
        Assert.Equal(a, b);
        Assert.Equal("user@example.com", a.Value);
        Assert.Equal("user@example.com", b.Value);
    }
}