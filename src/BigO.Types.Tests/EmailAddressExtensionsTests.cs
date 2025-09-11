using System.Net.Mail;
using System.Text;

namespace BigO.Types.Tests;

public class EmailAddressExtensionsTests
{
    [Fact]
    public void ToDisplayString_WithDisplayName_UsesMailAddressQuoting()
    {
        var email = EmailAddress.From("john.doe@example.com", "Doe, John");
        var expected = new MailAddress(email.Address, email.DisplayName!).ToString();

        Assert.Equal(expected, email.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_NoDisplayName_ReturnsAddressOnly()
    {
        var email = EmailAddress.From("john.doe@example.com");
        Assert.Equal("john.doe@example.com", email.ToDisplayString());
    }

    [Fact]
    public void HasDisplayName_TrueWhenPresent_FalseWhenMissing()
    {
        Assert.True(EmailAddress.From("user@example.com", "Jane").HasDisplayName());
        Assert.False(EmailAddress.From("user@example.com").HasDisplayName());
    }

    [Fact]
    public void Username_ReturnsLocalPart_IncludingPlusTag()
    {
        var email = EmailAddress.From("user+tag@example.com");
        Assert.Equal("user+tag", email.Username());
    }

    [Theory]
    [InlineData("user@example.com", "example.com")]
    [InlineData("user@sub.example.co.uk", "sub.example.co.uk")]
    public void DomainAndHost_ReturnsExpected(string raw, string expectedDomain)
    {
        var email = EmailAddress.From(raw);
        Assert.Equal(expectedDomain, email.Domain());
        Assert.Equal(expectedDomain, email.Host());
    }

    [Fact]
    public void ToMailAddress_PreservesDisplayName_WhenPresent()
    {
        var email = EmailAddress.From("user@example.com", "John Doe");
        var ma = email.ToMailAddress();

        Assert.Equal("user@example.com", ma.Address);
        Assert.Equal("John Doe", ma.DisplayName);
    }

    [Fact]
    public void ToMailAddress_OverrideDisplayName_UsesOverride_Trimmed()
    {
        var email = EmailAddress.From("user@example.com", "Original");
        var ma = email.ToMailAddress("  Overridden Name  ");

        Assert.Equal("user@example.com", ma.Address);
        Assert.Equal("Overridden Name", ma.DisplayName);
    }

    [Fact]
    public void ToMailAddress_OverrideDisplayName_Whitespace_RemovesDisplayName()
    {
        var email = EmailAddress.From("user@example.com", "Original");
        var ma = email.ToMailAddress("   "); // whitespace -> no display name

        Assert.Equal("user@example.com", ma.Address);
        Assert.Equal(string.Empty, ma.DisplayName); // BCL uses empty string when no display name supplied
    }

    [Fact]
    public void ToMailAddress_WithEncoding_AllowsNonAsciiDisplayName()
    {
        var email = EmailAddress.From("user@example.com");
        var ma = email.ToMailAddress("Jöhn Döe", Encoding.UTF8);

        Assert.Equal("user@example.com", ma.Address);
        Assert.Equal("Jöhn Döe", ma.DisplayName);
        // Encoding behavior is exercised at header serialization; at object level we validate no exception and proper state.
    }
}