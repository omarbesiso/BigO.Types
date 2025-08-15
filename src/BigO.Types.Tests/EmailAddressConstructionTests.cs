namespace BigO.Types.Tests;

/// <summary>
///     Tests covering construction of <see cref="EmailAddress" /> instances from strings.
/// </summary>
public sealed class EmailAddressConstructionTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("user.name+tag+sorting@example.co.uk")]
    [InlineData("x@x.io")]
    [InlineData("first_last-123@sub.domain.example")]
    public void From_ValidInputs_CreateInstance(string input)
    {
        var e = EmailAddress.From(input);
        Assert.False(string.IsNullOrEmpty(e.Value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NullOrWhitespace_Throws(string? input)
    {
        var ex = Assert.Throws<ArgumentException>(() => EmailAddress.From(input!));
        Assert.Contains("Invalid email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("userexample.com")] // missing '@'
    [InlineData("user@")] // missing domain
    [InlineData("@example.com")] // missing local
    [InlineData("user@@example.com")] // multiple '@'
    [InlineData("user@exa mple.com")] // space in domain
    [InlineData("us er@example.com")] // space in local
    public void From_InvalidInputs_Throw(string input)
    {
        Assert.ThrowsAny<Exception>(() => EmailAddress.From(input));
    }

    [Fact]
    public void Parse_Valid_ReturnsValue()
    {
        var e = EmailAddress.Parse("user@example.com");
        Assert.Equal("user@example.com", e.Value); // domain normalized
    }

    [Fact]
    public void Parse_Invalid_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => EmailAddress.Parse("invalid"));
    }
}