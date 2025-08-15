using System.Text;

namespace BigO.Types.Tests;

/// <summary>
///     Tests for converting <see cref="EmailAddress" /> instances into
///     <see cref="System.Net.Mail.MailAddress" /> values.
/// </summary>
public sealed class EmailAddressMailAddressAdapterTests
{
    [Fact]
    public void ToMailAddress_RoundTripsAddress()
    {
        var e = EmailAddress.From("User.Name+tag@Example.Com");
        var m = e.ToMailAddress();

        Assert.Equal("User.Name+tag@example.com", m.Address);
        Assert.Equal(string.Empty, m.DisplayName);
    }

    [Fact]
    public void ToMailAddress_WithDisplayName_And_Encoding()
    {
        var e = EmailAddress.From("jose@example.com");
        var name = "José Test";
        var enc = Encoding.UTF8;

        var m = e.ToMailAddress(name, enc);

        Assert.Equal("jose@example.com", m.Address);
        Assert.Equal(name, m.DisplayName);
    }
}