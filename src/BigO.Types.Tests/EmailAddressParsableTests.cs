using System.Globalization;

namespace BigO.Types.Tests;

public class EmailAddressParsableTests
{
    [Fact]
    public void Parse_TrimsWhitespace()
    {
        var e = EmailAddress.Parse("   user@example.com   ", CultureInfo.InvariantCulture);
        Assert.Equal("user@example.com", e.Address);
    }

    [Fact]
    public void Parse_ThrowsOnNullOrWhitespace()
    {
        Assert.Throws<FormatException>(() => EmailAddress.Parse("   ", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TryParse_TrimsWhitespace_AndSucceeds()
    {
        var ok = EmailAddress.TryParse("   John Doe <USER@EXAMPLE.COM>   ", CultureInfo.InvariantCulture, out var e);
        Assert.True(ok);
        Assert.Equal("user@example.com", e.Address);
        Assert.Equal("John Doe", e.DisplayName);
    }

    [Fact]
    public void TryParse_ReturnsFalse_OnGarbage()
    {
        Assert.False(EmailAddress.TryParse("###", CultureInfo.InvariantCulture, out _));
    }
}