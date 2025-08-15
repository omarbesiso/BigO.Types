namespace BigO.Types.Tests;

/// <summary>
///     Tests for <see cref="EmailAddress.TryParse(string?, out EmailAddress)" />
///     and related validation helpers.
/// </summary>
public sealed class EmailAddressTryParseAndValidationTests
{
    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("USER@EXAMPLE.COM", true)]
    [InlineData(" user@example.com ", true)]
    [InlineData("user.name+tag@sub.example.co.uk", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("no-at-symbol", false)]
    [InlineData("user@", false)]
    [InlineData("@domain.com", false)]
    [InlineData("user@@domain.com", false)]
    [InlineData("user@exa mple.com", false)]
    public void TryParse_Matches_IsEmailAddressValid(string? input, bool expected)
    {
        var ok = EmailAddress.TryParse(input, out var result);
        Assert.Equal(expected, ok);
        Assert.Equal(expected, EmailAddress.IsEmailAddressValid(input));
        if (ok)
        {
            // Both APIs should agree and produce a non-empty normalized value
            Assert.False(string.IsNullOrWhiteSpace(result.Value));
        }
    }

    [Fact]
    public void Normalization_UsesSameRules_InTryParse_And_From()
    {
        var s = "  Local.Part+tag@ExAmple.Com ";
        var ok = EmailAddress.TryParse(s, out var t);
        Assert.True(ok);

        var f = EmailAddress.From(s);

        Assert.Equal(t, f);
        Assert.Equal(t.Value, f.Value);
    }
}