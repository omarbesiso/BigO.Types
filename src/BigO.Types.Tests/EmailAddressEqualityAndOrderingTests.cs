namespace BigO.Types.Tests;

/// <summary>
///     Tests verifying equality and comparison semantics for <see cref="EmailAddress" />.
/// </summary>
public sealed class EmailAddressEqualityAndOrderingTests
{
    [Theory]
    [InlineData("Alice@Example.com", "alice@example.COM")]
    [InlineData("USER+X@EXAMPLE.COM", "user+x@example.com")]
    [InlineData("Mixed.Case@Sub.Example.Com", "mixed.case@sub.example.com")]
    public void Equality_IgnoresCase_InBothParts(string a1, string a2)
    {
        var a = EmailAddress.From(a1);
        var b = EmailAddress.From(a2);
        Assert.True(a.Equals(b));
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Inequality_DifferentLocalOrDomain()
    {
        var a = EmailAddress.From("user@example.com");
        var b = EmailAddress.From("user2@example.com");
        var c = EmailAddress.From("user@other.com");

        Assert.NotEqual(a, b);
        Assert.NotEqual(a, c);
    }
}