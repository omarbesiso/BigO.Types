using System.Globalization;

namespace BigO.Types.Tests;

public class EmailAddressCoreTests
{
    [Fact]
    public void From_Valid_Minimal_Normalizes_Lowercases_And_TitleCases()
    {
        var email = EmailAddress.From("  JOHN.DOE@Example.COM  ", "  jOHN doE  ");

        Assert.Equal("john.doe@example.com", email.Address);
        Assert.Equal("John Doe", email.DisplayName);
    }

    [Fact]
    public void From_WhitespaceEmail_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => EmailAddress.From("   "));
        Assert.Equal("email", ex.ParamName);
    }

    [Fact]
    public void From_NullEmail_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => EmailAddress.From(null));
        Assert.Equal("email", ex.ParamName);
    }

    [Theory]
    [InlineData("john")] // no '@'
    [InlineData("john@")] // no domain
    [InlineData("@example.com")] // no local part
    public void From_InvalidEmail_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => EmailAddress.From(input));
    }

    [Fact]
    public void From_EmptyOrWhitespaceDisplayName_BecomesNull()
    {
        var e1 = EmailAddress.From("user@example.com", "");
        var e2 = EmailAddress.From("user@example.com", "   ");

        Assert.Null(e1.DisplayName);
        Assert.Null(e2.DisplayName);
    }

    [Fact]
    public void Parse_RawAddress_Works()
    {
        var email = EmailAddress.Parse("USER@EXAMPLE.COM", null);
        Assert.Equal("user@example.com", email.Address);
        Assert.Null(email.DisplayName);
    }

    [Fact]
    public void TryParse_Invalid_ReturnsFalse_AndDefaultOut()
    {
        var ok = EmailAddress.TryParse("not-an-email", CultureInfo.InvariantCulture, out var result);
        Assert.False(ok);
        Assert.Equal(default, result);
    }

    [Fact]
    public void TryParse_Valid_ReturnsTrue_AndValue()
    {
        var ok = EmailAddress.TryParse("Jane Doe <JANE@EXAMPLE.COM>", CultureInfo.InvariantCulture, out var result);
        Assert.True(ok);
        Assert.Equal("jane@EXAMPLE.COM".ToLowerInvariant(), result.Address);
        Assert.Equal("Jane Doe", result.DisplayName);
    }

    [Fact]
    public void Equality_AfterNormalization_IsByValue()
    {
        var a = EmailAddress.From("John.Doe@Example.com", "john DOE");
        var b = EmailAddress.From("john.doe@example.COM", "John Doe");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}