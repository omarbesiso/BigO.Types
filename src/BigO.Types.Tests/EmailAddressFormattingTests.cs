using System.Globalization;
using System.Net.Mail;

namespace BigO.Types.Tests;

public class EmailAddressFormattingTests
{
    [Fact]
    public void ToString_Default_Equals_G_Invariant()
    {
        var e = EmailAddress.From("user@example.com", "John Doe");
        Assert.Equal(e.ToString("G", CultureInfo.InvariantCulture), e.ToString());
    }

    [Fact]
    public void ToString_G_ReturnsQuotedWhenNeeded()
    {
        var e = EmailAddress.From("john.doe@example.com", "Doe, John");
        var expected = new MailAddress(e.Address, e.DisplayName!).ToString(); // BCL handles quoting

        Assert.Equal(expected, e.ToString("G", CultureInfo.InvariantCulture));
        Assert.Equal(expected, e.ToString("F", CultureInfo.InvariantCulture)); // "F" equivalent
    }

    [Fact]
    public void ToString_A_ReturnsAddressOnly()
    {
        var e1 = EmailAddress.From("user@example.com", "John Doe");
        var e2 = EmailAddress.From("user@example.com");

        Assert.Equal("user@example.com", e1.ToString("A", CultureInfo.InvariantCulture));
        Assert.Equal("user@example.com", e2.ToString("A", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToString_UnsupportedFormat_Throws()
    {
        var e = EmailAddress.From("user@example.com");
        Assert.Throws<FormatException>(() => e.ToString("Z", CultureInfo.InvariantCulture));
    }
}